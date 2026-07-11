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
