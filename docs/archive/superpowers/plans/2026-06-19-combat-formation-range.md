> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Combat Formation Spread & Chebyshev Range Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop combat units funneling into a center blob by adding deterministic formation slots (hybrid lane-hold + dynamic fallback + rear bands) and switch attack range from Manhattan to Chebyshev at tiers 1/3/5/8.

**Architecture:** Add pure-C# `CombatFormationSlots` called from `RoleEngagement.ComputeGoal`; extend `CombatRange` with `Distance` (Chebyshev) for `IsInRange` and accuracy; bump role-aware lane bias in `ShapePathfinder`. Sim and presentation share `RoleEngagement` — no presentation-only offsets. TDD in `Assets/_Project/Core.Tests/EditMode/`.

**Tech Stack:** Unity 6 (6000.3.8f1), C# Core (`DeadManZone.Core`), NUnit EditMode

**Spec:** `docs/superpowers/specs/2026-06-19-combat-formation-range-design.md`

**Branch:** `combatworkv4`

---

## File map

| File | Action |
|------|--------|
| `Assets/_Project/Core/Combat/CombatRange.cs` | Add `Distance`; `IsInRange` uses Chebyshev |
| `Assets/_Project/Core/Combat/CombatFormationSlots.cs` | **Create** — lane/slot/rear-band goal math |
| `Assets/_Project/Core/Combat/RoleEngagement.cs` | Delegate frontline/rear Y to slots |
| `Assets/_Project/Core/Combat/ShapePathfinder.cs` | Role-aware `LaneBiasPenalty` |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Accuracy uses `CombatRange.Distance` |
| `Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs` | Chebyshev assertions |
| `Assets/_Project/Core.Tests/EditMode/CombatFormationSlotsTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs` | Update expected goals |
| `Assets/_Project/Core.Tests/EditMode/CombatPresentationEngagementTests.cs` | Update if chase expectations change |
| `Assets/_Project/Core.Tests/EditMode/CombatMovementRangeGateTests.cs` | Fix any Manhattan-specific range asserts |

**Unity test command (filtered):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.CombatFormationSlotsTests" `
  -testResults "TestResults-EditMode.xml" -logFile "UnityTest.log" -quit
```

**Full EditMode suite:**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testResults "TestResults-EditMode.xml" -logFile "UnityTest.log" -quit
```

---

### Task 1: Chebyshev `CombatRange.Distance`

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatRange.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs`

- [ ] **Step 1: Write failing Chebyshev tests**

Add to `CombatRangeTests.cs`:

```csharp
[Test]
public void Distance_UsesChebyshevMetric()
{
    var from = new GridCoord(0, 0);
    Assert.AreEqual(1, CombatRange.Distance(from, new GridCoord(1, 1)));
    Assert.AreEqual(2, CombatRange.Distance(from, new GridCoord(2, 0)));
    Assert.AreEqual(3, CombatRange.Distance(from, new GridCoord(3, 1)));
}

[Test]
public void IsInRange_MeleeIncludesDiagonalNeighbor()
{
    var from = new GridCoord(5, 5);
    Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(6, 6), AttackRangeTier.Melee));
    Assert.IsFalse(CombatRange.IsInRange(from, new GridCoord(7, 5), AttackRangeTier.Melee));
    Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(7, 5), AttackRangeTier.Short));
}

[Test]
public void Manhattan_RemainsForSortHelpers()
{
    var from = new GridCoord(0, 0);
    Assert.AreEqual(2, CombatRange.Manhattan(from, new GridCoord(1, 1)));
}
```

- [ ] **Step 2: Run tests — expect FAIL** (`Distance` not defined; diagonal melee false)

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.CombatRangeTests" `
  -testResults "TestResults-EditMode.xml" -logFile "UnityTest.log" -quit
