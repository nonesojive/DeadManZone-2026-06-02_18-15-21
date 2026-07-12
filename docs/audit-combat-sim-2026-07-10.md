# Combat Sim / Replay / Director Audit — 2026-07-10

Scope: `Assets/_Project/Core/Combat/*` (56 files), `Presentation/Combat/CombatDirector.cs`, `CombatFlowPresenter.cs`, `Presentation/Combat/Arena/*` (critical-path files read in full, rest skimmed), relevant `Core.Tests`/`Presentation.Tests`/`Tests.PlayMode`. Read-only audit ahead of wiring the deterministic event replay to the new 3D toon-ink actors (ADR-0002/0003).

---

## 1. Executive summary

**Grade: C+.** The bones are good: Core is genuinely UnityEngine-free, RNG is a seeded xorshift with no `DateTime`/dictionary-order/float-accumulation hazards in the tick loop, targeting/movement everywhere tie-breaks on ordinal `InstanceId`, and the sim→event-log→director→actor pipeline is the right shape for the 3D rework (the director and event log are already presentation-agnostic). But the pause-command path has accumulated **three real correctness bugs** from the v4 pacing rework (command events logged into a segment that never plays; ability/strike kills never freed from the occupancy grid; `ShieldAllies` armor zeroed in the same batch that grants it), one **dead rule** (marksman stealth expiry mathematically unreachable under the current single pause threshold), and one **save/resume determinism leak** in the orchestrator. On the presentation side, `CombatUnitActor` — the class the handoff calls "presentation-agnostic" — is hard-wired to `CombatUnitVisual2D` and does nothing without it, which is the single biggest obstacle to the 3D wiring. Everything else is deletable 2D-era mass and moderate duplication. Fix the five must-fix items (small, test-backed diffs) and the replay path is clean enough to build on.

---

## 2. Architecture map (as it exists today)

**Sim (Core, deterministic):**
- `RunOrchestrator.BeginCombat` → `TickCombatRun.Start(playerBoard, enemyBoard, seed, authority, buildBoards)`. Ctor spawns `CombatantState`s (enemy X mirrored via `BattlefieldLayout.MirrorEnemyAnchorX`), applies `PieceAbilityEngine` synergies + `CriticalMassEngine` modifiers + `TacticEffects`, registers footprints in `CombatOccupancyGrid`, and opens the **opening pause** (`AwaitingCommand`, `CurrentPauseIndex = 0`).
- `RunManager.AdvanceCombat` → `RunOrchestrator.AdvanceCombat` → `TickCombatRun.Continue(commands filtered by AfterCheckpoint)`. Per tick: movement (goal from `RoleEngagement`+`CombatFormationSlots`, step from `ShapePathfinder` against the occupancy grid, charge cost from `CombatMovement`/`CombatMovementSpeed`), gas after tick 300 (`GasDamageSystem`), attacks (`TacticTargeting` → `CombatRoleTargeting` → `CombatAccuracyResolver(Rng)` → `CombatDamageResolver`). One mid-fight pause fires when either army drops to 60% (`CombatPacingConfig.PauseThresholds`, `TryFireCheckpoint`).
- Every effect is appended to `CombatEventLog` as a stringly-typed `CombatEvent {Segment, Tick, ActorId, ActionType, TargetId, Value}`. Segment = `CheckpointsFired` at the time (0 = start→pause, 1 = pause→end). Pause commands run through `CommandProcessor` → `TacticPauseValidator` / `CombatAbilityExecutor`.

**Replay (Presentation):**
- `Continue` returns `CombatAdvanceResult {Status, SegmentIndex, EventLog, PauseTrigger, BattleReport}` → `RunManager.CombatAdvanced` event → `CombatDirector.OnCombatAdvanced` → coroutine walks the segment's events tick-by-tick in real time (`CombatSegmentPlayback` groups by tick; empty "charge" ticks paced at `EmptyTickPaceScale = 0.45`, capped at 64), firing `EventReplayed` per event.
- `EventReplayed` consumers (3 today): **`CombatArenaPresenter`** (grid anchors via `CombatReplayState`, per-unit HP via its own `ArmyHealthReplayTracker`, actor moves / attack / hurt / death VFX+audio), **`CombatFlowPresenter`** → `ArmyHealthBarPresenter` (army bars, a *second* `ArmyHealthReplayTracker`). `CombatArenaFreezeController` listens to `PausedForCommands`/`SegmentPlaybackStarting`; presenter snaps actors to anchors on `SegmentPlaybackFinished` (non-free-chase mode only).
- Pause loop: director `FinishPlayback` → `PausedForCommands` → `CombatFlowPresenter` → `TacticPausePanel` → `RunOrchestrator.SubmitCombatCommands` → `CombatDirector.ContinueCombat` → `AdvanceCombat` again.

**Actors:**
- `CombatArenaPresenter.InitializeArena` rents a `CombatUnitActor` per participating cell from `CombatUnitActorPool`, positions via `CombatGridMapper` (grid↔world). Actor `Update()` smooth-follows its replay anchor; optional Top Troops free-chase (`CombatArenaChaseController` computes per-frame world goals by re-running Core `CombatPresentationEngagement`/`RoleEngagement` — deliberate rule-sharing so visuals stay replay-truthful). All rendering is delegated to a runtime-added `CombatUnitVisual2D` (sprite strips).

**Save/resume:**
- Only seed + boards + submitted commands (+ event log at completion) are persisted. `RunOrchestrator.RestoreActiveCombatFromSave` re-runs the sim via `TickCombatRun.FastForwardFromSave`; `CombatFlowPresenter.InitializeArenaFromRunState` + `CombatArenaPresenter.RestoreState` replay saved events into anchors/HP, excluding the in-progress segment.

---

## 3. Findings table

Severity: **B** blocker (for this milestone), **H** high, **M** medium, **L** low. Paths relative to `Assets/_Project/`.

