# HP-Triggered Combat Pauses & Army Health Bars Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace fixed-time combat segments with one continuous fight that pauses for player commands when either army's health drops to 75% / 30%, with Top Troops-style army health bars over the arena.

**Architecture:** Approach B from the spec — `CombatPhase` (Deployment/Grind/FinalPush) is deleted and replaced by a checkpoint-index model across the sim, commands, event log, save state, playback, and UI. The sim runs one continuous tick loop at flat 1.0x damage with a single global tick counter; gas becomes a time-based anti-stall. A pure `ArmyHealthTracker` in Core feeds both the sim triggers and the new UI bars.

**Tech Stack:** Unity 6, C#, Unity Test Framework (Edit Mode + Play Mode), existing asmdefs under `Assets/_Project/`.

**Spec:** `docs/superpowers/specs/2026-06-12-hp-triggered-combat-pauses-design.md`

---

## Compilation reality check (read first)

All assemblies (`Core`, `Core.Tests`, `Game`, `Presentation`, `Tests.PlayMode`) compile together in Unity. Deleting `CombatPhase` breaks every consumer at once, so **Tasks 2–5 form one compile unit**: the Unity Test Runner cannot run until Task 5 finishes. Task 1 is independently green. Commit at the end of each task anyway (feature-branch WIP commits are fine); the test gate is at the end of Task 5.

Run tests via Unity: **Window → General → Test Runner → EditMode → Run All** (or filter by fixture name). PlayMode tests: same window, PlayMode tab.

## Vocabulary used by every task (memorize)

| Concept | Definition |
|---------|-----------|
| **Checkpoint index** | `0` = first pause (75%), `1` = second pause (30%). `-1` = "not at a pause" sentinel. |
| **CheckpointsFired** | Count of checkpoints that have fired so far (0, 1, or 2). When awaiting at a pause, the pause's index is `CheckpointsFired - 1`. |
| **Segment** | Playback chunk index on events. Events log with `Segment = CheckpointsFired` at log time: segment 0 = start → pause 0; segment 1 = pause 0 → pause 1; segment 2 = remainder. A merged pause (0→2 in one burst) means segment 1 has no events. |
| **Global tick** | Single tick counter for the whole fight, never reset. Replaces per-segment `SegmentTick`. |
| **Pause commands** | `PhaseCommand.AfterCheckpoint == pauseIndex`. Command events log with `Segment = pauseIndex + 1`, `Tick = GlobalTick` (the `-1` tick convention is removed). |

## File map

| File | Action |
|------|--------|
| `Assets/_Project/Core/Combat/ArmyHealthTracker.cs` | Create — army HP sums + fractions (sim side) |
| `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs` | Create — per-unit HP tracking from replayed events (UI side) |
| `Assets/_Project/Core/Combat/CombatEvent.cs` | Rewrite — `Segment` int replaces `Phase`; add `PauseTriggerContext`; delete `CombatPhase` enum |
| `Assets/_Project/Core/Combat/CombatAdvanceResult.cs` | Create — moved out of `PhasedCombatRun.cs`, reshaped |
| `Assets/_Project/Core/Combat/PhasedCombatRun.cs` | Delete (+ `.meta`) — legacy, only self-referenced |
| `Assets/_Project/Core/Combat/CombatPacingConfig.cs` | Rewrite — thresholds + `GasStartTick`; segment budgets deleted |
| `Assets/_Project/Core/Combat/CombatSegment.cs` | Delete (+ `.meta`) |
| `Assets/_Project/Core/Combat/PhaseCommand.cs` | Modify — `AfterCheckpoint : int` |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Rewrite — continuous loop, checkpoint triggers |
| `Assets/_Project/Core/Combat/CombatResolver.cs` | Rewrite — loop until completed |
| `Assets/_Project/Core/Combat/CombatSegmentPlayback.cs` | Rewrite — segment-index grouping, no budgets |
| `Assets/_Project/Core/Combat/TacticPauseValidator.cs` | Modify — checkpoint index params |
| `Assets/_Project/Core/Combat/CombatAbilityExecutor.cs` | Modify — checkpoint index + (segment, tick) logging |
| `Assets/_Project/Core/Combat/CommandProcessor.cs` | Modify — checkpoint index + (segment, tick) logging |
| `Assets/_Project/Core/Combat/CombatMovement.cs` | Modify — drop unused `CombatSegment` params |
| `Assets/_Project/Core/Combat/GasDamageSystem.cs` | Modify — param rename only |
| `Assets/_Project/Core/Combat/CombatLogFormatter.cs` | Modify — segment label |
| `Assets/_Project/Core/Run/RunState.cs` | Modify — save fields, schema v5 |
| `Assets/_Project/Game/RunOrchestrator.cs` | Modify — checkpoint flow + old-save fallback |
| `Assets/_Project/Game/RunSaveBootstrap.cs` | Modify — field mapping |
| `Assets/_Project/Presentation/Combat/CombatDirector.cs` | Modify — segment playback, trigger payload |
| `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs` | Modify — int segments, health-bar wiring |
| `Assets/_Project/Presentation/Combat/TacticPausePanel.cs` | Modify — trigger-based title |
| `Assets/_Project/Presentation/Combat/PhaseCommandPanel.cs` | Modify — pause-index titles |
| `Assets/_Project/Presentation/Combat/CombatBoardPresenter.cs` | Modify — int segments |
| `Assets/_Project/Presentation/Combat/CombatReplayVisuals.cs` | Modify — int segments |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs` | Modify — int segments |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaFreezeController.cs` | Modify — payload type |
| `Assets/_Project/Presentation/Combat/Arena/ArmyHealthBarView.cs` | Create — tweened fill bar |
| `Assets/_Project/Presentation/Combat/Arena/ArmyHealthBarPresenter.cs` | Create — wires tracker to two views |
| `Assets/_Project/Core.Tests/EditMode/ArmyHealthTrackerTests.cs` | Create |
| `Assets/_Project/Core.Tests/EditMode/TickCombatRunTriggerTests.cs` | Create |
| `Assets/_Project/Core.Tests/EditMode/*.cs` (existing combat/run suites) | Migrate |
| `Assets/_Project/Tests.PlayMode/*.cs` (combat suites) | Migrate |
| `Assets/_Project/Tests.PlayMode/ArmyHealthBarPlayModeTests.cs` | Create |

---

### Task 1: ArmyHealthTracker + ArmyHealthReplayTracker (independently green)