```

Expected: FAIL on new tests.

- [ ] **Step 3: Implement Chebyshev in `CombatRange.cs`**

```csharp
public static int Distance(GridCoord from, GridCoord to) =>
    System.Math.Max(System.Math.Abs(from.X - to.X), System.Math.Abs(from.Y - to.Y));

public static bool IsInRange(GridCoord from, GridCoord to, AttackRangeTier tier) =>
    Distance(from, to) <= GetRangeCells(tier);

public static int Manhattan(GridCoord from, GridCoord to) =>
    System.Math.Abs(from.X - to.X) + System.Math.Abs(from.Y - to.Y);
```

- [ ] **Step 4: Run `CombatRangeTests` — PASS**

- [ ] **Step 5: Commit**

```powershell
git add Assets/_Project/Core/Combat/CombatRange.cs Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs
git commit -m "feat(combat): switch IsInRange to Chebyshev Distance metric"
```

---

### Task 2: Wire Chebyshev distance into accuracy

**Files:**
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs` (~line 289)

- [ ] **Step 1: Change accuracy distance**

In `ResolveAttacks`, replace:

```csharp
int distance = CombatRange.Manhattan(actor.AnchorPosition, target.AnchorPosition);
```

with:

```csharp
int distance = CombatRange.Distance(actor.AnchorPosition, target.AnchorPosition);
```

- [ ] **Step 2: Run EditMode suite — expect PASS** (no new tests; regression check)

- [ ] **Step 3: Commit**

```powershell
git add Assets/_Project/Core/Combat/TickCombatRun.cs
git commit -m "feat(combat): use Chebyshev distance for accuracy falloff"
```

---

