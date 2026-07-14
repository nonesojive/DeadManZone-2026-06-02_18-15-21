> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Mechanics Sandbox Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement every core mechanic in the DeadManZone sim + UI so internal testers can validate systems in isolation (mechanics sandbox — not demo polish).

**Architecture:** Phased delivery: wire economy/shop stubs first (Phase 0–0b), then combat footprint + A* pathfinding + role engagement (Phases 1–3), presentation + tooling (Phases 4–7), minimal content + integration gate (Phases 8–9). Core sim stays Unity-free under `Assets/_Project/Core/`; presentation mirrors via existing replay/event-log patterns.

**Tech Stack:** Unity 6, C#, Unity Test Framework (EditMode + PlayMode), Newtonsoft JSON save schema, existing asmdefs (`Core`, `Core.Tests`, `Game`, `Presentation`, `Tests.PlayMode`).

**Spec:** `docs/superpowers/specs/2026-06-12-mechanics-sandbox-prototype-design.md`

---

## Execution order & compile gates

| Phase | Depends on | Green gate |
|-------|------------|------------|
| **0** Wire stubs | — | EditMode: EmergencyDraft, SynergyEngine, Muster, SpecialtyLane tests |
| **0b** Salvage | Phase 0 | EditMode: SalvageChance + ShopGenerator salvage tests |
| **1** Footprints | — (parallel OK after 0b) | EditMode: CombatFootprint + occupancy tests |
| **2** Pathfinding | Phase 1 | EditMode: ShapePathfinder tests; determinism regression |
| **3** Role engagement | Phase 2 | EditMode: per-role movement goal fixtures |
| **4** Arena buildings | Phase 1 | PlayMode: building prefab visible in arena |
| **5** Attack/armor | — | EditMode: matrix fixture all combos |
| **6** Tag Creator | Phase 0 | Editor menu creates tag → Unit Creator lists it |
| **7** Tooltips | 0b, 4.1 synergies | EditMode: PieceCardViewModel salvage/synergy fields |
| **8** Content | Phase 6 | ContentDatabase has 10 neutral + 15 IronMarch pieces |
| **9** Integration | All | Full EditMode suite + save/resume determinism |

Run tests: **Window → General → Test Runner → EditMode → Run All** (filter by fixture name when iterating).

Phases 0–0b and 1 can start on separate branches; merge before Phase 3.

---

## Master file map

### Phase 0 — Wire stubs

| File | Action |
|------|--------|
| `Assets/_Project/Game/RunOrchestrator.cs` | Wire `TryEmergencyDraft` |
| `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs` | Populate demo rules |
| `Assets/_Project/Core/Run/MusterCalculator.cs` | Implement supply synergy |
| `Assets/_Project/Core/Shop/SpecialtyLaneRuleCatalog.cs` | Board-composition resolver |
| `Assets/_Project/Core.Tests/EditMode/SynergyEngineTests.cs` | Add non-empty catalog tests |

### Phase 0b — Salvage

| File | Action |
|------|--------|
| `Assets/_Project/Data/ScriptableObjects/FactionSO.cs` | Add `baseSalvageChancePercent` |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | Add `SalvageChanceBoost5` flag + `SalvageChanceBonus` |
| `Assets/_Project/Core/Run/RunState.cs` | `LastEnemyFactionId`, `SalvageChancePercent`; schema **v6** |
| `Assets/_Project/Core/Shop/SalvageChanceCalculator.cs` | Create |
| `Assets/_Project/Core/Shop/SalvageBoardBoostAggregator.cs` | Create |
| `Assets/_Project/Core/Shop/SalvageShopPool.cs` | Create |
| `Assets/_Project/Core/Shop/ShopOffer.cs` | Add `IsSalvaged` |
| `Assets/_Project/Core/Run/ShopOfferRecord.cs` | Persist `IsSalvaged` |
| `Assets/_Project/Core/Shop/ShopGenerator.cs` | Per-slot salvage roll |
| `Assets/_Project/Game/RunOrchestrator.cs` | Aftermath salvage state |
| `Assets/_Project/Game/RunOrchestrator.Shop.cs` | Pass salvage context to generator |
| `Assets/_Project/Presentation/Run/RunHudView.cs` | Salvage indicator |
| `Assets/_Project/Presentation/Shop/ShopOfferView.cs` | Salvaged badge |
| `Assets/_Project/Core.Tests/EditMode/SalvageChanceCalculatorTests.cs` | Create |
| `Assets/_Project/Core.Tests/EditMode/SalvageShopGeneratorTests.cs` | Create |