**Files:**
- Create: `Assets/_Project/Core/Combat/ArmyHealthTracker.cs`
- Create: `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/ArmyHealthTrackerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Project/Core.Tests/EditMode/ArmyHealthTrackerTests.cs`:

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ArmyHealthTrackerTests
    {
        private static CombatantState MakeCombatant(string id, int maxHp, int currentHp, bool combatant = true)
        {
            var tags = new List<string>();
            if (combatant)
                tags.Add(GameTagIds.Combatant);

            return new CombatantState
            {
                InstanceId = id,
                Definition = new PieceDefinition { Id = id, MaxHp = maxHp, Tags = tags },
                CurrentHp = currentHp
            };
        }

        [Test]
        public void Evaluate_SumsCombatantHpOnly()
        {
            var army = new List<CombatantState>
            {
                MakeCombatant("a", 100, 60),
                MakeCombatant("b", 50, 50),
                MakeCombatant("hq", 200, 200, combatant: false)
            };

            var health = ArmyHealthTracker.Evaluate(army);

            Assert.AreEqual(110, health.CurrentHp);
            Assert.AreEqual(150, health.StartingHp);
            Assert.AreEqual(110f / 150f, health.Fraction, 0.0001f);
        }

        [Test]
        public void Evaluate_ClampsNegativeHpToZero()
        {
            var army = new List<CombatantState> { MakeCombatant("a", 100, -25) };

            var health = ArmyHealthTracker.Evaluate(army);

            Assert.AreEqual(0, health.CurrentHp);
            Assert.AreEqual(0f, health.Fraction);
        }

        [Test]
        public void Evaluate_EmptyArmyHasZeroFraction()
        {
            var health = ArmyHealthTracker.Evaluate(new List<CombatantState>());
            Assert.AreEqual(0f, health.Fraction);
        }

        [Test]
        public void ReplayTracker_TracksDamageAndDestroyedEvents()
        {
            var tracker = new ArmyHealthReplayTracker();
            tracker.RegisterUnit("p1", CombatSide.Player, maxHp: 100);
            tracker.RegisterUnit("p2", CombatSide.Player, maxHp: 100);
            tracker.RegisterUnit("e1", CombatSide.Enemy, maxHp: 80);

            tracker.ApplyEvent(new CombatEvent { ActionType = "damage", TargetId = "p1", Value = 40 });
            Assert.AreEqual(160f / 200f, tracker.GetFraction(CombatSide.Player), 0.0001f);

            tracker.ApplyEvent(new CombatEvent { ActionType = "destroyed", ActorId = "p2" });
            Assert.AreEqual(60f / 200f, tracker.GetFraction(CombatSide.Player), 0.0001f);

            Assert.AreEqual(1f, tracker.GetFraction(CombatSide.Enemy), 0.0001f);
        }

        [Test]
        public void ReplayTracker_IgnoresUnknownTargetsAndNonDamageEvents()
        {
            var tracker = new ArmyHealthReplayTracker();
            tracker.RegisterUnit("p1", CombatSide.Player, maxHp: 100);

            tracker.ApplyEvent(new CombatEvent { ActionType = "move", ActorId = "p1", TargetId = "3,2" });
            tracker.ApplyEvent(new CombatEvent { ActionType = "damage", TargetId = "ghost", Value = 10 });

            Assert.AreEqual(1f, tracker.GetFraction(CombatSide.Player), 0.0001f);
        }
    }
}
```

Note: `MakeCombatant` mirrors the construction pattern used by existing Core tests (`CombatantState` with inline `PieceDefinition`). If `PieceDefinition.Tags` is a different collection type in this codebase, match it (check `Assets/_Project/Core/Board/PieceDefinition.cs`).

- [ ] **Step 2: Run tests to verify they fail**

Test Runner → EditMode → filter `ArmyHealthTracker` → Run.
Expected: compile errors — `ArmyHealthTracker` and `ArmyHealthReplayTracker` do not exist.

- [ ] **Step 3: Implement `ArmyHealthTracker`**

Create `Assets/_Project/Core/Combat/ArmyHealthTracker.cs`:

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Army-wide HP totals for Combatant-tagged units (HQ/buildings excluded).</summary>
    public readonly struct ArmyHealth
    {
        public int CurrentHp { get; init; }
        public int StartingHp { get; init; }
        public float Fraction => StartingHp <= 0 ? 0f : (float)CurrentHp / StartingHp;
    }

    public static class ArmyHealthTracker
    {
        public static ArmyHealth Evaluate(IEnumerable<CombatantState> combatants)
        {
            int current = 0;
            int starting = 0;
            if (combatants != null)
            {
                foreach (var combatant in combatants)
                {
                    if (!combatant.HasTag(GameTagIds.Combatant))
                        continue;

                    starting += combatant.Definition.MaxHp;
                    current += System.Math.Max(0, combatant.CurrentHp);
                }
            }

            return new ArmyHealth { CurrentHp = current, StartingHp = starting };
        }
    }
}
```

- [ ] **Step 4: Implement `ArmyHealthReplayTracker`**

Create `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs`:

```csharp
using System.Collections.Generic;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Rebuilds army HP fractions from replayed combat events so UI bars stay in
    /// sync with what the player is watching (the sim itself finishes instantly).
    /// </summary>
    public sealed class ArmyHealthReplayTracker
    {
        private sealed class UnitHealth
        {
            public CombatSide Side;
            public int MaxHp;
            public int CurrentHp;
        }

        private readonly Dictionary<string, UnitHealth> _units = new();

        public void Clear() => _units.Clear();

        public void RegisterUnit(string instanceId, CombatSide side, int maxHp)
        {
            _units[instanceId] = new UnitHealth { Side = side, MaxHp = maxHp, CurrentHp = maxHp };
        }

        public void ApplyEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return;

            switch (combatEvent.ActionType)
            {
                // All HP-reducing actions target TargetId with Value damage.
                case "damage":
                case "gas_damage":
                case "grenade_lob":
                case "cannon_blast":
                case "cannon_blast_splash":
                case "call_strike":
                    if (_units.TryGetValue(combatEvent.TargetId ?? string.Empty, out var hit))
                        hit.CurrentHp = System.Math.Max(0, hit.CurrentHp - combatEvent.Value);
                    break;

                // "destroyed" events carry the victim in ActorId.
                case "destroyed":
                    if (_units.TryGetValue(combatEvent.ActorId ?? string.Empty, out var dead))
                        dead.CurrentHp = 0;
                    break;
            }
        }

        public float GetFraction(CombatSide side)
        {
            int current = 0;
            int max = 0;
            foreach (var unit in _units.Values)
            {
                if (unit.Side != side)
                    continue;

                current += unit.CurrentHp;
                max += unit.MaxHp;
            }

            return max <= 0 ? 0f : (float)current / max;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Test Runner → EditMode → filter `ArmyHealthTracker` → Run. Expected: all 5 PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Core/Combat/ArmyHealthTracker.cs Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs Assets/_Project/Core.Tests/EditMode/ArmyHealthTrackerTests.cs
git commit -m "feat: add army health trackers for sim triggers and replay UI"
```

(Unity generates `.meta` files for new scripts — stage them too. This applies to every task below.)

---

### Task 2: Core combat types & sim rewrite (compile unit begins — no test run until Task 5)

**Files:**
- Rewrite: `Assets/_Project/Core/Combat/CombatEvent.cs`
- Create: `Assets/_Project/Core/Combat/CombatAdvanceResult.cs`
- Delete: `Assets/_Project/Core/Combat/PhasedCombatRun.cs` + `.meta`
- Rewrite: `Assets/_Project/Core/Combat/CombatPacingConfig.cs`
- Delete: `Assets/_Project/Core/Combat/CombatSegment.cs` + `.meta`
- Modify: `PhaseCommand.cs`, `TacticPauseValidator.cs`, `CombatAbilityExecutor.cs`, `CommandProcessor.cs`, `CombatMovement.cs`, `GasDamageSystem.cs`, `CombatLogFormatter.cs`
- Rewrite: `TickCombatRun.cs`, `CombatResolver.cs`, `CombatSegmentPlayback.cs`

- [ ] **Step 1: Rewrite `CombatEvent.cs`**

Replace the entire file (deletes the `CombatPhase` enum, adds `PauseTriggerContext`):

```csharp
namespace DeadManZone.Core.Combat
{
    /// <summary>Why a combat pause fired: which checkpoint, which side crossed, at what threshold.</summary>
    public sealed class PauseTriggerContext
    {
        public int CheckpointIndex { get; init; }
        public CombatSide TriggeredBy { get; init; }
        public float Threshold { get; init; }
    }

    public sealed class CombatEvent
    {
        /// <summary>Playback segment: 0 = start→pause 1, 1 = pause 1→pause 2, 2 = remainder.</summary>
        public int Segment { get; init; }
        /// <summary>Global fight tick — never resets across segments.</summary>
        public int Tick { get; init; }
        public string ActorId { get; init; }
        public string ActionType { get; init; }
        public string TargetId { get; init; }
        public int Value { get; init; }
    }

    public sealed class CombatEventLog
    {
        public System.Collections.Generic.List<CombatEvent> Events { get; } = new();

        public void Append(
            int segment,
            int tick,
            string actorId,
            string actionType,
            string targetId,
            int value) =>
            Events.Add(new CombatEvent
            {
                Segment = segment,
                Tick = tick,
                ActorId = actorId,
                ActionType = actionType,
                TargetId = targetId,
                Value = value
            });
    }

    public sealed class CombatResult
    {
        public CombatEventLog EventLog { get; init; }
        public bool PlayerWon { get; init; }
    }
}
```

- [ ] **Step 2: Create `CombatAdvanceResult.cs`, delete `PhasedCombatRun.cs`**

`CombatAdvanceStatus`/`CombatAdvanceResult` currently live inside `PhasedCombatRun.cs`. Create `Assets/_Project/Core/Combat/CombatAdvanceResult.cs`:

```csharp
using System.Collections.Generic;

namespace DeadManZone.Core.Combat
{
    public enum CombatAdvanceStatus
    {
        AwaitingCommand,
        Completed
    }

    public sealed class CombatAdvanceResult
    {
        public CombatAdvanceStatus Status { get; init; }
        /// <summary>Segment whose events were generated by this Continue call (for playback).</summary>
        public int SegmentIndex { get; init; }
        /// <summary>Set when Status is AwaitingCommand: why the pause fired.</summary>
        public PauseTriggerContext PauseTrigger { get; init; }
        public bool PlayerWon { get; init; }
        public bool IsDraw { get; init; }
        public BattleReport BattleReport { get; init; }
        public CombatEventLog EventLog { get; init; }
        public int PlayerCombatantsTotal { get; init; }
        public int PlayerCombatantsLost { get; init; }
        public bool PlayerHqDamaged { get; init; }
        public IReadOnlyList<string> SurvivingPlayerCombatantIds { get; init; } =
            System.Array.Empty<string>();
        public IReadOnlyList<CombatantState> PlayerCombatantsAtEnd { get; init; } =
            System.Array.Empty<CombatantState>();
    }
}
```

