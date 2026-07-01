> **Superseded (2026-07-01):** `FightRewardTable` removed. See `2026-07-01-ironmarch-union-content-pass-design.md`.

# Tutorial Balance Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement [2026-06-04 tutorial balance pass spec](../specs/2026-06-04-deadmanzone-tutorial-balance-pass-design.md): restore combat pacing, set tutorial economy (125 start, 100/105/110 rewards for fights 1–3), soften enemy templates 1–3, and validate ≥90% pause #2 reach on reference board — with **no fight-index combat modifiers**.

**Architecture:** Content-first balance pass. Change `CombatPacingConfig`, `FightRewardTable`, `iron_vanguard.asset`, and three enemy template assets. Add `TutorialBalanceFixtures` + `TutorialBalanceTests` using existing `TickCombatRun` headless API. Tune enemy templates further only if balance test fails.

**Tech Stack:** Unity 6, C#, Unity Test Framework (Edit Mode), existing asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-04-deadmanzone-tutorial-balance-pass-design.md`

**Branch:** `feat/combat-units-demo`

---

## File map

| Path | Change |
|------|--------|
| `Assets/_Project/Core/Combat/CombatPacingConfig.cs` | Revert Opening/MainFight ticks to 50/300 |
| `Assets/_Project/Game/FightRewardTable.cs` | Fights 1–3 supplies → 100, 105, 110 |
| `Assets/_Project/Data/Resources/DeadManZone/Factions/iron_vanguard.asset` | `startingSupplies: 125` |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_1.asset` | HQ + 1 conscript (support) |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_2.asset` | Rename Patrol; conscript + medic |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_3.asset` | HQ + 2 conscripts; no grenade/medic |
| `Assets/_Project/Core.Tests/EditMode/TutorialBalanceFixtures.cs` | Reference board + pause #2 helper |
| `Assets/_Project/Core.Tests/EditMode/TutorialBalanceTests.cs` | Economy + pause #2 seed sweep |
| `Assets/_Project/Core.Tests/EditMode/FightRewardTableTests.cs` | Reward assertions fights 1–4 |

---

### Task 1: Revert combat pacing

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatPacingConfig.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/CombatSegmentPlaybackTests.cs` (already references config)

- [ ] **Step 1: Write the failing test**

Add to `Assets/_Project/Core.Tests/EditMode/CombatPacingConfigTests.cs` (new file):

```csharp
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatPacingConfigTests
    {
        [Test]
        public void OpeningAndMainFightTicks_MatchDemoSpec()
        {
            Assert.AreEqual(50, CombatPacingConfig.OpeningTicks);
            Assert.AreEqual(300, CombatPacingConfig.MainFightTicks);
            Assert.AreEqual(50, CombatPacingConfig.BriefPushTicks);
            Assert.AreEqual(10, CombatPacingConfig.TicksPerSecond);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Edit Mode tests filtered to `CombatPacingConfigTests`.  
Expected: FAIL — `OpeningTicks` is 30 (dev temp value).

- [ ] **Step 3: Restore pacing values**

Replace `Assets/_Project/Core/Combat/CombatPacingConfig.cs` body:

```csharp
namespace DeadManZone.Core.Combat
{
    public static class CombatPacingConfig
    {
        public const int TicksPerSecond = 10;
        public const int OpeningTicks = 50;
        public const int MainFightTicks = 300;
        public const int BriefPushTicks = 50;
        public const int MaxGasTicks = 10_000;
        public const int GasRampReferenceTicks = 200;
    }
}
```

Remove the `TEMP (dev)` comment block entirely.

- [ ] **Step 4: Run tests to verify they pass**

Run: `CombatPacingConfigTests`, `CombatSegmentPlaybackTests`  
Expected: PASS (playback tests already use `CombatPacingConfig.MainFightTicks - 1`).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Combat/CombatPacingConfig.cs Assets/_Project/Core.Tests/EditMode/CombatPacingConfigTests.cs
git commit -m "fix: restore demo combat pacing to 50/300/50 ticks"
```

