> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

> **Superseded (2026-07-01):** `FightRewardTable` removed. See `2026-07-01-ironmarch-union-content-pass-design.md`.

# DeadManZone Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate the existing vertical slice to the [2026-06-03 rework spec](../specs/2026-06-03-deadmanzone-rework-design.md): four resources, horizontal battlefield with column zones, 10-fight gauntlet, tick combat with movement/gas, `Combatant`/HQ wins, and offensive/defensive/specialty shop lanes.

**Architecture:** Evolve `DeadManZone.Core` in place—rename economy fields, add `BattlefieldState` for combined-grid combat, replace instant `PhasedCombatRun` phase blobs with `TickCombatRun` driven by segment tick budgets. Keep deterministic event log + JSON save. Unity presentation catches up after core tests pass.

**Tech Stack:** Unity 2022.3+ LTS, C# 10+, Unity Test Framework (Edit Mode), Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`), existing asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-03-deadmanzone-rework-design.md`

**Recommended workspace:** Git worktree or feature branch off `master` after commit `0f0bdf3`.

---

## File map (new & heavily modified)

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Core/Common/GameTags.cs` | `Combatant`, `HQ`, synergy tag constants |
| `Assets/_Project/Core/Run/RunEconomy.cs` | Supplies, Manpower, Authority, Morale accessors + round reset |
| `Assets/_Project/Core/Run/ManpowerCalculator.cs` | Upkeep, gate, survivor refund |
| `Assets/_Project/Core/Run/MoraleCalculator.cs` | Loss on defeat (severity × fight index) |
| `Assets/_Project/Core/Run/EmergencyDraft.cs` | Once-per-run manpower relief |
| `Assets/_Project/Core/Board/BoardLayout.cs` | **Modify:** column zones (`rearCols`, `supportCols`) |
| `Assets/_Project/Core/Board/BattlefieldLayout.cs` | Player + neutral + enemy column counts |
| `Assets/_Project/Core/Board/BattlefieldState.cs` | Combined grid occupancy + cell movement |
| `Assets/_Project/Core/Shop/ShopLane.cs` | **Modify:** `Offensive`, `Defensive`, `Specialty` |
| `Assets/_Project/Core/Shop/SpecialtyLaneUnlock.cs` | Faction + board milestone evaluation |
| `Assets/_Project/Core/Combat/CombatSegment.cs` | `Opening`, `MainFight`, `GasFinal` |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Segment tick loop, pauses, win detection |
| `Assets/_Project/Core/Combat/CombatMovement.cs` | Cell movement + neutral movement cost |
| `Assets/_Project/Core/Combat/GasDamageSystem.cs` | Segment 3 ramping gas |
| `Assets/_Project/Core/Combat/CombatWinChecker.cs` | HQ destroyed + no enemy `Combatant` |
| `Assets/_Project/Core/Run/RunState.cs` | Four currencies, `SaveSchemaVersion`, draft flag |
| `Assets/_Project/Core/Run/RunSaveSerializer.cs` | v2 fields + optional v1 migration |
| `Assets/_Project/Game/FightRewardTable.cs` | 10 fights, Supplies rewards |
| `Assets/_Project/Game/RunOrchestrator.cs` | `MaxFights = 10`, manpower gate, Authority |
| `Assets/_Project/Data/ScriptableObjects/FactionSO.cs` | `rearCols`/`supportCols`, starting economy |
| `Assets/_Project/Data/Editor/VerticalSliceContentGenerator.cs` | Regenerate pieces/enemies for rework |

---

## Phase 1 — Economy & run state

### Task 1: GameTags and Combatant on pieces

**Files:**
- Create: `Assets/_Project/Core/Common/GameTags.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs` (no schema change—tags already exist)

- [ ] **Step 1: Write failing test**

Add to `Assets/_Project/Core.Tests/EditMode/GameTagsTests.cs` (new file):

```csharp
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class GameTagsTests
    {
        [Test]
        public void Combatant_Constant_IsStable()
        {
            Assert.AreEqual("Combatant", GameTags.Combatant);
            Assert.AreEqual("HQ", GameTags.Hq);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity Edit Mode test `GameTagsTests` or  
`Unity -runTests -batchmode -projectPath "<repo>" -testResults results.xml -testPlatform editmode -assemblyNames DeadManZone.Core.Tests`  
Expected: FAIL — `GameTags` not found

- [ ] **Step 3: Implement**

```csharp
namespace DeadManZone.Core.Common
{
    public static class GameTags
    {
        public const string Combatant = "Combatant";
        public const string Hq = "HQ";
    }
}
```

- [ ] **Step 4: Tag test pieces**

In `TestPieces.cs`, ensure combat units include `GameTags.Combatant` in `Tags` and HQ piece includes `GameTags.Hq`.

- [ ] **Step 5: Run tests — PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Core/Common/GameTags.cs Assets/_Project/Core.Tests/EditMode/GameTagsTests.cs Assets/_Project/Core.Tests/EditMode/TestPieces.cs
git commit -m "feat(core): add Combatant and HQ tag constants"
```

---

### Task 2: Replace Gold/Requisition with four resources on RunState

**Files:**
- Modify: `Assets/_Project/Core/Run/RunState.cs`
- Modify: `Assets/_Project/Core/Run/RunSaveSerializer.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Game/RunManager.cs` (if it references Gold)
- Modify: `Assets/_Project/Presentation/Run/RunHudView.cs`

- [ ] **Step 1: Write failing save round-trip test**

In `RunSaveSerializerTests.cs` add:

```csharp
[Test]
public void SerializeDeserialize_PreservesFourResources()
{
    var state = new RunState
    {
        Supplies = 120,
        Manpower = 8,
        Authority = 3,
        Morale = 45,
        SaveSchemaVersion = 2
    };
    var json = RunSaveSerializer.Serialize(state);
    var loaded = RunSaveSerializer.Deserialize(json);
    Assert.AreEqual(120, loaded.Supplies);
    Assert.AreEqual(8, loaded.Manpower);
    Assert.AreEqual(3, loaded.Authority);
    Assert.AreEqual(45, loaded.Morale);
}
```

- [ ] **Step 2: Run test — FAIL** (properties missing)

- [ ] **Step 3: Update RunState**

Replace `Gold`/`Requisition` with:

```csharp
public int SaveSchemaVersion { get; set; } = 2;
public int Supplies { get; set; }
public int Manpower { get; set; }
public int Authority { get; set; }
public int Morale { get; set; }
public bool EmergencyDraftUsed { get; set; }
```

Update `CreateNew`:

```csharp
public static RunState CreateNew(
    string factionId,
    int runSeed,
    int startingSupplies,
    int startingManpower,
    int startingAuthority,
    int startingMorale)
```

- [ ] **Step 4: Update serializer**

Serialize all new fields. Add **v1 migration** in `Deserialize`: if `SaveSchemaVersion` absent/1, map `Gold`→`Supplies`, `Requisition`→`Authority`, set `Manpower`/`Morale` from faction defaults.

- [ ] **Step 5: Fix compile errors project-wide**

Replace `State.Gold` → `State.Supplies`, `State.Requisition` → `State.Authority` in `RunOrchestrator`, tests, HUD strings.

- [ ] **Step 6: Run Edit Mode tests — PASS**

- [ ] **Step 7: Commit**

```bash
git commit -am "feat(core): migrate run state to four-resource economy"
```

---

### Task 3: ManpowerCalculator and start-battle gate

**Files:**
- Create: `Assets/_Project/Core/Run/ManpowerCalculator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/ManpowerCalculatorTests.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void ComputeUpkeep_SumsManpowerCostPerCombatantOnBoard()
{
    var board = TestBoards.SingleRifleSquad();
    int upkeep = ManpowerCalculator.ComputeUpkeep(board, TestContentRegistry.Instance);
    Assert.Greater(upkeep, 0);
}

[Test]
public void CanStartBattle_FalseWhenUpkeepExceedsManpower()
{
    var board = TestBoards.FullGauntletBoard();
    Assert.IsFalse(ManpowerCalculator.CanStartBattle(board, manpower: 1, TestContentRegistry.Instance));
}
```

Add `ManpowerCost` to `PieceDefinition` (default 1 for units, 0 for passive buildings):

```csharp
public int ManpowerCost { get; init; }
```

- [ ] **Step 2: Run tests — FAIL**

- [ ] **Step 3: Implement ManpowerCalculator**

```csharp
public static int ComputeUpkeep(BoardState board, ContentRegistry content) { /* sum Combatant tag */ }
public static bool CanStartBattle(BoardState board, int manpower, ContentRegistry content) =>
    manpower >= ComputeUpkeep(board, content);
public static int RefundSurvivors(IReadOnlyList<string> survivingInstanceIds, ContentRegistry content) { /* ... */ }
```

- [ ] **Step 4: Gate `FinalizeBoardAndStartCombat` in RunOrchestrator**

Return `false` + reason when `!CanStartBattle`; expose `TryEmergencyDraft()` delegating to `EmergencyDraft` (Task 4).

- [ ] **Step 5: Run tests — PASS**

- [ ] **Step 6: Commit**

```bash
git commit -am "feat(core): manpower upkeep and start-battle gate"
```

---

### Task 4: Emergency draft + MoraleCalculator

**Files:**
- Create: `Assets/_Project/Core/Run/EmergencyDraft.cs`
- Create: `Assets/_Project/Core/Run/MoraleCalculator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/MoraleCalculatorTests.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs` (apply on loss)

- [ ] **Step 1: Morale test**

```csharp
[Test]
public void LossMorale_HigherOnLaterFightsAndWorseSeverity()
{
    int early = MoraleCalculator.ComputeLoss(fightIndex: 2, combatantsLost: 1, totalCombatants: 5, hqDamage: false);
    int late = MoraleCalculator.ComputeLoss(fightIndex: 9, combatantsLost: 4, totalCombatants: 5, hqDamage: true);
    Assert.Greater(late, early);
}
```

- [ ] **Step 2: Implement**

```csharp
// MoraleCalculator.cs — example coefficients (tune in playtest)
public static int ComputeLoss(int fightIndex, int combatantsLost, int totalCombatants, bool hqDamage)
{
    float severity = totalCombatants == 0 ? 1f : (float)combatantsLost / totalCombatants;
    int baseLoss = (int)(6 * severity) + (hqDamage ? 4 : 0);
    int scale = 1 + fightIndex / 3;
    return baseLoss * scale;
}
```

```csharp
// EmergencyDraft.cs
public static bool TryUse(RunState state, int manpowerShortfall)
{
    if (state.EmergencyDraftUsed) return false;
    state.Manpower += manpowerShortfall;
    state.EmergencyDraftUsed = true;
    return true;
}
```

- [ ] **Step 3: On combat loss in RunOrchestrator**

`State.Morale -= MoraleCalculator.ComputeLoss(...)`; if `Morale <= 0` → `RunPhase.Defeat`.

- [ ] **Step 4: Tests + commit**

```bash
git commit -am "feat(core): morale loss and emergency manpower draft"
```

---

### Task 5: Authority round reset + FightRewardTable ×10

**Files:**
- Modify: `Assets/_Project/Game/FightRewardTable.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`

- [ ] **Step 1: Extend rewards array to 10 entries**

```csharp
public readonly struct FightReward
{
    public int Supplies { get; }
    public int BonusAuthority { get; }
    public int BonusManpower { get; }
}

private static readonly FightReward[] Rewards =
{
    new(15, 1, 2), new(18, 1, 2), new(20, 1, 2), new(22, 2, 2),
    new(25, 2, 3), new(28, 2, 3), new(30, 2, 3), new(32, 3, 3),
    new(35, 3, 4), new(45, 4, 4)
};
```

- [ ] **Step 2: Set `RunOrchestrator.MaxFights = 10`**

Update victory check `State.FightIndex >= MaxFights`.

- [ ] **Step 3: Aftermath**

```csharp
State.Supplies += reward.Supplies;
State.Manpower += reward.BonusManpower;
State.Authority = AuthorityCalculator.ComputeRoundPool(State); // new helper from HQ/buildings
```

Call round reset **before** `GenerateShop`.

- [ ] **Step 4: Update regression tests** expecting 10 fights

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(game): ten-fight gauntlet and supplies-based rewards"
```

---

## Phase 2 — Horizontal board zones

### Task 6: Column-based BoardLayout

**Files:**
- Modify: `Assets/_Project/Core/Board/BoardLayout.cs`
- Modify: `Assets/_Project/Core/Run/BoardSnapshot.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/BoardStateTests.cs`

- [ ] **Step 1: Failing zone test**

```csharp
[Test]
public void CreateHorizontalZones_RearIsLeftmostColumn()
{
    var layout = BoardLayout.CreateHorizontalZones(
        width: 9, height: 6, rearCols: 3, supportCols: 3,
        specialTiles: new[] { new GridCoord(1, 2) });
    Assert.AreEqual(ZoneType.Rear, layout.GetZone(new GridCoord(0, 3)));
    Assert.AreEqual(ZoneType.Front, layout.GetZone(new GridCoord(8, 3)));
}
```

- [ ] **Step 2: Implement `CreateHorizontalZones`**

```csharp
public static BoardLayout CreateHorizontalZones(
    int width, int height, int rearCols, int supportCols, GridCoord[] specialTiles)
{
    var zones = new ZoneType[width, height];
    for (int x = 0; x < width; x++)
    {
        var zone = x < rearCols ? ZoneType.Rear
            : x < rearCols + supportCols ? ZoneType.Support
            : ZoneType.Front;
        for (int y = 0; y < height; y++)
            zones[x, y] = zone;
    }
    return new BoardLayout(width, height, zones, specialTiles.ToList());
}
```

Keep `CreateStandard` temporarily; mark obsolete; migrate callers to horizontal.

- [ ] **Step 3: FactionSO fields**

Replace `rearRows`/`supportRows` with `rearCols`/`supportCols` (default 3/3 on width 9). Update `CreateEmptyBoardSnapshot` to store cols.

- [ ] **Step 4: Fix placement tests + commit**

```bash
git commit -am "feat(core): horizontal column zones for board layout"
```

---

## Phase 3 — Shop lanes & specialty unlock

### Task 7: Rename shop lanes

**Files:**
- Modify: `Assets/_Project/Core/Shop/ShopLane.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Modify: `Assets/_Project/Data/Editor/VerticalSliceContentGenerator.cs`
- Modify: all tests referencing `ShopLane.General`

- [ ] **Step 1: Enum change**

```csharp
public enum ShopLane
{
    Offensive,
    Defensive,
    Specialty
}
```

- [ ] **Step 2: ShopGenerator — 3 base offers per lane**

`OffersPerLane = 3`. Specialty lane returns empty offers when locked.

- [ ] **Step 3: Regenerate content** (Editor menu) mapping old lanes: General→Offensive, Engineers→Defensive, Requisition→Specialty pieces move to Offensive/Defensive by category.

- [ ] **Step 4: Run `ShopGeneratorTests` — fix + commit**

```bash
git commit -am "feat(core): offensive defensive specialty shop lanes"
```

---

### Task 8: SpecialtyLaneUnlock evaluator

**Files:**
- Create: `Assets/_Project/Core/Shop/SpecialtyLaneUnlock.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/SpecialtyLaneUnlockTests.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`

- [ ] **Step 1: Tests**

```csharp
[Test]
public void Unlocked_WhenCommandBunkerOnBoardAtShopOpen()
{
    var board = TestBoards.WithPiece("command_bunker");
    Assert.IsTrue(SpecialtyLaneUnlock.IsUnlocked(board, "iron_vanguard", TestContentRegistry.Instance));
}
```

- [ ] **Step 2: Implement evaluator**

Check faction rules table (hardcode demo rules in static class first) + `PieceDefinition.GrantsSpecialtyLane` flag on buildings.

- [ ] **Step 3: ShopGenerator uses `IsUnlocked` before rolling specialty pool**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): specialty lane unlock from board and faction rules"
```

---

## Phase 4 — Combined battlefield & combat foundation

### Task 9: BattlefieldLayout and BattlefieldState

**Files:**
- Create: `Assets/_Project/Core/Board/BattlefieldLayout.cs`
- Create: `Assets/_Project/Core/Board/BattlefieldState.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/BattlefieldStateTests.cs`

- [ ] **Step 1: Layout constants**

Demo dimensions: player half width `Pw=9`, neutral `Nw=2`, enemy half `Ew=9`, height `H=6` → total width 20.

```csharp
public sealed class BattlefieldLayout
{
    public int PlayerOriginX => 0;
    public int NeutralStartX => PlayerHalfWidth;
    public int EnemyOriginX => PlayerHalfWidth + NeutralWidth;
    public bool IsNeutralColumn(int x) => x >= NeutralStartX && x < NeutralStartX + NeutralWidth;
}
```

- [ ] **Step 2: `BattlefieldState.FromRunBoards(player, enemy)`**

Copies placed pieces into global coordinates; enemy pieces offset by `EnemyOriginX`.

- [ ] **Step 3: Test neutral detection + mirror placement**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): combined battlefield layout and state"
```

---

### Task 10: CombatWinChecker (HQ + Combatant)

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatWinChecker.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatWinCheckerTests.cs`
- Modify: `Assets/_Project/Core/Combat/PhasedCombatRun.cs` (interim wiring before TickCombatRun)

- [ ] **Step 1: Tests**

```csharp
[Test]
public void PlayerWins_WhenEnemyHQDestroyed()
{
    var result = CombatWinChecker.Evaluate(enemyCombatants, enemyHqAlive: false, playerHqAlive: true);
    Assert.IsTrue(result.PlayerWon);
}
```

- [ ] **Step 2: Implement**

```csharp
public static (bool fightOver, bool playerWon) Evaluate(
    IReadOnlyList<CombatantState> player,
    IReadOnlyList<CombatantState> enemy,
    bool playerHqAlive,
    bool enemyHqAlive)
{
    if (!enemyHqAlive) return (true, true);
    if (!playerHqAlive) return (true, false);
    bool enemyCombatants = enemy.Any(c => c.IsAlive && c.HasTag(GameTags.Combatant));
    bool playerCombatants = player.Any(c => c.IsAlive && c.HasTag(GameTags.Combatant));
    if (!enemyCombatants) return (true, true);
    if (!playerCombatants) return (true, false);
    return (false, false);
}
```

Add `HasTag` on `CombatantState`; spawn HQ as non-`Combatant` piece with `GameTags.Hq`.

- [ ] **Step 3: Replace army HP sum win check in `PhasedCombatRun`**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): HQ and Combatant tag win conditions"
```

---

## Phase 5 — Tick combat, movement, gas

### Task 11: CombatSegment + tick budgets

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatSegment.cs`
- Create: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Core/Combat/CombatEvent.cs` (add `Move`, `GasDamage` action types)
- Modify: `Assets/_Project/Core/Combat/PhasedCombatRun.cs` — deprecate; delegate to `TickCombatRun` from `CombatResolver`

- [ ] **Step 1: Segment enum and tick constants**

```csharp
public enum CombatSegment { Opening = 1, MainFight = 2, GasFinal = 3 }