Then delete `Assets/_Project/Core/Combat/PhasedCombatRun.cs` and `PhasedCombatRun.cs.meta` (verified: only self-referenced).

- [ ] **Step 3: Rewrite `CombatPacingConfig.cs` and delete `CombatSegment.cs`**

```csharp
namespace DeadManZone.Core.Combat
{
    public static class CombatPacingConfig
    {
        public const int TicksPerSecond = 10;

        /// <summary>Army HP fractions that fire command pauses, in firing order.</summary>
        public static readonly float[] PauseThresholds = { 0.75f, 0.30f };

        /// <summary>Global tick at which anti-stall gas starts ramping (~30s of fight).</summary>
        public const int GasStartTick = 300;

        /// <summary>Absolute fight-length bound; reaching it forces a draw.</summary>
        public const int MaxFightTicks = 10_000;

        public const int GasRampReferenceTicks = 200;
    }
}
```

Delete `Assets/_Project/Core/Combat/CombatSegment.cs` and its `.meta`.

- [ ] **Step 4: Modify `PhaseCommand.cs`**

Replace the `AfterPhase` property (keep everything else, including the legacy `Stance` alias):

```csharp
public sealed class PhaseCommand
{
    /// <summary>Pause index this command applies at: 0 = first pause, 1 = second.</summary>
    public int AfterCheckpoint { get; set; }
    public CommandType Type { get; set; }
    public TacticType Tactic { get; set; }
    public GrantedAbility Ability { get; set; }
    public int Cost { get; set; }
    public string SourcePieceId { get; set; }
    public GridCoord? TargetCell { get; set; }

    // Legacy alias for older saves/tests.
    public TacticType Stance
    {
        get => Tactic;
        set => Tactic = value;
    }
}
```

- [ ] **Step 5: Modify `TacticPauseValidator.cs`**

Replace every `CombatPhase pauseAfterPhase` parameter with `int checkpointIndex`. The cost rule maps "extra cost at the Grind pause" to "extra cost at the second pause":

```csharp
public static int GetTacticCost(TacticType selected, TacticType previous, int checkpointIndex)
{
    int cost = selected == TacticType.ProtectSupport ? 1 : 0;
    if (checkpointIndex != 1 || selected == previous)
        return cost;

    return cost + 1;
}
```

Update `CanContinue`, `GetTotalPauseCost`, and `ValidatePause` signatures the same way (`CombatPhase pauseAfterPhase` → `int checkpointIndex`); their bodies only pass the value through.

- [ ] **Step 6: Modify `CombatAbilityExecutor.cs`**

Replace phase params. Gating and costs map Deployment→checkpoint 0, Grind→checkpoint 1:

```csharp
public static bool CanUseAtPause(GrantedAbility ability, int checkpointIndex) =>
    ability switch
    {
        GrantedAbility.CannonBlast => checkpointIndex == 1,
        _ => checkpointIndex == 0 || checkpointIndex == 1
    };

public static int GetAuthorityCost(GrantedAbility ability, int checkpointIndex) =>
    ability switch
    {
        GrantedAbility.GrenadeLob when checkpointIndex == 0 => 2,
        GrantedAbility.GrenadeLob => 3,
        GrantedAbility.ShieldAllies => 2,
        GrantedAbility.CannonBlast => 4,
        _ => 0
    };
```

`Execute` and all private helpers (`ExecuteGrenadeLob`, `ExecuteShieldAllies`, `ExecuteCannonBlast`, `ApplyAreaDamage`, `ApplyDamage`): replace the `CombatPhase phase` parameter with `int logSegment, int logTick`, and every `log.Append(phase, tick: -1, ...)` with `log.Append(logSegment, logTick, ...)`. New `Execute` signature:

```csharp
public static CommandResult Execute(
    GrantedAbility ability,
    string sourcePieceId,
    BoardState board,
    IList<CombatantState> playerCombatants,
    IList<CombatantState> enemyCombatants,
    CombatEventLog log,
    int logSegment,
    int logTick,
    GridCoord? targetCell = null)
```

- [ ] **Step 7: Modify `CommandProcessor.cs`**

Replace `CombatPhase completedPhase` with `int checkpointIndex, int globalTick` throughout. Command events belong to the segment that plays *after* the pause:

```csharp
public IReadOnlyList<AvailableCommand> GetAvailableCommands(
    BoardState board,
    int requisition,
    int checkpointIndex)
```
(body: `CombatAbilityExecutor.CanUseAtPause(ability, checkpointIndex)` and `GetAuthorityCost(ability, checkpointIndex)`)

```csharp
public CommandResult TryApplyBatch(
    IReadOnlyList<PhaseCommand> commands,
    BoardState board,
    ref int authority,
    TacticState tactics,
    IList<CombatantState> playerCombatants,
    IList<CombatantState> enemyCombatants,
    CombatEventLog log,
    int checkpointIndex,
    int globalTick)
{
    int logSegment = checkpointIndex + 1;
    // ... unchanged validation flow, with these substitutions:
    // _tacticValidator.CanContinue(..., checkpointIndex, ref authority, out var reason)
    // log.Append(logSegment, globalTick, "tactic", "tactic_set", null, (int)tacticCommand.Tactic);
    // CombatAbilityExecutor.GetAuthorityCost(command.Ability, checkpointIndex)
    // CombatAbilityExecutor.Execute(command.Ability, command.SourcePieceId, board,
    //     playerCombatants, enemyCombatants, log, logSegment, globalTick, command.TargetCell)
    // TryApplyLegacy(command, board, ref authority, tactics, playerCombatants,
    //     enemyCombatants, log, logSegment, globalTick)
}
```

`TryApply` and `TryApplyLegacy` take `int checkpointIndex, int globalTick` (TryApply) / `int logSegment, int logTick` (TryApplyLegacy and `ApplyStrikeDamage`); all `log.Append(completedPhase, tick: -1, ...)` become `log.Append(logSegment, logTick, ...)`.

- [ ] **Step 8: Modify `CombatMovement.cs` and `GasDamageSystem.cs`**

`CombatMovement`: delete the unused `CombatSegment segment` parameter from `GetMoveCost`, `GetStepChargeCost`, and `StepTowardTarget` (verified unused in all three bodies).

`GasDamageSystem.GetDamage`: rename `int segmentTick` → `int ticksSinceGasStart` (formula unchanged — the caller now passes `GlobalTick - GasStartTick`).

- [ ] **Step 9: Rewrite `TickCombatRun.cs`**

Full replacement. Constructor and the unchanged private helpers (`SpawnCombatants`, `ApplyTacticDamageBuffs`, `SetPlayerTactic`, `RebuildOccupied`, `ComputePlayerLossStats`, synergy/critical-mass setup) keep their existing bodies — only the members shown here change. Complete new flow:

```csharp
public sealed class TickCombatRun
{
    // ... existing fields unchanged, plus:
    private bool _awaitingCommand;

    public BoardState PlayerBoard => _playerBoard;
    public int Authority { get; private set; }
    public int Requisition => Authority;
    public int CheckpointsFired { get; private set; }
    public int GlobalTick { get; private set; }
    public PauseTriggerContext LastPauseTrigger { get; private set; }
    public CombatEventLog Log => _log;
    public bool IsFightOver { get; private set; }
    public bool PlayerWon { get; private set; }
    public bool IsDraw { get; private set; }

    public bool AwaitingCommand => !IsFightOver && _awaitingCommand;

    /// <summary>Index of the pause currently awaiting commands, or -1.</summary>
    public int CurrentPauseIndex => AwaitingCommand ? CheckpointsFired - 1 : -1;

    public CombatAdvanceResult Continue(IReadOnlyList<PhaseCommand> commands)
    {
        if (IsFightOver)
            return CompleteResult();

        if (_awaitingCommand)
        {
            ApplyCommands(commands, CheckpointsFired - 1);
            _awaitingCommand = false;
            if (TryEndFight(CheckpointsFired))
                return CompleteResult();
        }

        int segment = CheckpointsFired;
        RunUntilPauseOrEnd(segment);
        return IsFightOver ? CompleteResult(segment) : AwaitingResult(segment);
    }

    public void FastForwardToCheckpoint(int checkpointsFired, IReadOnlyList<PhaseCommand> submittedCommands)
    {
        if (checkpointsFired <= 0)
            return;

        Continue(System.Array.Empty<PhaseCommand>());
        while (!IsFightOver && _awaitingCommand && CheckpointsFired < checkpointsFired)
            Continue(FilterCommands(submittedCommands, CheckpointsFired - 1));
    }

    private void RunUntilPauseOrEnd(int segment)
    {
        while (!IsFightOver)
        {
            if (GlobalTick >= CombatPacingConfig.MaxFightTicks)
            {
                EndAsDraw(segment);
                return;
            }

            TryMoveSide(_playerCombatants, _enemyCombatants, segment);
            TryMoveSide(_enemyCombatants, _playerCombatants, segment);
            if (TryEndFight(segment))
                return;

            if (GlobalTick >= CombatPacingConfig.GasStartTick)
            {
                ApplyGas(segment);
                if (TryEndFight(segment))
                    return;
            }

            ResolveAttacks(_playerCombatants, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, segment);
            if (TryEndFight(segment))
                return;

            ResolveAttacks(_enemyCombatants, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, segment);
            if (TryEndFight(segment))
                return;

            GlobalTick++;
            if (TryFireCheckpoint(segment))
                return;
        }
    }

    /// <summary>
    /// Fires the next pause when either army crosses its threshold. Consumes ALL
    /// thresholds crossed at once (merge rule) so pauses never fire back-to-back.
    /// </summary>
    private bool TryFireCheckpoint(int segment)
    {
        var thresholds = CombatPacingConfig.PauseThresholds;
        if (CheckpointsFired >= thresholds.Length)
            return false;

        var player = ArmyHealthTracker.Evaluate(_playerCombatants);
        var enemy = ArmyHealthTracker.Evaluate(_enemyCombatants);
        float lowest = System.Math.Min(player.Fraction, enemy.Fraction);

        int consumed = 0;
        float lastThreshold = 0f;
        while (CheckpointsFired + consumed < thresholds.Length &&
               lowest <= thresholds[CheckpointsFired + consumed])
        {
            lastThreshold = thresholds[CheckpointsFired + consumed];
            consumed++;
        }

        if (consumed == 0)
            return false;

        var triggeredBy = player.Fraction <= enemy.Fraction ? CombatSide.Player : CombatSide.Enemy;
        _log.Append(segment, GlobalTick, "combat", "checkpoint", triggeredBy.ToString(), (int)(lastThreshold * 100));

        CheckpointsFired += consumed;
        _awaitingCommand = true;
        LastPauseTrigger = new PauseTriggerContext
        {
            CheckpointIndex = CheckpointsFired - 1,
            TriggeredBy = triggeredBy,
            Threshold = lastThreshold
        };
        return true;
    }

    private void EndAsDraw(int segment)
    {
        IsFightOver = true;
        IsDraw = true;
        PlayerWon = false;
        _log.Append(segment, GlobalTick, "combat", "fight_end", "draw", 0);
    }

    private void ApplyCommands(IReadOnlyList<PhaseCommand> commands, int checkpointIndex)
    {
        var pauseCommands = FilterCommands(commands, checkpointIndex);
        if (pauseCommands.Count == 0)
            return;

        int authority = Authority;
        _commandProcessor.TryApplyBatch(
            pauseCommands,
            _playerBoard,
            ref authority,
            _tactics,
            _playerCombatants,
            _enemyCombatants,
            _log,
            checkpointIndex,
            GlobalTick);
        Authority = authority;

        foreach (var combatant in _playerCombatants)
            combatant.ArmorBuffSteps = 0;
    }

    private static IReadOnlyList<PhaseCommand> FilterCommands(
        IReadOnlyList<PhaseCommand> commands,
        int checkpointIndex) =>
        commands?.Where(c => c.AfterCheckpoint == checkpointIndex).ToList()
        ?? (IReadOnlyList<PhaseCommand>)new List<PhaseCommand>();
}
```

Adapt the surviving helpers to the new signatures:
- `TryMoveSide(movers, targets, int segment)` — same body; the two `CombatMovement` calls lose their segment arg; the move log line becomes `_log.Append(segment, GlobalTick, mover.InstanceId, "move", $"{next.Value.X},{next.Value.Y}", 0);`
- `ApplyGas(int segment)` — `int damage = GasDamageSystem.GetDamage(combatant.Position, GlobalTick - CombatPacingConfig.GasStartTick, _layout);` and `_log.Append(segment, GlobalTick, "gas", combatant.InstanceId, "gas_damage", damage);` death log via `LogDestroyed(segment, ...)`
- `ResolveAttacks(attackers, defenders, tactic, damageBuff, int segment)` — drop the `damageScale` parameter; `CombatDamageResolver.ComputeDamage(actor.Definition, target.Definition, 1f, target.ArmorBuffSteps, actor.DamageBonus + damageBuff)`; log via `_log.Append(segment, GlobalTick, actor.InstanceId, "damage", target.InstanceId, damage);`
- `TryEndFight(int segment = -1)` — same win-checker body; when `segment >= 0` log `_log.Append(segment, GlobalTick, "combat", "fight_end", outcome, 0);` (the parameterless call from `Continue` after applying commands passes the upcoming `CheckpointsFired` as segment — use `TryEndFight(CheckpointsFired)` there instead of the bare overload to keep all fight_end events tagged).
- `LogDestroyed(int segment, string victimId, string sourceId)` — `_log.Append(segment, GlobalTick, victimId, "destroyed", sourceId, 0);`
- `AwaitingResult(int segment)` / `CompleteResult(int segment = ...)`:

```csharp
private CombatAdvanceResult AwaitingResult(int segment) =>
    new CombatAdvanceResult
    {
        Status = CombatAdvanceStatus.AwaitingCommand,
        SegmentIndex = segment,
        PauseTrigger = LastPauseTrigger,
        EventLog = _log
    };
```

`CompleteResult(int segment)` keeps its existing body but sets `SegmentIndex = segment` and drops `CompletedPhase`. Add a parameterless overload `CompleteResult() => CompleteResult(CheckpointsFired);` for the early-out paths.

Delete the old `RunSegment`, `RunGasUntilEnd`, `CompleteFight`, `LastCompletedPhase`, `ActiveSegment`, `SegmentTick` members.

- [ ] **Step 10: Rewrite `CombatResolver.cs`**

```csharp
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class CombatResolver
    {
        public CombatResult Resolve(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            IReadOnlyList<PhaseCommand> commands,
            int requisition = 0)
        {
            var run = TickCombatRun.Start(playerBoard, enemyBoard, seed, requisition);
            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(FilterCommands(commands, run.CurrentPauseIndex));

            return new CombatResult
            {
                EventLog = run.Log,
                PlayerWon = run.PlayerWon
            };
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            int checkpointIndex) =>
            commands?.Where(c => c.AfterCheckpoint == checkpointIndex).ToList()
            ?? (IReadOnlyList<PhaseCommand>)System.Array.Empty<PhaseCommand>();
    }
}
```

- [ ] **Step 11: Rewrite `CombatSegmentPlayback.cs`**

Tick budgets are gone; playback ranges derive from the events themselves:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>Tick-paced segment replay helpers (segment = playback chunk between pauses).</summary>
    public static class CombatSegmentPlayback
    {
        public static float SecondsPerTick => 1f / CombatPacingConfig.TicksPerSecond;

        /// <summary>First global tick of a segment's events, or -1 when the segment is empty.</summary>
        public static int ResolveFirstTick(int segment, IEnumerable<CombatEvent> events) =>
            events?
                .Where(e => e.Segment == segment)
                .Select(e => e.Tick)
                .DefaultIfEmpty(-1)
                .Min() ?? -1;

        /// <summary>Last global tick of a segment's events, or -1 when the segment is empty.</summary>
        public static int ResolveLastTick(int segment, IEnumerable<CombatEvent> events) =>
            events?
                .Where(e => e.Segment == segment)
                .Select(e => e.Tick)
                .DefaultIfEmpty(-1)
                .Max() ?? -1;

        public static bool SegmentContainsFightEnd(IEnumerable<CombatEvent> events, int segment) =>
            events?.Any(e => e.Segment == segment && e.ActionType == "fight_end") == true;

        public static Dictionary<int, List<CombatEvent>> GroupEventsByTick(
            int segment,
            IEnumerable<CombatEvent> events)
        {
            var grouped = new Dictionary<int, List<CombatEvent>>();
            if (events == null)
                return grouped;

            foreach (var combatEvent in events.Where(e => e.Segment == segment).OrderBy(e => e.Tick))
            {
                if (!grouped.TryGetValue(combatEvent.Tick, out var list))
                {
                    list = new List<CombatEvent>();
                    grouped[combatEvent.Tick] = list;
                }

                list.Add(combatEvent);
            }

            return grouped;
        }
    }
}
```

- [ ] **Step 12: Modify `CombatLogFormatter.cs`**

In `Format`, replace `string phase = combatEvent.Phase.ToString();` with `string phase = $"S{combatEvent.Segment}";` (variable name can stay). Add a case for the new checkpoint event above the catch-all:

```csharp
"checkpoint" =>
    $"[{phase} t{tick}] Pause — {Label(combatEvent.TargetId)} forces at {combatEvent.Value}%",