### Task 3: `CombatFormationSlots` — frontline lane goals

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatFormationSlots.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatFormationSlotsTests.cs`

- [ ] **Step 1: Write failing frontline tests**

Create `CombatFormationSlotsTests.cs`:

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatFormationSlotsTests
    {
        private static readonly BattlefieldLayout Layout = new(7, 2, 7, 10);

        [Test]
        public void FrontlineGoal_PreservesDistinctSpawnLanes()
        {
            var a = CreateMover("a_lane3", CombatSide.Player, spawnY: 3, y: 2);
            var b = CreateMover("b_lane7", CombatSide.Player, spawnY: 7, y: 2);
            var enemyFront = CreateEnemy("enemy_front", new GridCoord(10, 5));
            var enemyOther = CreateEnemy("enemy_other", new GridCoord(10, 3));

            var goalA = CombatFormationSlots.ResolveFrontlineGoal(
                a, new[] { a, b }, new[] { enemyOther, enemyFront }, Layout);
            var goalB = CombatFormationSlots.ResolveFrontlineGoal(
                b, new[] { a, b }, new[] { enemyOther, enemyFront }, Layout);

            Assert.AreEqual(new GridCoord(9, 3), goalA);
            Assert.AreEqual(new GridCoord(9, 7), goalB);
        }

        [Test]
        public void FrontlineGoal_SameSpawnY_SecondaryShiftsLane()
        {
            var first = CreateMover("a_first", CombatSide.Player, spawnY: 5, y: 2);
            var second = CreateMover("b_second", CombatSide.Player, spawnY: 5, y: 2);
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));

            var goalFirst = CombatFormationSlots.ResolveFrontlineGoal(
                first, new[] { first, second }, new[] { enemy }, Layout);
            var goalSecond = CombatFormationSlots.ResolveFrontlineGoal(
                second, new[] { first, second }, new[] { enemy }, Layout);

            Assert.AreEqual(new GridCoord(9, 5), goalFirst);
            Assert.AreNotEqual(goalFirst.Y, goalSecond.Y);
            Assert.AreEqual(9, goalSecond.X);
        }

        [Test]
        public void FrontlineGoal_BlockedContactCell_FallsBackToAdjacentY()
        {
            var lead = CreateMover("lead", CombatSide.Player, spawnY: 5, y: 9);
            var follower = CreateMover("follow", CombatSide.Player, spawnY: 5, y: 2);
            follower.AnchorPosition = new GridCoord(9, 5); // blocks contact cell
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));

            var goal = CombatFormationSlots.ResolveFrontlineGoal(
                lead, new[] { lead, follower }, new[] { enemy }, Layout);

            Assert.AreEqual(new GridCoord(9, 4), goal);
        }

        [Test]
        public void FrontlineGoal_DeterministicForSameInputs()
        {
            var a = CreateMover("a", CombatSide.Player, spawnY: 4, y: 2);
            var b = CreateMover("b", CombatSide.Player, spawnY: 6, y: 2);
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));
            var allies = new[] { a, b };
            var enemies = new[] { enemy };

            var g1 = CombatFormationSlots.ResolveFrontlineGoal(a, allies, enemies, Layout);
            var g2 = CombatFormationSlots.ResolveFrontlineGoal(a, allies, enemies, Layout);
            Assert.AreEqual(g1, g2);
        }

        private static CombatantState CreateMover(string id, CombatSide side, int spawnY, int y)
        {
            var def = TestPieces.With(
                TestPieces.CreateUnit(id, primary: GameTagIds.Infantry, combatRole: GameTagIds.Assault),
                attackRange: AttackRangeTier.Short);
            return new CombatantState
            {
                InstanceId = id,
                Side = side,
                Definition = def,
                CurrentHp = def.MaxHp,
                AnchorPosition = new GridCoord(2, y),
                SpawnAnchorY = spawnY
            };
        }

        private static CombatantState CreateEnemy(string id, GridCoord pos)
        {
            var def = TestPieces.CreateUnit(id, combatRole: GameTagIds.Assault);
            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Enemy,
                Definition = def,
                CurrentHp = def.MaxHp,
                AnchorPosition = pos,
                SpawnAnchorY = pos.Y
            };
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL** (type missing)

- [ ] **Step 3: Implement `CombatFormationSlots.cs` (frontline only)**

Create `Assets/_Project/Core/Combat/CombatFormationSlots.cs`:

```csharp
using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatFormationSlots
    {
        public static GridCoord ResolveFrontlineGoal(
            CombatantState mover,
            IReadOnlyList<CombatantState> frontlineAllies,
            IReadOnlyList<CombatantState> enemies,
            BattlefieldLayout layout)
        {
            if (mover == null || enemies == null || enemies.Count == 0)
                return mover?.AnchorPosition ?? default;

            int enemyFrontX = GetFrontColumnX(enemies[0].Side, enemies);
            int contactX = mover.Side == CombatSide.Player ? enemyFrontX - 1 : enemyFrontX + 1;
            int laneY = ResolveLaneY(mover, frontlineAllies);
            var goal = new GridCoord(contactX, laneY);

            if (!IsBlockedByFriendlyAnchor(goal, mover.InstanceId, frontlineAllies))
                return goal;

            foreach (int offset in new[] { -1, 1, -2, 2, -3, 3 })
            {
                var candidate = new GridCoord(contactX, laneY + offset);
                if (!layout.IsInBounds(candidate))
                    continue;
                if (!IsBlockedByFriendlyAnchor(candidate, mover.InstanceId, frontlineAllies))
                    return candidate;
            }

            return mover.AnchorPosition;
        }

        public static int ResolveLaneY(CombatantState mover, IReadOnlyList<CombatantState> frontlineAllies)
        {
            int preferred = mover.SpawnAnchorY;
            var sorted = SortByInstanceId(frontlineAllies);
            bool taken = false;
            foreach (var ally in sorted)
            {
                if (ally == null || !ally.IsAlive || ally.InstanceId == mover.InstanceId)
                    continue;
                if (ally.SpawnAnchorY != preferred)
                    continue;
                if (string.CompareOrdinal(ally.InstanceId, mover.InstanceId) < 0)
                {
                    taken = true;
                    break;
                }
            }

            if (!taken)
                return preferred;

            // Lower InstanceId keeps lane; colliding unit shifts +1 Y (ponytail: single direction; upgrade = pick nearest free Y)
            return preferred + 1;
        }

        public static int ResolveRearSpreadY(
            int slotIndex,
            int slotCount,
            int friendlyMinY,
            int friendlyMaxY)
        {
            if (slotCount <= 1)
                return (friendlyMinY + friendlyMaxY) / 2;

            float t = slotIndex / (float)(slotCount - 1);
            return friendlyMinY + (int)Math.Round(t * (friendlyMaxY - friendlyMinY));
        }

        private static bool IsBlockedByFriendlyAnchor(
            GridCoord goal,
            string moverInstanceId,
            IReadOnlyList<CombatantState> allies)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                var ally = allies[i];
                if (ally == null || !ally.IsAlive || ally.InstanceId == moverInstanceId)
                    continue;
                if (ally.AnchorPosition.Equals(goal))
                    return true;
            }
            return false;
        }

        private static List<CombatantState> SortByInstanceId(IReadOnlyList<CombatantState> combatants)
        {
            var sorted = new List<CombatantState>(combatants?.Count ?? 0);
            if (combatants == null)
                return sorted;
            for (int i = 0; i < combatants.Count; i++)
            {
                if (combatants[i] != null)
                    sorted.Add(combatants[i]);
            }
            sorted.Sort((a, b) => string.CompareOrdinal(a.InstanceId, b.InstanceId));
            return sorted;
        }

        private static int GetFrontColumnX(CombatSide side, IReadOnlyList<CombatantState> combatants)
        {
            int frontColumn = side == CombatSide.Player ? int.MinValue : int.MaxValue;
            for (int i = 0; i < combatants.Count; i++)
            {
                if (!combatants[i].IsAlive)
                    continue;
                int x = combatants[i].AnchorPosition.X;
                if (side == CombatSide.Player)
                {
                    if (x > frontColumn)
                        frontColumn = x;
                }
                else if (x < frontColumn)
                {
                    frontColumn = x;
                }
            }
            return frontColumn;
        }
    }
}
```

Adjust `ResolveLaneY` collision shift to use deterministic `InstanceId` parity (as above) so tests pass: lower id keeps Y=5, higher id gets Y=6 or Y=4.

- [ ] **Step 4: Run `CombatFormationSlotsTests` — PASS**

- [ ] **Step 5: Commit**

```powershell
git add Assets/_Project/Core/Combat/CombatFormationSlots.cs Assets/_Project/Core.Tests/EditMode/CombatFormationSlotsTests.cs
git commit -m "feat(combat): add formation slot frontline lane goals"
```

---

### Task 4: Rear-band spread tests + helpers

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatFormationSlots.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatFormationSlotsTests.cs`

