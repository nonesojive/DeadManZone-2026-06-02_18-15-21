> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Critical Mass System — Design Spec

**Date:** 2026-06-21  
**Branch:** `critmass+synergyworkv1`  
**Status:** Approved

## Goals

- Implement all 30 critical mass rules from planning doc with **highest tier only** (TFT-style).
- **ScriptableObject database** for Inspector balancing without recompile.
- Modular: add rules by adding rows to the database asset.
- **Buff strip + meta tracking** for player feedback. **No card hints.**

## Architecture

### Data (Unity)

- `CriticalMassDatabaseSO` — single asset at `Resources/DeadManZone/CriticalMassDatabase.asset`
- `CriticalMassRuleEntry` — serializable row: id, count tag/category, tiers[], stat, mod type, scope, target filter
- Editor menu **DeadManZone/Generate Critical Mass Database** seeds all 30 rules from planning defaults

### Core (Unity-free)

- `CriticalMassEngine` — count board, resolve tiers, build snapshot, apply combat/run bonuses
- `CriticalMassRuleDefinition` / `CriticalMassTier` / `CriticalMassTargetFilter` — pure structs
- `CriticalMassRuleSource` — test injectable; runtime loads SO from Resources

### Combat integration

Fight-start snapshot (immutable mid-fight):

| Stat | Application |
|------|-------------|
| MaxHp flat/percent | Spawn: adjust `CurrentHp` |
| Damage flat | `CombatantState.DamageBonus` |
| Damage percent | Multiply in `CombatDamageResolver` |
| Accuracy percent | Add in `CombatAccuracyResolver.Resolve` |
| AttackSpeed steps | Step tier in cooldown calc |
| MovementSpeed steps | `MoveChargePercentBonus` (5% per step ponytail) |
| AttackRange steps | Step tier in range calc |
| Authority | Added to fight-start authority |
| Supplies flat | Granted at fight start |
| Supplies percent | ponytail: stored; future shop hook |

### UI

- `BuffStripEvaluator` — tier progress, active/near-miss from snapshot
- `CriticalMassIconsSO` — maps rule id → sprite from `Assets/critmassicons`
- `BuffIconStripView` — shows sprites when available
- Remove `CriticalMassHint` from card view model / binding

### Meta

- `RecordCriticalMassIfTriggered` — fires when any rule reaches tier 1+

## Tier rule

At count 7 with thresholds 5/7/10 → tier 2 magnitude only (+15 HP), not cumulative.
