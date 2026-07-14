> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Tag Vocabulary Rework Design

**Date:** 2026-06-09  
**Status:** Approved — ready for implementation plan  
**Goal:** Replace the demo Synergy/Ability/Flavor tag vocabulary with Attack Type registry entries while keeping Primary, Combat Role, System tags and all downstream systems (SynergyEngine, critical mass, adjacency infrastructure) intact.

**Supersedes (partial):** [2026-06-07-tag-keyword-system-design.md](./2026-06-07-tag-keyword-system-design.md) §2.7 and §6.1 regarding Attack Type as enum-only / icon-only display. Attack Types are now a registry category with player-visible chips.

---

## 1. Scope

### In scope

- Remove demo Synergy tag entries from `TagRegistry` and piece content.
- Add `TagCategory.AttackType` with seven registry entries bound to the `AttackType` enum.
- Expand `AttackType` enum with `Shredding`, `Fire`, `Melee`, `Gas`.
- Update `CombatDamageResolver` matchups to match the design sheet.
- Show Attack Type as a player-visible identity chip (faction-like pattern).
- Clear demo synergy rules, trait thresholds, and the `vanguard` critical-mass synergy rule.
- Content migration: strip old `synergyTags` from piece assets.

### Out of scope

- Removing or rewriting `SynergyEngine`, adjacency filtering, critical mass infrastructure, or synergy UI shell.
- Implementing Undecided-column tags (documented as future candidates only).
- Fire burn status effect (multiplier stays neutral until status system exists).
- Melee matchup rules (neutral multiplier until sheet defines them).
- New synergy, ability, or flavor tags from the old sheet columns.

---

## 2. Unchanged categories

Primary, Combat Role, and System tags remain exactly as defined in the 2026-06-07 spec. No ID or behavior changes.

| Category | Count | Notes |
|----------|-------|-------|
| Primary | 4 | `infantry`, `vehicle`, `building`, `structure` |
| Combat Role | 8 | includes `defender` |
| System | 3 | `combatant`, `noncombatant`, `hq` |

`TagCategory.Synergy`, `Ability`, and `Flavor` enum values **remain** for future content. Registry entries for those categories are **empty** after this rework.

---

## 3. Removed registry entries

The following Synergy tags are removed from `TagRegistry`, `GameTagIds`, demo catalogs, and piece assets:

| Removed ID | Former category |
|------------|-----------------|
| `supply` | Synergy |
| `medic` | Synergy |
| `command` | Synergy |
| `echo` | Synergy |
| `stealth` | Synergy |
| `vanguard` | Synergy |
| `mechanical` | Synergy |
| `gas` | Synergy |

No Ability or Flavor tags were registered in code; nothing to remove there.

---

## 4. New category: Attack Type

### 4.1 Pattern (faction-like)

| Concern | Source of truth | Registry role |
|---------|-----------------|---------------|
| Authoring | `PieceDefinition.AttackType` enum field on `PieceDefinitionSO` | Validates enum value exists |
| Combat math | Enum value → `AttackTypeProfile` lookup | Tooltips mirror profile text |
| Player card | Enum value → registry `TagDefinition` | Identity chip + hover tooltip |

No separate attack-type tag array on pieces. The enum is the single canonical field.

Enum value maps to registry ID via lowercase name: `AttackType.Ballistic` → `ballistic`.

### 4.2 Registry entries

| ID | Display name | Tooltip |
|----|--------------|---------|
| `ballistic` | Ballistic | Strong vs Medium armor, weak vs Heavy |
| `piercing` | Piercing | Strong vs Heavy armor, weak vs Light |
| `shredding` | Shredding | Strong vs Light armor, weak vs Medium |
| `explosive` | Explosive | Strong vs Heavy armor and structures |
| `fire` | Fire | Applies burn status |
| `melee` | Melee | Close-quarters attack (matchups TBD) |
| `gas` | Gas | Strong vs Infantry, weak vs buildings |

`AttackType.None` has no registry entry and no player chip.

### 4.3 Enum expansion

```csharp
public enum AttackType
{
    None,
    Ballistic,
    Explosive,
    Piercing,
    Shredding,
    Fire,
    Melee,
    Gas
}
```

Existing piece assets keep their current enum assignments. New values are available for authoring only.

---

## 5. Combat matchups

### 5.1 `AttackTypeProfile` catalog

New data module (`AttackTypeProfileCatalog` or equivalent) holds per-type matchup rules and multiplier values. `CombatDamageResolver` and registry tooltips both derive from this catalog so combat math and player text stay aligned.

### 5.2 Multiplier table

Uses the existing demo band: **strong ×1.25–1.35**, **weak ×0.85**, **neutral ×1.0**.

| Attack type | Strong condition | Multiplier | Weak condition | Multiplier |
|-------------|------------------|------------|----------------|------------|
| Ballistic | Defender armor = Medium | ×1.25 | Defender armor = Heavy | ×0.85 |
| Piercing | Defender armor = Heavy | ×1.35 | Defender armor = Light | ×0.85 |
| Shredding | Defender armor = Light | ×1.25 | Defender armor = Medium | ×0.85 |
| Explosive | Defender armor = Heavy **or** defender Primary = `building`/`structure` | ×1.30 | — | ×1.0 |
| Gas | Defender Primary = `infantry` | ×1.25 | Defender Primary = `building`/`structure` | ×0.85 |
| Fire | — | ×1.0 | — | ×1.0 |
| Melee | — | ×1.0 | — | ×1.0 |

Evaluation order unchanged: `(BaseDamage + flat bonus) × damageScale` → baseline armor reduction → attack type multiplier → floor at 1.

Gas and Explosive structure checks use `PieceTagQueries.HasPrimaryTag` (or equivalent), not armor tier.

