> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — HP-Triggered Combat Pauses & Army Health Bars Design

**Date:** 2026-06-12  
**Engine:** Unity 6  
**Status:** Approved in brainstorming — pending written-spec review  
**Builds on:** `2026-06-10-deadmanzone-combat-arena-presentation-design.md`, `2026-06-04-deadmanzone-combat-sim-completion-design.md`  
**Supersedes (pacing only):** Fixed segment structure (Opening / MainFight / BriefPush) from `2026-06-04-deadmanzone-combat-sim-completion-design.md`

---

## Summary

Replace the fixed-time combat segments with **one continuous fight** that pauses for player commands when **either army's health drops to a threshold** — first pause at **75%**, second at **30%** (Top Troops-style army health bars drive both the triggers and a new on-screen UI). The `CombatPhase` vocabulary (Deployment / Grind / FinalPush) is replaced by a **checkpoint index** model across commands, events, save state, playback, and UI.

---

## Locked decisions

| Area | Choice |
|------|--------|
| Health bar definition | Sum of current HP across **Combatant-tagged units only** (HQ/buildings excluded) vs. their starting HP |
| Trigger rule | Pause when **either side's** fraction drops to ≤ next unfired threshold; thresholds `[0.75, 0.30]`, data-driven |
| Threshold skip | **Merge** — crossing multiple thresholds before a pause yields one pause, never back-to-back windows |
| Fight structure | One continuous fight at flat 1.0x damage; Opening/MainFight/BriefPush segments and 0.2x/1.2x scaling deleted |
| Anti-stall | Gas ramps after a configurable global tick budget, independent of pauses; unchanged ramp math |
| Pre-fight window | **None** — fight starts immediately with current/default tactic; all interaction at the two HP pauses |
| Architecture | **Approach B** — rework `TickCombatRun` in place and replace `CombatPhase` with a new checkpoint model |
| Old mid-combat saves | **No migrator** — fall back to re-entering combat from fight start (re-resolve from saved boards + seed) |

---

## Section 1 — Core sim model

### ArmyHealthTracker (new, pure C#)

`Assets/_Project/Core/Combat/ArmyHealthTracker.cs`

- Input: a side's `CombatantState` list.
- Output: current HP, starting HP, fraction — counting only units with `GameTagIds.Combatant`.
- Consumed by both the sim trigger check and (via replayed events) the UI bars, so the two can never disagree.

### Checkpoint model (replaces `CombatPhase`)

- `CombatPacingConfig` gains `PauseThresholds = [0.75f, 0.30f]` (ordered, descending).
- After each tick, the sim checks whether either side's army HP fraction is ≤ the next unfired threshold.
- **Merge rule:** all thresholds crossed at that moment are consumed together and fire a single pause.
- Pause identity is a **checkpoint index**: `0` = first pause, `1` = second.

Carriers of the new identity:

| Old | New |
|-----|-----|
| `PhaseCommand.AfterPhase : CombatPhase` | `PhaseCommand.AfterCheckpoint : int` |
| `CombatEvent.Phase : CombatPhase` | `CombatEvent.Segment : int` (0 = start→pause 1, 1 = pause 1→pause 2, 2 = remainder; merged pauses mean fewer segments) |
| `CombatSaveState.CompletedPhase` | `CombatSaveState.CheckpointsFired : int` (+ existing `AwaitingCommand`) |
| `CombatEvent.Tick` (segment-local) | Global tick counter, never reset |

`CombatPhase`, `CombatSegment`, and `SegmentTickBudget` are deleted. The headless `CombatResolver.Resolve` path filters commands by checkpoint index instead of phase; the legacy `PhasedCombatRun` wrapper is migrated or removed if no longer referenced.

### Tick loop

`TickCombatRun.Continue(commands)` keeps its shape (apply commands → run ticks → return `AwaitingCommand` or `Completed`) but internally:

- One loop, flat 1.0x damage scale, single global tick counter.
- Per-tick order unchanged: move both sides → gas (if active) → player attacks → enemy attacks → win check after each step.
- Threshold check after each full tick; fight-over check always wins over a pause check on the same tick.

### Anti-stall gas

- Gas activates when the global tick counter exceeds `GasStartTick` (configurable; initial value ~300 ticks ≈ 30 s of fight, tuned during balance pass).
- Ramp math unchanged from `GasDamageSystem`; gas persists across pauses (a pause neither resets nor delays it).
- `MaxGasTicks` hard cap retained as the absolute fight-length bound.

### Edge cases