public static class SegmentTickBudget
{
    public const int Opening = 100;    // ~10s at 10 tps presentation
    public const int MainFight = 500;  // ~50s
    public const int GasFinal = 200;   // ~20s
}
```

- [ ] **Step 2: `TickCombatRun.Start(BattlefieldState, seed, authority)`**

Mirror `PhasedCombatRun` API: `Continue(commands)` → runs segment ticks until pause or complete.

- [ ] **Step 3: Determinism test**

```csharp
[Test]
public void SameSeedAndCommands_IdenticalEventLog()
{
    var log1 = RunCombat(seed: 42, commands);
    var log2 = RunCombat(seed: 42, commands);
    Assert.AreEqual(log1.ToJson(), log2.ToJson());
}
```

- [ ] **Step 4: Wire `CombatResolver.Resolve` to `TickCombatRun`**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): tick-based combat segments with pause windows"
```

---

### Task 12: CombatMovement + neutral column cost

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatMovement.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatMovementTests.cs`

- [ ] **Step 1: Movement toward nearest enemy combatant**

Each tick, movable units take one step on 4-neighborhood grid toward target if not in range.

- [ ] **Step 2: Neutral column movement cost = 2 ticks per step**

Units entering neutral columns consume extra tick budget before moving (contested ground).

- [ ] **Step 3: Log `CombatEventType.Move` with from/to coords**

- [ ] **Step 4: Tests for contested slowdown**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): cell movement with contested neutral columns"
```

