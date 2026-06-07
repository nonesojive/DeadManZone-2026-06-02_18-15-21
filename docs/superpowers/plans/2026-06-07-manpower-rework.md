# Manpower Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace spend-and-refund manpower with a squad-based pool: fielding threshold at fight start, damage-based casualties after fights, and building-driven muster income each shop phase.

**Architecture:** Extend `PieceDefinition` / `FactionSO` with `musterPerShop` and `baseMusterPerShop`. Refactor `ManpowerCalculator` for fielding + hybrid casualties. Add `MusterCalculator` for shop-phase income. Wire `RunOrchestrator` to stop deducting/refunding manpower and apply casualties/muster at the correct lifecycle points. Rescale piece HP/DPS and enemy templates in a final balance pass validated by existing tutorial tests.

**Tech Stack:** Unity 6, C#, Unity Test Framework (Edit Mode), existing asmdefs under `Assets/_Project/`.

**Spec:** `docs/superpowers/specs/2026-06-07-manpower-rework-design.md`

---

## File map

| File | Responsibility |
|------|----------------|
| `Core/Run/ManpowerCalculator.cs` | Fielding requirement, hybrid casualties, hp-per-body helper |
| `Core/Run/MusterCalculator.cs` | **New** — faction baseline + piece muster + tag bonuses |
| `Core/Run/EmergencyDraft.cs` | Deprecate auto shortfall fill |
| `Core/Board/PieceDefinition.cs` | Add `MusterPerShop` |
| `Data/ScriptableObjects/PieceDefinitionSO.cs` | `musterPerShop` field + `ToCore()` mapping |
| `Data/ScriptableObjects/FactionSO.cs` | `baseMusterPerShop` field |
| `Core/Combat/BattleReport.cs` | `ManpowerCasualties` replaces `ManpowerRefunded` |
| `Core/Combat/BattleReportBuilder.cs` | Accept casualties param |
| `Core/Combat/PhasedCombatRun.cs` | `PlayerCombatantsAtEnd` on `CombatAdvanceResult` |
| `Core/Combat/TickCombatRun.cs` | Populate `PlayerCombatantsAtEnd`; update `BattleReportBuilder` call |
| `Game/RunOrchestrator.cs` | Remove deduct/refund; apply casualties; call muster |
| `Game/RunOrchestrator.Shop.cs` | `ApplyMuster()` on shop refresh / dismiss aftermath |
| `Game/FightRewardTable.cs` | Zero `BonusManpower` |
| `Core/Shop/SalvageCalculator.cs` | Zero manpower salvage |
| `Presentation/Combat/BattleReportPresenter.cs` | "Casualties" copy |
| `Presentation/Run/RunSceneController.cs` | Remove or hide emergency draft button |
| `Data/Editor/DemoPieceFactory.cs` | Rescaled stats + muster values |
| `Data/Editor/DemoFactionFactory.cs` | `startingManpower: 100`, `baseMusterPerShop` |
| `Core.Tests/EditMode/ManpowerCalculatorTests.cs` | Rewrite for new API |
| `Core.Tests/EditMode/MusterCalculatorTests.cs` | **New** |
| Enemy `fight_*.asset` + `TutorialBalanceFixtures` | Rebalance pass |

---

### Task 1: Data model — `musterPerShop` and `baseMusterPerShop`

**Files:**
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`

- [ ] **Step 1: Add `MusterPerShop` to `PieceDefinition`**

```csharp
public int MusterPerShop { get; init; }
```

- [ ] **Step 2: Add field to `PieceDefinitionSO` and map in `ToCore()`**

```csharp
[Header("Manpower")]
public int musterPerShop;