### Phases 1–3 — Combat movement

| File | Action |
|------|--------|
| `Assets/_Project/Core/Combat/CombatFootprint.cs` | Create — offsets from shape + rotation |
| `Assets/_Project/Core/Combat/CombatOccupancyGrid.cs` | Create |
| `Assets/_Project/Core/Combat/CombatantState.cs` | Anchor, offsets, occupied cells |
| `Assets/_Project/Core/Combat/ShapePathfinder.cs` | Create — A* on anchor |
| `Assets/_Project/Core/Combat/RoleEngagement.cs` | Create — movement goals |
| `Assets/_Project/Core/Tags/CombatRoleProfile.cs` | Extend biases for movement |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Footprint spawn + path steps |
| `Assets/_Project/Core/Combat/CombatMovementRules.cs` | Use engagement goals |
| `Assets/_Project/Core.Tests/EditMode/CombatFootprintTests.cs` | Create |
| `Assets/_Project/Core.Tests/EditMode/ShapePathfinderTests.cs` | Create |
| `Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs` | Create |

### Phases 4–9 — See task sections below

---

## Phase 0 — Wire stubs

### Task 1: Emergency Draft orchestrator

**Files:**
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/EmergencyDraftTests.cs` (exists — add orchestrator integration test in new file)

- [ ] **Step 1: Write failing integration test**

Create `Assets/_Project/Core.Tests/EditMode/RunOrchestratorEmergencyDraftTests.cs`:

```csharp
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class RunOrchestratorEmergencyDraftTests
    {
        [Test]
        public void TryEmergencyDraft_WhenShortfall_AppliesManpowerOnce()
        {
            var orchestrator = RunOrchestratorTestHarness.CreateWithBoard(
                manpower: 5,
                boardManpowerCost: 8);

            Assert.IsTrue(orchestrator.TryEmergencyDraft());
            Assert.AreEqual(8, orchestrator.State.Manpower);
            Assert.IsTrue(orchestrator.State.EmergencyDraftUsed);
            Assert.IsFalse(orchestrator.TryEmergencyDraft());
        }
    }
}
```

Add minimal `RunOrchestratorTestHarness` in same file or `Assets/_Project/Core.Tests/EditMode/TestHarness/RunOrchestratorTestHarness.cs` if not present — construct orchestrator with mocked content + board whose combatant upkeep exceeds manpower by 3.

- [ ] **Step 2: Run test — expect FAIL** (`TryEmergencyDraft` returns false)

- [ ] **Step 3: Implement orchestrator hook**

In `RunOrchestrator.cs`, replace:

```csharp
public bool TryEmergencyDraft() => false;
```

with:

```csharp
public bool TryEmergencyDraft()
{
    int shortfall = ComputeManpowerShortfallForNextFight();
    if (!EmergencyDraft.TryUse(State, shortfall))
        return false;

    Persist();
    return true;
}

private int ComputeManpowerShortfallForNextFight()
{
    var board = GetPlayerBoard();
    int upkeep = ManpowerGateCalculator.ComputeBoardUpkeep(board);
    return System.Math.Max(0, upkeep - State.Manpower);
}
```

Use existing `ManpowerGateCalculator` if present; otherwise sum `ManpowerCost` on combatant-tagged board pieces minus `State.Manpower`.

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Game/RunOrchestrator.cs Assets/_Project/Core.Tests/EditMode/RunOrchestratorEmergencyDraftTests.cs
git commit -m "fix: wire emergency draft through run orchestrator"
```

---

### Task 2: Populate SynergyRuleCatalog

**Files:**
- Modify: `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/SynergyEngineTests.cs`

- [ ] **Step 1: Write failing test**

Add to `SynergyEngineTests.cs`:

```csharp
[Test]
public void SupplyAdjacentAny_ProducesMusterBonus()
{
    var rules = SynergyRuleCatalog.GetRulesForSourceTag(GameTagIds.Supply);
    Assert.IsNotEmpty(rules);
    Assert.AreEqual(SynergyStat.Muster, rules[0].Stat);
}
```

