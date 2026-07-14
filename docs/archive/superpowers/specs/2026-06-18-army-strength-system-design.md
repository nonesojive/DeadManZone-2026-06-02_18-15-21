> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Army Strength System Design Spec

**Date:** 2026-06-18  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-06-deadmanzone-master-design.md`, `2026-06-18-combat-accuracy-range-design.md`  
**Reference:** Top Troops fist-icon army strength (matchup preview)

---

## Summary

Add a **combat army strength** rating (Top Troops–style) so players can judge matchup difficulty on the build screen and designers can target enemy templates and balance passes with a fast, deterministic metric.

**Locked decisions:**

| Area | Choice |
|------|--------|
| What counts | **Combat fielding set only** — pieces tagged `Combatant` or `Hq` (same as manpower gate); buildings excluded |
| UI | **Two numbers + ratio label** — player vs enemy effective strength; Favorable / Even / Dangerous |
| Rating model | **Hybrid** — **Base** (canonical for tooling) + **Effective** (layout-aware); show `(+N)` synergy delta when non-zero |
| Formula | **Stat-heuristic** in pure C# — HP × DPS proxy from existing combat stats; optional one-time calibration vs reference boards |
| Synergies | Effective rating applies `SynergyEngine.EvaluateFightStart` bonuses per piece |
| Matchup compare | `playerEffective / enemyEffective` |

---

## Section 1 — Goals

### Purpose

- Give players an at-a-glance **matchup read** before Begin Fight (like Top Troops fist totals).
- Provide designers a **canonical base strength** number for enemy template generation and gauntlet curve checks.
- Complement (not replace) `TutorialBalanceFixtures` seed sweeps — strength is the fast pre-filter; sim sweeps remain truth checks.

### Success criteria

1. EditMode tests prove per-piece ordering, fielding filter, synergy bump, and label thresholds.
2. Build screen shows player vs enemy strength + label; refreshes on board/fight changes.
3. Editor menu reports fight 1–10 enemy strength vs reference player board.
4. All existing EditMode tests still pass.

### Non-goals (v1)

- Per-piece strength on shop cards
- Reserves bench strength
- Combat-phase HUD duplicate
- Simulation-derived win-probability strength

---

## Section 2 — Core rating model

### New Core types

| Type | Responsibility |
|------|----------------|
| `CombatStrengthConfig` | Scale factor, effective fire rate, range/ability/HQ weights, matchup ratio thresholds |
| `PieceCombatRating` | Static per-`PieceDefinition` rating with optional synergy modifiers |
| `ArmyStrengthSnapshot` | `{ BaseTotal, EffectiveTotal, SynergyBonus }` for a board |
| `ArmyStrengthCalculator` | Computes snapshot from `BoardState` |
| `MatchupAssessment` | `{ Player, Enemy, Ratio, Label }` + `MatchupLabel` enum |

All live under `Assets/_Project/Core/Combat/` (pure C#, no Unity refs).

### Fielding filter

Reuse manpower fielding semantics: include pieces with tag `Combatant` or `Hq`. Expose as shared helper on `ManpowerCalculator` (`CountsTowardFielding`).

### Per-piece base formula (v1)

```
cooldown       = CombatAttackSpeed.GetEffectiveCooldown(CooldownTicks, AttackSpeed)
accuracyFactor = GetBaseAccuracy(piece) / 100 × EffectiveFireRate   // EffectiveFireRate ≈ 0.75
dps            = (BaseDamage + synergyDamageBonus) / cooldown × accuracyFactor
ehp            = MaxHp / BaselineArmorMultiplier(StepArmor(ArmorType, synergyArmorSteps))
rangeMult      = RangeMultiplier(AttackRange)   // small bump Long > Short > Melee
abilityBonus   = AbilityFlatBonus(GrantedAbility)

HQ:             rating = round(ehp × HqHpWeight + HqCommandBonus)
Other:          rating = round(sqrt(ehp × dps) × Scale × rangeMult + abilityBonus)
```

- `Scale` tuned so mid-game reference boards land in **low thousands** (Top Troops feel).
- Minimum rating 1 for any fielding piece.

### Board totals

- **BaseTotal** — sum of base ratings (no synergies).
- **EffectiveTotal** — sum of ratings with per-piece synergy from `SynergyEngine.EvaluateFightStart(board)`.
- **SynergyBonus** — `EffectiveTotal - BaseTotal` (UI shows `(+N)` only when `> 0`).

### Enemy templates

`EnemyTemplateSO.BuildBoard(faction, registry)` → same calculator. Enemy effective strength includes placement synergies on the template board.

---

## Section 3 — Matchup labels

Compare **effective** totals: `ratio = playerEffective / enemyEffective`.

| Ratio | Label |
|-------|-------|
| ≥ 1.15 | **Favorable** |
| 0.85 – 1.14 | **Even** |
| < 0.85 | **Dangerous** |

Thresholds live in `CombatStrengthConfig` for tuning without code changes to consumers.

---

## Section 4 — Presentation (build screen)

New compact row on top HUD (near fight index / Begin Fight):

```
⚔ 1,240 (+120)  vs  ⚔ 980     ·  Favorable
     You              Enemy
```

- Primary display = **effective** total; `(+N)` when `SynergyBonus > 0`.
- Refreshes via existing `RunHudView.Refresh` / `RunStateChanged` / board change hooks.
- New view component `MatchupStrengthView` wired from `RunHudPanelBuilder` / `RunBuildUiBootstrap`.

**Data flow:**

```
RunOrchestrator / RunManager
  → player BoardState from BoardView
  → enemy BoardState from ContentDatabase.GetEnemyTemplate(fightIndex).BuildBoard(...)
  → ArmyStrengthCalculator + MatchupAssessment
  → MatchupStrengthView
```

---

## Section 5 — Balance & template tooling

### Editor menu: `DeadManZone/Combat Strength Report`

Logs table for fights 1–10:

- Reference player **base** strength (`TutorialBalanceFixtures.BuildReferencePlayerBoard`)
- Each enemy template **base** + **effective**
- Ratio vs reference player effective
- Warn rows outside target band (default 0.7×–1.3× for tutorial fights; configurable)

### Inspector (v1.1 optional)

- `EnemyTemplateSO` custom inspector shows computed strength on asset.

### Tests

| Test class | Focus |
|------------|-------|
| `PieceCombatRatingTests` | Ordering (MG > conscript), HQ counts, building excluded |
| `ArmyStrengthCalculatorTests` | Sums, synergy bump, empty board |
| `MatchupAssessmentTests` | Label boundaries |
| `EnemyTemplateStrengthCurveTests` | All fights within soft bands (report-style, not brittle exact totals until calibration) |

### Calibration (post-v1)

One-time pass: tune `CombatStrengthConfig.Scale` so reference player boards vs fight N enemies produce sensible ratios aligned with `TutorialBalanceFixtures` survival rates.

---

## Section 6 — Architecture notes

- **Layer:** Core sim only for rating math; Game layer may expose helper on `RunOrchestrator`; Presentation reads via `RunManager`.
- **Determinism:** No RNG in strength calc; same board → same numbers.
- **Performance:** O(pieces) per refresh; synergy eval already used on board change — acceptable for build screen.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Heuristic mis-ranks some synergies | Show synergy delta; keep seed sweeps for balance truth |
| Scale feels wrong vs Top Troops | Tunable `Scale`; calibration pass against fights 1–10 |
| Players over-trust the label | Copy/tooltip: "estimate — positioning and tactics still matter" (optional v1.1) |

---

*Approved in brainstorming session 2026-06-18.*