---

### Task 2: Tutorial fight rewards

**Files:**
- Modify: `Assets/_Project/Game/FightRewardTable.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/FightRewardTableTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class FightRewardTableTests
    {
        [Test]
        public void GetReward_Fights1Through3_UseTutorialSupplies()
        {
            Assert.AreEqual(100, FightRewardTable.GetReward(1).Supplies);
            Assert.AreEqual(105, FightRewardTable.GetReward(2).Supplies);
            Assert.AreEqual(110, FightRewardTable.GetReward(3).Supplies);
        }

        [Test]
        public void GetReward_Fight4_KeepsExistingCurve()
        {
            Assert.AreEqual(22, FightRewardTable.GetReward(4).Supplies);
        }

        [Test]
        public void GetReward_Draw_HalvesTutorialSupplies()
        {
            Assert.AreEqual(50, FightRewardTable.GetReward(1, isDraw: true).Supplies);
            Assert.AreEqual(52, FightRewardTable.GetReward(2, isDraw: true).Supplies);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `FightRewardTableTests`  
Expected: FAIL — fight 1 supplies is 15.

- [ ] **Step 3: Update reward table**

In `Assets/_Project/Game/FightRewardTable.cs`, change only the first three entries:

```csharp
private static readonly FightReward[] Rewards =
{
    new FightReward(100, 1, 2),
    new FightReward(105, 1, 2),
    new FightReward(110, 1, 2),
    new FightReward(22, 2, 2),
    new FightReward(25, 2, 3),
    // ... remainder unchanged
};
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Game/FightRewardTable.cs Assets/_Project/Core.Tests/EditMode/FightRewardTableTests.cs
git commit -m "feat: bump tutorial fight rewards for fights 1-3"
```

---

### Task 3: Starting supplies

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Factions/iron_vanguard.asset`
- Test: `Assets/_Project/Core.Tests/EditMode/TutorialBalanceTests.cs` (economy portion — create file in Task 5, or add quick test here)

- [ ] **Step 1: Write the failing test**

Add to `FightRewardTableTests.cs` or new `TutorialEconomyTests.cs`:

```csharp
[Test]
public void IronVanguard_StartingSupplies_Is125()
{
    var database = ContentDatabase.Load();
    var faction = database.GetFaction("iron_vanguard");
    Assert.AreEqual(125, faction.startingSupplies);
}
```

Requires `using DeadManZone.Data;` and existing `ContentDatabase.Load()` pattern from `VerticalSliceRegressionTests`.

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — `startingSupplies` is 400.

- [ ] **Step 3: Update faction asset**

In `Assets/_Project/Data/Resources/DeadManZone/Factions/iron_vanguard.asset`:

```yaml
startingSupplies: 125
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Data/Resources/DeadManZone/Factions/iron_vanguard.asset Assets/_Project/Core.Tests/EditMode/TutorialEconomyTests.cs
git commit -m "feat: set tutorial starting supplies to 125"
```

---

### Task 4: Soften enemy templates (fights 1–3)

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_1.asset`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_2.asset`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_3.asset`

Piece GUIDs (unchanged):
- HQ `hq_command`: `a09195287ae98b44490baf1a5d3e1a68`
- Conscript `conscript_rifleman`: `f1a2b3c4d5e6478899aabbccddeeff01`
- Field Medic `field_medic`: `f3a4b5c6d7e8498899aabbccddeeff03`

- [ ] **Step 1: Update fight_1.asset**

Target YAML `placements` (HQ + 1 conscript in support):

```yaml
placements:
- piece: {fileID: 11400000, guid: a09195287ae98b44490baf1a5d3e1a68, type: 2}
  anchor: {x: 0, y: 4}
  instanceId: enemy_hq
- piece: {fileID: 11400000, guid: f1a2b3c4d5e6478899aabbccddeeff01, type: 2}
  anchor: {x: 4, y: 4}
  instanceId:
```

Remove the second conscript entry at `{x: 6, y: 4}`.

- [ ] **Step 2: Update fight_2.asset**

```yaml
displayName: Patrol
previewTag: Infantry
placements:
- piece: {fileID: 11400000, guid: a09195287ae98b44490baf1a5d3e1a68, type: 2}
  anchor: {x: 0, y: 4}
  instanceId: enemy_hq
- piece: {fileID: 11400000, guid: f1a2b3c4d5e6478899aabbccddeeff01, type: 2}
  anchor: {x: 4, y: 4}
  instanceId:
- piece: {fileID: 11400000, guid: f3a4b5c6d7e8498899aabbccddeeff03, type: 2}
  anchor: {x: 5, y: 4}
  instanceId:
```

Remove grenade thrower entry (`f2a3b4c5d6e7488899aabbccddeeff02`).

- [ ] **Step 3: Update fight_3.asset**

Keep `displayName: Field Support`. Replace placements with HQ + 2 conscripts only:

```yaml
placements:
- piece: {fileID: 11400000, guid: a09195287ae98b44490baf1a5d3e1a68, type: 2}
  anchor: {x: 0, y: 4}
  instanceId: enemy_hq
- piece: {fileID: 11400000, guid: f1a2b3c4d5e6478899aabbccddeeff01, type: 2}
  anchor: {x: 4, y: 4}
  instanceId:
- piece: {fileID: 11400000, guid: f1a2b3c4d5e6478899aabbccddeeff01, type: 2}
  anchor: {x: 5, y: 4}
  instanceId:
```

Remove medic and grenade entries.

- [ ] **Step 4: Verify content test still passes**

Run: `VerticalSliceRegressionTests.Content_HasEnemyTemplatesForAllFights`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_1.asset Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_2.asset Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_3.asset
git commit -m "feat: soften tutorial enemy templates for fights 1-3"
```

---

### Task 5: Pause #2 balance validation tests

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/TutorialBalanceFixtures.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/TutorialBalanceTests.cs`

- [ ] **Step 1: Write fixtures**

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public static class TutorialBalanceFixtures
    {
        public const int SeedSweepCount = 40;
        public const float MinPauseTwoReachRate = 0.90f;

        public static BoardState BuildReferencePlayerBoard(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction);

            var board = new BoardState(faction.CreateBoardLayout());
            var hq = database.GetPiece("hq_command");
            var conscript = database.GetPiece("conscript_rifleman");
            Assert.NotNull(hq);
            Assert.NotNull(conscript);

            Assert.IsTrue(board.TryPlace(hq, new GridCoord(0, 4), "hq_player").Success);
            Assert.IsTrue(board.TryPlace(conscript, new GridCoord(5, 4), "conscript_1").Success);
            Assert.IsTrue(board.TryPlace(conscript, new GridCoord(5, 6), "conscript_2").Success);
            return board;
        }

        public static BoardState BuildEnemyBoard(ContentDatabase database, int fightIndex)
        {
            var faction = database.GetFaction("iron_vanguard");
            var template = database.GetEnemyTemplate(fightIndex);
            Assert.NotNull(template);
            return template.BuildBoard(faction, database.Registry);
        }

        public static bool ReachesPauseTwo(BoardState player, BoardState enemy, int seed)
        {
            var run = TickCombatRun.Start(player, enemy, seed, authority: 0);

            run.Continue(new List<PhaseCommand>());
            if (run.IsFightOver)
                return false;

            var deploymentCommands = new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.DisciplinedFire,
                    SourcePieceId = "player_tactic"
                }
            };

            run.Continue(deploymentCommands);
            return !run.IsFightOver
                   && run.AwaitingCommand
                   && run.LastCompletedPhase == CombatPhase.Grind;
        }

        public static float MeasurePauseTwoReachRate(int fightIndex, ContentDatabase database, int seedBase = 5000)
        {
            var player = BuildReferencePlayerBoard(database);
            var enemy = BuildEnemyBoard(database, fightIndex);
            int pass = 0;

            for (int i = 0; i < SeedSweepCount; i++)
            {
                if (ReachesPauseTwo(player, enemy, seedBase + i))
                    pass++;
            }

            return pass / (float)SeedSweepCount;
        }
    }
}
```