---

### Task 13: GasDamageSystem (segment 3 ramp)

**Files:**
- Create: `Assets/_Project/Core/Combat/GasDamageSystem.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`

- [ ] **Step 1: Gas only in `CombatSegment.GasFinal`**

```csharp
public int GetDamage(GridCoord pos, int segmentTick, BattlefieldLayout layout)
{
    if (!layout.IsNeutralColumn(pos.X)) return BaseGasFront;
    float ramp = 1f + segmentTick / (float)SegmentTickBudget.GasFinal;
    return (int)(NeutralGasBase * ramp);
}
```

- [ ] **Step 2: Apply to all alive combatants each tick; respect `GasMask` tag → 0**

- [ ] **Step 3: Test ramp monotonic increase in neutral column**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): ramping gas damage in final segment"
```

---

### Task 14: Authority in CommandProcessor

**Files:**
- Modify: `Assets/_Project/Core/Combat/CommandProcessor.cs`
- Modify: `Assets/_Project/Core/Combat/PhaseCommand.cs`
- Modify: `Assets/_Project/Core/Combat/PhasedCombatRun.cs` or `TickCombatRun.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CommandProcessorTests.cs`
- Modify: `Assets/_Project/Core/Run/CombatSaveState.cs` (`Authority` not `Requisition`)

- [ ] **Step 1: Rename combat spend pool `Requisition` → `Authority`**

- [ ] **Step 2: Rename `SpendRequisitionBuff` → `SpendAuthorityBuff` in `CommandActionFlags`**

- [ ] **Step 3: Update tests + RunOrchestrator combat resume**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): authority pool for pause commands"
```