```

- [ ] **Step 13: Commit (still does not compile repo-wide — expected)**

```bash
git add -A Assets/_Project/Core/Combat
git commit -m "wip: replace CombatPhase with checkpoint model in core combat sim"
```

---

### Task 3: Run/save layer migration

**Files:**
- Modify: `Assets/_Project/Core/Run/RunState.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Game/RunSaveBootstrap.cs`

- [ ] **Step 1: Modify `RunState.cs`**

Replace `CombatPauseContext`, `CombatSaveState`, and `CombatEventRecord`; bump the schema:

```csharp
public sealed class CombatPauseContext
{
    public int CheckpointIndex { get; init; }
    public PauseTriggerContext Trigger { get; init; }
    public int Authority { get; init; }
    public TacticType ActiveTactic { get; init; }
    public bool HqAlive { get; init; }
    public bool HasCommandPiece { get; init; }
    public IReadOnlyList<AvailableCommand> AvailableAbilities { get; init; }
    public TacticType? PendingSelectedTactic { get; init; }
    public IReadOnlyList<GrantedAbility> PendingSelectedAbilities { get; init; }
}

public sealed class CombatSaveState
{
    public int CombatSeed { get; set; }
    public BoardSnapshot EnemyBoard { get; set; }
    public int CheckpointsFired { get; set; }
    public int GlobalTick { get; set; }
    /// <summary>Segment index of the most recently simulated events (playback resume).</summary>
    public int LastSegmentIndex { get; set; }
    public bool AwaitingCommand { get; set; }
    public int Requisition { get; set; }
    public int Authority { get; set; }
    public TacticType PlayerTactic { get; set; } = TacticType.DisciplinedFire;
    public TacticType? PendingSelectedTactic { get; set; }
    public List<GrantedAbility> PendingSelectedAbilities { get; set; } = new();
    public List<PhaseCommand> SubmittedCommands { get; set; } = new();
    public List<CombatEventRecord> EventLog { get; set; } = new();
}

public sealed class CombatEventRecord
{
    public int Segment { get; set; }
    public int Tick { get; set; }
    public string ActorId { get; set; }
    public string ActionType { get; set; }
    public string TargetId { get; set; }
    public int Value { get; set; }
}
```

In `RunState`: change `SaveSchemaVersion` default and `CreateNew` from `4` to `5`.

- [ ] **Step 2: Modify `RunOrchestrator.cs`**

Apply these exact substitutions:

`BeginCombat` (end of method):
```csharp
_activeCombat = TickCombatRun.Start(playerBoard, enemyBoard, combatSeed, State.Authority);
State.Combat.AwaitingCommand = false;
State.Combat.CheckpointsFired = 0;
State.Combat.GlobalTick = 0;
State.Combat.LastSegmentIndex = 0;
Persist();
```

`GetAvailableCommands`:
```csharp
return _commandProcessor.GetAvailableCommands(
    GetPlayerBoard(),
    _activeCombat.Requisition,
    _activeCombat.CurrentPauseIndex);
```

`GetCombatPauseContext` — replace `CompletedPhase = _activeCombat.LastCompletedPhase,` with:
```csharp
CheckpointIndex = _activeCombat.CurrentPauseIndex,
Trigger = _activeCombat.LastPauseTrigger,
```

`AdvanceCombat`:
```csharp
public CombatAdvanceResult AdvanceCombat()
{
    if (_activeCombat == null)
        throw new InvalidOperationException("No active combat.");

    int pauseIndex = _activeCombat.CurrentPauseIndex;
    var pending = State.Combat.SubmittedCommands
        .Where(c => c.AfterCheckpoint == pauseIndex)
        .ToList();
    var result = _activeCombat.Continue(pending);
    SyncCombatFromRunner(result);

    if (result.Status == CombatAdvanceStatus.Completed)
        _pendingCombatCompletion = result;

    Persist();
    return result;
}
```
(When the fight hasn't started, `CurrentPauseIndex` is `-1` and no submitted command matches — same behavior as the old `default` phase filter.)

`SyncCombatFromRunner`:
```csharp
private void SyncCombatFromRunner(CombatAdvanceResult step)
{
    State.Combat.Requisition = _activeCombat.Requisition;
    State.Combat.Authority = _activeCombat.Authority;
    State.Combat.PlayerTactic = _activeCombat.PlayerTactic;
    State.Combat.CheckpointsFired = _activeCombat.CheckpointsFired;
    State.Combat.GlobalTick = _activeCombat.GlobalTick;
    State.Combat.LastSegmentIndex = step.SegmentIndex;
    State.Combat.AwaitingCommand = step.Status == CombatAdvanceStatus.AwaitingCommand;
    State.Combat.EventLog = _activeCombat.Log.Events
        .Select(e => new CombatEventRecord
        {
            Segment = e.Segment,
            Tick = e.Tick,
            ActorId = e.ActorId,
            ActionType = e.ActionType,
            TargetId = e.TargetId,
            Value = e.Value
        })
        .ToList();
}
```

`RestoreActiveCombatFromSave` — add the old-save fallback (spec: no migrator; mid-combat saves from schema < 5 restart the fight):
```csharp
private void RestoreActiveCombatFromSave()
{
    _activeCombat = null;
    if (State.Phase != RunPhase.Combat || State.Combat == null)
        return;

    // Pre-checkpoint saves can't resume mid-fight; restart the fight from its start.
    if (State.SaveSchemaVersion < 5)
    {
        State.Combat.SubmittedCommands = new List<PhaseCommand>();
        State.Combat.EventLog = new List<CombatEventRecord>();
        State.Combat.CheckpointsFired = 0;
        State.Combat.GlobalTick = 0;
        State.Combat.LastSegmentIndex = 0;
        State.Combat.AwaitingCommand = false;
    }

    var playerBoard = GetPlayerBoard();
    var enemyBoard = BoardSnapshotMapper.ToBoard(State.Combat.EnemyBoard, _registry);
    _activeCombat = TickCombatRun.Start(
        playerBoard,
        enemyBoard,
        State.Combat.CombatSeed,
        State.Combat.Authority > 0 ? State.Combat.Authority : State.Combat.Requisition);

    _activeCombat.FastForwardToCheckpoint(
        State.Combat.CheckpointsFired,
        State.Combat.SubmittedCommands);

    if (State.Combat.PlayerTactic != default)
        _activeCombat.SetPlayerTactic(State.Combat.PlayerTactic);
}
```

In `TryLoadSavedRun`, after `RestoreActiveCombatFromSave();` add `State.SaveSchemaVersion = 5;` so the fallback only runs once.

- [ ] **Step 3: Modify `RunSaveBootstrap.cs`**

Open the file and apply the field mapping wherever combat save fields are referenced: `CompletedPhase` → `CheckpointsFired` (type `int`), `ActiveSegment`/`SegmentTick` → `GlobalTick`/`LastSegmentIndex`, `CombatEventRecord.Phase` → `.Segment`, `PhaseCommand.AfterPhase` → `.AfterCheckpoint` (int literals: `CombatPhase.Deployment` → `0`, `CombatPhase.Grind` → `1`). Remove any `using` of deleted types.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Core/Run/RunState.cs Assets/_Project/Game/RunOrchestrator.cs Assets/_Project/Game/RunSaveBootstrap.cs
git commit -m "wip: migrate run/save layer to checkpoint model, schema v5 with mid-combat fallback"
```

---

### Task 4: Presentation migration

**Files:** all under `Assets/_Project/Presentation/Combat/` listed in the file map.

- [ ] **Step 1: `CombatDirector.cs`**

Substitutions (keep overall structure):
- Field `CombatPhase _playbackSegmentPhase` → `int _playbackSegment`; add `private PauseTriggerContext _playbackPauseTrigger;`
- Event `public event Action<CombatPhase> PausedForCommands;` → `public event Action<PauseTriggerContext> PausedForCommands;`
- `PresentCombatAfterLoading`:

```csharp
var combat = RunManager.Instance.State.Combat;
if (combat.CheckpointsFired == 0 && !combat.AwaitingCommand)
{
    AdvanceCombatNow();
    return;
}

if (combat.AwaitingCommand)
    PlaySegmentFromSave(combat.LastSegmentIndex, CombatAdvanceStatus.AwaitingCommand);
```
- `PlayLog(CombatEventLog eventLog, int segment)`; `PlaySegmentFromSave(int segment, ...)` maps records with `Segment = e.Segment` instead of `Phase = e.Phase`.
- `OnCombatAdvanced`:

```csharp
private void OnCombatAdvanced(CombatAdvanceResult result)
{
    _playbackPauseTrigger = result.PauseTrigger;
    PlaySegment(result.EventLog?.Events, result.SegmentIndex, result.Status);
}
```
- `PlaySegment(IEnumerable<CombatEvent> events, int segment, CombatAdvanceStatus status)` stores `_playbackSegment = segment;`. On the save-resume path `_playbackPauseTrigger` is null — fetch it inside `FinishPlayback` (next bullet).
- `PlaybackSegmentRoutine(IEnumerable<CombatEvent> events, int segment, CombatAdvanceStatus status)` — the loop must start at the segment's first tick (global ticks no longer start at 0):

```csharp
var eventsByTick = CombatSegmentPlayback.GroupEventsByTick(segment, events);
bool segmentEndsFight = status == CombatAdvanceStatus.Completed ||
                        CombatSegmentPlayback.SegmentContainsFightEnd(events, segment);
int firstTick = CombatSegmentPlayback.ResolveFirstTick(segment, events);
int lastTick = CombatSegmentPlayback.ResolveLastTick(segment, events);
bool fightEnded = false;

for (int tick = System.Math.Max(0, firstTick); tick <= lastTick && !fightEnded; tick++)
{
    // body unchanged
}
```
(`lastTick == -1` for an empty segment means the loop is skipped and playback finishes immediately — correct for merged-pause empty segments.)
- `FinishPlayback` pause branch:

```csharp
if (_playbackAdvanceStatus == CombatAdvanceStatus.AwaitingCommand &&
    ShouldPauseAfterPlayback(_playbackSegment))
{
    var trigger = _playbackPauseTrigger
        ?? RunManager.Instance?.GetCombatPauseContext()?.Trigger;
    PausedForCommands?.Invoke(trigger);
    return;
}
```
(If `RunManager` exposes the orchestrator's pause context under a different member name, use that; the existing `OnPausedForCommands` consumers already pull context from `RunManager`, so follow the same access path.)
- `ShouldPauseAfterPlayback(int segment)`:

```csharp
var combat = RunManager.Instance?.State?.Combat;
return combat is { AwaitingCommand: true } && combat.LastSegmentIndex == segment;
```

- [ ] **Step 2: `CombatFlowPresenter.cs`**

- `OnPausedForCommands(CombatPhase completedPhase)` → `OnPausedForCommands(PauseTriggerContext trigger)`; pass `trigger` (or the pause context from `RunManager`) along to whatever panel-show call follows in the body.
- Arena restore: `var excludePhase = state.Combat.AwaitingCommand ? state.Combat.CompletedPhase : (CombatPhase?)null;` → `var excludeSegment = state.Combat.AwaitingCommand ? state.Combat.LastSegmentIndex : (int?)null;` and pass to `arenaPresenter.RestoreState(battlefield, ..., excludeSegment)`.

- [ ] **Step 3: `CombatReplayVisuals.cs` and `Arena/CombatArenaPresenter.cs`**

In both: parameter `CombatPhase? excludePhase` → `int? excludeSegment`; skip condition `combatEvent.Phase == excludePhase.Value` → `combatEvent.Segment == excludeSegment.Value`. If `OrderEvents` orders by `Phase`, order by `Segment` then `Tick` instead.

- [ ] **Step 4: `Arena/CombatArenaFreezeController.cs`**

`private void OnPausedForCommands(CombatPhase _) => SetFrozen(true);` → `private void OnPausedForCommands(PauseTriggerContext _) => SetFrozen(true);`

- [ ] **Step 5: `TacticPausePanel.cs`**

- Every `_context.CompletedPhase` → `_context.CheckpointIndex` (validator/cost calls now take the int).
- `AfterPhase = _context.CompletedPhase` → `AfterCheckpoint = _context.CheckpointIndex` (both command constructions).
- Title from the trigger:

```csharp
private static string GetPauseTitle(CombatPauseContext context)
{
    if (context?.Trigger == null)
        return "Combat Pause";

    string side = context.Trigger.TriggeredBy == CombatSide.Player ? "Your" : "Enemy";
    int percent = (int)(context.Trigger.Threshold * 100);
    return $"Pause — {side} forces at {percent}%";
}
```
Call site: `titleText.text = GetPauseTitle(_context);` (delete the old `GetPhaseTitle`).

- [ ] **Step 6: `PhaseCommandPanel.cs`**

- Field `CombatPhase _completedPhase` → `int _pauseIndex`; `ShowCommands(..., CombatPhase completedPhase, ...)` → `ShowCommands(..., int pauseIndex, ...)`.
- `AfterPhase = _completedPhase` → `AfterCheckpoint = _pauseIndex`.
- Replace `GetPhaseTitle`/`GetPhaseFlavor`:

```csharp
private static string GetPauseTitle(int pauseIndex) =>
    pauseIndex == 0 ? "First Pause" : "Second Pause";

private static string GetPauseFlavor(int pauseIndex) =>
    pauseIndex == 0
        ? "The lines have buckled — issue orders."
        : "The fight nears its end — commit your reserves.";
```

- [ ] **Step 7: `CombatBoardPresenter.cs`**

- `result.CompletedPhase` → `result.SegmentIndex`; `RestoreReplayStateBeforeSegment(CombatPhase? excludePhase)` → `(int? excludeSegment)`; save path `state.Combat.CompletedPhase` → `state.Combat.LastSegmentIndex`.
- `ShowPhaseBanner(CombatPhase phase)` → `ShowSegmentBanner(int segment)`:

```csharp
phaseBannerText.text = segment == 0 ? "The Battle Begins" : "The Fight Continues";
```

- [ ] **Step 8: Commit**

```bash
git add -A Assets/_Project/Presentation
git commit -m "wip: migrate combat presentation to checkpoint segments and pause triggers"
```

---

### Task 5: Test migration, new trigger tests, compile & test gate

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/TickCombatRunTriggerTests.cs`
- Migrate: `CombatResolverTests.cs`, `CommandProcessorTests.cs`, `CombatSegmentPlaybackTests.cs`, `CombatEventLogTests.cs`, `CombatAbilityExecutorTests.cs`, `CombatMovementSpeedTests.cs`, `RunSaveSerializerTests.cs`, `RunOrchestratorTests.cs`, `VerticalSliceRegressionTests.cs`, `VerticalSliceTestFixtures.cs`, `TutorialBalanceFixtures.cs` (all in `Assets/_Project/Core.Tests/EditMode/`)
- Migrate: `Assets/_Project/Tests.PlayMode/` — `PhaseCommandPanelPlayModeTests.cs`, `TacticPausePanelPlayModeTests.cs`, `CombatDirectorPlayModeTests.cs`

- [ ] **Step 1: Write the new trigger tests**

Create `Assets/_Project/Core.Tests/EditMode/TickCombatRunTriggerTests.cs`. Build boards using the same fixture helpers the existing combat tests use (see `VerticalSliceTestFixtures.cs` for board/piece construction — reuse its helpers rather than inventing new ones):

```csharp
using System.Linq;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TickCombatRunTriggerTests
    {
        // Use the existing fixture helpers to build a player board and an enemy
        // board whose armies are closely matched (long fight, gradual attrition).
        // Replace `MakeMatchedBoards` with the local fixture equivalent.

        [Test]
        public void FirstContinue_PausesWhenEitherSideDropsTo75Percent()
        {
            var (player, enemy) = TestBoards.MakeMatchedBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, result.Status);
            Assert.AreEqual(1, run.CheckpointsFired);
            Assert.AreEqual(0, run.CurrentPauseIndex);
            Assert.NotNull(result.PauseTrigger);
            Assert.AreEqual(0.75f, result.PauseTrigger.Threshold, 0.0001f);

            // The losing side's army fraction must actually be at or below 75%.
            float lowest = System.Math.Min(
                ArmyHealthTracker.Evaluate(run.PlayerCombatantsForTests).Fraction,
                ArmyHealthTracker.Evaluate(run.EnemyCombatantsForTests).Fraction);
            Assert.LessOrEqual(lowest, 0.75f);
        }

        [Test]
        public void SecondContinue_PausesAt30Percent()
        {
            var (player, enemy) = TestBoards.MakeMatchedBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
            {
                Assert.AreEqual(2, run.CheckpointsFired);
                Assert.AreEqual(0.30f, result.PauseTrigger.Threshold, 0.0001f);
            }
            else
            {
                // Legitimate if the fight ended before 30% was crossed.
                Assert.IsTrue(run.IsFightOver);
            }
        }

        [Test]
        public void Stomp_ProducesZeroPausesWhenFightEndsFirst()
        {
            var (player, enemy) = TestBoards.MakeStompBoards(); // overwhelming player army vs one weak enemy
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status);
            Assert.IsTrue(run.PlayerWon);
        }

        [Test]
        public void BurstThroughBothThresholds_FiresSingleMergedPause()
        {
            var (player, enemy) = TestBoards.MakeBurstBoards(); // enemy army of one high-HP unit that dies in few big hits: 100% -> <30% in one tick
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
            {
                Assert.AreEqual(2, run.CheckpointsFired, "merged pause must consume both thresholds");
                Assert.AreEqual(1, run.CurrentPauseIndex);
            }
        }

        [Test]
        public void Determinism_FastForwardReproducesIdenticalEventLog()
        {
            var (player, enemy) = TestBoards.MakeMatchedBoards();
            var commands = new[]
            {
                new PhaseCommand { AfterCheckpoint = 0, Type = CommandType.SetTactic, Tactic = TacticType.Advance, SourcePieceId = "player_tactic" }
            };

            var first = TickCombatRun.Start(player, enemy, seed: 99);
            first.Continue(System.Array.Empty<PhaseCommand>());
            first.Continue(commands);

            var (player2, enemy2) = TestBoards.MakeMatchedBoards();
            var second = TickCombatRun.Start(player2, enemy2, seed: 99);
            second.FastForwardToCheckpoint(first.CheckpointsFired, commands);

            var firstLog = first.Log.Events.Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}").ToList();
            var secondLog = second.Log.Events.Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}").ToList();
            CollectionAssert.AreEqual(firstLog.Take(secondLog.Count), secondLog);
        }

        [Test]
        public void GasAntiStall_FinishesMutuallyUnkillableFight()
        {
            var (player, enemy) = TestBoards.MakeUnkillableBoards(); // both sides all-armor, zero effective damage vs each other
            var run = TickCombatRun.Start(player, enemy, seed: 1);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.IsFightOver);
            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "gas_damage"));
        }
    }
}
```

Implementation notes for this step:
- Add a small `TestBoards` static helper class **in this file** with `MakeMatchedBoards`, `MakeStompBoards`, `MakeBurstBoards`, `MakeUnkillableBoards`, each built from the existing fixture/piece-construction helpers in `VerticalSliceTestFixtures.cs` (board layout + `PieceDefinition` patterns already exist there — copy the construction style, tune HP/damage numbers to produce the named scenarios).
- The first test references `run.PlayerCombatantsForTests` / `run.EnemyCombatantsForTests`. Add these to `TickCombatRun`:

```csharp
public IReadOnlyList<CombatantState> PlayerCombatantsForTests => _playerCombatants;
public IReadOnlyList<CombatantState> EnemyCombatantsForTests => _enemyCombatants;
```

- [ ] **Step 2: Migrate existing EditMode suites**

Apply this substitution table mechanically across the listed test files; rework assertions where semantics changed:

| Old | New |
|-----|-----|
| `CombatPhase.Deployment` (as command tag / pause id) | `0` |
| `CombatPhase.Grind` | `1` |
| `CombatPhase.FinalPush` (as event tag) | segment `2` (or drop the assertion if it tested fixed segment structure) |
| `command.AfterPhase = X` | `command.AfterCheckpoint = <0 or 1>` |
| `result.CompletedPhase` | `result.SegmentIndex` (or assert on `run.CheckpointsFired`) |
| `e.Phase` on events | `e.Segment` |
| `log.Append(phase, tick, ...)` | `log.Append(segment, tick, ...)` |
| `processor.TryApplyBatch(..., CombatPhase.X)` | `processor.TryApplyBatch(..., checkpointIndex, globalTick: 0)` |
| `CombatAbilityExecutor.Execute(..., phase, target)` | `CombatAbilityExecutor.Execute(..., logSegment: checkpointIndex + 1, logTick: 0, target)` |
| `validator.*(..., CombatPhase.X, ...)` | `validator.*(..., checkpointIndex, ...)` |
| `GetTickBudget` / `SegmentTickBudget.*` assertions | delete — budgets no longer exist |
| `CombatSegmentPlayback.ResolveLastTick(phase, events, endsFlag)` | `ResolveLastTick(segment, events)` — assert against event max tick only |
| `PhasedCombatRun` usages | replace with `TickCombatRun` (same `Start`/`Continue` shape) |
| Save-state `CompletedPhase`/`ActiveSegment`/`SegmentTick` | `CheckpointsFired`/`GlobalTick`/`LastSegmentIndex` |
| `SaveSchemaVersion` expectations of `4` | `5` |

Semantic rewrites to watch for:
- Any test asserting "exactly two pauses always happen" must become conditional (pauses are now fight-dependent) or use boards engineered to cross both thresholds.
- `RunSaveSerializerTests`: round-trip the new fields (`CheckpointsFired`, `GlobalTick`, `LastSegmentIndex`, `CombatEventRecord.Segment`); add one test asserting a `SaveSchemaVersion = 4` state with `Phase = Combat` loads and restarts the fight (empty `SubmittedCommands`/`EventLog` after `TryLoadSavedRun` — drive through `RunOrchestrator` if the serializer test has access, otherwise put it in `RunOrchestratorTests`).
- `RunOrchestratorTests`: the advance flow now loops `AdvanceCombat()` while `State.Combat.AwaitingCommand` rather than expecting exactly 3 phases.

- [ ] **Step 3: Migrate PlayMode test files**

Same substitution table. `PhaseCommandPanelPlayModeTests` uses the new `ShowCommands(..., int pauseIndex, ...)`; `TacticPausePanelPlayModeTests` builds `CombatPauseContext` with `CheckpointIndex`/`Trigger`; `CombatDirectorPlayModeTests` scripted logs use `Segment`/global `Tick` and `PlayLog(log, segment)`.

- [ ] **Step 4: Compile gate**

Switch to Unity and let it compile. Fix every remaining `CombatPhase`/`CombatSegment` reference the compiler finds (search the repo for `CombatPhase` — zero hits outside `.worktrees/` when done).

- [ ] **Step 5: Run all EditMode tests**

Test Runner → EditMode → Run All. Expected: all green **except possibly `TutorialBalanceFixtures`-driven balance assertions** (damage-ramp removal changes outcomes — those failures move to Task 7). Anything else red gets fixed now.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: HP-triggered combat pauses replace fixed segments (checkpoint model)"
```