// In ToCore():
MusterPerShop = musterPerShop,
```

- [ ] **Step 3: Add `baseMusterPerShop` to `FactionSO`**

```csharp
[Header("Manpower")]
public int baseMusterPerShop = 12;
```

Also set `startingManpower = 100` on `iron_vanguard.asset` (and other playable factions in a later content task).

- [ ] **Step 4: Verify project compiles in Unity**

---

### Task 2: `ManpowerCalculator` — fielding + casualties (TDD)

**Files:**
- Modify: `Assets/_Project/Core/Run/ManpowerCalculator.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/ManpowerCalculatorTests.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs` (add squad-sized test pieces)

- [ ] **Step 1: Write failing casualty tests**

Add to `ManpowerCalculatorTests.cs`:

```csharp
[Test]
public void ComputeCasualties_Survivor_UsesDamageOverHpPerBody()
{
    var rifle = TestPieces.RifleSquadTenMan(); // 100 HP, cost 10
    var combatants = new[]
    {
        new CombatantState
        {
            InstanceId = "rifle_1",
            Definition = rifle,
            CurrentHp = 78,
            DamageTakenThisFight = 22
        }
    };
    Assert.AreEqual(2, ManpowerCalculator.ComputeCasualties(combatants));
}

[Test]
public void ComputeCasualties_Destroyed_AlwaysCostsFullSquad()
{
    var rifle = TestPieces.RifleSquadTenMan();
    var combatants = new[]
    {
        new CombatantState
        {
            InstanceId = "rifle_1",
            Definition = rifle,
            CurrentHp = 0,
            DamageTakenThisFight = 15 // one-shot — still full squad loss
        }
    };
    Assert.AreEqual(10, ManpowerCalculator.ComputeCasualties(combatants));
}

[Test]
public void ComputeCasualties_CapsSurvivorLossAtManpowerCost()
{
    var rifle = TestPieces.RifleSquadTenMan();
    var combatants = new[]
    {
        new CombatantState
        {
            InstanceId = "rifle_1",
            Definition = rifle,
            CurrentHp = 1,
            DamageTakenThisFight = 99
        }
    };
    Assert.AreEqual(9, ManpowerCalculator.ComputeCasualties(combatants));
}

[Test]
public void ComputeFieldingRequirement_IncludesHqManpowerCost()
{
    var board = TestBoards.WithHqAndRifle(); // HQ cost 8 + rifle 10
    Assert.AreEqual(18, ManpowerCalculator.ComputeFieldingRequirement(board, Registry));
}
```

Add helper in `TestPieces.cs`:

```csharp
public static PieceDefinition RifleSquadTenMan() => new()
{
    Id = "rifle_squad",
    DisplayName = "Rifle Squad",
    MaxHp = 100,
    ManpowerCost = 10,
    Tags = new[] { GameTagIds.Combatant }
};
```

- [ ] **Step 2: Run tests — expect FAIL**

Unity Test Runner → EditMode → `ManpowerCalculatorTests`

- [ ] **Step 3: Implement `ManpowerCalculator`**

```csharp
public static int HpPerBody(PieceDefinition definition)
{
    if (definition == null || definition.ManpowerCost <= 0)
        return definition?.MaxHp ?? 1;
    return definition.MaxHp / definition.ManpowerCost;
}

public static int ComputeFieldingRequirement(BoardState board, ContentRegistry content)
{
    if (board == null) return 0;
    return board.Pieces
        .Where(p => CountsTowardFielding(p.Definition))
        .Sum(p => p.Definition.ManpowerCost);
}

public static int ComputeCasualties(IReadOnlyList<CombatantState> playerCombatants)
{
    if (playerCombatants == null || playerCombatants.Count == 0)
        return 0;

    int total = 0;
    foreach (var c in playerCombatants)
    {
        if (c?.Definition == null || c.Definition.ManpowerCost <= 0)
            continue;
        if (c.DamageTakenThisFight <= 0 && c.IsAlive)
            continue;

        if (!c.IsAlive)
        {
            total += c.Definition.ManpowerCost;
            continue;
        }

        int hpPerBody = HpPerBody(c.Definition);
        int bodies = hpPerBody > 0 ? c.DamageTakenThisFight / hpPerBody : 0;
        total += Math.Min(c.Definition.ManpowerCost, bodies);
    }
    return total;
}