---

### Task 15: Mid-combat save fields for tick combat

**Files:**
- Modify: `Assets/_Project/Core/Run/CombatSaveState.cs`
- Modify: `Assets/_Project/Core/Run/RunSaveSerializer.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs`

- [ ] **Step 1: Add fields**

```csharp
public CombatSegment ActiveSegment { get; set; }
public int SegmentTick { get; set; }
public BattlefieldSnapshot Battlefield { get; set; }
public int Authority { get; set; }
```

- [ ] **Step 2: Save/load mid-pause test**

Deserialize → resume `TickCombatRun` → same log completion as uninterrupted run.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(core): save mid-tick combat state"
```

---

## Phase 6 — Game layer & content

### Task 16: RunOrchestrator manpower aftermath

**Files:**
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs`

- [ ] **Step 1: After win, refund manpower for surviving combatant instance IDs from combat result**

- [ ] **Step 2: Track lost combatants vs survived in `CombatResult`**

- [ ] **Step 3: Test full fight cycle manpower decreases then refunds partial**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(game): manpower refund for combat survivors"
```

---

### Task 17: Rework content generator (1 faction, 10 enemies)

**Files:**
- Modify: `Assets/_Project/Data/Editor/VerticalSliceContentGenerator.cs`
- Create: `Assets/_Project/Data/Content/Rework/` output path for SOs
- Modify: enemy templates count → 10

- [ ] **Step 1: Add `manpowerCost`, tags `Combatant`/`HQ` on each piece**

- [ ] **Step 2: Author `hq_command` required piece in rear zone**

- [ ] **Step 3: Generate `enemy_fight_01` … `enemy_fight_10` templates with escalating boards**

- [ ] **Step 4: Editor menu: DeadManZone → Generate Rework Vertical Slice Content**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(data): rework content for ten-fight demo"
```