- [ ] **Step 2: Run — expect FAIL** (empty catalog)

- [ ] **Step 3: Populate rules array**

In `SynergyRuleCatalog.cs`:

```csharp
private static readonly SynergyEffectDefinition[] Rules =
{
    new() { SourceSynergyTagId = GameTagIds.Supply, Direction = SynergyDirection.Adjacent, NeighborFilter = NeighborFilter.Any, Stat = SynergyStat.Muster, ModType = SynergyModType.Flat, Magnitude = 1 },
    new() { SourceSynergyTagId = GameTagIds.Medic, Direction = SynergyDirection.Adjacent, NeighborFilter = NeighborFilter.Tag, NeighborFilterTagId = GameTagIds.Infantry, Stat = SynergyStat.ArmorBuffSteps, ModType = SynergyModType.Flat, Magnitude = 1 },
    new() { SourceSynergyTagId = GameTagIds.Command, Direction = SynergyDirection.Adjacent, NeighborFilter = NeighborFilter.Tag, NeighborFilterTagId = GameTagIds.Artillery, Stat = SynergyStat.DamageBonus, ModType = SynergyModType.Flat, Magnitude = 2 },
    new() { SourceSynergyTagId = GameTagIds.Echo, Direction = SynergyDirection.Adjacent, NeighborFilter = NeighborFilter.Tag, NeighborFilterTagId = GameTagIds.Stealth, Stat = SynergyStat.DamageBonus, ModType = SynergyModType.Flat, Magnitude = 1 },
    new() { SourceSynergyTagId = GameTagIds.Inspiring, Direction = SynergyDirection.Adjacent, NeighborFilter = NeighborFilter.Friendly, Stat = SynergyStat.MoveChargePercent, ModType = SynergyModType.Flat, Magnitude = 5 },
};
```

Adjust enum/field names to match existing `SynergyEffectDefinition` + `SynergyEngine` (read `SynergyEngine.cs` before editing — use exact `NeighborFilter` values the engine supports).

- [ ] **Step 4: Run SynergyEngineTests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: populate demo synergy rules in combat catalog"
```

---

### Task 3: Muster supply synergy

**Files:**
- Modify: `Assets/_Project/Core/Run/MusterCalculator.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/MusterCalculatorTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void SupplySynergy_AdjacentPair_AddsOneMuster()
{
    var board = TestBoards.TwoAdjacentSupplyDepots();
    int muster = MusterCalculator.Compute(board, baseMusterPerShop: 10);
    Assert.AreEqual(11, muster);
}
```

- [ ] **Step 2: Run — expect FAIL** (stub returns 0)

- [ ] **Step 3: Implement adjacency count**

```csharp
private static int CountSupplySynergyBonus(BoardState board)
{
    if (board?.Pieces == null)
        return 0;

    int pairs = 0;
    var supplyPieces = board.Pieces
        .Where(p => PieceTagQueries.HasSynergyTag(p.Definition, GameTagIds.Supply))
        .ToList();

    for (int i = 0; i < supplyPieces.Count; i++)
    {
        for (int j = i + 1; j < supplyPieces.Count; j++)
        {
            if (SynergyEngine.AreAdjacent(board, supplyPieces[i], supplyPieces[j]))
                pairs++;
        }
    }

    return pairs;
}
```

If `SynergyEngine.AreAdjacent` does not exist, add a package-private static on `SynergyEngine` reusing existing neighbor scan logic.

- [ ] **Step 4: Run MusterCalculatorTests — PASS**

- [ ] **Step 5: Commit**

---

### Task 4: Specialty lane rules

**Files:**
- Modify: `Assets/_Project/Core/Shop/SpecialtyLaneRuleCatalog.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/SpecialtyLaneRuleCatalogTests.cs`

- [ ] **Step 1: Write failing tests** for board signals (2+ infantry → assault bias, etc.)

- [ ] **Step 2: Implement `TryResolveSpecialty(BoardState board, ContentRegistry registry, out SpecialtyLaneContext context)`** returning preferred combat roles / tags for weighted pool filtering in `ShopGenerator` specialty lane roll.

- [ ] **Step 3: Hook `ShopGenerator` specialty lane** to filter/weight pool using context before `PickWeighted`.

- [ ] **Step 4: Run tests — PASS**

- [ ] **Step 5: Commit**

---

## Phase 0b — Salvage system

### Task 5: Salvage data model + faction base

**Files:**
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` + mapper
- Modify: `Assets/_Project/Core/Run/RunState.cs` (schema v6)