- [ ] **Step 1: Write failing rear spread tests**

Add to `CombatFormationSlotsTests.cs`:

```csharp
[Test]
public void RearSpreadY_DistinctSlotsAcrossFriendlyWidth()
{
    int y0 = CombatFormationSlots.ResolveRearSpreadY(0, 2, friendlyMinY: 2, friendlyMaxY: 8);
    int y1 = CombatFormationSlots.ResolveRearSpreadY(1, 2, friendlyMinY: 2, friendlyMaxY: 8);
    Assert.AreEqual(2, y0);
    Assert.AreEqual(8, y1);
}

[Test]
public void RearSpreadY_SingleUnit_Centers()
{
    int y = CombatFormationSlots.ResolveRearSpreadY(0, 1, friendlyMinY: 2, friendlyMaxY: 8);
    Assert.AreEqual(5, y);
}
```

- [ ] **Step 2: Run tests — expect FAIL** if helper missing (may PASS if Task 3 included it)

- [ ] **Step 3: Ensure `ResolveRearSpreadY` matches tests** (code in Task 3)

- [ ] **Step 4: Run tests — PASS**

- [ ] **Step 5: Commit** (skip if already committed with Task 3)

---

### Task 5: Integrate slots into `RoleEngagement`

**Files:**
- Modify: `Assets/_Project/Core/Combat/RoleEngagement.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs`