| ID | Sev | Location | Finding | Evidence |
|----|-----|----------|---------|----------|
| F1 | H | `Core/Combat/CommandProcessor.cs:78` | Pause-command events logged one segment late: `logSegment = checkpointIndex + 1` (values 1/2) vs. actual playback segment `CheckpointsFired` (0/1). Mid-pause ability events (`cannon_blast`, `grenade_lob`, their `destroyed`, `tactic_set`, `shield_allies`) land in **segment 2, which never plays** — kills by pause abilities are never presented; the victim's actor stands there alive-looking. Opening-pause events land in segment 1 and replay mid-fight, minutes late. `fight_end` from a pause kill is logged at the *correct* segment (`TickCombatRun.cs:392-403`), so the log even disagrees with itself. `CombatDirector.ResolveLastPlaybackSegment` picks the max segment from a saved log (=2) and would replay only the orphaned command events on restore. | `TickCombatRun.RunUntilPauseOrEnd(segment = CheckpointsFired)` at `TickCombatRun.cs:112`; director plays `result.SegmentIndex` only. Formula predates the v4 opening-pause/single-threshold scheme (two mid-fight checkpoints → segment N+1 was correct then). |
| F2 | H | `Core/Combat/CombatAbilityExecutor.cs:172-174, 198-200`; `CommandProcessor.cs:249-256` | Ability and call-strike kills log `destroyed` but never remove the victim from `CombatOccupancyGrid` (only `TickCombatRun.LogDestroyed` does, and only for attack/gas kills). Dead units block pathfinding for the rest of the fight. `TickCombatRun.RebuildOccupied()` (`TickCombatRun.cs:470-483`) is the written-but-never-called fix — dead code today. | `TickCombatRunFootprintTests.DestroyedUnit_ReleasesOccupancyGrid` covers attack kills only. |
| F3 | H | `Game/RunOrchestrator.cs:555-568` | Save/resume determinism leak: `RestoreActiveCombatFromSave` calls `SetPlayerTactic(saved tactic)` **before** `FastForwardFromSave`. The saved tactic may be a mid-fight change; live fights apply mid-fight tactic changes via `CommandProcessor` (which updates `PlayerTactic` but *not* `PlayerDamageBuff` or ProtectSupport armor — `CommandProcessor.cs:101`), while the restore path recomputes fight-start damage/armor buffs from the *end* tactic. Restored fights can diverge from the live fight the player watched, breaking the "deterministic resume" premise documented at `RunOrchestrator.cs:292-296`. | `Determinism_FastForwardReproducesIdenticalEventLog` covers `TickCombatRun` only; no orchestrator resume-determinism test exists. |
| F4 | H | `Core/Combat/CombatStealthRules.cs:16-17` | Marksman stealth expiry is unreachable: `tacticsCheckpointIndex >= 2`, but `CheckpointsFired` maxes at `PauseThresholds.Length == 1`. `ironclad_marksman` is **permanently untargetable** by direct attacks in every real fight (only gas kills it). The unit test passes `2` directly, so the suite stays green while the integration is dead. Bonus violations: hardcoded content id in Core; the Stealth keyword's documented behavior ("hidden until this attacks", `KeywordTagCatalog.cs:41`) is not implemented for any other stealth unit (they're simply always targetable). | Confirmed `PauseThresholds = { 0.60f }` both now and at the feature's commit (`4b350177`). |
| F5 | **B** | `Presentation/Combat/Arena/CombatUnitActor.cs:88, 141`; `CombatArenaPresenter.cs:522` | The actor the 3D units must reuse is hard-wired to the 2D pipeline: `Initialize` does `AddComponent<CombatUnitVisual2D>`, `Update()` early-returns when `_visual2D == null` (no visual → no movement, no facing, no walk state), and `PlayAttackToward/PlayHurt/PlayDeath` all route through `_visual2D`. `CombatArenaPresenter.PlayDestroyedEvent` times death VFX to `CombatUnitVisual2D.DieStripSeconds`. There is no seam to plug a 3D visual in. | Handoff claims free-chase smoothing is "presentation-agnostic" — the math is, the class isn't. |
| F6 | H | `Core/Combat/TickCombatRun.cs:388-389` | `ApplyCommands` zeroes **all** player `ArmorBuffSteps` *after* executing the command batch. `ShieldAllies` (+1 armor to adjacent infantry, costs 2 Authority) is granted inside that same batch → wiped before a single tick runs. **The ability is a paid no-op** (only its log line survives). The zeroing also silently strips fight-start synergy/critical-mass/ProtectSupport armor at the first pause — but only if the player submitted any command (`pauseCommands.Count == 0` early-returns first), so buff lifetime depends on whether you pressed a button. | No test runs `ShieldAllies` through `TickCombatRun`; `CombatAbilityExecutorTests` test the executor in isolation (where the buff *does* apply). |
| M1 | M | `Core/Combat/CombatStealthRules.cs`, `TickCombatRun.cs:38-40`, `PhaseCommand.cs:18`, `CombatAbilityExecutor.cs:14-29` | Vestigial pause-index scheme: `CurrentPauseIndex` maps *every* mid-fight pause to `1`; `AfterCheckpoint` doc says "0 = first pause, 1 = second"; `PauseTriggerContext.CheckpointIndex` is hardcoded 0/1; ability windows/costs keyed to 0/1. All of it only works because `PauseThresholds.Length == 1` — an invariant asserted nowhere. If a second threshold is ever re-added, `FastForwardToCheckpoint` would replay the same `AfterCheckpoint == 1` commands at every pause. | Compare `TryFireCheckpoint` multi-consume loop (`TickCombatRun.cs:196-211`) — written for N thresholds — vs. everything downstream assuming 2 pauses total. |
| M2 | M | `Core/Combat/CombatEvent.cs` + 6 consumer sites | Stringly-typed event protocol: action types (`"damage"`, `"move"`, `"destroyed"`, …) and coord payloads (`"x,y"`) are raw literals produced in 4 files and parsed in 6 (`CombatReplayState`, `ArmyHealthReplayTracker`, `CombatLogFormatter`, `CombatArenaPresenter` — which has its own duplicate `TryParseCoord`, `CombatDirector`, `CombatFlowPresenter`). A typo is a silent visual no-op; `CombatLogFormatter`'s `Contains("damage")` fallback (`CombatLogFormatter.cs:59-61`) shows the drift already happened once. The 3D actor layer becomes consumer #7. | — |
| M3 | M | `CombatArenaPresenter.cs:25` + `ArmyHealthBarPresenter.cs:18` | Duplicated replay-state consumers: two independent `ArmyHealthReplayTracker` instances fed the same events via two different paths, and the save-restore replay ("apply all events except excluded segment") is implemented twice (`CombatFlowPresenter.InitializeArenaFromRunState` and `CombatArenaPresenter.ApplySavedAnchors`). Each new consumer re-implements event application; drift between them = visual desync. | — |
| M4 | M | `CombatArenaPresenter.cs:192` vs `CriticalMassEngine.cs:101-105`, `PieceAbilityEngine.cs:104-112` | Replay HP baseline is wrong for buffed units: trackers register `Definition.MaxHp`, but the sim starts buffed units at modified `CurrentHp` (MaxHp synergy/critical-mass). Damage events carry absolute values, so replayed HP fractions (unit bars, army bars, pause thresholds display) drift from sim truth for any MaxHp-buffed army. `destroyed` snaps to 0, so it self-heals only at death. | `ArmyHealthTracker.Evaluate` has the same mismatch inside Core (fraction can start > 1.0). |
| M5 | M | `Core/Combat/CommandProcessor.cs:66-159` | `TryApplyBatch` is not transactional: on a mid-batch failure it restores `authority` but keeps already-applied tactic changes, ability damage, and log entries. A diverging replay of a saved command batch fails silently halfway. | — |
| M6 | M | `Core/Combat/CombatMovement.cs:10-18` vs `CombatMovementSpeed.cs:5-6`; `CombatDirector.cs:19` vs `CombatArenaConfigSO.moveSpeedPresentationScale` | Duplicated pacing math: move cost 1/2 encoded twice (unit cost + charge cost, mapped by equality check in `GetStepChargeCost`); walk-speed calibration split across a Core-side constant (`EmptyTickPaceScale = 0.45`) and a Presentation config knob that "is calibrated to match" by comment only. Two knobs, one invariant, zero enforcement. | `CombatArenaMoveSpeedResolver` correctly derives world speed from sim constants — the good pattern; `EmptyTickPaceScale` isn't derived. |
| M7 | M | Naming drift (glossary: CONTEXT.md) | (a) **Authority vs Requisition**: `TickCombatRun.Requisition => Authority` alias, `CombatSaveState` carries *both* fields, restore reads `Authority > 0 ? Authority : Requisition` (`RunOrchestrator.cs:552`), `AvailableCommand.RequisitionCost`, `SpendRequisitionBuff`. (b) **Tactic vs Stance**: `CommandType.SetTactic` *and* `ChangeStance` both live; `PhaseCommand.Stance` legacy alias has zero code readers (verify old saves before deleting). (c) **checkpoint/pause/segment** used interchangeably (`AfterCheckpoint` means pause index; `checkpoint` event means pause fired; segment means playback chunk). `CombatSide` itself matches the glossary's "side" correctly. | — |
| M8 | M | `Core/Combat/CombatAbilityExecutor.cs:208-209, 160-201`; `GasDamageSystem.cs:20`; `CombatAbilityExecutor.cs:204` | (a) `OccupiesCell` checks anchor only — multi-cell units can't be targeted on non-anchor cells despite the name. (b) Damage application + destroy logging duplicated 4× with diverging behavior (`ResolveAttacks`, `ApplyDamage`, `ApplyAreaDamage`, `ApplyStrikeDamage`) — the divergence *is* F2. (c) Tag literals bypass `GameTagIds`: `"GasMask"` (case-sensitive `Contains`, unlike every other tag check) and `"infantry"`. | — |
| M9 | M | `Core/Combat/TacticEffects.cs:28-48` | `ApplyProtectSupportBuffs` stacks `+2 ArmorBuffSteps` on every `SetPlayerTactic` call and never removes it on switching away. Currently masked by F6's zeroing and by DisciplinedFire defaults — fragile by coincidence. | `SetPlayerTactic` is called twice on the restore path (ctor + orchestrator). |
| M10 | M | `CombatDirector.cs:130-140` | Manual `CombatEventRecord → CombatEvent` mapping duplicates `CombatEventMapper.FromRecords` (used correctly by `CombatFlowPresenter`). Third mapping site inline in `RunOrchestrator.SyncCombatFromRunner`. Twin DTOs (`CombatEvent`/`CombatEventRecord`) are identical field-for-field. | — |
| D1 | M (dead) | `Core/Combat/CriticalMassRules.cs` | Entire facade has **zero callers**. | grep: no references outside the file. |
| D2 | M (dead) | `Core/Combat/CombatMovement.cs:29-75`, `CombatMovementRules.cs:37-43` | `StepTowardTarget` + private helpers and `SelectNearestEnemyPosition`: no callers (pre-footprint pathfinding era). `ShouldAttemptMove`'s `enemies` parameter is also unused. | grep confirmed. |
| D3 | L (dead) | `Core/Combat/CommandProcessor.cs:64, 161-197` | `GetBonusActionSlots` hardcoded 0 (its one caller `RunOrchestrator.cs:204` guards a dead branch); `TryApply(single)` used only by one test. | — |
| D4 | L (dead) | `Presentation/Combat/Arena/CombatArenaMuzzleAnchor.cs`, `CombatArenaFxCull.cs` | No code references anywhere (verify no prefab GUID refs before deleting). | grep confirmed. |
| D5 | L (dead) | `CombatArenaPresentationMode.cs:9`; `CombatArenaPresenter.cs:643-648`; `CombatUnitActor.cs:35` | `IsTopTroops2D` hardcoded `true`; `ResolveVfxPresenter(bool use2D)` ignores its parameter and always returns the 2D VFX; `_moveLerpFallbackSeconds` field written, never read. All vestiges of the removed first 3D backend. | — |
| L1 | L | `Core/Combat/PieceAbilityEngine.cs:265-271` | `SynergyLink` recorded *before* the stat switch; unsupported stats (`AttackRange`, default) return after adding the link → phantom links with no effect. Also ~100 lines of copy-paste struct rebuilding (`ApplyEffect`) — a mutable local would halve the file. | — |
| L2 | L | `Core/Combat/CriticalMassRuleSource.cs` | Static mutable test override in Core — a leaked override changes sim results process-wide. Tests do clear it in teardown; acceptable, but it's the only global mutable state in the sim. | — |
| L3 | L | `Core/Combat/CombatWinChecker.cs:15-16` | Mutual annihilation returns `playerWon: true, isDraw: true` — quirky double-flag; consumers must know to check `IsDraw` first. | — |
| L4 | L | `TickCombatRun.cs:213-217` | `PauseTriggerContext.CheckpointIndex` hardcoded `1` for every mid-fight pause; `Threshold` reports only the last consumed threshold. Harmless under one threshold (see M1). | — |

**Layering verdict (B):** clean. No `UnityEngine` anywhere in `Core` (verified by grep). No game rules in Presentation — `CombatPresentationEngagement`/`CombatArenaChaseController` *reuse* Core rules rather than duplicating them, which is the intended pattern. The only rule-ish Presentation logic is F5's death-timing constant and the F1-forced workarounds.

**Determinism verdict (E):** the tick loop is solid — seeded `Rng` (xorshift32), all selections tie-break on ordinal InstanceId, iteration is over `List`s ordered at spawn, integer math in the hot path. The determinism risks are at the edges: F3 (restore ordering) and M5 (non-transactional batch replay). `UnityEngine.Random` appears only in attack-stagger visuals (`CombatArenaPresenter.cs:420`), which is fine.

---

## 4. Blocker / high findings — recommended fixes