- [ ] **Step 2: Write failing balance tests**

```csharp
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TutorialBalanceTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp() => _database = ContentDatabase.Load();

        [Test]
        public void Fight1_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(1, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 1 pause #2 reach rate was {rate:P0}");
        }

        [Test]
        public void Fight2_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(2, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 2 pause #2 reach rate was {rate:P0}");
        }

        [Test]
        public void Fight3_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(3, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 3 pause #2 reach rate was {rate:P0}");
        }
    }
}
```

- [ ] **Step 3: Run tests**

Run: `TutorialBalanceTests`  
Expected: PASS if enemy templates are soft enough; if FAIL, **do not add damage modifiers** — return to Task 4 and further reduce enemy pressure (e.g. fight 3 → 1 conscript, deeper rear placement at x=3).

- [ ] **Step 4: If a fight fails, tune enemy template and re-run**

Acceptable follow-up tweaks (content only):
- Move conscripts to `{x: 3, y: 4}` (deeper rear)
- Fight 3: drop to 1 conscript if 2 still ends grind early too often

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core.Tests/EditMode/TutorialBalanceFixtures.cs Assets/_Project/Core.Tests/EditMode/TutorialBalanceTests.cs
git commit -m "test: add tutorial pause #2 reach rate validation"
```

---

### Task 6: Full regression pass

**Files:**
- Verify: all EditMode tests under `Assets/_Project/Core.Tests/EditMode/`

- [ ] **Step 1: Run full Edit Mode suite**

Unity batch command (adjust Unity path):

```bash
"<UnityEditorPath>/Unity.exe" -batchmode -nographics -projectPath "<repo>" -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit
```

Expected: all tests PASS.

- [ ] **Step 2: Fix any failures**

Likely spots:
- `RunOrchestratorTests` — if any test hardcodes old supply values (currently uses dynamic `startingSupplies` — should be fine)
- `VerticalSliceRegressionTests` — uses faction asset starting supplies dynamically
- Gauntlet board full loop — should still win fights; if fight 1–3 become too easy, no change required this pass

- [ ] **Step 3: Manual playtest (Run scene)**

- [ ] Shop 1: 125 supplies → buy 2 conscripts (100), 25 left
- [ ] Fight 1: ~5s opening → pause 1 → ~30s grind → pause 2
- [ ] Win fight 1: battle report shows +100 supplies
- [ ] Fight 4 feels tougher than fight 3 (unchanged template)

- [ ] **Step 4: Commit any test fixes**

```bash
git commit -m "test: fix regressions after tutorial balance pass"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Pacing 50/300/50 | Task 1 |
| Starting supplies 125 | Task 3 |
| Rewards 100/105/110 fights 1–3 | Task 2 |
| Fight 4+ rewards unchanged | Task 2 test |
| Enemy templates fights 1–3 | Task 4 |
| No fight-index combat modifiers | Enforced by design — no task adds them |
| ≥90% pause #2 reference board | Task 5 |
| Unit costs unchanged | No task modifies piece assets |

## Self-review notes

- No placeholders or TBD steps.
- If Task 5 fails, remediation is explicitly content-only (Task 4 follow-up).
- `TutorialEconomyTests` can live in same file as `TutorialBalanceTests` to reduce file count — merge if preferred during implementation.
- Do **not** revert dev-only UI (Last Log button) in this pass.

---

## Manual verification checklist

- [ ] `CombatPacingConfig`: 50 / 300 / 50
- [ ] `iron_vanguard.startingSupplies`: 125
- [ ] Fight 1 reward: 100 supplies
- [ ] Fight 2 enemy: no grenade
- [ ] Fight 3 enemy: no grenade, no medic
- [ ] `TutorialBalanceTests`: all three fights ≥90%
- [ ] Player unit damage identical in fight 1 vs fight 5 (no code modifiers added)
