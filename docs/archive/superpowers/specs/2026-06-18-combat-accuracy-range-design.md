> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Combat Accuracy & Range Design Spec

**Date:** 2026-06-18  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-04-deadmanzone-combat-sim-completion-design.md`, `2026-06-16-combat-rework-v2-design.md`  
**Scope:** Four-band attack range, hybrid accuracy with hit / graze / miss outcomes, event log + presentation hooks, content migration

---

## Summary

Combat currently applies **100% hit rate** whenever a target is in range and off cooldown. Range uses **three tiers** (Short 1 / Medium 3 / Long 6). This pass adds **accuracy** (full hit, graze, clean miss) and expands range to **four tiers** (Melee 1 / Short 3 / Medium 5 / Long 8) to improve combat feel and positioning pressure.

**Locked decisions:**

| Area | Choice |
|------|--------|
| Accuracy model | **Hybrid** — base from attack type + role table, optional per-piece override; distance falloff; future tactic/ability modifiers |
| Outcomes | **Full hit**, **graze (33% damage)**, **clean miss (0 damage, cooldown spent)** |
| Graze band | **Widens with distance** — near zero at point-blank; **2× baseline** at max range |
| Accuracy falloff | **Soft cap** — no drop in inner **60%** of max range; linear drop in outer **40%** to floor |
| Range tiers | **Melee 1 · Short 3 · Medium 5 · Long 8** (Manhattan) |
| Implementation | **`CombatAccuracyResolver`** (pure C#) called from `TickCombatRun.ResolveAttacks` |
| Abilities (v1) | Grenade / cannon / strike stay **direct damage** — no accuracy roll |

---

## Section 1 — Goals

### Purpose

- Make **closing distance** and **range band choice** matter without adding player micro.
- Long-range fire feels **suppressive** (grazes) rather than laser-accurate.
- Point-blank engagements feel **decisive** (mostly hits, few grazes).
- Misses still **burn cooldown** so wasted volleys create tension.
- Preserve **determinism** (seeded `Rng`, same log for save/replay).

### Success criteria

1. EditMode tests prove hit/graze/miss math, distance falloff, and graze band scaling.
2. Existing range-gated targeting and movement tests updated for four tiers.
3. Event log distinguishes `damage`, `graze`, and `miss`; replay/health tracking unchanged for damage paths.
4. Content migration maps old three-tier enum to new four-tier enum without silent range regressions on artillery/snipers.
5. Manual smoke: a seeded fight shows visible mix of full hits, grazes, and misses at long range.

---

## Section 2 — Attack range

### Tier table

| Tier | Manhattan cells | Typical use |
|------|-----------------|-------------|
| **Melee** | 1 | Bayonets, knives |
| **Short** | 3 | Rifle squads, trench distance |
| **Medium** | 5 | MG teams, medium support |
| **Long** | 8 | Artillery, snipers, field guns |

### Code changes

- `AttackRangeTier` enum: `Melee, Short, Medium, Long` (replaces `Short, Medium, Long`).
- `CombatRange.GetRangeCells` returns values above.
- All consumers updated: `TacticTargeting`, `RoleEngagement`, `SynergyEngine` (AttackRange stat), tests, Unit Creator, `PieceDefinitionSO`.

### Content migration

Automated pass (editor menu or script):

| Old tier | Old cells | New tier | New cells |
|----------|-----------|----------|-----------|
| Short | 1 | **Melee** | 1 |
| Medium | 3 | **Short** | 3 |
| Long | 6 | **Medium** | 5 |

**Auto-bump to Long (8):** any piece with combat role `Artillery` or `Sniper`. Manual review for field guns and demo long-range units.

---

## Section 3 — Accuracy model

### Base accuracy (Hybrid C)

1. Lookup **attack type + combat role** in `CombatAccuracyDefaults`.
2. If `PieceDefinition.AccuracyOverride` is set (nullable int 0–100), use override instead of table value.

**Starting default table (tunable in `CombatAccuracyConfig` / defaults class):**

| Source | Base accuracy |
|--------|---------------|
| Melee attack type | 92 |
| Sniper role | 88 |
| Ballistic (default) | 78 |
| Piercing | 80 |
| Explosive / Artillery role | 72 |
| Shredding | 68 |

Role stacks on attack type where both apply (e.g. Ballistic + Sniper = 88, not 78).

### Distance falloff (Soft cap D)

Let `d` = Manhattan distance attacker → target, `R` = `CombatRange.GetRangeCells(attacker.AttackRange)`.

- **Inner zone** (`d ≤ 0.6 × R`): effective accuracy = base (no falloff).
- **Outer zone** (`d > 0.6 × R`): linear interpolation from base at `0.6 × R` down to **floor** at `d = R`.
- **Floor:** `max(40, round(base × 0.5))` (constants in `CombatAccuracyConfig`).

### Graze band (Distance widens D)

Single roll `1–100` after computing effective accuracy `A` (clamped 0–100, includes future modifiers).

- **Hit:** `roll ≤ A` → full damage via `CombatDamageResolver`.
- **Graze:** `roll ≤ A + grazeBand(d)` → `max(1, round(fullDamage × 0.33))`.
- **Miss:** otherwise → 0 damage.

`grazeBand(d)`:

- At `d = 1` (minimum engagement): **2** points.
- At `d = R`: **2 × baseline** (baseline **12** → max band **24**).
- Linear interpolation between `d = 1` and `d = R`.

### Future modifiers (v1 stub)

`AccuracyModifierCollector.Collect(...)` returns **0** in v1. Later: tactics (e.g. Disciplined Fire +accuracy), abilities, cover/smoke. Applied to `A` before roll; clamp 0–100.

### Cooldown

**Always spent** on hit, graze, and miss (same as current hit path).

---

## Section 4 — Combat integration

### New Core types

| Type | Role |
|------|------|
| `CombatAccuracyConfig` | Tunable constants (falloff ratio, graze baseline/multiplier, graze damage ratio, accuracy floor) |
| `CombatAccuracyDefaults` | Attack type + role → base accuracy |
| `CombatAccuracyResolver` | Pure resolver: `Resolve(Rng, attacker, target, distance, modifiers)` → `CombatAttackOutcome` |
| `CombatAttackOutcome` | `Hit / Graze / Miss`, `Damage`, `Roll`, `EffectiveAccuracy` |
| `AccuracyModifierCollector` | v1 stub; extension point for tactics/abilities |

### `TickCombatRun.ResolveAttacks` flow

```
1. Select target (unchanged — TacticTargeting + CombatRange.IsInRange)
2. distance = CombatRange.Manhattan(attacker, target)
3. modifiers = AccuracyModifierCollector.Collect(...)  // 0 v1
4. outcome = CombatAccuracyResolver.Resolve(_rng, ...)
5. Log: "damage" | "graze" | "miss" with appropriate Value
6. If outcome.Damage > 0: apply HP, update damage dealt/taken stats
7. Set cooldown (always)
```

Abilities executed via `CombatAbilityExecutor` remain direct damage in v1.

---

## Section 5 — Event log & presentation

### Action types

| ActionType | Value | HP | Presentation |
|------------|-------|-----|--------------|
| `damage` | full hit | yes | Existing muzzle + impact (unchanged) |
| `graze` | graze damage | yes | Same attack anim; lighter/smaller impact VFX |
| `miss` | 0 | no | Muzzle/tracer only; no impact; optional whiz SFX |

### Consumers

- `ArmyHealthReplayTracker` — treat `graze` like `damage`.
- `CombatLogFormatter` — human-readable graze/miss lines.
- `CombatArenaPresenter.ApplyEventVisual` — `graze` → lighter impact; `miss` → muzzle only.

Determinism: roll order follows existing attack iteration (`InstanceId` sort).

---

## Section 6 — Data authoring

### `PieceDefinition`

- Add `int? AccuracyOverride` (null = defaults table).

### `PieceDefinitionSO` / Unit Creator

- Optional accuracy override field in inspector.

### GDD

- Update Appendix B range table and add accuracy section after implementation lands.

---

## Section 7 — Testing

### New EditMode — `CombatAccuracyResolverTests`

- Point-blank high accuracy → high hit rate, minimal graze/miss.
- Max range → wider graze band, lower effective accuracy, more misses.
- Graze damage = `max(1, round(full × 0.33))`.
- Miss = 0 damage.
- Same seed → same outcomes.
- `AccuracyOverride` beats default.

### Updated EditMode

- `CombatRangeTests` — four tiers, new cell values.
- `CombatMovementRangeGateTests` — Melee vs Short naming/cells.
- `RoleEngagementTests` — Medium 5, Long 8 goals.

### Integration smoke

- `TickCombatRun` fixture: seeded log contains expected `damage` / `graze` / `miss` mix.

### Optional

- `CombatLogFormatterTests` for new action types.

Run via project TDD conventions (`Assets/_Project/Core.Tests/EditMode/`).

---

## Section 8 — Scope boundaries

### In v1

- Four range tiers + enum migration
- Accuracy resolver, defaults, override, modifier stub
- Hit / graze / miss in sim, log, basic presentation
- Tests and demo content migration

### Out of v1

- Tactic accuracy modifiers
- Ability accuracy effects
- Cover / smoke / elevation penalties
- Accuracy on AoE abilities
- Graze/miss presentation juice beyond lighter impact

---

## Section 9 — Risks

| Risk | Mitigation |
|------|------------|
| Fights feel too RNG-heavy | Inner 60% reliable; tune graze band and floor |
| Miss streak frustration | Cooldown cost is intentional; tune base accuracy in balance pass |
| Migration drops artillery range | Role-based auto-bump to Long |
| Flat miss presentation | Acceptable v1; spectacle pass can add whiz-by VFX |

---

## Section 10 — Implementation approach

**Recommended:** Approach 2 from brainstorming — dedicated `CombatAccuracyResolver` + outcome struct, thin `TickCombatRun` call site. Matches `CombatDamageResolver` / `CombatRange` patterns; EditMode-testable; clean hook for tactics/abilities without a full synergy-style pipeline.

**Deferred:** Inline rolls in `ResolveAttacks` (too brittle); full modifier pipeline framework (YAGNI until tactic hooks ship).

---

*Next step: implementation plan via `writing-plans` skill after spec review.*
