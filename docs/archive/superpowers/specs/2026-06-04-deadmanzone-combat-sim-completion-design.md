> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Combat Sim Completion Design Spec

**Date:** 2026-06-04  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-04-deadmanzone-combat-units-demo-design.md`, `2026-06-04-deadmanzone-tutorial-balance-pass-design.md`  
**Scope:** Wire missing combat stat plumbing, widen neutral no-man's-land to 7 columns — **pause tutorial balance tuning**

---

## Summary

Fights are ending too early during the grind segment (~tick 130) because the sim ignores `MovementSpeed` and `AttackSpeed` on piece data — every combatant moves one cell per tick. This pass makes the combat sim match the approved demo spec **before** another balance pass.

**Locked decisions:**

| Area | Choice |
|------|--------|
| Priority | **Sim completeness first** — pause tutorial balance tuning |
| Neutral zone | **7 columns** → **25-wide** battlefield (9 + 7 + 9) |
| Movement feel | **Tier-based** by piece; basic rifleman ~2–3 cells in 50-tick opening |
| Movement system | **Move-charge budget** with terrain step costs (neutral 2×) |
| Tactic speed bias | **Deferred** — future hook when tactics deepen |
| Content rebalance | **Paused** — keep current HP/damage assets; no `TutorialBalanceTests` gating |

---

## Section 1 — Scope & goals

### Purpose

Fix pacing at the source (movement + range closure) rather than by inflating HP/damage. Establish a trustworthy sim baseline for the next tutorial balance pass.

### In scope

- Neutral width **7** (config constant, default for all fights)
- Wire **movement speed** (charge budget + terrain cost + move only when out of attack range)
- Wire **attack speed** (cooldown multiplier on `CooldownTicks`)
- Confirm existing wiring for attack range, armor/RPS, and system tags (`Combatant`, `HQ`, `Command`)
- Headless tests for movement and attack-speed behavior
- Presentation adapts via existing `TotalWidth` usage

### Out of scope

- Tutorial fights 1–3 rebalance, spongy HP revert, `TutorialBalanceTests` thresholds
- Tactic-based movement speed modifiers (`Advance` faster, `Stand Ground` slower)
- Tag synergies, adjacency keyword rules, new keywords
- Player board zone resize (still 4 rear / 3 support / 2 front per half)
- Gas damage retune for wider neutral band

### Success criteria

1. Basic rifleman (Medium movement) advances **~2–3 cells** in a 50-tick opening from a typical support spawn (headless test, seeded)
2. High-tier scouts noticeably outpace Low/heavy pieces in the same fixture
3. Neutral columns cost **2×** charge vs friendly ground (measurable in test)
4. Fast vs Slow attack speed produces spec cooldown difference on same base `CooldownTicks`
5. Fights no longer routinely end in the first third of grind **purely from instant cross-board movement** (manual smoke check; no % target this pass)

---

## Section 2 — Battlefield layout

### Combined battlefield

```
[ Player 9: Rear(4) | Support(3) | Front(2) ] [ Neutral 7 ] [ Enemy 9: Front(2) | Support(3) | Rear(4) ]
|<------------------- PlayerHalfWidth=9 ------------------->| x=9..15 |<--- EnemyOriginX=16 --- ... total=25
```

Per-half zone bands are unchanged. Only the contested neutral gap widens.

### Configuration

New `CombatBattlefieldConfig` (alongside `CombatPacingConfig`):

| Constant | Value |
|----------|-------|
| `NeutralColumnCount` | **7** |
| `PlayerHalfWidth` | derived from `FactionSO.boardWidth` (9 today) |
| `TotalWidth` | `boardWidth + NeutralColumnCount + boardWidth` → **25** |

### Code touchpoints

| Area | Change |
|------|--------|
| `BattlefieldLayout.FromPlayerBoard` | Default neutral width reads `CombatBattlefieldConfig.NeutralColumnCount` (was hardcoded `2`) |
| `BattlefieldState.FromBoards` | Pass config through; single source of truth |
| `BattlefieldZoneMap` | Neutral band widens via `IsNeutralColumn`; no zone logic rewrite |
| `GasDamageSystem` | Still uses `IsNeutralColumn`; wider neutral = more gas-exposed columns during ramp (intentional) |
| `BoardView` | Already uses `TotalWidth`; no layout hack expected |
| Special tiles | Player-board coords (x=1,4,7) unchanged; no neutral landmarks this pass |

### Tests

- `TotalWidth == 25`, `NeutralWidth == 7`
- Enemy HQ mirrors to `EnemyOriginX + (boardWidth - 1)`
- Neutral columns occupy `x ∈ [9, 15]` for current 9-wide halves

---

## Section 3 — Movement & attack-speed sim

### Root cause

`TickCombatRun.TryMoveSide` moves every living `Combatant`-tagged unit one Manhattan cell per tick with no `MovementSpeed` gating. `CooldownRemaining` resets to raw `CooldownTicks` with no `AttackSpeed` multiplier.

### Movement — move-charge budget

**New state on `CombatantState`:**

- `MoveCharge` (int fixed-point accumulator)

**New module:** `CombatMovementSpeed.cs`

| Tier | Charge gained per tick | ~50-tick opening (normal ground) |
|------|------------------------|----------------------------------|
| None | 0 | never moves |
| Low | +3 | ~1–2 cells |
| Medium | +5 | ~2–3 cells |
| High | +6 | ~3–4 cells |

**Step costs** via `CombatMovement.GetMoveCost` scaled to fixed-point:

| Terrain | Cost |
|---------|------|
| Friendly or enemy zone step | **100** |
| Step into or out of neutral column | **200** |

**Move attempt rules:**

1. `MovementSpeed != None`
2. **No living enemy within attack range** (Manhattan, piece `AttackRange` tier)
3. Step one cell toward **nearest** living enemy (Manhattan distance; tie-break by `InstanceId`)
4. If `MoveCharge >= stepCost`, subtract cost and apply step
5. **Max one step per unit per tick** (replay readability)

**Tick order** (unchanged): player moves → enemy moves → player attacks → enemy attacks.

**Static units:** `MovementSpeed.None` — never move; attack only when in range.

**Future hook (not implemented):**

```csharp
// CombatMovementSpeed.GetChargePerTick(MovementSpeedTier tier, TacticType tactic)
// Advance +10%, Stand Ground -10%, etc. — wired in tactics pass
```

Document as comment or stub signature only.

### Attack speed

**New module:** `CombatAttackSpeed.cs`

| Tier | Multiplier on `CooldownTicks` |
|------|-------------------------------|
| Slow | ×1.5 (round up) |
| Medium | ×1.0 |
| Fast | ×0.75 (round down, min 1) |

Apply when resetting `CooldownRemaining` after an attack in `TickCombatRun`. Mirror in `PhasedCombatRun` if still referenced by tests.

### Tags / keywords

No new mechanics this pass. Existing usage:

| Tag | Use |
|-----|-----|
| `Combatant` | Win checks, movement eligibility |
| `HQ` | Instant loss when destroyed |
| `Command` | Protect Support tactic unlock |

Auto-inject `Combatant` on units/buildings in `PieceDefinitionSO` unchanged.

**Deferred:** synergy tags, adjacency keyword rules, editor taxonomy validation.

### Content data

Keep current piece assets (including tutorial HP inflation). Verify demo pieces have sensible movement/attack speed tier enums after sim wiring. **No HP/damage retune** this pass.

---

## Section 4 — Testing & verification

| Test file | Asserts |
|-----------|---------|
| `CombatBattlefieldConfigTests` | Neutral = 7, total width = 25 |
| `CombatMovementSpeedTests` | Medium ~2–3 cells in 50-tick opening; neutral 2× cost; High > Medium > Low |
| `CombatAttackSpeedTests` | Slow/Fast cooldown vs Medium |
| `CombatMovementRangeGateTests` | In-range unit holds; out-of-range unit moves |
| `BattlefieldStateTests` (update) | Neutral indices for width 7 |

**Not gating this pass:** `TutorialBalanceTests` pause #2 / survival thresholds.

**Manual smoke:** Fight 1 — slow opening advance across wide gap; grind should not collapse ~tick 130 from movement alone.

---

## Section 5 — Implementation notes

### File map (expected)

| File | Action |
|------|--------|
| `Core/Combat/CombatBattlefieldConfig.cs` | **New** — `NeutralColumnCount = 7` |
| `Core/Combat/CombatMovementSpeed.cs` | **New** — charge rates, step costs, future tactic hook |
| `Core/Combat/CombatAttackSpeed.cs` | **New** — cooldown multiplier |
| `Core/Combat/CombatantState.cs` | Add `MoveCharge` |
| `Core/Combat/TickCombatRun.cs` | Wire charge movement + attack speed |
| `Core/Combat/CombatMovement.cs` | Expose scaled step cost helper |
| `Core/Board/BattlefieldLayout.cs` | Default neutral from config |
| `Core.Tests/EditMode/*` | New/updated tests per Section 4 |

### Relationship to tutorial balance pass

The tutorial balance pass remains valid for economy and enemy composition but **stat thresholds and spongy HP are stale** until this sim pass lands and a follow-up balance pass runs on the corrected movement model.

### Supersedes (partial)

This spec **replaces** the demo spec movement interval table (move every 1/2/3 ticks) with the move-charge model and tuned rates above. Attack range, attack speed, and armor/RPS tables from the demo spec remain authoritative.

---

## Appendix — Brainstorming decisions log

| Question | Answer |
|----------|--------|
| Neutral zone width | 7 columns, 25 total |
| Opening travel target | Tier-based (C); rifleman ~2–3 cells |
| Tactic movement bias | Noted; deferred |
| Work priority | Sim completeness first (A); balance paused |