### 5.3 Deferred mechanics

- **Fire:** tooltip references burn; damage multiplier stays neutral until a status-effect system exists.
- **Melee:** neutral multiplier until the design sheet defines matchups.

---

## 6. Player piece card

### 6.1 Identity chips (updated order)

1. Primary  
2. Combat Role  
3. Faction (from `FactionId`, unchanged)  
4. **Attack Type** (new — from enum → registry)  
5. Optional synergy/ability chips (empty until future tags)  
6. `+N` overflow if optional chips exceed cap  

Attack Type chip uses registry `displayName` and `tooltip`. Display priority ~62 (between Faction at 65 and optional chips).

### 6.2 Stats row

Remove the `"Attack Type: …"` text line from `PieceHoverCard`. Attack type identity moves entirely to the chip. Armor type display in the stats row is unchanged.

---

## 7. Systems continuity

Systems are **not removed**. Demo data referencing old synergy tags is cleared.

| System | After rework |
|--------|--------------|
| `SynergyEngine` | Unchanged logic; `SynergyRuleCatalog` returns empty rules until new synergy tags are authored |
| `SynergyTraitRegistry` | Empty catalog; side panel shows no traits |
| `CriticalMassRuleCatalog` | Remove `vanguard` synergy rule; keep Primary/Role rules (`infantry`, `vehicle`, `artillery`, `assault`) |
| `NeighborFilter.SynergyTagId` | Retained for future rules |
| `PieceDefinition.synergyTags` / `abilityTags` fields | Retained on SO and runtime model |
| `TagContentMigrator` | Mappings updated; new menu action clears legacy synergy tags from all pieces |

---

## 8. Content migration

### 8.1 Piece assets affected

Fourteen demo pieces currently carry old synergy tags (e.g. `supply_depot` → `supply`, `field_medic` → `medic`, `phantom_agent` → `stealth`/`echo`). Migration clears all `synergyTags` arrays.

### 8.2 Migrator updates

- Remove synergy assignments from `PieceMappings`.
- Remove old synergy IDs from `KnownLegacyTags`.
- Add menu item **DeadManZone / Clear Legacy Synergy Tags** that strips `synergyTags` from every `PieceDefinitionSO` under `Assets/_Project/Data/Resources/DeadManZone/Pieces`.

### 8.3 Validation

- Unknown synergy tag on a piece after migration → stripped by migrator; logged once per asset.
- Enum `attackType` with no registry entry → editor warning on inspect.

---

## 9. Future tag candidates (not implemented)

Documented for a later pass. Do not add to `TagRegistry` in this rework.

| Tag | Notes from sheet |
|-----|------------------|
| `hacker` | No description in sheet |
| `resonance` | When buffed, spreads weaker buff to adjacent allies |
| `recon` | No description |
| `iron_will` | No description |
| `shadownet` | Adjacent Stealth pieces gain bonus damage attacking from stealth |
| `revenant` | When adjacent ally dies, temporary power spike |
| `prophet` | No description |
| `warden` | No description |
| `martyr` | No description |
| `oathbreaker` | No description |
| `bastion` | No description |
| `ironclad` | No description |
| `convoy` | No description |
| `supply_line` | No description |
| `gas_division` | No description |
| `chemical_corps` | No description |

When these are ready, assign each to Synergy, Ability, or Flavor category and wire rules through the existing engines.

---

## 10. Code touchpoints

1. `TagCategory` — add `AttackType`
2. `GameTagIds` — remove synergy constants; add attack type ID constants
3. `TagRegistry` — remove synergy entries; add seven attack type entries
4. `AttackType` enum — add four values
5. `AttackTypeProfileCatalog` (new) — matchup data
6. `CombatDamageResolver` — consume profile catalog
7. `PieceTagQueries` — add attack type identity chip
8. `PieceHoverCard` — remove stats-row attack type text
9. `TagPickerCatalog` — expose attack types for editor
10. `SynergyRuleCatalog` — clear demo rules
11. `SynergyTraitRegistry` — clear demo traits
12. `CriticalMassRuleCatalog` — remove vanguard rule
13. `TagContentMigrator` — update mappings + clear action
14. Tests — registry, resolver, tag queries, critical mass

---

## 11. Testing expectations

| Area | Tests |
|------|-------|
| Registry | Seven attack type entries; zero synergy entries; unknown ID rejection |
| Resolver | Ballistic vs Medium/Heavy; Piercing vs Heavy/Light; Shredding vs Light/Medium; Explosive vs Heavy and structures; Gas vs Infantry/Building |
| Piece card | Attack type chip in identity tags; system tags still hidden |
| Critical mass | Vanguard synergy rule absent; Primary/Role rules still fire |
| Migration | Demo pieces have empty `synergyTags` after migrator runs |

---

## 12. Implementation order

1. Registry + enum expansion + `AttackTypeProfileCatalog`
2. `CombatDamageResolver` + resolver tests
3. Player card chip (`PieceTagQueries` + hover card)
4. Clear synergy catalog entries + critical mass rule
5. Content migration (assets + migrator)
6. Registry and tag query tests

---

## Decision log

| Date | Decision |
|------|----------|
| 2026-06-09 | Registry vocabulary swap only — keep SynergyEngine and related systems |
| 2026-06-09 | Attack Types: enum canonical + `TagCategory.AttackType` registry (faction-like) |
| 2026-06-09 | Undecided-column tags documented only; not implemented |
| 2026-06-09 | Synergy/Ability/Flavor categories kept empty, not removed from enum |
| 2026-06-09 | Fire burn and Melee matchups deferred; neutral multipliers for now |
| 2026-06-09 | Attack type chip replaces stats-row text on hover card |