private static bool CountsTowardFielding(PieceDefinition definition) =>
    PieceTagQueries.HasTag(definition, GameTagIds.Combatant)
    || PieceTagQueries.HasTag(definition, GameTagIds.Hq);

// Keep ComputeUpkeep as alias or replace all callers with ComputeFieldingRequirement
public static int ComputeUpkeep(BoardState board, ContentRegistry content) =>
    ComputeFieldingRequirement(board, content);
```

Remove `RefundSurvivors`.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Delete obsolete `RefundSurvivors` test; update `CanStartBattle` tests to use squad costs**

---

### Task 3: `MusterCalculator` (TDD)

**Files:**
- Create: `Assets/_Project/Core/Run/MusterCalculator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/MusterCalculatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void ComputeMuster_IncludesFactionBaseline()
{
    var board = TestBoards.HqOnly();
    int muster = MusterCalculator.Compute(board, baseMusterPerShop: 12);
    Assert.AreEqual(12, muster);
}

[Test]
public void ComputeMuster_AddsPieceMusterPerShop()
{
    var board = TestBoards.WithSupplyDepot(); // depot musterPerShop = 3
    int muster = MusterCalculator.Compute(board, baseMusterPerShop: 12);
    Assert.AreEqual(15, muster);
}

[Test]
public void ComputeMuster_SupplySynergyBonus_TwoOrMoreSupplyTags()
{
    var board = TestBoards.WithTwoSupplyBuildings();
    int muster = MusterCalculator.Compute(board, baseMusterPerShop: 10);
    Assert.GreaterOrEqual(muster, 12); // 10 + buildings + 2 synergy
}
```

- [ ] **Step 2: Implement `MusterCalculator`**

```csharp
public static class MusterCalculator
{
    public const int SupplySynergyThreshold = 2;
    public const int SupplySynergyMusterBonus = 2;

    public static int Compute(BoardState board, int baseMusterPerShop)
    {
        if (board == null)
            return baseMusterPerShop;

        int fromPieces = board.Pieces.Sum(p => p.Definition.MusterPerShop);
        int synergyBonus = CountSupplySynergyBonus(board);
        return baseMusterPerShop + fromPieces + synergyBonus;
    }

    private static int CountSupplySynergyBonus(BoardState board)
    {
        int supplyCount = board.Pieces.Count(p =>
            p.Definition.SynergyTags != null
            && p.Definition.SynergyTags.Contains("supply"));
        return supplyCount >= SupplySynergyThreshold ? SupplySynergyMusterBonus : 0;
    }
}
```

- [ ] **Step 3: Run `MusterCalculatorTests` — expect PASS**

---

### Task 4: Battle report — casualties field

**Files:**
- Modify: `Assets/_Project/Core/Combat/BattleReport.cs`
- Modify: `Assets/_Project/Core/Combat/BattleReportBuilder.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Core/Combat/PhasedCombatRun.cs` (if still used in tests)

- [ ] **Step 1: Rename `ManpowerRefunded` → `ManpowerCasualties` on `BattleReport`**

```csharp
public int ManpowerCasualties { get; init; }
```

- [ ] **Step 2: Update `BattleReportBuilder.Build` signature**

```csharp
public static BattleReport Build(
    IEnumerable<CombatantState> playerCombatants,
    bool playerWon,
    bool isDraw,
    int manpowerCasualties,
    int suppliesEarned,
    int moraleDelta,
    int topCount = 3)
```

- [ ] **Step 3: Add `PlayerCombatantsAtEnd` to `CombatAdvanceResult`**

```csharp
public IReadOnlyList<CombatantState> PlayerCombatantsAtEnd { get; init; } =
    System.Array.Empty<CombatantState>();
```

In `TickCombatRun.CompleteResult()`:

```csharp
PlayerCombatantsAtEnd = _playerCombatants.ToList(),
BattleReport = BattleReportBuilder.Build(
    _playerCombatants,
    PlayerWon,
    IsDraw,
    manpowerCasualties: ManpowerCalculator.ComputeCasualties(_playerCombatants),
    suppliesEarned: 0,
    moraleDelta: 0)