- [ ] **Step 1: Add fields**

`FactionSO.cs`:

```csharp
[Header("Salvage")]
[Range(0, 50)]
public int baseSalvageChancePercent = 10;
```

`PieceDefinition.cs`:

```csharp
public int SalvageChanceBonus { get; init; }

// In ShopModifierFlags enum:
SalvageChanceBoost5 = 1 << 4,
```

`RunState.cs`:

```csharp
public string LastEnemyFactionId { get; set; }
public int SalvageChancePercent { get; set; }
public int SaveSchemaVersion { get; set; } = 6;
```

Map `SalvageChanceBoost5` → +5 in aggregator; add `salvageChanceBonus` int on SO.

Set faction assets: `iron_vanguard` = 10, `dust_scourge` = 18, `cartel_of_echoes` = 12.

- [ ] **Step 2: Bump save schema** in `RunSaveSerializer` migration if version-gated fields exist.

- [ ] **Step 3: Commit** data model only.

---

### Task 6: SalvageChanceCalculator

**Files:**
- Create: `Assets/_Project/Core/Shop/SalvageChanceCalculator.cs`
- Create: `Assets/_Project/Core/Shop/SalvageBoardBoostAggregator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/SalvageChanceCalculatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Defeat_ReturnsFactionBaseOnly()
{
    int chance = SalvageChanceCalculator.Compute(
        baseSalvagePercent: 10,
        boardBoost: 15,
        outcome: FightOutcome.Defeat,
        destroyedUniqueTypes: 3);
    Assert.AreEqual(10, chance);
}

[Test]
public void Victory_IncludesBoardBoostAndWinBonus()
{
    int chance = SalvageChanceCalculator.Compute(
        baseSalvagePercent: 10,
        boardBoost: 10,
        outcome: FightOutcome.Victory,
        destroyedUniqueTypes: 2);
    // 10 base + 10 board + 10 win + 4 destroyed = 34
    Assert.AreEqual(34, chance);
}

[Test]
public void Draw_TreatedSameAsVictory()
{
    int victory = SalvageChanceCalculator.Compute(10, 5, FightOutcome.Victory, 0);
    int draw = SalvageChanceCalculator.Compute(10, 5, FightOutcome.Draw, 0);
    Assert.AreEqual(victory, draw);
}

[Test]
public void Result_CappedAt50()
{
    int chance = SalvageChanceCalculator.Compute(10, 40, FightOutcome.Victory, 5);
    Assert.AreEqual(50, chance);
}
```

- [ ] **Step 2: Implement**

```csharp
namespace DeadManZone.Core.Shop
{
    public enum FightOutcome { Victory, Defeat, Draw }

    public static class SalvageChanceCalculator
    {
        public const int VictoryBonusPercent = 10;
        public const int DestroyedTypeBonusPercent = 2;
        public const int DestroyedTypeBonusCap = 10;
        public const int GlobalCapPercent = 50;

        public static int Compute(
            int baseSalvagePercent,
            int boardBoost,
            FightOutcome outcome,
            int destroyedUniqueTypes)
        {
            if (outcome == FightOutcome.Defeat)
                return baseSalvagePercent;

            int destroyedBonus = System.Math.Min(
                destroyedUniqueTypes * DestroyedTypeBonusPercent,
                DestroyedTypeBonusCap);

            int total = baseSalvagePercent + boardBoost + VictoryBonusPercent + destroyedBonus;
            return System.Math.Min(total, GlobalCapPercent);
        }
    }
}
```

`SalvageBoardBoostAggregator.cs`:

```csharp
public static int SumBoardBoost(BoardState board)
{
    int sum = 0;
    foreach (var piece in board.Pieces)
    {
        sum += piece.Definition.SalvageChanceBonus;
        if (piece.Definition.ShopModifiers.HasFlag(ShopModifierFlags.SalvageChanceBoost5))
            sum += 5;
    }
    return sum;
}
```

- [ ] **Step 3: Run tests — PASS**

- [ ] **Step 4: Commit**

---

### Task 7: Salvage shop pool + generator