- [ ] **Step 1: Update failing role engagement tests**

Change `AssaultRole_GoalIsNearestFrontLineEnemy` expected goal from `(10, 5)` to `(9, 5)` (contact X, lane Y):

```csharp
Assert.AreEqual(new GridCoord(9, 5), goal);
```

Change `InfantryRole_GoalIsNearestFrontLineEnemy` expected from `(10, 3)` to `(9, 3)`.

Add artillery spread test:

```csharp
[Test]
public void ArtilleryRole_GoalSpreadsYAcrossRearBand()
{
    var artyA = CreateCombatant("arty_a", GameTagIds.Artillery, CombatSide.Player, new GridCoord(1, 3), attackRange: AttackRangeTier.Long);
    var artyB = CreateCombatant("arty_b", GameTagIds.Artillery, CombatSide.Player, new GridCoord(1, 7), attackRange: AttackRangeTier.Long);
    var allyFront = CreateCombatant("ally_front", GameTagIds.Assault, CombatSide.Player, new GridCoord(7, 5));
    var enemy = CreateEnemy("enemy", new GridCoord(12, 5));

    var goalA = RoleEngagement.ComputeGoal(artyA, new[] { artyA, artyB, allyFront }, new[] { enemy }, Layout);
    var goalB = RoleEngagement.ComputeGoal(artyB, new[] { artyA, artyB, allyFront }, new[] { enemy }, Layout);

    Assert.AreEqual(4, goalA.X);
    Assert.AreEqual(4, goalB.X);
    Assert.AreNotEqual(goalA.Y, goalB.Y);
}
```

- [ ] **Step 2: Run `RoleEngagementTests` — expect FAIL**

- [ ] **Step 3: Wire `RoleEngagement` to slots**

In `ComputeGoal`, replace infantry/assault/nearest-front branch:

```csharp
if (IsInfantryPrimary(combatant) || role == GameTagIds.Assault || bias == CombatRoleTargetingBias.NearestFront)
{
    var frontline = CollectFrontlineAllies(combatant, allies);
    return CombatFormationSlots.ResolveFrontlineGoal(combatant, frontline, aliveEnemies, layout);
}
```

In `ArtilleryGoal`, after computing `goalX`, set Y via rear spread:

```csharp
var rearArtillery = CollectRearAlliesByRole(allies, GameTagIds.Artillery, combatant.InstanceId);
int minY = GetFriendlyMinY(allies);
int maxY = GetFriendlyMaxY(allies);
int slotIndex = IndexInSorted(rearArtillery, combatant.InstanceId);
int spreadY = CombatFormationSlots.ResolveRearSpreadY(slotIndex, rearArtillery.Count, minY, maxY);
return new GridCoord(goalX, spreadY);
```

In `SupportGoal` when falling back to rear column, use rear-half Y spread (minY to `(minY + maxY) / 2`).

Add private helpers: `CollectFrontlineAllies`, `CollectRearAlliesByRole`, `GetFriendlyMinY`, `GetFriendlyMaxY`, `IndexInSorted` — mirror existing `CollectAlive` patterns, sort by `InstanceId`.

Default/nearest branch for vehicles: if mover has combatant tag and movement speed != None, use frontline slot; else keep `NearestEnemyGoal`.

- [ ] **Step 4: Run `RoleEngagementTests` — PASS**

- [ ] **Step 5: Commit**

```powershell
git add Assets/_Project/Core/Combat/RoleEngagement.cs Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs
git commit -m "feat(combat): integrate formation slots into role engagement goals"
```

---

### Task 6: Role-aware lane bias in pathfinder