---

### Task 6: Army health bar UI

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/ArmyHealthBarView.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/ArmyHealthBarPresenter.cs`
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs` (wiring)
- Test: `Assets/_Project/Tests.PlayMode/ArmyHealthBarPlayModeTests.cs`
- Scene: combat UI canvas (two bars + threshold notches)

- [ ] **Step 1: Create `ArmyHealthBarView.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>One army health bar: tweened fill with threshold notches placed in-scene.</summary>
    public sealed class ArmyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private float fractionsPerSecond = 1.5f;

        private float _target = 1f;

        public float DisplayedFraction => fillImage != null ? fillImage.fillAmount : _target;

        public void SetFractionImmediate(float fraction)
        {
            _target = Mathf.Clamp01(fraction);
            if (fillImage != null)
                fillImage.fillAmount = _target;
        }

        public void SetFraction(float fraction) => _target = Mathf.Clamp01(fraction);

        private void Update()
        {
            if (fillImage == null || Mathf.Approximately(fillImage.fillAmount, _target))
                return;

            fillImage.fillAmount = Mathf.MoveTowards(
                fillImage.fillAmount, _target, fractionsPerSecond * Time.deltaTime);
        }
    }
}
```

- [ ] **Step 2: Create `ArmyHealthBarPresenter.cs`**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Drives the two army health bars from replayed combat events (not sim state),
    /// so the bars fall in sync with the fight the player is watching.
    /// </summary>
    public sealed class ArmyHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private ArmyHealthBarView playerBar;
        [SerializeField] private ArmyHealthBarView enemyBar;

        private readonly ArmyHealthReplayTracker _tracker = new();

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed += OnEventReplayed;
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed -= OnEventReplayed;
        }

        /// <summary>Register all Combatant-tagged units at full HP and snap bars to 100%.</summary>
        public void InitializeFromBattlefield(BattlefieldState battlefield)
        {
            _tracker.Clear();
            if (battlefield != null)
            {
                foreach (var combatant in battlefield.AllCombatants)
                {
                    if (!combatant.HasTag(GameTagIds.Combatant))
                        continue;

                    _tracker.RegisterUnit(combatant.InstanceId, combatant.Side, combatant.Definition.MaxHp);
                }
            }

            SnapBars();
        }

        /// <summary>Apply a saved event without tweening (save/resume restore path).</summary>
        public void ApplyEventStateOnly(CombatEvent combatEvent)
        {
            _tracker.ApplyEvent(combatEvent);
        }

        public void SnapBars()
        {
            playerBar?.SetFractionImmediate(_tracker.GetFraction(CombatSide.Player));
            enemyBar?.SetFractionImmediate(_tracker.GetFraction(CombatSide.Enemy));
        }

        private void OnEventReplayed(CombatEvent combatEvent)
        {
            _tracker.ApplyEvent(combatEvent);
            playerBar?.SetFraction(_tracker.GetFraction(CombatSide.Player));
            enemyBar?.SetFraction(_tracker.GetFraction(CombatSide.Enemy));
        }
    }
}
```

Note: `BattlefieldState.AllCombatants` — if no such member exists, check `Assets/_Project/Core/Board/BattlefieldState.cs` and use whatever exposes spawned combatant lists; `CombatArenaPresenter.InitializeArena` already iterates them, so mirror its access pattern. If the battlefield exposes pieces rather than `CombatantState`s, register from pieces (id, side, `Definition.MaxHp`) instead.

- [ ] **Step 3: Wire into `CombatFlowPresenter.cs`**

- Add `[SerializeField] private ArmyHealthBarPresenter healthBarPresenter;` and include it in the same `EnsureArenaComponents()` pattern used for the arena presenter.
- Wherever the flow presenter initializes the arena for a fresh fight (the call path that ends in `arenaPresenter.InitializeArena(battlefield)` or equivalent), add `healthBarPresenter?.InitializeFromBattlefield(battlefield);`
- In the save-restore path (where `arenaPresenter.RestoreState(battlefield, events, excludeSegment)` is called), add:

```csharp
healthBarPresenter?.InitializeFromBattlefield(battlefield);
foreach (var savedEvent in ConvertSavedEvents(state.Combat.EventLog))
{
    if (excludeSegment.HasValue && savedEvent.Segment == excludeSegment.Value)
        continue;
    healthBarPresenter.ApplyEventStateOnly(savedEvent);
}
healthBarPresenter?.SnapBars();
```

- [ ] **Step 4: Scene setup**

In the combat UI canvas (same canvas hosting `TacticPausePanel`): add two horizontal bar containers top-center (player left, enemy right), each = background `Image` + child fill `Image` (Image Type: Filled, Horizontal) + two thin notch `Image` markers positioned at 75% and 30% of the bar width. Attach `ArmyHealthBarView` to each, assign fills; add `ArmyHealthBarPresenter` next to `CombatDirector`, assign references.

- [ ] **Step 5: Write the PlayMode test**

Create `Assets/_Project/Tests.PlayMode/ArmyHealthBarPlayModeTests.cs` (follow the setup pattern of `CombatDirectorPlayModeTests` for creating the director):

```csharp
using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ArmyHealthBarPlayModeTests
{
    [UnityTest]
    public IEnumerator BarsDescendDuringReplay()
    {
        var go = new GameObject("director");
        var director = go.AddComponent<CombatDirector>();
        director.SetSecondsPerTickForTests(0f);

        var barGo = new GameObject("bar", typeof(Image));
        var view = barGo.AddComponent<ArmyHealthBarView>();
        // assign fillImage via serialized field setter or reflection per existing PlayMode test conventions

        var presenterGo = new GameObject("presenter");
        var presenter = presenterGo.AddComponent<ArmyHealthBarPresenter>();
        // assign director + playerBar via the same convention

        // Register one player unit directly, then replay a scripted log.
        // (Use an InitializeForTests helper on the presenter mirroring the panels' pattern.)
        var log = new CombatEventLog();
        log.Append(0, 0, "e1", "damage", "p1", 50);

        // presenter test hook: register p1 (Player, 100 HP), then:
        director.PlayLog(log, segment: 0);
        yield return null;
        yield return null;

        Assert.Less(view.DisplayedFraction, 1f);

        Object.Destroy(go); Object.Destroy(barGo); Object.Destroy(presenterGo);
    }
}
```

Add the matching `InitializeForTests(CombatDirector director, ArmyHealthBarView player, ArmyHealthBarView enemy)` and `RegisterUnitForTests(string id, CombatSide side, int maxHp)` methods to `ArmyHealthBarPresenter` (the existing panels already use this `InitializeForTests` convention).

- [ ] **Step 6: Run PlayMode test, then commit**

Test Runner → PlayMode → `ArmyHealthBarPlayModeTests` → Run. Expected: PASS.

```bash
git add -A
git commit -m "feat: army health bars driven by replayed combat events"
```

---

### Task 7: Balance re-validation & tuning

**Files:**
- Modify (as needed): `Assets/_Project/Core.Tests/EditMode/TutorialBalanceFixtures.cs`, enemy template data assets, `CombatPacingConfig.GasStartTick`

- [ ] **Step 1: Run the balance-fixture suites**

Test Runner → EditMode → run `TutorialBalanceFixtures` + `VerticalSliceRegressionTests`. Record every failure.

- [ ] **Step 2: Triage each failure with these criteria**

The contract to preserve: **the tutorial/early fights the player was expected to win are still winnable with the fixture boards, and expected-loss fights still lose.** Removing the 0.2x opening means both sides hit harder sooner — outcomes may flip in either direction.

- Outcome flipped (win→loss or loss→win): tune the enemy template data for that fight (HP/damage/count) until the expected outcome returns. Prefer data changes over fixture-expectation changes.
- Casualty/HP-margin assertions off but outcome correct: update the fixture's expected numbers to the new sim's values **after sanity-checking** the fight log (`CombatLogFormatter.FormatAll`) reads as a plausible battle.
- Fights dragging into gas unexpectedly (check for `gas_damage` events in fights that shouldn't stall): raise or lower `CombatPacingConfig.GasStartTick` — it should be comfortably above the typical fight length of healthy boards (inspect a few logs' final `GlobalTick`).

- [ ] **Step 3: Re-run until green, then commit**

```bash
git add -A
git commit -m "balance: re-tune early fights for flat-damage continuous combat"
```

---

### Task 8: Final verification

- [ ] **Step 1: Full EditMode run** — Test Runner → EditMode → Run All. Expected: all green.
- [ ] **Step 2: Full PlayMode run** — Test Runner → PlayMode → Run All. Expected: all green.
- [ ] **Step 3: Manual smoke in editor** — start a run, enter combat, verify: bars at 100% on fight start; bars descend during replay; pause panel appears with "…forces at 75%" title when a bar crosses the notch; tactic/ability submission resumes the fight; second pause at 30%; battle report at the end. Save mid-pause (quit to menu), reload, verify the arena and bars restore and the same pause panel reappears.
- [ ] **Step 4: Commit any wiring fixes**

```bash
git add -A
git commit -m "fix: combat pause flow polish from manual smoke pass"
```

---

## Self-review checklist (run after writing, before execution)

1. **Spec coverage:** thresholds data-driven (Task 2 Step 3) ✓; merge rule (Task 2 Step 9 `TryFireCheckpoint`) ✓; combatant-only bar (Task 1) ✓; flat damage + gas anti-stall (Task 2 Step 9) ✓; checkpoint migration of commands/events/save/playback/director/UI (Tasks 2–4) ✓; old-save fallback (Task 3 Step 2) ✓; health bars from replayed events (Task 6) ✓; determinism test (Task 5) ✓; balance re-validation (Task 7) ✓; `CombatResolver`/`PhasedCombatRun` handled (Task 2 Steps 2, 10) ✓.
2. **Known approximations (flagged, not placeholders):** `TestBoards` helpers and a few wiring points (`RunSaveBootstrap`, `CombatFlowPresenter` internals, `BattlefieldState` combatant access) reference existing code patterns the executor must read at the call site — exact member names are specified where known and the lookup instruction is explicit where not.
3. **Type consistency:** `AfterCheckpoint : int`, `Segment : int`, `CheckpointsFired : int`, `GlobalTick : int`, `LastSegmentIndex : int`, `CurrentPauseIndex : int`, `PauseTriggerContext { CheckpointIndex, TriggeredBy, Threshold }`, `CombatAdvanceResult { SegmentIndex, PauseTrigger }` — used identically across Tasks 2–6.