---

### Task 18: Vertical slice regression test (10 fights)

**Files:**
- Modify: `Assets/_Project/Core.Tests/EditMode/VerticalSliceRegressionTests.cs`

- [ ] **Step 1: Update fixtures for horizontal layout + 10 fights**

- [ ] **Step 2: Headless orchestrator run with scripted pause commands**

- [ ] **Step 3: Assert determinism hash of final log on fight 10 victory path**

- [ ] **Step 4: Commit**

```bash
git commit -am "test(core): ten-fight rework regression harness"
```

---

## Phase 7 — Presentation (minimal playable)

### Task 19: RunHud four resources + manpower gate UI

**Files:**
- Modify: `Assets/_Project/Presentation/Run/RunHudView.cs`
- Modify: `Assets/_Project/Presentation/Run/RunSceneController.cs` (or equivalent)

- [ ] **Step 1: Display Supplies / Manpower / Authority / Morale**

- [ ] **Step 2: Disable Start Battle when `!CanStartBattle`; show shortfall count**

- [ ] **Step 3: Emergency Draft button (once per run)**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(presentation): rework resource HUD and manpower gate"
```

---

### Task 20: Combat view — extended grid + segment replay

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/CombatDirector.cs` (create if missing)
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`

- [ ] **Step 1: Render 20-column battlefield during combat**

- [ ] **Step 2: Replay `Move` and `GasDamage` events**

- [ ] **Step 3: Show pause panel at segment 1/2 end (wire existing `PhaseCommandPanel`)**

- [ ] **Step 4: Play Mode smoke: start fight → pause → resume**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(presentation): horizontal combat grid and tick replay"
```