**Files:**
- Create: `Assets/_Project/Core/Shop/SalvageShopPool.cs`
- Modify: `Assets/_Project/Core/Shop/ShopOffer.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/SalvageShopGeneratorTests.cs`

- [ ] **Step 1: Add `IsSalvaged` to `ShopOffer`**

- [ ] **Step 2: Implement `SalvageShopPool.Filter`**

```csharp
public static IReadOnlyList<PieceDefinition> GetPool(
    ContentRegistry registry,
    ShopLane lane,
    string lastEnemyFactionId,
    string playerFactionId,
    int fightIndex)
{
    return registry.GetPool(lane)
        .Where(p => p.FactionId == lastEnemyFactionId)
        .Where(p => p.FactionId != playerFactionId)
        .ToList();
}
```

- [ ] **Step 3: Extend `ShopGenerator.Generate`** signature:

```csharp
public ShopState Generate(
    BoardState board,
    string factionId,
    int round,
    int seed,
    string lastEnemyFactionId = null,
    int salvageChancePercent = 0,
    bool? specialtyUnlocked = null)
```

Inside slot roll loop:

```csharp
bool trySalvage = !string.IsNullOrEmpty(lastEnemyFactionId)
    && salvageChancePercent > 0
    && rng.NextInt(0, 100) < salvageChancePercent;

if (trySalvage)
{
    var salvagePool = SalvageShopPool.GetPool(_registry, lane, lastEnemyFactionId, factionId, round);
    if (salvagePool.Count > 0)
    {
        var piece = salvagePool[rng.NextInt(0, salvagePool.Count)];
        offers.Add(CreateOffer(lane, piece, modifiers, rng, round, i, isSalvaged: true));
        continue;
    }
}
// else existing PickWeighted path
```

- [ ] **Step 4: Tests** — 100% chance fixture yields only last-enemy faction; 0% yields none; deterministic seed.

- [ ] **Step 5: Commit**

---

### Task 8: Aftermath wiring

**Files:**
- Modify: `Assets/_Project/Game/RunOrchestrator.cs` (aftermath / fight completion path)
- Modify: `Assets/_Project/Game/RunOrchestrator.Shop.cs`

- [ ] **Step 1: On fight complete**, read `EnemyTemplateSO.enemyFactionId` from current fight template.

- [ ] **Step 2: Set salvage state**

```csharp
State.LastEnemyFactionId = enemyTemplate.enemyFactionId;
int boardBoost = SalvageBoardBoostAggregator.SumBoardBoost(fightStartBoardSnapshot);
State.SalvageChancePercent = SalvageChanceCalculator.Compute(
    Faction.baseSalvageChancePercent,
    outcome == FightOutcome.Defeat ? 0 : boardBoost,
    outcome,
    destroyedUniqueTypes);
```

Store `fightStartBoardSnapshot` at fight begin for boost evaluation.

- [ ] **Step 3: Pass salvage params into `ShopGenerator.Generate`**

- [ ] **Step 4: Commit**

---

### Task 9: Salvage save/resume + UI

**Files:**
- Modify: `Assets/_Project/Core/Run/ShopOfferRecord.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs`
- Modify: `Assets/_Project/Presentation/Run/RunHudView.cs`
- Modify: `Assets/_Project/Presentation/Shop/ShopOfferView.cs`

- [ ] **Step 1: Serialize `LastEnemyFactionId`, `SalvageChancePercent`, `IsSalvaged`**

- [ ] **Step 2: RunSaveSerializerTests** — round-trip salvage fields

- [ ] **Step 3: HUD** — show `"Salvage: {displayName} — {percent}%"` when `LastEnemyFactionId` set

- [ ] **Step 4: ShopOfferView** — badge when `offer.IsSalvaged`

- [ ] **Step 5: Commit Phase 0b**

```bash
git commit -m "feat: salvage shop offers from last enemy faction"
```

---

## Phase 1 — Combat footprints