### F5 (blocker) — `CombatUnitActor` hard-wired to `CombatUnitVisual2D`
- **Quick:** extract the one interface this milestone actually justifies — `ICombatUnitVisual` with the members the actor already calls (`SetWalking`, `FaceDirection`, `UpdateSortAndBob`, `PlayAttack`, `PlayHurt`, `PlayDeath`, `BlocksLocomotion`, `VisualHeight`, `DeathSeconds`). `CombatUnitVisual2D` implements it as-is; the 3D toon-ink visual becomes the second implementation. Actor `Initialize` takes the visual (or a factory flag) instead of `AddComponent<CombatUnitVisual2D>`; `PlayDestroyedEvent` reads `DeathSeconds` off the visual instead of the 2D constant.
- **Cleaner:** same interface, plus move visual creation out of the actor entirely (presenter decides 2D/3D per config). Don't go further — no visual-strategy registries.
- **Blast radius:** `CombatUnitActor`, `CombatUnitVisual2D` (add interface), `CombatArenaPresenter` (2 call sites). Movement/chase math untouched.
- **Tests:** PlayMode `CombatArenaReplayPlayModeTests` (move/destroyed/full-sequence) cover the actor through the 2D path and must stay green; no EditMode coverage of the actor (gap — acceptable, it's a MonoBehaviour).

### F1 (high) — command events logged one segment late
- **Quick:** in `TickCombatRun.ApplyCommands`, pass the segment the events will actually play in — `CheckpointsFired` — into `TryApplyBatch` as the log segment (replace `logSegment = checkpointIndex + 1`). One-line semantic change plus the `TryApply` legacy overload.
- **Cleaner:** have `TickCombatRun` own log-segment entirely (pass `segment` explicitly, keep `checkpointIndex` for costs/windows only), and add an assert/test that no event's `Segment` exceeds the max playable segment.
- **Blast radius:** Core only; log output changes (segment field of pause-command events). `CombatReplayState.RestoreFromBattlefieldAndEvents` orders by segment/tick, so restore behavior *improves*. Check `LastBattleLogReviewPresenter` (formats all events; unaffected).
- **Tests:** **gap** — nothing asserts command-event segments. Add a `TickCombatRun` test: use CannonBlast at the mid-pause, assert its `damage`/`destroyed` events share the segment of subsequent playback (and of `fight_end` when it ends the fight).

### F2 (high) — occupancy grid never freed on ability/strike kills
- **Quick:** call the already-written `RebuildOccupied()` at the end of `TickCombatRun.ApplyCommands`. Three lines, uses existing dead code.
- **Cleaner:** delete `RebuildOccupied`, route *all* HP-to-zero transitions through one `TickCombatRun` kill method that logs `destroyed` + frees the grid; `CombatAbilityExecutor`/`CommandProcessor` report kills instead of logging them (fixes the 4× duplication in M8b at the same time).
- **Blast radius:** quick option — Core, ~3 lines, zero API change. Cleaner option — `CombatAbilityExecutor`, `CommandProcessor` signatures.
- **Tests:** extend `TickCombatRunFootprintTests` with "ability kill releases occupancy" (mirror the existing attack-kill test). Currently a gap.

### F6 (high) — `ShieldAllies` is a paid no-op / armor buffs stripped inconsistently
- **Quick:** move the `ArmorBuffSteps = 0` reset to the *top* of `ApplyCommands` (before `TryApplyBatch`), and run it unconditionally (not only when commands exist). Decide explicitly: if fight-start armor buffs are meant to persist, delete the reset instead and re-derive ShieldAllies' intended duration.
- **Cleaner:** give armor buffs an explicit lifetime (e.g., `PauseArmorBuffSteps` cleared at each pause, permanent `ArmorBuffSteps` never cleared) — only if design actually wants both.
- **Blast radius:** Core; combat balance shifts slightly (ShieldAllies starts working; synergy armor either persists or consistently strips). This is a *design decision disguised as a bug* — confirm intent with the owner before choosing.
- **Tests:** gap. Add `TickCombatRun`-level test that ShieldAllies raises the target's effective armor for the following segment.

### F4 (high) — marksman stealth expiry unreachable
- **Quick:** change `>= 2` to `>= 1` (targetable after the mid-fight pause — i.e., the "second tactics window" as counted with the opening pause), update `CombatStealthRulesTests` to pass indices a real fight can produce, and add one integration assertion through `TickCombatRun`.
- **Cleaner:** implement the keyword as documented ("hidden until this attacks") using a `HasAttacked` flag on `CombatantState`, and move the piece-id special case out of Core (data-driven flag on the definition). Do this only if more stealth pieces are planned.
- **Blast radius:** Core + tests; balance change (marksman becomes killable).
- **Tests:** existing unit tests are green-but-lying; the integration test is the real fix.

### F3 (high) — restore applies end-tactic at fight start
- **Quick:** in `RestoreActiveCombatFromSave`, set the player tactic only when it's the fight's *starting* tactic (e.g., persist `StartingTactic` in `CombatSaveState` at `BeginCombat`, restore that; mid-fight changes replay via `SubmittedCommands` exactly as they did live). Also resolve the live-path inconsistency: `CommandProcessor` should update `PlayerDamageBuff` when it changes the tactic (or `SetPlayerTactic` should be the single entry point it calls).
- **Cleaner:** make `TacticState` derive `PlayerDamageBuff` from `PlayerTactic` on read (delete the cached field) so there is no second copy to desync; note `SpendRequisitionBuff` also writes `PlayerDamageBuff += 2`, so keep a separate additive field for that.
- **Blast radius:** `RunOrchestrator`, `CombatSaveState` (schema bump), `CommandProcessor`/`TacticState`.
- **Tests:** **gap** — no orchestrator-level mid-fight-resume determinism test. Add one: play to mid-pause with a tactic change, serialize, restore, assert identical event logs.

---

## 5. Recommended refactor order for THIS milestone

**Must fix before wiring 3D actors** (each is a small, independently testable diff; order chosen so Core is trustworthy before the presentation seam opens):

1. **F1** — segment off-by-one (+ regression test). Everything downstream of the event log is about to be rebuilt against it; wire the 3D actors to a log that lies and you'll debug ghost units in the new pipeline.
2. **F2** — occupancy on ability/strike kills (quick option: call `RebuildOccupied`; + test).
3. **F6** — armor-buff zeroing order (confirm design intent first; + test).
4. **F4** — marksman stealth `>= 1` (+ integration test). Two-line fix; do it in the same Core pass.
5. **F5** — extract `ICombatUnitVisual` seam in `CombatUnitActor`/`CombatArenaPresenter`. This is the actual milestone enabler; do it last so the Core fixes are already validated through the existing 2D path before the visual layer forks.

**Do during the milestone, not blocking:** F3 (restore determinism — needed before anyone tests save/resume against 3D fights), M2 (a `CombatEventTypes` static class of const strings — cheap, do it while touching every consumer for the 3D wiring), M10 (use `CombatEventMapper` in `CombatDirector`).

**Can wait (post-milestone cleanup pass):** M1 pause-index scheme, M3 tracker consolidation, M4 buffed-HP replay baseline (fix when rebuilding health bars for 3D), M5 batch transactionality, M6 pacing-constant dedup, M7 naming sweep (Authority/Requisition, Stance), M8/M9, L1-L4, all deletions below.

**Explicitly do NOT:** introduce event-type enums with serialization migration, a replay-consumer framework, or any visual-abstraction beyond the one interface in F5. The project's YAGNI bar is right for this codebase.

---

## 6. Delete vs keep

### Safe to DELETE now (dead, zero code references)
| File / member | Note |
|---|---|
| `Core/Combat/CriticalMassRules.cs` | Facade, zero callers. |
| `Core/Combat/CombatMovement.cs` — `StepTowardTarget`, `GetNeighbors`, `Manhattan` | Pre-footprint pathfinding. Keep `GetMoveCost`/`GetStepChargeCost` (used). |
| `Core/Combat/CombatMovementRules.cs` — `SelectNearestEnemyPosition` | Zero callers. |
| `Core/Combat/CommandProcessor.cs` — `GetBonusActionSlots` (+ dead branch `RunOrchestrator.cs:204`), `TryApply` single-command overload (fold its one test into batch) | — |
| `Core/Combat/TickCombatRun.cs` — `RebuildOccupied` | Unless used as the F2 quick fix. |
| `Presentation/Combat/Arena/CombatArenaMuzzleAnchor.cs`, `CombatArenaFxCull.cs` | Verify no prefab GUID references first (grep the arena scene/prefabs for the script GUIDs). |
| `Presentation/Combat/Arena/CombatArenaPresentationMode.cs` | Hardcoded `true`; inline and delete, along with `ResolveVfxPresenter`'s ignored parameter. |
| `CombatUnitActor._moveLerpFallbackSeconds` field | Written, never read. |
| `PhaseCommand.Stance` alias + `CommandType.ChangeStance` | Zero code readers of `.Stance`; confirm no serialized saves bind the JSON name before removing (save schema is versioned — cheap to check). |

### DELETE at 3D switchover (2D-era, but currently the only working renderer — keep until the 3D path renders)
`CombatArena2DBattlefieldDressing/BattlefieldView/BuildingVisual/EnvironmentArt/PlaceholderSprites/ProjectileArc/SilhouetteArt/SkyBillboard/SortOrder/SpriteBillboard/SpriteMaterial/SpriteMesh/SpriteMetrics/SpriteQuad/Vfx/VfxArt/VfxSpriteAnim.cs` (17 files), `CombatUnitVisual2D.cs`, `CombatUnit2DStripPlayer.cs`, `CombatUnit2DVisualScale.cs`, `CombatUnitSpriteResolver.cs`, `CombatUnitProceduralJog.cs`, `TopTroopsAtmosphere.cs`, `TopTroopsBattlefieldPalette.cs`, `CombatArenaOrthographicFramer.cs` (ADR camera is perspective ¾), `SidekickPreviewSceneCleanup.cs`, `SyntyRuntimeAssetLoader.cs` + `CombatApocalypseHudPaths.cs` (note: `CombatHealthBarUiFactory` loads Synty HUD prefabs through these — health-bar art needs a replacement path first), `CombatSliceConstants.cs`/`CombatSliceLayouts.cs` (only `Tests.PlayMode/CombatArenaTestBoards` uses layouts — migrate the test fixture). Corresponding `Presentation.Tests`: `CombatArena2D*Tests`, `CombatUnit2DStrip*Tests`.

### MUST KEEP (presentation-agnostic, carries over per handoff)
`CombatDirector.cs` (pacing incl. the v4 lunge fix), `CombatFlowPresenter.cs`, `CombatArenaPresenter.cs` (after F5 seam), `CombatUnitActor.cs` + `CombatUnitActorPool.cs`, `CombatGridMapper.cs`, `CombatArenaVisualPlacement.cs`, `CombatArenaChaseController.cs` + `CombatArenaFreeChaseMovement.cs` + `CombatArenaMoveSpeedResolver.cs`, `CombatArenaFreezeController.cs`, `CombatArenaSession.cs` + `CombatArenaSceneLoader.cs`, `CombatArenaBootstrap.cs` (camera setup will change, structure stays), `CombatArenaCameraFramer/PoseUtility/Shake/Tuner.cs` (review framer vs. new perspective camera), `CombatArenaAudioPresenter.cs`, `CombatAttackPresentationKind/Profile/ProfileResolver.cs`, `ICombatArenaVfxPresenter.cs` (the seam a 3D VFX presenter implements), `ArmyHealthBar*`, `CombatUnitHealthBar.cs`, `CombatHealthBarUiFactory.cs`/`CombatHudChromeBuilder.cs` (art source changes), `CombatGrimdarkSkin.cs`, `CombatFightBanner.cs`, `TacticPausePanel.cs`, `BattleReportPresenter.cs`, `LastBattleLogReviewPresenter.cs`. All of `Core/Combat` except the dead members listed above.

---

## Test coverage summary (sim/replay/director path)

**Good:** tick-loop determinism (`Determinism_FastForwardReproducesIdenticalEventLog`), pause trigger at 60%, gas anti-stall, footprint/occupancy on spawn+attack-kill, role engagement, accuracy/damage/attack-speed matrices, critical mass, command processor basics; PlayMode covers director event ordering and presenter replay (move/damage/destroyed, restore-snap, unload).

**Gaps (all named above):** command-event segment alignment (F1), ability-kill occupancy (F2), ShieldAllies end-to-end (F6), marksman stealth through `TickCombatRun` (F4), orchestrator mid-fight resume determinism (F3), `CombatReplayState`/`CombatEventMapper` direct tests, buffed-MaxHp replay fractions (M4).

---

## Fix log (2026-07-11)

All must-fix findings implemented (owner-approved scope; F6 confirmed as a bug). Written offline — needs one Editor compile + EditMode test run to confirm.

### F1 — command events logged one segment late
- `Core/Combat/CommandProcessor.cs`: `TryApplyBatch` no longer derives `logSegment = checkpointIndex + 1`; it takes an explicit `logSegment` parameter (kept `checkpointIndex` for costs/validation only).
- `Core/Combat/TickCombatRun.cs` (`ApplyCommands`): passes `CheckpointsFired` — the segment the subsequent playback actually plays — as the log segment.
- `Core/Combat/CombatEvent.cs`: corrected the stale `Segment` doc comment.
- Tests: `Core.Tests/EditMode/TickCombatRunCommandTests.cs` — `OpeningPauseAbilityKill_LogsKillAndFightEndInSameSegment`, `MidPauseAbilityKill_LogsInSegmentThatPlaysNext` (both also assert no event exceeds the max playable segment). Updated `CombatResolverTests.StanceCommand_IsLoggedBetweenPhases` call site.

### F2 — ability/strike kills never freed from the occupancy grid
- `Core/Combat/TickCombatRun.cs` (`ApplyCommands`): calls the previously-dead `RebuildOccupied()` after the command batch, so batch kills release their cells.
- Test: `TickCombatRunCommandTests.MidPauseAbilityKill_ReleasesOccupancyGrid`.

### F6 — ShieldAllies wiped by the post-batch armor reset
- `Core/Combat/TickCombatRun.cs` (`ApplyCommands`): the player `ArmorBuffSteps = 0` reset moved BEFORE `TryApplyBatch`, so armor granted in the batch survives until the next pause. The reset still only runs when the pause had commands (pre-existing quirk, unchanged: fight-start armor is stripped at the first commanded pause).
- Test: `TickCombatRunCommandTests.ShieldAllies_GrantedAtPause_SurvivesTheCommandBatch`.

### F4 — marksman stealth expiry unreachable
- `Core/Combat/CombatStealthRules.cs`: `tacticsCheckpointIndex >= 2` → `>= 1` (CheckpointsFired maxes at 1); hardcoded id hoisted to `MarksmanPieceId` const (data-driven flag deferred until more stealth pieces exist, per audit).
- Tests: `CombatStealthRulesTests` unit tests re-pointed at reachable indices (0 hidden / 1 targetable) and a new integration test `Marksman_StealthExpiresInRealFight_AttackedOnlyAfterMidFightPause` runs a full fight through `TickCombatRun`.

### F3 — restore applies end-tactic at fight start
- `Core/Run/RunState.cs`: `CombatSaveState.StartingTactic` (nullable; null on older saves → falls back to old behavior).
- `Game/RunOrchestrator.cs`: `BeginCombat` persists `StartingTactic`; `RestoreActiveCombatFromSave` applies the starting tactic before `FastForwardFromSave` (mid-fight changes replay from `SubmittedCommands`), then re-syncs `State.Combat.PlayerTactic` from the runner.
- `Core/Combat/CommandProcessor.cs`: mid-fight tactic changes now update `tactics.PlayerDamageBuff` (mirrors `SetPlayerTactic`), fixing the live-path half of the divergence. Note: like `SetPlayerTactic`, this overwrites a prior `SpendRequisitionBuff` +2 — consistent live/restore, flagged for the M-tier cleanup.
- Test: `RunOrchestratorTests.RestoreAfterMidFightTacticChange_ReproducesLiveEventLog` (full-log equality, live vs save/reload mid-fight).

### F5 — `ICombatUnitVisual` seam
- New `Presentation/Combat/Arena/ICombatUnitVisual.cs`: exactly the members the actor/presenter consume (`SetWalking`, `FaceDirection`, `UpdateSortAndBob`, `PlayAttack`, `PlayHurt`, `PlayDeath`, `Clear`, `BlocksLocomotion`, `VisualHeight`, `DeathSeconds`).
- `CombatUnitVisual2D.cs`: implements it; added `DeathSeconds => DieStripSeconds` (only change to the file).
- `CombatUnitActor.cs`: holds `ICombatUnitVisual _visual`; the 2D `AddComponent` is the single remaining 2D touchpoint in `Initialize`; exposes `DeathSeconds` (visual seam or 0.35s fallback).
- `CombatArenaPresenter.cs`: `PlayDestroyedEvent` times death VFX via `actor.DeathSeconds` instead of `CombatUnitVisual2D.DieStripSeconds` — presenter no longer references the 2D class. 2D behavior unchanged (DeathSeconds == DieStripSeconds == 1.2s).

### Dead-code deletions (zero callers re-verified by grep; prefab/scene GUID grep clean)
- Deleted files (+ `.meta`): `Core/Combat/CriticalMassRules.cs`, `Presentation/Combat/Arena/CombatArenaMuzzleAnchor.cs`, `CombatArenaFxCull.cs`, `CombatArenaPresentationMode.cs`.
- `Core/Combat/CombatMovement.cs`: removed `StepTowardTarget` + private `GetNeighbors`/`Manhattan` (kept `GetMoveCost`/`GetStepChargeCost`).
- `Core/Combat/CommandProcessor.cs`: removed `GetBonusActionSlots` and the single-command `TryApply` overload (its one test folded into `TryApplyBatch`).
- `Game/RunOrchestrator.cs`: `GetPrimaryActionBudget` dead bonus-slot branch removed (`=> 1`).
- Tests: `CommandProcessorTests.BonusActionSlots_AreDisabledInDemo` deleted; `CombatArena2DHelpersTests.PresentationMode_2DIsCanonical` deleted.
- NOT deleted (kept per scope): `CombatMovementRules.SelectNearestEnemyPosition`, `PhaseCommand.Stance`/`ChangeStance`, `_moveLerpFallbackSeconds`, all `CombatArena2D*` files, `CombatUnitVisual2D`. `RebuildOccupied` is now live (F2).

---

## Combat3D wiring (2026-07-11)

3D toon-ink actors driven by the real Core sim, behind the F5 `ICombatUnitVisual` seam. Written offline (no Editor available) — compiles by inspection; needs one Editor compile + the run steps below to confirm.

### Files added (all `Assets/_Project/`)
| File | Purpose |
|---|---|
| `Presentation/Combat/Arena/CombatUnitVisual3D.cs` | Second `ICombatUnitVisual` implementer: rigged model instance, Animator (`Moving` bool / `Die` trigger), smoothed yaw facing, placeholder punch-forward attack pose with the 2D path's muzzle/impact timing hooks, `_HitFlash` pulse + `_DissolveAmount` death ramp via MaterialPropertyBlock (DMZ/ToonInk), side-channel base ring (blue player / red enemy). `DeathSeconds` = die clip length + corpse linger + dissolve. No game rules. |
| `Presentation/Combat/Arena/CombatUnitVisual3DInstaller.cs` | Scene-level opt-in: while enabled, installs `CombatUnitActor.VisualFactory` (model, generated controller, side materials/rings, unit height, yaw offset). Missing refs → LogError once + fall back to 2D. |
| `Presentation/Combat/Arena/Combat3DDemoDriver.cs` | Demo harness (auto-starts in `Start()`): builds two 3-unit `conscript_rifleman` armies from ContentDatabase exactly like `CombatArenaTestBoards` does, runs `TickCombatRun` to completion (seeded, zero pause commands), then replays the log segment-by-segment through `CombatDirector.PlayLog` onto the arena presenter. Logs + shows a TMP result banner from the sim result; logs the replayed `fight_end` event. No RunManager/meta flow. |
| `Presentation/Editor/Combat3DDemoSceneBootstrap.cs` | Menu item `DeadManZone → Combat3D → Build Combat3D Demo Scene`. Guards `EditorApplication.isPlaying`; validates every spike asset path with a friendly abort; generates (under `Assets/_Project/Combat3D/`) looped `.anim` copies of the GLB clips, a fresh `RiflemanCombat3D.controller` (Idle default / Walk / Die; `Moving` bool, `Die` trigger — spike controller stays throwaway), a `CombatArena3DDemoConfig` (ToonInk3D, free-chase ON), ground/sandbag materials; then builds and saves `Assets/_Project/Scenes/Combat3D_Demo.unity` with spike-look environment (warm key light 40/-52° @1.7, trilight cool ambient, exp2 fog, P0_Grade global volume, graybox ground + sandbag boxes, perspective camera fov 42) and the arena rig (bootstrap/director/presenter/audio/loader/installer/driver on one GameObject). |

### Files modified (smallest diffs)
- `Data/CombatArenaVisualMode.cs` — added `ToonInk3D = 2`.
- `Presentation/Combat/Arena/CombatUnitActor.cs` — added static `VisualFactory` hook; `Initialize` uses it when installed, otherwise the 2D `AddComponent` path unchanged (factory is static because actors are pooled/runtime-created — no serialized field to hang it on).
- `Presentation/Combat/Arena/CombatArenaBootstrap.cs` — `Is3DMode` (config.visualMode == ToonInk3D) skips: forcing orthographic, the skybox/background override, `TopTroopsAtmosphere`, `CombatArenaPostFx`, the 2D battlefield view build, and the orthographic framer. All existing configs (TopTroops2D/Legacy3D) take the exact old code path.
- `Presentation/Combat/Arena/CombatArenaSceneLoader.cs` — added `MarkEmbeddedArenaLoaded()` so a scene that embeds the arena (instead of additively loading `CombatArena2D`) can activate `CombatArenaSession`, which gates `CombatArenaPresenter.OnEventReplayed`.

Reused as-is (not duplicated): `CombatDirector` v4 pacing (`EmptyTickPaceScale`, per-tick playback), `CombatArenaChaseController`/`CombatPresentationEngagement` free-chase, `CombatUnitActor` SmoothDamp anchor-follow, `CombatGridMapper`, `CombatArenaVisualPlacement.PlaceOnGround` (model scaling/ground align), `CombatUnitActorPool`, `CombatUnitHealthBar`, `CombatArenaMoveSpeedResolver`.

### How to run
1. Ensure content exists: `DeadManZone → Generate Demo Content (5 Factions)` (needs `conscript_rifleman` + `ironmarch_union`).
2. `DeadManZone → Combat3D → Build Combat3D Demo Scene` (not in Play mode). Watch the Console — missing spike assets abort with an explicit list.
3. Open `Assets/_Project/Scenes/Combat3D_Demo.unity` (the menu item leaves it open) and press Play. Expected: both trios march in (walk anim + free chase), rifles exchange fire (hit flashes on victims), casualties play the die clip then dissolve, and the result banner + `[Combat3D] fight_end` log line fire from the real event log.

### Open risks (verify in Editor)
- **GLB clip names/looping**: clip lookup is by substring (`idle`/`walk`/`dead|die`) with a single-clip fallback; loop flags are set on `.anim` copies because glTFast sub-assets are read-only. If glTFast's `AnimationClipSettings` copy misbehaves, idle/walk will play once and freeze.
- **Cross-GLB retargeting**: walk/die clips from sibling GLBs must bind to the idle GLB's rig paths (spike says same 24-bone rig; unverified in code).
- **Model forward axis**: facing assumes authored +Z forward; if the rifleman marches sideways, set `modelYawOffsetDegrees` on `CombatUnitVisual3DInstaller` (e.g. 180/±90).
- **Camera framing**: pos (0, 13.5, −19), pitch 34°, fov 42 is a calculated guess for the 17×6-cell field (~30.6×10.8 m at 1.8 m cells) using the spike's angle family — tune in the saved scene.
- **`PlaceOnGround` local-vs-world y**: it assigns a local-space bounds min to a world y; error is negligible at ~1.0 model scale (human-size rifleman → 1.7 m target) but check feet don't float/sink.
- **P0_Grade profile**: the asset's `components:` list looked empty in YAML — if the grade does nothing, re-save the profile from the spike scene's volume.
- **MPB vs SRP batcher**: `_HitFlash`/`_DissolveAmount` sit in the UnityPerMaterial cbuffer; per-renderer MPB overrides work but break SRP batching for flashed units (fine at 6 units).
- **Pause-skip pacing**: the demo plays segments back-to-back with a 0.6 s beat where the real flow shows the tactic pause panel; `fight_end` stops playback mid-segment by design.
- **2D regression surface**: `CombatUnitActor.Initialize` and `CombatArenaBootstrap` were touched — run the PlayMode `CombatArenaReplayPlayModeTests` to confirm the 2D path is still green (no factory installed → identical behavior expected).

## Verification + presentability pass (2026-07-11, editor session)

Everything above was validated live in the editor:

- **EditMode: 352/352 green** (345 baseline + 7 net new after fixes; two new `MidPauseAbilityKill_*` tests initially failed because `TestBoards.Layout`'s Rear zone rejects Unit pieces — the helper's unchecked `TryPlace(GridCoord(0,0))` silently no-oped. Fixed: conscript placed via `SupportLineAnchor(1,0)` with an asserted `PlacementResult.Success`.)
- **PlayMode `CombatArenaReplayPlayModeTests`: 7/7 green** — the 2D path is byte-identical through the `ICombatUnitVisual` seam.
- **Combat3D demo proven end-to-end**: bootstrap menu built `Combat3D_Demo.unity`; Play mode ran a real seeded 3v3 `conscript_rifleman` fight through `TickCombatRun` (106 events / 2 segments), replayed via `CombatDirector` — walk anims, hit flashes, deaths, side rings, health bars, victory banner from the replayed `fight_end` (seed 20260711 is an enemy-side win).

Presentability tuning discovered and backported into the bootstrap:
- **Interior-ink saturation**: the fullscreen edge-detect inks small-on-screen characters into solid silhouettes. Fixed two ways: camera moved close (0, 8.5, −12, pitch 30, fov 40) and `InteriorInk.mat` softened (`_NormalScale` 1.1, `_DepthBias` 0.9, `_InkStrength` 0.55, `_Thickness` 1.0 — NOTE: this is the *shared/global* spike material; the spike scene inherits the gentler look).
- **ToonInk shadow band**: spike `_ShadowColor` 0.10 reads black at gameplay distance. Tuned `_Project/Combat3D/Unit_{Player,Enemy}_3D.mat` copies: `_ShadowColor` (0.45, 0.44, 0.50), `_ShadowThreshold` −0.15, `_InkStrength` 0.28, `_MidStrength` 0.45; bootstrap now generates these via `LoadOrCreateTunedUnitMaterial`.
- **Lighting**: brightened trilight ambient + added a shadowless cool fill light (0.45 @ 25°/160°) so the toon shadow band never collapses to black.

Open items for the next session: scope the interior-ink feature per-layer instead of globally (still the outstanding arena-spec decision), punch-in camera beats on kill/crit events, custom shoot animation (attack is a placeholder pose), seed/roster variety on `Combat3DDemoDriver` (serialized `combatSeed` / `pieceId` / `unitsPerSide`), and the deferred armor-lifetime cleanup (fight-start buffs still strip at the first commanded pause).

## Roster integration (2026-07-11)

Three new Meshy 12k units (image3d → remesh → rig → animate, job ids in docs/meshy-roster-jobs-2026-07-11.md) integrated into the Combat3D demo:

- **GLBs**: `Assets/_Project/Combat3D/Models/{bulwark_squad,field_medic,grenade_thrower}/{idle,walk,die}.glb` (source of truth stays in `tools/meshy/units/<unit>/glb12k/`).
- **Archetype mechanism**: `CombatUnitVisual3DInstaller` grew a serialized `archetypes` array (`pieceId` → model + controller), resolved in the visual factory by the same ContentDatabase piece id the sim uses; unknown ids fall back to the default rifleman visuals. The bootstrap generates a looped-clip AnimatorController per unit (same discovery logic as the rifleman) and wires the array.
- **Piece-id mapping**: `bulwark_squad` and `field_medic` map 1:1; **no `grenade_thrower` piece exists in content** — that model is worn by `ironclad_mortars`.
- **Demo rosters** (serialized on `Combat3DDemoDriver`): player = conscript_rifleman, bulwark_squad, field_medic; enemy = conscript_rifleman, ironclad_mortars, conscript_rifleman. `BuildArmy` now advances placement rows by each piece's footprint height (ironclad_mortars is 2 cells tall).
- **Verified**: mixed silhouettes on both sides in Play mode, fight completed (53 events / 2 segments, fight_end at segment 1 tick 122, defeat banner), EditMode 352/352 green.
- **Note**: the session-limit cutoff mid-agent left `Combat3DDemoSceneBootstrap.cs` with a duplicated tail (stale pre-archetype copy after `#endif`); removed — if the scene ever rebuilds oddly, check that file first.
- **Texture roulette**: all three units came out in acceptable olive/camo tones this gen; no re-gens needed.

## Outline fix + mortars regen (2026-07-11, later)

**Scratchy silhouettes diagnosed**: A/B isolation (hull outline off vs fresnel ink off, close-up A-pose lineup) proved the scribbly unit edges were the ToonInk **inverted-hull outline pass** fragmenting on Meshy skinned meshes' noisy/split vertex normals — not the fresnel rim, not the fullscreen pass. Fix: `_OutlineWidth = 0` on `Unit_{Player,Enemy}_3D` (baked into the bootstrap's material tune). The combat renderer's fullscreen depth/normal edge detect draws the clean screen-space silhouette instead — verified close-up and at gameplay distance. Doctrine: **inverted hull is for clean-normal meshes only; skinned Meshy units get their line from the fullscreen pass.**