---

## Phase 8 — Verification checklist

- [ ] All Edit Mode tests pass (`DeadManZone.Core.Tests`)
- [ ] `VerticalSliceRegressionTests` completes 10-fight headless path
- [ ] Save mid-pause → reload → identical combat outcome (Task 15 test)
- [ ] Manual: shop specialty unlocks with command bunker on board
- [ ] Manual: cannot start fight over manpower; emergency draft works once
- [ ] Manual: gas visible in segment 3 on neutral columns

---

## Spec coverage self-review

| Spec requirement | Task(s) |
|------------------|---------|
| Four resources | 2, 5, 14, 19 |
| Manpower gate + draft + relief | 3, 4, 16, 17, 19 |
| Morale loss severity × index | 4 |
| Horizontal zones rear\|support\|front | 6 |
| Combined grid + movement | 9, 12 |
| Neutral contested + gas seg 3 | 12, 13 |
| Combatant/HQ win | 1, 10 |
| Tick segments + 2 pauses | 11, 14, 20 |
| Shop lanes + specialty unlock | 7, 8 |
| 10 fights | 5, 17, 18 |
| Save mid-combat | 15 |
| Deterministic log | 11, 18 |
| 1 faction demo content | 17 |

**Deferred to post-demo tuning (explicit):** exact DPS, manpower costs per piece, morale coefficients, gas numbers — adjust in ScriptableObjects after Task 17 without schema changes.

---

## Execution handoff

**Plan complete and saved to `docs/superpowers/plans/2026-06-03-deadmanzone-rework.md`. Two execution options:**

1. **Subagent-Driven (recommended)** — Fresh subagent per task, review between tasks, fast iteration  
2. **Inline Execution** — Implement task-by-task in this session with checkpoints  

**Which approach do you want?**