### Task 10: CombatFootprint + occupancy grid

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatFootprint.cs`
- Create: `Assets/_Project/Core/Combat/CombatOccupancyGrid.cs`
- Modify: `Assets/_Project/Core/Combat/CombatantState.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatFootprintTests.cs`

- [ ] **Step 1: Tests** — L-shape at anchor (3,4) with rotation 0 occupies expected cells; `CanPlace` rejects overlap.

- [ ] **Step 2: Implement `CombatFootprint.ComputeOffsets(PieceShape shape, int rotation)`** using existing board shape rotation helpers from `PieceShape` / `PrimaryZoneRules`.

- [ ] **Step 3: `CombatOccupancyGrid`** — dictionary `GridCoord → instanceId`; `TryPlace`, `TryMove`, `Remove`.

- [ ] **Step 4: Extend `CombatantState`**

```csharp
public GridCoord AnchorPosition { get; set; }
public IReadOnlyList<GridCoord> ShapeOffsets { get; init; }
public IReadOnlyList<GridCoord> OccupiedCells { get; private set; }

public void RecomputeOccupiedCells()
{
    var cells = new List<GridCoord>(ShapeOffsets.Count);
    foreach (var offset in ShapeOffsets)
        cells.Add(new GridCoord(AnchorPosition.X + offset.X, AnchorPosition.Y + offset.Y));
    OccupiedCells = cells;
}
```

Keep `Position` as alias to `AnchorPosition` during migration (obsolete attribute) until TickCombatRun fully migrated.

- [ ] **Step 5: Commit**

---

### Task 11: Footprint spawn in TickCombatRun

**Files:**
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/*Combat*` fixtures

- [ ] **Step 1: Update `SpawnCombatants`** — compute offsets from placed piece shape + rotation; call `RecomputeOccupiedCells`; populate occupancy grid for all cells.

- [ ] **Step 2: Update `RebuildOccupied`** to use full footprints.

- [ ] **Step 3: Fix range/damage tests** that assumed single-cell positions — run full EditMode combat suite.

- [ ] **Step 4: Commit**

---

## Phase 2 — ShapePathfinder (A*)

### Task 12: A* on anchor positions

**Files:**
- Create: `Assets/_Project/Core/Combat/ShapePathfinder.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/ShapePathfinderTests.cs`

- [ ] **Step 1: Tests**

```csharp
[Test]
public void FindStep_RoutesAroundBlockingAlly()
{
    // 3-wide corridor, ally blocks direct path, expect detour step
}

[Test]
public void FindStep_ReturnsNullWhenNoValidPlacement()
{
    // boxed in multi-cell piece
}
```

- [ ] **Step 2: Implement A*`** over anchor positions; neighbor anchors = current ± (1,0) or (0,1); edge cost from `CombatMovement.GetStepChargeCost`; heuristic = Manhattan to goal; validate full footprint via `CombatOccupancyGrid.CanPlace`.

- [ ] **Step 3: Replace `CombatMovement.StepTowardTarget` call** in `TickCombatRun.TryMoveSide` with `ShapePathfinder.FindStep` (return next anchor or null).

- [ ] **Step 4: Run determinism test** — same seed + commands → identical event log (extend existing regression test).

- [ ] **Step 5: Commit**

---

## Phase 3 — Role engagement

### Task 13: RoleEngagement goals

**Files:**
- Create: `Assets/_Project/Core/Combat/RoleEngagement.cs`
- Modify: `Assets/_Project/Core/Tags/CombatRoleProfile.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs`

- [ ] **Step 1: Implement per-role goal anchors** (Artillery holds max range behind friendly front X, Assault nearest enemy front column, etc.) returning `GridCoord` engagement goal.

- [ ] **Step 2: Lane bias** — add cost penalty in pathfinder when |Y - spawnY| > 1.

- [ ] **Step 3: Spacing** — if next step anchor would overlap friendly goal cell, hold (except Assault with critical mass charge tag — check `MoveChargePercentBonus` from critical mass).

- [ ] **Step 4: Per-role EditMode fixtures**

- [ ] **Step 5: Commit**

---

## Phase 4 — Buildings in combat arena

### Task 14: Arena building prefabs

**Files:**
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` (ensure `combatArenaPrefab` field used)
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`
- Create: placeholder prefabs under `Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/`

- [ ] **Step 1: Spawn static mesh/prefab** for each building footprint cell (or single prefab scaled to footprint bounds).

- [ ] **Step 2: HQ + field gun + supply depot visible** in PlayMode smoke test.

- [ ] **Step 3: Commit**

---

## Phase 5 — Attack / armor matrix

### Task 15: Matrix audit

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatDamageResolver.cs`
- Modify: `Assets/_Project/Core/Tags/AttackTypeProfileCatalog.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/AttackArmorMatrixTests.cs`