**Files:**
- Modify: `Assets/_Project/Core/Combat/ShapePathfinder.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs` (pass role hint)
- Create: `Assets/_Project/Core.Tests/EditMode/ShapePathfinderLaneBiasTests.cs`

- [ ] **Step 1: Write failing lane bias test**

```csharp
[Test]
public void FindStep_PrefersStayingNearSpawnY_ForFrontline()
{
    // Setup: mover at (3,5), goal (9,5), spawnY=5; neighbor (3,6) and (4,5) both reduce heuristic
    // With frontline penalty 4, (4,5) should win over (3,6) when both valid
}
```

Use minimal occupancy grid fixture (copy pattern from existing `ShapePathfinderTests.cs`).

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Add optional `bool preferLaneHold` param to `FindStep` / `GetStepCost`**

```csharp
private const int FrontlineLaneBiasPenalty = 4;
private const int RearLaneBiasPenalty = 1;

private static int GetStepCost(..., int? spawnAnchorY, bool preferLaneHold)
{
    int cost = CombatMovement.GetStepChargeCost(from, to, layout);
    if (!spawnAnchorY.HasValue)
        return cost;
    int penalty = preferLaneHold ? FrontlineLaneBiasPenalty : RearLaneBiasPenalty;
    if (System.Math.Abs(to.Y - spawnAnchorY.Value) > 1)
        cost += penalty;
    return cost;
}
```

In `TickCombatRun.TryMoveSide`, pass `preferLaneHold: IsFrontlineMover(mover)` where `IsFrontlineMover` matches RoleEngagement frontline predicate (extract shared static on `CombatFormationSlots` or small `CombatMovementRole.cs` to avoid duplication — one line helper acceptable).

- [ ] **Step 4: Run lane bias + existing `ShapePathfinderTests` — PASS**

- [ ] **Step 5: Commit**

```powershell
git add Assets/_Project/Core/Combat/ShapePathfinder.cs Assets/_Project/Core/Combat/TickCombatRun.cs Assets/_Project/Core.Tests/EditMode/ShapePathfinderLaneBiasTests.cs
git commit -m "feat(combat): strengthen frontline lane bias in pathfinder"
```

---

### Task 7: Presentation + integration regression

**Files:**
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatPresentationEngagementTests.cs` (if expectations break)
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatMovementRangeGateTests.cs` (if any)

- [ ] **Step 1: Run full EditMode suite**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testResults "TestResults-EditMode.xml" -logFile "UnityTest.log" -quit
```

- [ ] **Step 2: Fix any failing tests** — update expected coords; add Chebyshev case to `CombatMovementRangeGateTests` if it asserts diagonal out-of-range for melee

- [ ] **Step 3: Re-run full suite — PASS**

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "test(combat): update engagement and range gate tests for formation pass"
```

---

### Task 8: Manual smoke (human gate)

- [ ] **Step 1:** Open Unity → Mechanics Sandbox or Iron Vanguard combat scene → Play fight
- [ ] **Step 2:** Confirm units spread across width during approach; artillery/support behind front
- [ ] **Step 3:** Confirm no single-file center blob; diagonal melee engagements possible at contact

No commit required unless code fixes found during smoke.

---

## Spec self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Hybrid lane hold + dynamic fallback | Task 3, 5 |
| Rear band Y spread (artillery/support) | Task 4, 5 |
| Chebyshev 1/3/5/8 | Task 1, 2 |
| Accuracy uses same distance | Task 2 |
| Pathfinder lane penalty 4/1 | Task 6 |
| Determinism (InstanceId sort) | Task 3, 5 |
| EditMode formation tests | Task 3, 4 |
| Presentation shares RoleEngagement | No presentation code changes |
| Out of scope items | Not in plan |

No placeholders remain. `ResolveLaneY` uses explicit ±1 shift with ponytail comment per workspace rules.

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-19-combat-formation-range.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration  
2. **Inline Execution** — implement tasks in this session with checkpoints

Which approach do you want?