```

- [ ] **Step 4: Grep for `ManpowerRefunded` and update all references**

---

### Task 5: `RunOrchestrator` lifecycle wiring

**Files:**
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.Shop.cs`

- [ ] **Step 1: Remove manpower deduction in `BeginCombat`**

Delete lines:

```csharp
int upkeep = ManpowerCalculator.ComputeUpkeep(playerBoard, _registry);
State.Manpower -= upkeep;
```

Keep `CanStartBattle` threshold check unchanged (update message to say "fielding requirement" instead of "upkeep").

- [ ] **Step 2: Replace refund with casualties in `CompleteCombat`**

```csharp
var playerCombatants = result.PlayerCombatantsAtEnd;
if (playerCombatants == null || playerCombatants.Count == 0)
    playerCombatants = System.Array.Empty<CombatantState>();

int casualties = ManpowerCalculator.ComputeCasualties(playerCombatants);
State.Manpower = Math.Max(0, State.Manpower - casualties);
```

Pass `casualties` into `BattleReportBuilder.Build` instead of `manpowerRefunded`.

- [ ] **Step 3: Remove fight reward manpower**

In `CompleteCombat` win branch, delete:

```csharp
State.Manpower += reward.BonusManpower;
```

Set all `BonusManpower` values to `0` in `FightRewardTable.cs`.

- [ ] **Step 4: Add `ApplyMuster()` private method**

```csharp
private void ApplyMuster()
{
    var board = GetPlayerBoard();
    int gained = MusterCalculator.Compute(board, Faction.baseMusterPerShop);
    State.Manpower += gained;
    State.LastMusterGained = gained; // optional: add int? to RunState for UI flash
}
```

- [ ] **Step 5: Call `ApplyMuster()` at run start and shop open**

In `StartNewRun`, after `PlaceStartingHq()`:

```csharp
ApplyMuster(); // first-shop staffing (B)
```

In `DismissAftermath`, before `RefreshShop()`:

```csharp
ApplyMuster();
```

Also call `ApplyMuster()` when entering build phase after loss (`CompleteCombat` defeat branch) — before `RefreshShop()`.

**Do not** call muster at fight end (casualties only there).

- [ ] **Step 6: Deprecate `TryEmergencyDraft`**

`TryEmergencyDraft` returns `false` always, or remove button wiring in `RunSceneController`. Keep achievement ID for saves compatibility but gate button off permanently.

- [ ] **Step 7: Zero salvage manpower in `SalvageCalculator`**

```csharp
int manpower = 0;
```

---

### Task 6: UI copy updates

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/BattleReportPresenter.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs` (`CanStartBattle` message)
- Modify: `Assets/_Project/Presentation/Run/RunSceneController.cs` (hide emergency draft)

- [ ] **Step 1: Battle report text**

```csharp
summaryText.text =
    $"Casualties: −{report.ManpowerCasualties}\n" +
    $"Supplies: {report.SuppliesEarned}\n" +
    $"Morale: {report.MoraleDelta:+#;-#;0}";
```

- [ ] **Step 2: Fielding failure message**

```csharp
failureReason =
    $"Insufficient manpower: fielding requires {requirement} but only {State.Manpower} available.";