- [ ] **Step 1: Test every `AttackType` × `ArmorType` combo** returns defined multiplier > 0.

- [ ] **Step 2: Fill gaps** in catalog/resolver.

- [ ] **Step 3: Commit**

---

## Phase 6 — Tag Creator

### Task 16: Tag Creator editor window

**Files:**
- Create: `Assets/_Project/Data/Editor/TagCreatorWindow.cs`
- Create: `Assets/_Project/Data/Editor/TagCreatorPersistence.cs`
- Modify: `Assets/_Project/Data/Editor/UnitCreatorFormSections.cs`

- [ ] **Step 1: Editor window** — form fields, validation, append to `TagRegistry.BuildCatalog()` source via controlled codegen or ScriptableObject list (match spec: codegen v1).

- [ ] **Step 2: Unit Creator tag dropdowns** read `TagRegistry.GetAll()` — remove hardcoded lists.

- [ ] **Step 3: Manual test** — add tag `test_salvage` → appears in Unit Creator → save piece → tag on asset.

- [ ] **Step 4: Commit**

---

## Phase 7 — UnitCard tooltips

### Task 17: PieceCard extensions

**Files:**
- Modify: `Assets/_Project/Core/Tags/PieceCardViewModel.cs`
- Modify: `Assets/_Project/Core/Tags/PieceCardViewModelBuilder.cs`
- Modify: `Assets/_Project/Presentation/UI/PieceHoverCard.cs` (locate actual path via glob)

- [ ] **Step 1: Add fields** — synergy lines, critical mass hint string, salvage context string, attack/armor tooltip text.

- [ ] **Step 2: Builder pulls** from `SynergyEngine`, `CriticalMassRules`, shop offer context.

- [ ] **Step 3: EditMode tests** for builder output strings.

- [ ] **Step 4: Commit**

---

## Phase 8 — Test content

### Task 18: Neutral + IronMarch roster

**Files:**
- Modify: `Assets/_Project/Data/Editor/DemoContentGenerator.cs`
- Modify: `Assets/_Project/Data/Editor/DemoPieceFactory.cs`

- [ ] **Step 1: Author 10 neutral + 15 IronMarch pieces** per spec §5.2 — stats/tags cover every mechanic; placeholder icons OK.

- [ ] **Step 2: Assign salvage boost to one Dust Scourge / neutral test building (+5 flag).

- [ ] **Step 3: Run Generate Demo Content menu** — verify ContentDatabase counts.

- [ ] **Step 4: Commit**

---

## Phase 9 — Integration gate

### Task 19: Sandbox checklist automation

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/MechanicsSandboxChecklistTests.cs`

- [ ] **Step 1: One test per success criterion** from spec §1 (where automatable).

- [ ] **Step 2: Run full EditMode suite — all green**

- [ ] **Step 3: Manual save/resume mid-combat** on footprint build — verify identical outcome.

- [ ] **Step 4: Final commit**

```bash
git commit -m "test: mechanics sandbox integration checklist"
```

---

## Spec coverage self-review

| Spec § | Task(s) |
|--------|---------|
| 1 Success criteria 1–3 | Tasks 10–13 |
| 1 §4 buildings | Tasks 10–11, 14 |
| 1 §5 attack/armor | Task 15 |
| 1 §6 synergies | Task 2 |
| 1 §7 salvage | Tasks 5–9 |
| 1 §8 specialty | Task 4 |
| 1 §9 emergency draft | Task 1 |
| 1 §10 tag creator | Task 16 |
| 1 §11 tooltips | Task 17 |
| 1 §12 content | Task 18 |
| 1 §13 determinism | Tasks 12, 19 |
| 3.2 shop matrix | Task 4 + content filters in Task 18 |
| 3.5 muster synergy | Task 3 |

**Open tuning (spec §7):** handled via constants on `SalvageChanceCalculator` + `FactionSO` inspector — no code task until playtest pass after Task 19.

---

## Estimated calendar

| Phase | Days |
|-------|------|
| 0 | 2–3 |
| 0b | 3–4 |
| 1–3 | 11–14 |
| 4–5 | 4–5 |
| 6–7 | 5–7 |
| 8–9 | 6–8 |
| **Total** | **~6–8 weeks** |