**Tuning note (non-blocking)**: the `_HitFlash` pulse reads near pure-white at full intensity — consider a lower peak or shorter pulse when polishing.

**grenade_thrower model retired**: it was an abandoned test (oversized head) — being replaced by a proper `ironclad_mortars` gen from its own ref (`tools/meshy/units/ironclad_mortars/ref.png`). Job ids: image3d 019f524e-6a9c-7649-8246-65e0afb23680 ✅, remesh 019f5252-aef2-7734-8035-b231428bdd7c ✅, rig 019f5254-6f0b-710f-9585-413d33e3de12 ✅, anims idle 019f5255-c6a1-7f38-b8ee-25506b5f9096 ✅ / die 019f5255-c9df-7823-b9f8-1096f419e550 ✅. Landed: GLBs at Assets/_Project/Combat3D/Models/ironclad_mortars/ (source tools/meshy/units/ironclad_mortars/glb12k/), archetype folder mapping updated (ironclad_mortars → ironclad_mortars), abandoned grenade_thrower model deleted from _Project, scene rebuilt, new unit verified in Play mode (normal proportions, ironclad gas-mask look), EditMode 352/352 green.

**Housekeeping**: `git prune` + `gc` run; pack 97.7 MiB, 0 loose objects.

## Rifles + shoot presentation (2026-07-11)