```

- [ ] **Step 3: Hide emergency draft button** (`emergencyDraftButton.gameObject.SetActive(false)`)

---

### Task 7: Content rebalance — player pieces

**Files:**
- Modify: `Assets/_Project/Data/Editor/DemoPieceFactory.cs`
- Modify: all `Assets/_Project/Data/Resources/DeadManZone/Pieces/*.asset` (via generator refresh or manual)
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Factions/*.asset`

**Target ratios (hpPerBody = 10 everywhere):**

| Piece | manpowerCost | maxHp | baseDamage | musterPerShop |
|-------|--------------|-------|------------|---------------|
| hq_command | 8 | 80 | 0 | 0 |
| conscript_rifleman | 6 | 60 | 12 | 0 |
| rifle_squad | 10 | 100 | 20 | 0 |
| mg_team | 12 | 120 | 24 | 0 |
| field_medic | 4 | 40 | 0 | 0 |
| armored_transport | 5 | 50 | 10 | 0 |
| supply_depot | 0 | 50 | 0 | 3 |
| field_workshop | 0 | 40 | 0 | 2 |
| signal_relay | 0 | 30 | 0 | 1 |
| radio_array | 0 | 40 | 0 | 0 |

Faction baselines:

| Faction | startingManpower | baseMusterPerShop |
|---------|------------------|-------------------|
| iron_vanguard | 100 | 12 |
| dust_scourge | 100 | 10 |
| cartel_of_echoes | 100 | 14 |

- [ ] **Step 1: Update `DemoPieceFactory` with new stats**
- [ ] **Step 2: Update `DemoFactionFactory` / `DemoContentGenerator.SaveFaction` to accept `baseMusterPerShop`**
- [ ] **Step 3: Run content generator menu or re-save assets**
- [ ] **Step 4: Update `TestPieces.cs` and `TutorialBalanceFixtures` board builders if HP changes break placement assumptions**

---

### Task 8: Content rebalance — enemy templates + tutorial validation

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_1.asset` through `fight_10.asset`
- Modify: `Assets/_Project/Data/Editor/DemoEnemyFactory.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TutorialBalanceFixtures.cs` (if needed)

- [ ] **Step 1: Rescale enemy piece HP/DPS proportionally (~10× HP, ~2× damage baseline)**
- [ ] **Step 2: Run EditMode tests**

```
TutorialBalanceTests (all 6)
EnemyTemplatePlacementTests
VerticalSliceRegressionTests
ManpowerCalculatorTests
MusterCalculatorTests
```

- [ ] **Step 3: Tune fight 1–3 enemy templates until ≥90% pause + survival rates pass**
- [ ] **Step 4: Fix any orchestrator tests that assumed manpower refund or starting manpower of 10**

---

### Task 9: Save compatibility + cleanup

**Files:**
- Modify: `Assets/_Project/Core/Run/RunSaveSerializer.cs`
- Modify: `Assets/_Project/Core/Run/RunState.cs`

- [ ] **Step 1: Bump legacy default manpower from 10 → 100 in deserializer**
- [ ] **Step 2: Optional `LastMusterGained` on `RunState` for UI (default 0)**
- [ ] **Step 3: Grep codebase for `RefundSurvivors`, `ManpowerRefunded`, `BonusManpower`, `EmergencyDraft` — ensure no stale references**

---

## Verification checklist

- [ ] New run: manpower = 100 + first muster (e.g. 112 with Iron Vanguard HQ-only board)
- [ ] Begin fight: pool unchanged; blocked when below fielding requirement
- [ ] Win/loss aftermath: pool reduced by casualties; report shows `Casualties: −N`
- [ ] Shop opens: pool increased by muster; Supply Depot on board adds +3
- [ ] Selling pieces: no manpower salvage
- [ ] Fight rewards: no bonus manpower
- [ ] All EditMode tests green

---

## Spec coverage self-review

| Spec requirement | Task |
|------------------|------|
| Threshold fielding, no deduct | Task 5 |
| Hybrid C casualties | Task 2, 4, 5 |
| HQ same rules | Task 2 (HQ in fielding + casualties) |
| Muster at shop + run start | Task 3, 5 |
| No manpower cap | Task 5 (no clamp on add) |
| Building-driven muster | Task 3, 7 |
| Remove fight reward manpower | Task 5 |
| Full combat rebalance | Task 7, 8 |
| UI casualties copy | Task 6 |
| Approach 1 data model | Task 1, 7 |

No placeholders remain. Types consistent: `ManpowerCasualties`, `ComputeFieldingRequirement`, `MusterCalculator.Compute`.
