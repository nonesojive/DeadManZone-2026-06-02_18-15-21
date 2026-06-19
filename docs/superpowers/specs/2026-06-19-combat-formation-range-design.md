# DeadManZone — Combat Formation Spread & Chebyshev Range Design

**Date:** 2026-06-19  
**Branch:** `combatworkv4`  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-18-combat-accuracy-range-design.md`, `2026-06-16-combat-rework-v2-design.md`  
**Reference:** Top Troops idle combat — wide contact line, rear units visibly behind, light center-pull

---

## Summary

Combat units currently funnel into a **center blob**: infantry share a single front-column enemy anchor, pathfinding is orthogonal-only with weak lane bias, and rear roles adjust depth (X) but not lateral spread (Y). Range tiers are already **1 / 3 / 5 / 8** but measured as **Manhattan** distance, so diagonal neighbors are harder to engage than orthogonal ones.

This pass adds a **hybrid formation model** (lane hold + dynamic slot fallback + rear bands) and switches range measurement to **Chebyshev** (`max(|dx|, |dy|)`) so tiers behave like square/circular radii on the grid.

**Locked decisions:**

| Area | Choice |
|------|--------|
| Spread model | **Hybrid (C)** — infantry hold build lanes; dynamic slots when blocked/empty; rear roles in depth bands with lateral spread |
| Formation layer | **`CombatFormationSlots`** — deterministic per-side slot assignment each movement tick |
| Range metric | **Chebyshev** at tiers Melee 1 · Short 3 · Medium 5 · Long 8 (tier cell counts unchanged) |
| Distance for accuracy | Same Chebyshev metric via `CombatRange.Distance` (single source of truth) |
| Sim vs presentation | **Sim-only change** — `RoleEngagement` + `CombatPresentationEngagement` share slot logic; no presentation-only fudge |
| Pathfinder | Increase frontline `LaneBiasPenalty` 2 → 4; rear roles use penalty 1 |
| Implementation order | TDD — failing EditMode tests first, then `CombatFormationSlots`, then `RoleEngagement` integration |

---

## Section 1 — Goals

### Purpose

- Combat reads like Top Troops: units advance on a **wide contact line**, not a single-file march to board center.
- Long-range and support units stay **visibly behind** the front with spread across friendly width.
- Diagonal engagement at each range tier feels natural (melee hits all eight neighbors at distance 1).
- Preserve **determinism** for async PvP replay (seeded sim, stable sort by `InstanceId`).

### Success criteria

1. Infantry on different build rows remain on distinct Y bands through approach (±1 cell under normal conditions; ±2 only when band has no valid target).
2. When a lane goal is blocked by a friendly, the unit takes the **nearest open slot** on the contact row instead of stalling in a stack.
3. Artillery/support occupy **rear depth bands** with Y spread derived from slot index, not a single column.
4. Chebyshev range: `(1,1)` from origin is **Melee** in range; `(2,0)` is **out of Melee**, **in Short**.
5. EditMode tests for formation slots, updated range tests, and existing role-engagement tests green; no presentation/sim desync.

---

## Section 2 — Formation slot system

### New type: `CombatFormationSlots`

Pure static helper in `Assets/_Project/Core/Combat/`. Called from `RoleEngagement.ComputeGoal` (and nowhere else for goals).

**Inputs per call:** mover, allies, enemies, layout.  
**Output:** `GridCoord` engagement goal for that mover.

### Per-side slot assignment (deterministic)

Each movement tick, before resolving individual goals:

1. Collect alive combatants on the side, sorted by `InstanceId`.
2. Partition by role category:
   - **Frontline movers:** `Combatant` tag + `MovementSpeed != None` + role in {Infantry primary, Assault, default/nearest} (existing `RoleEngagement` frontline rules).
   - **Rear movers:** Artillery, Sniper, Support (existing role branches).
3. Compute **enemy front column X** and **friendly front/rear columns** (reuse existing helpers in `RoleEngagement`).

### Frontline slot rules

| Step | Rule |
|------|------|
| Preferred lane Y | `SpawnAnchorY` (build-phase row) |
| Lane collision | Two frontliners same preferred Y → secondary shifts ±1 Y (lower `InstanceId` keeps preferred Y) |
| Base contact X | Player: `enemyFrontX - 1`; Enemy: `enemyFrontX + 1` |
| Target in band | Nearest alive enemy with `\|enemy.Y - laneY\| <= 1` on front column (or nearest front enemy if none) |
| Center-pull widen | If no enemy in ±1 band, widen to ±2; then fall back to `NearestFrontEnemyGoal` behavior |
| Blocked cell | If goal occupied by friendly footprint, scan same contact X for nearest free Y (±1, ±2, … up to board height); if none, hold position |

### Rear band rules

| Role | Depth (X) | Lateral spread (Y) |
|------|-----------|-------------------|
| **Artillery** | `clamp(enemyFrontX ± maxRange, friendlyRear..friendlyFront)` per side | Index rear movers 0..N-1 across `[friendlyRearY .. friendlyFrontY]` using even spacing (integer Y slots) |
| **Sniper** | Unchanged priority: in-range rear target → else rear column enemy | Keep spawn Y unless blocked; then nearest rear-band slot |
| **Support** | Behind friendly front (existing rule) | Spread across rear half of friendly Y extent by slot index |

### Integration with `RoleEngagement`

- Replace direct `NearestFrontEnemyGoal` / raw enemy anchor returns for frontline with `CombatFormationSlots.ResolveFrontlineGoal(...)`.
- Artillery/Support/Sniper branches call slot helpers for Y spread; X logic stays as today unless slot resolver overrides.
- `CombatPresentationEngagement` unchanged structurally — still delegates to `RoleEngagement`.

### Pathfinder tweak (`ShapePathfinder`)

- `LaneBiasPenalty`: **4** when mover is frontline (infer from role/speed same as frontline set); **1** for rear movers.
- Keeps greedy + BFS behavior; no diagonal movement added.

---

## Section 3 — Range metric (Chebyshev)

### Definition

```csharp
// CombatRange.cs
public static int Distance(GridCoord from, GridCoord to) =>
    Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));