- Fight ends before any threshold → zero pauses (decisive stomp).
- Each checkpoint fires at most once; being below an already-fired threshold never re-fires it.
- Empty enemy or player army at fight start → existing win-checker behavior, no pause.

---

## Section 2 — Save/resume, replay, presentation

### Save format (breaking for mid-combat saves only)

- `CombatSaveState`: `CompletedPhase`, `ActiveSegment`, `SegmentTick` → replaced by `CheckpointsFired : int`, `GlobalTick : int`, existing `AwaitingCommand`.
- `CombatEventRecord`: `Phase` → `Segment`, ticks are global.
- Build-phase saves unaffected. Old mid-combat saves: **no migrator** — on load, re-enter combat from fight start (re-resolve from saved boards + seed). Determinism guarantees the same fight given the same commands at the same checkpoints.

### Resume

- `FastForwardToCheckpoint` becomes index-driven: replay `Continue` calls feeding saved commands for checkpoints `0..CheckpointsFired-1`.
- Triggers depend only on sim state, so re-simulation fires the same pauses at the same ticks.

### Playback

- `CombatSegmentPlayback` groups events by `Segment` index; fixed tick budgets removed — playback duration derives from each segment's global tick range at `SecondsPerTick`.
- `CombatDirector` contract unchanged: play one segment, then raise `PausedForCommands` when the sim awaits a command.
- `PausedForCommands` payload changes from `CombatPhase` to **checkpoint index + trigger context** (which side crossed, threshold value) so UI can show e.g. "Enemy forces at 30% — issue orders".

### Army health bars (new UI)

- Two bars overlaid on the combat arena (player / enemy), Top Troops style.
- Driven by **replayed events, not sim state**: new `ArmyHealthBarPresenter` subscribes to the `EventReplayed` bus, tracks per-unit HP from `damage` / `gas_damage` / `destroyed` events, and runs totals through `ArmyHealthTracker` math; bars tween down in sync with the visible fight.
- On save/resume restore, bars snap to log-derived values (same approach as actor positions).
- Visual polish inside the component: threshold notches at 75%/30%, hit-flash on large drops.

### Pause UI

- `TacticPausePanel` functionally unchanged (tactics, abilities, Authority costs, validation).
- Header/context line shows the trigger reason.
- `TacticPauseValidator` swaps its phase parameter for the checkpoint index.

---

## Section 3 — Testing, scope, risks

### Testing

| Layer | Tests |
|-------|-------|
| EditMode | `ArmyHealthTrackerTests` — combatant-only filtering, fraction math, empty-army edge |
| EditMode | Trigger tests — pause at 75% from either side; second at 30%; merged pause on burst; zero pauses on stomp; checkpoint fires once; fight-end beats pause same tick |
| EditMode | Determinism — resolve, then `FastForwardToCheckpoint` with same seed/commands → identical event logs |
| EditMode | Gas anti-stall — mutually unkillable armies still produce a finished fight |
| EditMode (migrated) | `CombatResolverTests`, `CommandProcessorTests`, `CombatSegmentPlaybackTests`, `RunSaveSerializerTests`, regression/fixture suites move to checkpoint indices |
| EditMode (re-validated) | Tutorial balance fixtures — removing 0.2x opening changes outcomes; re-tune as needed |
| PlayMode | Pause panel appears on threshold trigger with trigger context line |
| PlayMode | Health bars descend during replay; match log-derived values after resume |

### Balance note

Deleting the 0.2x Opening means full damage from tick one; existing enemy boards hit harder sooner. An early-run encounter tuning pass is **part of this work**, validated through the tutorial balance fixtures.

### In scope

- `ArmyHealthTracker` + continuous tick loop with checkpoint triggers
- Checkpoint migration: commands, events, save state, playback, director, pause UI
- Two army health bars over the arena
- Gas as time-based anti-stall
- Test migration + balance re-validation

### Out of scope

- More than two thresholds
- Enemy AI reacting at pauses
- Pre-fight tactic selection (declined)
- Bar polish beyond notches/tween
- Any build-phase changes

### Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Balance drift from removing the damage ramp | Tutorial balance fixture re-validation + tuning pass in scope |
| Replay/save subtle bugs from global tick change | Determinism test (resolve vs. fast-forward log equality) |
| Stall before gas in attrition-heavy boards | `GasStartTick` tunable; anti-stall EditMode test |

---

## Next step

After user review of this spec: invoke **writing-plans** to produce `docs/superpowers/plans/2026-06-12-hp-triggered-combat-pauses.md`.