Units now carry a generated rifle prop and visibly aim + recoil when they fire. All verified live in Play mode; EditMode 352/352 green.

### What was built
- **`Presentation/Editor/RiflePropBuilder.cs`** — menu `DeadManZone → Combat3D → Rebuild Rifle Prop` (also called by the scene bootstrap). Generates `Assets/_Project/Combat3D/Rifle_Prop.prefab`: ~0.73 m bolt-action silhouette from primitive cubes + a cylinder barrel (stock/receiver/forestock/barrel/bolt handle) with a `MuzzlePoint` empty at the tip. Local +Z = barrel, origin at the grip. Materials `Combat3D_Rifle{Metal,Wood}.mat` — DMZ/ToonInk with the **inverted-hull outline ON** (`_OutlineWidth` 2, clean primitive normals → crisp ink line, per outline doctrine), `_ShadowColor` = 0.55 × base, `_InkStrength` 0.25. Rebuild overwrites in place (GUID stable).
- **`CombatUnitVisual3D`** — `Build` gained a `riflePrefab` param (fed from a new serialized `riflePrefab` on `CombatUnitVisual3DInstaller`, wired by the bootstrap). `AttachRifle` finds the right hand by case-insensitive name search (`right`+`hand`), parents the rifle there so it inherits animation and falls/dissolves with the body (its renderers join the `_HitFlash`/`_DissolveAmount` MPB list — ToonInk on both, so the whole figure flashes and dissolves together). Missing hand → one LogError, unit goes rifle-less. Muzzle flashes now spawn at `MuzzlePoint.position` (routed through the existing `onMuzzle` Vector3 seam — `Combat3DVfxPresenter` untouched); shoulder-height fallback kept for rifle-less units.
- **Aim + recoil layer** (same file, `LateUpdate` after the Animator): swing the shoulder→hand line toward a chest-height point on the victim, straighten the forearm at half weight, small spine yaw twist, then level the barrel via the hand bone. Recoil kick (rifle + forearm back along the aim line) starts exactly at the muzzle-flash moment. Weight smoothsteps 0→1 over the blend-in, holds for the attack window, eases out after recovery — no pop against idle/walk.

### Bone names (verified identical on all four Meshy rigs — do not guess on new rigs, dump first)
`Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand` (+Left mirrors, neck/Head, legs). Hand bone +Y points along the fingers; bones carry ~0.01 armature scale.

### Two bugs worth remembering
1. **Bone scale**: parenting a prop under a Meshy hand bone inherits ~0.01 lossy scale — the rifle rendered at 7 mm. Fix: normalize `localScale` by `handBone.lossyScale` (and express the grip offset in world meters divided by the same).
2. **Additive-prop drift**: the Animator restores *bones* every frame but not the prop, so `_rifle.position += kick` accumulated and rifles drifted meters away. Fix: re-seat the rifle on its rest grip pose at the top of every `LateUpdate` before applying offsets.

### Tuning defaults (serialized on CombatUnitVisual3D)
| Knob | Value |
|---|---|
| rifleGripOffsetMeters (hand-bone axes) | (0, 0.05, 0.02) |
| rifleGripLocalEuler | (−90, 0, 0) — barrel along fingers |
| rifleWorldScale | 1.0 (scaled by visualHeight/1.7 and bone scale) |
| aimWeight | 0.65 |
| spineTwistWeight | 0.25 |
| aimBlendIn / aimBlendOut | 0.15 s / 0.20 s |
| recoilDistanceMeters | 0.05 |
| recoilOut / recoilSettle | 0.06 s / 0.15 s |

### Per-archetype grip verdicts (close-up screenshots, shared default offset)
- **rifleman / ironclad_mortars**: great — rifle across the body at rest, levels cleanly at the victim mid-volley, flash at the tip.
- **bulwark_squad / field_medic**: sane — muzzle-down side carry at rest (arms hang lower on these rigs), levels correctly when attacking. No per-unit override needed.
- Flash-position check: live `MuzzleFlash` measured 0.138 m from the nearest `MuzzlePoint` = exactly the presenter's 0.12 m toward-target nudge.

### Needs eyes / open
- The at-rest carry is muzzle-down at the side (barrel along fingers). Reads fine at gameplay distance; a "port arms" two-hand carry would need a left-hand IK or a different grip euler per idle pose — deferred.
- The left hand doesn't touch the rifle (single-hand grip). Acceptable at gameplay camera; revisit with a real shoot clip.
- MCP gotcha: `editor-application-set-state` with only `isPaused` **stops Play mode** (omitted `isPlaying` defaults false) — always pass both flags to pause.

## Armor lifetime rule (2026-07-11)

Closes the deferred "armor-lifetime cleanup" from the F6 fix. Owner-decided rule:
1. **Fight-start armor** (synergies, critical mass, ProtectSupport — granted during combat setup, before tick 0) is **permanent** for the whole fight. Never stripped by pause boundaries.
2. **Pause-granted armor** (ShieldAllies) **expires at the next pause boundary** — every checkpoint fire, regardless of whether commands were submitted at either pause.

### Implementation (split state — audit F6's "cleaner" option)
- `Core/Combat/CombatantState.cs`: `ArmorBuffSteps` keeps its name and becomes the fight-start/permanent bucket (its two fight-start writers — `PieceAbilityEngine.ApplyToCombatants`, `TacticEffects.ApplyProtectSupportBuffs` — needed no changes). New `PauseArmorBuffSteps` holds pause-granted armor; new `TotalArmorSteps => ArmorBuffSteps + PauseArmorBuffSteps` is what damage resolution reads.
- `Core/Combat/CombatAbilityExecutor.cs`: ShieldAllies grants into `PauseArmorBuffSteps`; the two ability-damage sites read `TotalArmorSteps`.
- `Core/Combat/TickCombatRun.cs`: the `ApplyCommands` armor reset (which only ran when the pause had commands — the source of the button-press-dependent lifetime) is **deleted**. Expiry now lives in `TryFireCheckpoint`: the moment a checkpoint fires, all player `PauseArmorBuffSteps` are zeroed — unconditional, so it fires at every pause boundary. `ResolveAttacks` reads `TotalArmorSteps`.
- Enemy side: enemies have no command path, so pause-granted enemy armor cannot exist — no reset machinery added for them (noted here instead). Fight-start enemy armor (PieceAbilityEngine) lands in `ArmorBuffSteps` and is permanent, per rule 1.
- Save/resume: armor is **not** persisted anywhere in `CombatSaveState`/`RunState` (grep-verified) — resume reconstructs it by replaying `SubmittedCommands` through the same `Continue`/`TryFireCheckpoint` path, so the split needs no schema change. The F3 orchestrator restore test already proves log equality for mid-fight commands.

### Tests (Core.Tests/EditMode/TickCombatRunCommandTests.cs — 5 new, suite 357/357)
- `FightStartArmor_SurvivesCommandedPause` — regression for the old strip-at-first-commanded-pause quirk.
- `FightStartArmor_SurvivesWholeFight_WithCommandsSubmitted` — runs to completion with commands at both pauses.
- `ShieldAllies_ExpiresWhenNextPauseFires_EvenWithNoCommandsThere` — armor granted at the opening pause is 0 the moment the mid pause fires, before any commands; stays 0 through an uncommanded Continue.
- `ShieldAllies_OnFightStartArmor_RevertsToBaselineAtNextPause` — ShieldAllies stacked on a medic-aura baseline; after the mid pause the unit is back to exactly baseline (1), not zero.
- `ShieldAllies_ReplayViaFastForward_ReproducesIdenticalLog` — live run vs `FastForwardFromSave` replay with a ShieldAllies command produce identical event logs (save-resume determinism for both armor buckets).
- New fixture helper `StartMatchedFightWithArmoredBuddy` grants fight-start armor through the real `PieceAbilityEngine` path (medic armor aura adjacent to infantry in the support zone). Gotcha re-hit: `SupportLineAnchor(0)` = column 3 is actually the Rear zone (rejects Units) — the trio sits at columns 4-6.

### Expectation changes in existing tests (each inspected)
- `ShieldAllies_GrantedAtPause_SurvivesTheCommandBatch`: asserts `PauseArmorBuffSteps == 1` instead of `ArmorBuffSteps == 1` — pure field rename, the expectation (armor survives the batch that grants it) is unchanged. No other test encoded the old strip behavior; the rest of the suite passed untouched.