public static bool IsInRange(...) => Distance(from, to) <= GetRangeCells(tier);
```

- **`Manhattan` remains** as a named helper where ordering/nearest-enemy tie-breaks need it (targeting sort, pathfinder heuristic).
- **`Distance`** is the combat range and accuracy distance metric everywhere else.

### Tier table (unchanged cell counts)

| Tier | Cells | Chebyshev shape |
|------|-------|-----------------|
| Melee | 1 | 3×3 square including diagonals |
| Short | 3 | 7×7 square |
| Medium | 5 | 11×11 square |
| Long | 8 | 17×17 square |

### Accuracy integration

- `TickCombatRun.ResolveAttacks`: pass `CombatRange.Distance(...)` instead of `Manhattan`.
- `CombatAccuracyResolver` unchanged — falloff/graze bands use the new distance values automatically.
- **Balance note:** Long range covers more lateral cells than Manhattan Long; intentional. No tier count rebalance in this pass.

### Consumer checklist

| File | Change |
|------|--------|
| `CombatRange.cs` | Add `Distance`; `IsInRange` uses Chebyshev |
| `TickCombatRun.cs` | Accuracy distance |
| `TacticTargeting.cs` | In-range filter (via `IsInRange`) |
| `CombatMovementRules.cs` | In-range check |
| `RoleEngagement.cs` | In-range filter for sniper |
| `CombatRoleTargeting.cs` | Keep Manhattan for furthest/nearest **sort** only |
| Tests | `CombatRangeTests`, any integration tests asserting Manhattan in-range |

---

## Section 4 — Role-specific behavior (unchanged intent, new geometry)

| Role | Movement intent | Change in this pass |
|------|-----------------|---------------------|
| Infantry / Assault | Close on front line | Lane-hold contact goal via slots |
| Artillery | Max range behind friendly front | Add Y spread across rear band |
| Sniper | Rear low-HP preference | Minor: slot fallback when blocked |
| Support | Stay behind friendly front | Add Y spread across rear half |
| Default / Vehicle | Nearest enemy | Frontline slot rules if mover; else nearest |

Targeting biases (`CombatRoleTargeting`) unchanged — only who is **in range** changes via Chebyshev.

---

## Section 5 — Testing

### New EditMode tests — `CombatFormationSlotsTests.cs`

- Frontliners at different `SpawnAnchorY` get different goal Y on same enemy front.
- Two frontliners same spawn Y → second shifts ±1.
- Blocked contact cell → fallback to adjacent Y on same contact X.
- Artillery rear band: two artillery get distinct Y when friendly width allows.
- Determinism: same inputs + sort → same goals.

### Updated tests

- `CombatRangeTests`: Chebyshev in/out cases (diagonal melee, square boundaries).
- `RoleEngagementTests`: adjust expected coords where slot math changes X/Y.
- `CombatAccuracyIntegrationTests`: smoke that distance uses Chebyshev (optional single assert).
- `CombatPresentationEngagementTests`: chase lead still valid with new goals.

### Manual smoke

- Iron Vanguard / sandbox fight: units spread across width during approach; rear MG/artillery visibly behind; no single-file center blob.
- Diagonal melee engagement visible when units offset by (1,1).

### TDD workflow

Per `tdd-iteration` skill: write failing formation + range tests → implement → full EditMode suite.

---

## Out of scope

- Diagonal grid movement steps
- Pre-battle deployment lane UI changes
- Content rebalance pass for Long-range pieces (follow-up if fights swing too hard)
- Presentation-only lateral offsets
- New tactics or pause threshold changes

---

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Chebyshev Long too strong laterally | Monitor playtests; tier counts fixed, content pass deferred |
| Slot fallback oscillation | Hold position when no free contact Y; deterministic scan order |
| Test churn on RoleEngagement expected coords | Update fixtures with documented slot math in test names |

---

## Files touched (implementation)

| File | Action |
|------|--------|
| `CombatFormationSlots.cs` | **Add** |
| `CombatFormationSlotsTests.cs` | **Add** |
| `RoleEngagement.cs` | Integrate slots |
| `CombatRange.cs` | Chebyshev `Distance` |
| `ShapePathfinder.cs` | Role-aware lane penalty |
| `TickCombatRun.cs` | Accuracy distance |
| `CombatRangeTests.cs` | Update |
| `RoleEngagementTests.cs` | Update |

---

## Approval

Brainstorming approved 2026-06-19: hybrid spread model (C), formation slot layer, Chebyshev range at 1/3/5/8.