### Deferred / notes
- **M9 unchanged and now unmasked**: `ApplyProtectSupportBuffs` still stacks `+2` on every `SetPlayerTactic(ProtectSupport)` call, and with permanent armor nothing ever removes it. Safe today because both live (`BeginCombat`) and restore paths call `SetPlayerTactic` exactly once after the ctor (whose default tactic is DisciplinedFire), and mid-fight tactic changes go through `CommandProcessor`, which doesn't touch armor. A second `SetPlayerTactic(ProtectSupport)` call would double-grant — fix when M9's idempotence cleanup lands.
- Mid-fight switch **to** ProtectSupport still grants no armor (pre-existing `CommandProcessor` behavior, flagged in F3's fix note); under the new rule that's consistent — ProtectSupport armor is a fight-start grant only.
- Presentation confirmed untouched: armor has no visual today (`shield_allies` only surfaces as a `CombatLogFormatter` line and ability names in `TacticPausePanel`/tooltips). No Presentation changes made.

## Environment pass (2026-07-11)

Replaced the graybox plane + sandbag boxes with a full Trenchline battlefield, all generated by the bootstrap. New file `Presentation/Editor/CombatEnvironmentBuilder.cs` (static builder, called from `Combat3DDemoSceneBootstrap.BuildDemoScene`); fully deterministic (fixed seed + hash-lattice value noise — no `UnityEngine.Random`), rebuilt repeatedly via the menu item with identical output. Editor-only code; Core/Game untouched, 2D path untouched.

### What was built (spec sections cited)
- **Ground** — 100x72-quad displaced mesh (140x100 m): gentle broken-earth relief, 14 shell craters with raised rims, and a churned crest that rises ~3 m toward the fog line so the frame-top horizon reads as jagged battlefield skyline (the camera is only 8.5 m up — the horizon has to happen inside z<40, not at the mesh edge). The 17x6-cell combat strip (±16.4 x ±6.4 m incl. margin) is EXACTLY flat (displacement smoothsteps in 0.5–5.5 m outside it); `AssertLanesFlat()` self-check runs every build so unit grounding at y=0 can never silently break. Ground material: URP/Lit, baked 2048px world-mapped albedo (mud/dry tonal patches, crater scorch, sparse pockmark speckle) + faint **marching-lane ruts** along the six rows — spec §2's "grid fades to diegetic ground markings (ruts)". Tiled 256px grey-noise detail map (~2 m repeat, trilinear + aniso 4) keeps the near field crisp.
- **Trenchworks** (spec §5 Trenchline dressing: "olive/dirt, sandbags, duckboards, wire") — at both deployment edges: two staggered jittered sandbag rows with a mid-line sally port (spike-proven stacked-box read), timber revetment posts, duckboard walkways, crate dumps, standing + toppled barrels.
- **Wire + tank traps** — two barbed-wire belts (jittered leaning posts + thin stretched strand segments) outside the strip, plus 10 anti-tank hedgehogs (three crossed steel beams each) strung along the belts and mid-ground.
- **Scatter** — leaning telegraph poles and half-buried debris planks near far-side craters.
- **Backdrop ring** — jagged low-poly ridge meshes (26-seg noise-displaced spines) on a 34–44 m ring, plus mid-distance ruin masses (rubble mounds, tilted wall slabs, chimneys) inside the visible band and larger ruined shells farther out; all URP/Lit (ToonInk doesn't apply fog — Lit is what lets the fog swallow them per spec §5) at a value above the mud so they read as hazed masses, not black cutouts.
- **Line law (bible §4)**: primitive props (sandbags/timber/crates/barrels/hedgehogs) carry the hull outline (`_OutlineWidth` 2, clean normals); wire strands and backdrop run hull-off and take their line from the combat renderer's fullscreen pass — same doctrine as the rifle prop vs. Meshy units.

### SO decision (step-5 check)
`CombatArenaAtmosphereProfileSO` / `CombatArenaBackdropRingSO` / `CombatArenaBackdropSpawnPoint` are 2D-era: their only reference anywhere is the serialized `atmosphereProfile` field on `CombatArenaConfigSO:113` — zero readers. NOT forced into the 3D path; the environment is editor-time generated, not runtime-assembled. Candidates for the 2D-switchover deletion list.

### Lighting/fog changes (base warm-key/cool-fill/trilight kept)
- Fog density 0.018 → **0.022** (exp2, same color) — tuned WITH the backdrop so the ridge ring sits at ~35–50% fog and distance reads; units at the far lane stay well under the readability bar.
- Camera background 0.13/0.15/0.19 → **0.17/0.19/0.24** (slightly above fog color): the fog line at the terrain crest is key-lit ground mixed with fog, and a bg at raw fog color read as a dark seam. Sky stays a solid grimdark band — the home frame's top edge is always ground crest, so a gradient skybox would render zero visible pixels; cheapest thing that looks intentional (deviation from "graded sky" noted below).
- Timber/prop values kept dark deliberately: under the 1.7x warm key anything brighter became the most saturated thing on screen, violating bible §3's saturation budget (VFX must win).

### Perf
315 renderers, **23,980 tris**, **8 shared materials** (Ground, Sandbag, Timber, Crate, Barrel, Wire, Steel, Backdrop), everything flagged BatchingStatic, no colliders, no runtime scripts, textures generated once as assets (2048 albedo + 256 detail). Renders under fullscreen ink + SSAO without measurable cost at this scale.

### Look iterations (3)
1. Baseline shot: strip read as an empty blurry dirt sheet; ruins read as flat black cutouts; spec props missing (no hedgehogs/barrels). 
2. Contrast + dressing pass: stronger mud/dry contrast, ruts widened/darkened, pockmark speckle added, backdrop value lifted (0.078 → 0.135), hedgehogs + barrels added. Verdict: field reads, but debris planks rendered as bright floating orange sticks (saturation-budget violation + crater-slope float).
3. Value/seating/resolution pass: timber darkened (0.26 → 0.16 base), planks seated −0.01 into ground, albedo 1024 → 2048 with trilinear/aniso. Final home-frame shot: churned earth with ruts and pockmarks, wire belt + hedgehog X-silhouettes mid-frame, crater field and hazed ruin masses to the fog line — reads as a stylized WW1-diesel battlefield illustration.

### Verification
- Menu rebuild (`DeadManZone → Combat3D → Build Combat3D Demo Scene`) regenerates the full environment deterministically (ran 4+ times this session; lanes-flat self-check silent every time).
- Play mode end-to-end: 3v3 sim fight (53 events / 2 segments), units march/fight/die on the flat strip, no prop occludes the fight read (mid-fight screenshot with a dissolving casualty + advancing enemy trio), `fight_end` replayed (segment 1, tick 122), defeat banner fired. Punch-in camera: pure dolly, ≤3.5 m from home toward mid-strip kill points — path stays over the prop-free flat strip; no clipping observed or geometrically possible.
- EditMode 357/357 green.

### Deferred
- **Siege ground / Fog field themes** (spec §5): Trenchline only; the other two re-dressings wait for their fight content.
- **Dust/ash particle drift**: skipped — the fullscreen ink turns sparse translucent particles into flickering edge noise; revisit with the VFX saturation pass (spec §6).
- **Trenchworks visibility**: the deployment-edge trenches sit just outside the tight home frame (readability camera from the interior-ink fix); they read in punch-ins near the edges and scene view. If the owner wants them in the home frame, that's a camera-pullback decision, not a dressing change.
- Graded sky dome: unnecessary while every frame's top edge is ground crest (see lighting notes).
- Synty meshes deliberately not used (spec direction is toon-ink primitives; ADR-0003 kitbash applies to units, not the arena).

## Hover fix + cleanups (2026-07-11)

Four bundled items, all verified live in the editor. EditMode **358/358** green (357 baseline + 1 new M9 regression test).

### 1. Units hovering above their base rings — fixed
- **Root cause** (`Presentation/Combat/Arena/CombatArenaVisualPlacement.PlaceOnGround`, sole caller `CombatUnitVisual3D.Build`): the helper assigned the **local-space** renderer-bounds min to a **world-space** y (`position.y = -localBounds.min.y`). Local bounds are scale-independent (`InverseTransformPoint` divides scale back out), so the feet ended up at `localMin.y * (scale − 1)` instead of 0. Meshy rigs have their pivot above the feet (`localMin.y` ≈ −0.4..−0.7) and scale to ~0.72–0.75 for the 1.7 m target height → feet floated **+0.10 m (riflemen) to +0.15 m (mortars)** above the rings. Measured in play mode: old math reproduced hover of 0.097/0.098/0.149 m per unit; the audit's "negligible at ~1.0 model scale" assumption didn't hold once real Meshy scales landed.
- **Fix** (no fudge constants): seat the pivot at the anchor, then measure the union of **world-space** renderer bounds and shift by `worldCenter.y − worldBounds.min.y` — feet rest exactly at the anchor's ground height (y=0, ring at +0.02) for any pivot/scale. New private `MeasureWorldBounds` helper; `MeasureLocalBounds` still used for the height-ratio scale.
- **2D path**: unaffected by construction — grep confirms `PlaceOnGround` has exactly one caller (`CombatUnitVisual3D.Build:117`); nothing 2D touches it. (`TryMeasureMeshFootprint` in the same file has zero callers — left alone, noted as dead.)
- **Verified**: play-mode probe (feet world y at build = anchor y; live skinned-bounds wobble of −0.1 during walk cycles is AABB conservatism, not mesh position) + proof screenshot `docs/framing/04_FINAL_baked_..._units_on_rings.png` — all six units planted on their rings.

### 2. M9 — ApplyProtectSupportBuffs idempotence (Core)
- After the armor-lifetime change made `ArmorBuffSteps` permanent, every `SetPlayerTactic(ProtectSupport)` call stacked +2 rear armor forever (ctor + orchestrator both call it on the restore path; a second call double-granted permanently).
- **Fix**: `TickCombatRun` tracks a `_protectSupportArmorGranted` flag; `ApplyTacticDamageBuffs` grants the ProtectSupport rear armor at most once per run (damage buffs still recompute every call). Switching away does not revoke (unchanged behavior — fight-start armor is permanent; mid-fight tactic changes never re-applied it anyway, `CommandProcessor` doesn't call `SetPlayerTactic`).
- **Restore path** (`RunOrchestrator.RestoreActiveCombatFromSave`): ctor applies the default tactic (never ProtectSupport pre-grant), then `SetPlayerTactic(StartingTactic)` grants exactly once — identical to the live `BeginCombat` path. Live/restore parity preserved.
- **Test**: `TickCombatRunCommandTests.SetPlayerTactic_ProtectSupportTwice_GrantsRearArmorOnce` (hybrid gas drone parked in the rear zone; asserts +2 after one call and still +2 after two).

### 3. 2D-era dead ScriptableObjects deleted
- Re-verified zero code readers by grep, then deleted (with `.meta`): `Data/ScriptableObjects/CombatArenaAtmosphereProfileSO.cs`, `CombatArenaBackdropRingSO.cs`, `CombatArenaBackdropSpawnPoint.cs`, **plus** `CombatArenaBackdropRing.cs` (the enum — its only two readers were the deleted files).
- `CombatArenaConfigSO.cs`: removed the dead `atmosphereProfile` field (former line 113 — its only reference anywhere) and trimmed the stale "Superseded by CombatArenaBackdrop…" tooltip on `spawnPerimeterProps`.
- Instances: `assets-find` found one — `Data/Resources/DeadManZone/CombatArenaAtmosphereProfile.asset`. Its GUID appears in zero scenes/prefabs/assets and no `Resources.Load` path references it → deleted with the class. No `CombatArenaBackdropRingSO` instances existed.
- Both `CombatArenaConfigSO` assets (`CombatArena3DDemoConfig`, `Resources/DeadManZone/CombatArenaConfig`) load cleanly after the field removal (Unity drops the unknown serialized data).

### 4. Camera pullback for trenchworks framing
- Iterated three candidates from the old pose (0, 8.5, −12 / pitch 30 / fov 40), screenshots in `docs/framing/` (00 = old baseline, 01–03 = candidates, 04 = final baked pose mid-fight):
  - **A** (0, 9.5, −13.5 / 30 / 40): wire belt + hedgehogs fully in frame, parapets still clipped.
  - **B** (0, 10.25, −14.5 / 31 / 41): steeper pitch pushed the backdrop out and added dead foreground — worst of the three.
  - **C — picked** (0, **10**, **−14** / **pitch 29** / **fov 42**): the shallower pitch is the trick — it lifts the parapet/crater backdrop into frame instead of buying more foreground dirt. Ruin masses, crater field, wire belt and hedgehogs all read; units keep ~80% of their old on-screen height (well above the 2/3 readability floor for the interior ink).
- Baked into `Combat3DDemoSceneBootstrap.CreateCamera`, scene rebuilt via the menu, saved. This closes the earlier "Trenchworks visibility" deferred item.

### Verification
- assets-refresh clean (no compile errors; only pre-existing warnings).
- Menu rebuild regenerates the scene with the new camera (pose confirmed on the saved scene's ArenaCamera).
- Play mode end-to-end on the rebuilt scene: 3v3 fight (53 events / 2 segments), units stand ON their rings (screenshot 04), `fight_end` replayed at segment 1 tick 122, defeat banner fired.
- EditMode 358/358 green.

## Ring health + army HUD (2026-07-11)

Owner-requested redesign of health presentation in the 3D arena: the base ring IS the unit health display (no floating overhead bars), plus a top-of-screen army health HUD.

### Health source (both parts)
`ArmyHealthReplayTracker` (Core, `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs`) — the same replay-driven tracker the 2D arena's bars consumed. `CombatArenaPresenter` already owned a per-unit instance (`_unitHealth`), fed by every replayed event in `OnEventReplayed`; `TryGetUnitFraction` drives `CombatUnitActor.SetHealthFraction` on damage/graze/gas_damage impacts and on arena (re)initialization. The army HUD reuses `ArmyHealthBarPresenter`, whose own tracker registers all Combatant-tagged units from the battlefield and aggregates per-side fractions. Presentation reads replayed state only — zero Core changes.

### Part 1 — ring-fill unit health (shader/mesh approach)
- New unlit URP shader `DMZ/CombatRingFill` (`Presentation/Combat/Arena/Shaders/CombatRingFill.shader`): flat quad at the unit's feet, circular silhouette via UV-radius clip. Pie fill by `atan2(d.x, d.y)` vs a `_Fill` float (0..1), sweeping clockwise from the far side; drained sector shows `_EmptyColor` (near-black). A thin always-on outer rim band (`_RimInnerRadius`..`_RimOuterRadius`) stays side-colored so a near-dead unit still reads blue/red. `Offset -1,-1` + 0.02 m lift avoids ground z-fighting.
- `CombatUnitVisual3D.BuildSideRing` now creates the quad with this shader instead of the spike's flat disc; `SetHealthFraction` sets a target fill and `Update` eases the displayed fill via `MoveTowards` (1.4 fill/s) through a `MaterialPropertyBlock`, so hits read as a short drain, not a pop. `PlayDeath` drains the ring to 0 as the unit falls; the ring hides when the dissolve completes.
- Materials are bootstrap-generated (`RingFill_Player.mat` / `RingFill_Enemy.mat` under the demo's Generated folder), colors sampled from the spike's RingBlue/RingRed palette with slightly lifted rims — muted per bible §3 saturation budget, reset on every menu rebuild.

### Bar gating for 3D (2D path untouched)
`ICombatUnitVisual.DisplaysHealth` is the seam: `CombatUnitVisual3D` returns true (ring present), `CombatUnitVisual2D` returns false. `CombatUnitActor.Initialize` only attaches `CombatUnitHealthBar` (the floating world-space bar) when the visual does NOT display health itself; `SetHealthFraction` forwards to both the bar (2D) and the visual (3D). `CombatUnitHealthBar` itself is unchanged — the 2D sprite arena keeps its overhead bars.

### Part 2 — army health HUD wiring
- `CombatArmyHealthHud` (`Presentation/Combat/Arena/CombatArmyHealthHud.cs`): own screen-space overlay canvas (sort 400, under the result banner at 500), 1920x1080-scaled — same pattern as the demo's banner canvas. Two opposing horizontal bars at the top: player left/blue filled from the left edge, enemy right/red mirrored via `Image.Type.Filled` + `OriginHorizontal.Right`, so damage widens the center gap. Ring-family muted colors, dark backing, `CombatHudChromeBuilder.AddSideLabel` for the YOUR FORCES / ENEMY FORCES tags. The 2D bar factory (`CombatHealthBarUiFactory`) was too Synty-prefab-coupled to reuse; the presenter + tracker are reused as-is.
- Subscribes to `CombatDirector.EventReplayed` and forwards to `ArmyHealthBarPresenter.HandleReplayEvent`; `Combat3DDemoDriver` calls `armyHud.Initialize(battlefield)` right after `presenter.InitializeArena` so bars snap to 100% before playback. `Combat3DDemoSceneBootstrap` adds + wires the component on the arena rig, so a menu rebuild includes it.

### Spec sections followed
Arena spec §6 (VFX & feedback): army bars kept as ambient-tier feedback, no full-screen interrupts; punch-in camera untouched and confirmed compatible. §1 camera framing unchanged. Bible §3 saturation budget respected (muted fills, no saturated discs).

### Verification
- assets-refresh clean, zero compile errors; menu rebuild (`DeadManZone → Combat3D → Build Combat3D Demo Scene`) regenerates scene + ring materials + HUD wiring.
- Play end-to-end (seed 20260711, defeat outcome): early — all six ring discs full, both army bars full, NO floating bars over units; mid — blue rings partially drained (pie sectors visible), player bar well below enemy bar; late (0.25x slow-mo to catch the fast kill phase) — near-dead blue units read as empty dark discs with a clearly readable thin blue rim, one unit mid-death-fall on its drained ring, punch-in camera active with the screen-space HUD unaffected; end — corpses dissolve, rings hide with them, defeated side's bar empty, banner over the intact HUD.
- EditMode 358/358 green.

### Deferred
- This seed's fight is lopsided (~8 s of damage events) — pacing/balance is a sim/content concern, not presentation.
- Bible §6 worn/aged chrome restyle for the HUD bars (checkpoint notches, damage pops) — flat muted quads for now.
- 2D arena bar factory consolidation onto the new minimal chrome — YAGNI until the 2D path is revisited.

## Audio + pause beat (2026-07-11)

Two presentation items: the 3D demo scene gets sound (it was fully silent) and the bare 0.6 s inter-segment gap becomes a watchable TACTICAL PAUSE beat. Core untouched, 2D path untouched.

### Audio — what existed vs. what was wired
- **Audit**: `CombatArenaAudioPresenter` was already on the rig and `CombatArenaPresenter` already drives it from the replayed event seams (`PlayAttackMuzzleVfx` → rifle/cannon shot, `PlayAttackImpactVfx` → impact/explosion, `PlayDeathVfxAfterDelay` → death when the fall completes). Three gaps made the scene silent: (1) the shared `Resources/DeadManZone/CombatArenaAudioSet.asset` has **all five clip fields null**; (2) the project ships **zero usable combat SFX** (`t:AudioClip` finds only SlimUI menu clicks, Unity AI voice samples, and a menu music track); (3) the bootstrap-built camera had **no AudioListener** — even with clips, nothing would ever be audible.
- **PLACEHOLDER clips** (per the no-real-audio contingency): new editor utility `Presentation/Editor/Combat3DPlaceholderAudioBuilder.cs` generates six deterministic (fixed seed 20260711) filtered-noise blips as 16-bit mono WAVs under `Assets/_Project/Combat3D/Audio/` — `placeholder_rifle_shot` (0.14 s crack), `placeholder_cannon_shot` (0.5 s thump), `placeholder_bullet_impact` (0.09 s tick), `placeholder_explosion` (0.8 s rumble), `placeholder_unit_death` (0.45 s body-fall thud), `placeholder_ambience_wind_loop` (6 s seamless wind bed, tail crossfaded into head, loop-periodic gust swells). All are throwaway: replace the .wav files (same names) or swap the set asset when real SFX land. Existing files are never regenerated (identical bytes anyway), so menu rebuilds don't churn OneDrive.
- **Wiring** (bootstrap only): a demo-only `Combat3DDemoAudioSet.asset` binds the five one-shots and is serialized onto the presenter's `audioSet` field (so the shared 2D Resources fallback asset stays untouched and the 2D arena's behavior is unchanged); `AudioListener` added in `CreateCamera`; `AmbienceBed` child with a plain looping 2D `AudioSource` (vol 0.16 — a floor, not a feature, per bible §3). Presenter `masterVolume` stays 0.55; `PlayClipAtPoint` gives cheap 3D positioning per event.
- **Verified programmatically** (can't hear in this session): injected a play-mode watcher counting `One shot audio` spawns — one full fight produced **55 one-shots** (27 rifle shots, 25 impacts, 3 death thuds landing at fall completion t=10.1/11.7/12.2), ambience source `isPlaying` through the whole run. No cannon/explosion fired this seed (all three roster archetypes resolve to the rifle presentation profile).

### Tactic pause beat (Combat3DDemoDriver)
- `betweenSegmentsSeconds` (bare 0.6 s wait) replaced by `pauseBeatSeconds = 1.5` / `pauseBeatTimeScale = 0.35`: at each segment boundary (never after the final segment — `fight_end` breaks the loop first) a screen-space canvas (sort 450: above HUD 400, under banner 500) fades in over 0.25 s, holds ~0.9 s, fades out 0.35 s. Styled with the shared `CombatGrimdarkSkin` kit (dark band upper-third, bone "TACTICAL PAUSE" title, "ORDERS HOLD — COMBAT RESUMES" subtitle) so it reads as the real pause panel's non-interactive cousin — the interactive `TacticPausePanel` is deliberately NOT wired (demo submits no commands).
- `Time.timeScale` drops to 0.35 for the beat (battlefield holds its breath — idles/dissolves slow, audio unaffected) and is restored before the next `PlayLog`; fades/hold run on unscaled/realtime so the beat is a fixed 1.5 s. `OnDestroy` restores timeScale if the run is torn down mid-beat. Determinism untouched by construction: the sim already ran to completion before playback starts; the beat only paces the replay.
- Verified: beat logged + screenshotted at full alpha mid-fight (band clear of units and HUD, muzzle flash frozen under it); combat resumes after; end state `banner=True beatActive=False timeScale=1.00` — including on a run where the editor itself was paused mid-beat.

### Verification
- assets-refresh clean, zero compile errors; menu rebuild regenerates scene + audio set + listener + ambience deterministically (idempotent across the script-execute retry storm — ~10 back-to-back rebuilds, identical result).
- Play end-to-end twice (seed 20260711, defeat outcome): volley SFX + impact SFX during both segments, pause beat between segments, death thuds as bodies fall, banner + army HUD intact.
- EditMode **358/358** green.

### Deferred
- **Real SFX**: everything generated here is a clearly-labeled placeholder — grimdark-quiet blips, not sound design. Swap-in path documented above.
- **Cannon/explosion audibility**: wired and clip generated, but no roster piece currently resolves to the cannon/artillery presentation profile in the demo — will light up when such a piece lands.
- **HUD pulse during the beat** (spec-optional flourish) — skipped as YAGNI; the time dilation + band already carry the moment.
- Audio mixer / volume settings plumbing — single hardcoded volumes are fine for a demo scene.

## Two-hand carry (2026-07-11)

Closes both "needs eyes" items from the rifles pass: the limp one-hand muzzle-down side carry is now a **port-arms rest carry** (rifle diagonal across the chest, barrel up-left ~45°), and the **left hand rides the forestock** via code-driven two-bone IK. All in the same `CombatUnitVisual3D.LateUpdate` additive stack — no authored clips, no animation-rigging package, Core/2D untouched. Verified live across all four archetypes; EditMode 358/358 green.

### Approach (LateUpdate order matters)
1. **Re-seat rifle** on its rest grip local pose (unchanged — kills recoil accumulation).
2. **Port-arms rest layer** — same self-correcting world-space math shape as the aim layer, so it lands the same pose on all four rigs regardless of local bone axes: swing the right shoulder→hand line toward a chest-front anchor (character space, scaled by `visualHeight/1.7`), forearm at 0.6× that weight, then rotate the *hand* (rifle is its child) so the barrel points up-left along `portArmsBarrelDirLocal`. Character space = `modelRoot.rotation * Euler(0,-yawOffset,0)` (authored yaw offset removed).
3. **Aim + recoil** — untouched. Rest layer scales by `(1-aim01)` so at full aim the pose is exactly the previously approved aim feel.
4. **Left-hand two-bone IK** onto a new `ForestockPoint` empty on the rifle prop (`RiflePropBuilder`, prefab regenerated in place) — runs LAST so the support hand tracks the rifle through aim blend-in/out AND recoil (recoil moves the rifle before the solve; hand follows automatically, no elbow pop seen at pause-frame inspection).
5. **Death fade** — everything above scales by `1 - clamp01((t-deathStart)/deathReleaseSeconds)`; `PlayDeath` also clamps `_aimEndTime` to now. Hands release over 0.35 s as the die clip starts, then the layer goes fully silent (die clip owns the body; rifle stays parented to the grip hand and falls/dissolves with it). The old `if (_dying) return` guard is gone — that's what let the release blend run.

### IK math shape (`SolveTwoBoneIk`, static, ~40 lines)
Standard analytic two-joint solution (Ryan Juckett / Daniel Holden form), world-rotation composition:
- clamp target distance into `[eps, L1+L2-eps]`;
- law-of-cosines for the new shoulder/elbow interior angles; rotate upper and lower about the **current bend-plane normal** (`cross(c-a, b-a)`, hint-plane fallback when the arm starts straight) by the angle deltas;
- `FromToRotation` swing on the upper arm so the wrist lands on the target;
- pole step: project elbow and hint onto the plane ⊥ shoulder→target, roll the upper arm by their signed angle (hint = character-space point down-left of the shoulder);
- weight <1 slerps both solved world rotations back toward the animated pose (upper first — lower depends on it).
Wrist after the solve: single `FromToRotation` (shortest arc, no added twist) taking hand +Y (fingers on these rigs) to `leftHandFingersDirRifleLocal` in rifle space — fingers wrap across the stock.

### Walk integration gotcha (iteration 2 of 3)
First pass scaled the whole rest layer by `portArmsMovingMultiplier` while Moving — on the low-hanging-arm rigs (bulwark/medic) the leftover 36% of a ~140° barrel arc read as a drooping muzzle mid-march. Fix: the moving multiplier now eases only the **arm swing** (that's what fights the walk clip's arm swing); the **barrel align stays full weight**, so the muzzle never droops. Walk still reads — the multiplier plus the hand-anchor swing leaves the torso bob and leg work fully visible.

### Live-tuning workflow (worth repeating)
Pose numbers were iterated **in Play mode via reflection** (script-execute setting the private serialized fields on live units + `screenshot-camera` on a spawned `InspectCam`), then baked into code defaults — three visual iterations without a single recompile. Freeze-frames via `EditorApplication.update` watches that pause exactly when a unit is mid-aim (`_aimStartTime/_aimEndTime` window) or mid-death-release.

### Knob defaults (serialized on CombatUnitVisual3D)
| Knob | Value |
|---|---|
| portArmsArmWeight / portArmsBarrelWeight | 0.65 / 0.95 |
| portArmsHandAnchorLocal (char space, 1.7 m figure) | (0.10, 1.15, 0.28) |
| portArmsBarrelDirLocal | (−0.85, 1, 0.25) — ~45° up-left, tilted forward to clear the shoulder |
| portArmsMovingMultiplier (arm swing only) | 0.75 |
| leftHandIkWeight | 1.0 |
| leftElbowHintLocal (char space) | (−0.38, 0.75, 0.10) |
| leftHandFingersDirRifleLocal | (0.9, 0.45, 0) |
| forestockGripOffsetMeters (rifle axes) | (0, −0.02, 0.03) |
| deathReleaseSeconds | 0.35 |
| ForestockPoint (rifle prefab local) | (0, −0.018, 0.16) |

### Per-archetype verdicts (close-up pause-frame screenshots)
- **conscript_rifleman**: rest, walk, and mid-volley aim all read — rifle levels at the victim with the left hand tracking the forestock; no wrist twist artifacts at aim blend boundaries.
- **bulwark_squad**: was the droop case (see gotcha); after the barrel-weight split it holds a textbook 45° port arms at rest and mid-march.
- **field_medic**: clean two-hand march carry; left-hand reach is comfortable (targetDist ~0.26 m vs 0.49 m arm length).
- **ironclad_mortars**: reads correctly front and back (barrel visible over the shoulder line from behind).
- Left-hand reach margin measured on all rigs (0.15–0.32 m target distance vs 0.47–0.49 m arm length) — nobody needed the "blend IK down for hopeless proportions" escape hatch.
- Gameplay-distance wide shot: three survivors at port arms read MORE soldierly, not noisier; full play-through completes (banner + army HUD fine, zero errors).

### Deferred
- Fingers don't articulate (rigid glove mesh) — palm-on-forestock contact is approximate by design; a few cm invisible at gameplay distance.
- Rest grip euler still the shared (−90,0,0) default — the barrel-align step makes per-archetype grip overrides unnecessary so far.
- Die-clip rifle flail: during some death frames the rifle (still hand-parented) swings past the torso; reads as losing grip at speed, revisit only if a slow-mo kill cam ever lands. **Fixed in the 2026-07-11 rifle-read pass below.**
- The transient mid-turn frame where the carry crosses high (arm swing + facing ease) — visible only in freeze-frame, not at speed. **Fixed in the 2026-07-11 rifle-read pass below.**

## Rifle read + orb drain (2026-07-11)

Four owner-requested fixes: rifle readability at gameplay distance (owner screenshot: prop vanished against the body from behind/above), ring health drain reshaped from pie-slice to a top-down orb level (owner design change), the deferred die-clip rifle flail, and the deferred high-carry single-frame pops. All presentation-side — Core and the 2D path untouched.

### 1 — Rifle reads at gameplay distance
Three levers combined (`RiflePropBuilder` + `CombatUnitVisual3D` defaults):
- **Thicker proportions** (stylized board-game bar, length unchanged at ~0.73 m authored): stock cross-section 0.045×0.085 → **0.060×0.105**, receiver 0.042×0.06 → **0.055×0.075**, forestock 0.04×0.05 → **0.055×0.065**, barrel radius 0.016 → **0.026**, bolt handle 0.015² → 0.022².
- **Lighter materials, still muted** (bible §3): gunmetal (0.16,0.17,0.20) → **(0.30,0.32,0.38)**; wood (0.30,0.20,0.12) → **(0.40,0.30,0.20)**. First wood attempt (0.46,0.32,0.19) rendered highlighter-orange under the toon light — pulled back a step (iteration 2 of 2). `EnsureToonInkMaterial` re-derives `_ShadowColor`, so rebuilds stay consistent.
- **Pose + scale**: `rifleWorldScale` 1 → **1.2**; carry anchor (0.10, 1.15, 0.28) → **(0.13, 1.15, 0.34)** (held slightly out from the chest so the prop separates from the torso); barrel rest direction (−0.85, 1, 0.25) → **(−1, 0.85, 0.22)** (flatter diagonal ⇒ more barrel crossing the silhouette from behind/above).
- Verified from the actual gameplay camera (0,10,−14 / pitch 29 / fov 42) with units facing away: rifle identifiable on every unit on the field, at port arms and mid-aim, both sides; behind-view close-up shows the barrel breaking the shoulder line on all rigs.

### 2 — Ring drain: pie → top-down level (health-orb read)
- `CombatRingFill.shader`: the `atan2` pie sweep is gone. Fill is now a flat cutoff along the quad's V axis: `level01 = saturate((d.y + _DiscRadius) / (2·_DiscRadius)); filled = level01 <= _Fill` — the disc empties from the far edge (screen top) downward, bottom `f` of the disc stays filled.
- **No quad reorientation needed**: `BuildSideRing` already lays the quad flat with local +V toward world +Z (`Euler(90,0,0)` under the actor root, which never rotates — only `ModelRoot3D` yaws), and the gameplay camera looks from −Z, so high-V = far edge = screen top by construction. Comments in both files pin this invariant.
- Kept: circular clip, near-black drained sector, always-on side-color rim, 1.4 fill/s eased drain, drain-to-zero during the death fall. Only the shape changed.
- Verified twice: controlled fills (0.65/0.4/0.15 injected on live survivors — flat horizontal cutoffs, correct fractions, bottom-anchored) and a real mid-fight pause (blue units showing dark-top/blue-bottom discs from the gameplay camera).

### 3 — Die-clip rifle flail damped
- `CombatUnitVisual3D.LateUpdate` now low-passes the rifle's **world rotation** while dying: after the 0.35 s hand-release fade (damping ramps in with `1−deathFade`), the rifle slerps from the previous frame's world rotation toward the animated pose with time constant `rifleDeathRotationSmoothSeconds = 0.12`. Fast hand-bone whips in the fall frames can't sling the rigidly-parented prop; the low-pass converges once the body settles, so the rifle still ends resting with the corpse and dissolves with it.
- Measured with an injected per-frame watcher across three deaths: **max per-frame rifle world-rotation delta 2.2° / 2.3° / 3.9°** (at 0.45× time; ≈5–8°/frame full-speed) — no frame above the 25° "whip" alarm. Corpse screenshots show rifles lying flat beside the bodies.

### 4 — High-carry transient pops rate-clamped
- Diagnosis: the port-arms additives are `FromToRotation`s computed fresh each frame against the animated pose — a walk-swing apex mid-turn (and the FromToRotation near-180° axis ambiguity) could jump the computed additive tens of degrees in a single frame.
- Fix: all three port-arms additive rotations (upper-arm swing, forearm raise, hand barrel-align) are now stored and advanced via `Quaternion.RotateTowards(prev, desired, PortArmsMaxDegreesPerSecond·dt)` with **720°/s** — fast enough to track the 720°/s facing turns and the 0.15 s aim blend-in exactly, slow enough that a single-frame extreme spreads over a few frames. State resets in `Clear()`. The aim layer itself is untouched (approved feel preserved).
- Verified by editor pause + `EditorApplication.Step()` frame-stepping through walking + turning units: five consecutive frames fully continuous, no pops.

### Verification
- assets-refresh clean, zero compile errors; rifle prefab + demo scene rebuilt via menu (idempotent through the script-execute retry storm).
- Full play-through twice: fight completes, DEFEAT banner + army HUD intact, rings drain top-down visibly mid-fight, corpse rings empty, rifles readable on all archetypes from the gameplay camera.
- EditMode **358/358** green.

### Deferred
- Wood still reads bright-tan at close-up range (toon light lifts it); at gameplay distance it sits in the muted-brown family — revisit only if a zoom cam lands.
- Port-arms looks near-vertical in extreme close-ups on some rigs (perspective); reads as a proper diagonal from the gameplay camera — no per-archetype barrel-dir overrides yet (still YAGNI).
- The moving-carry multiplier easing (4/s MoveTowards) was not implicated — left as-is.

## Unit pipeline tooling (2026-07-11)

The proven-by-hand Meshy chain is now a one-command orchestrator: `tools/meshy/generate_unit.py <unit_name>` (stdlib only, wraps `meshy_client.py`) runs image3d → remesh(12k) → rig(1.8 m) → animate(0=Idle, 8=Dead) with polling, downloads `idle/walk/die.glb` into `tools/meshy/units/<unit>/glb12k/` (walk from the rig's free walking anim; the animation_glb.glb download collision is avoided by writing final names directly), copies them to `Assets/_Project/Combat3D/Models/<unit>/`, and prints the remaining manual steps (RosterUnits entry with a piece-asset existence check + closest-id suggestion, menu rebuild, Play verification). Resumable via `tools/meshy/units/<unit>/pipeline_state.json` or `--resume <stage:id>`; `--dry-run` is free and side-effect free. Operator checklist + all gotchas: `docs/skills/meshy-unit-pipeline/SKILL.md` (tracked there because `.gitignore` excludes `.claude/skills/`).
