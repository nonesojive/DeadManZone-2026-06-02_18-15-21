# Piece Abilities & Tag Flavor Split — Design Spec

**Date:** 2026-06-21  
**Branch:** `critmass+synergyworkv1` (or successor)  
**Status:** Approved (user review pending)

## Goals

- **Tags are flavor-only** for combat and adjacency: synergy, ability, and flavor tag buckets are display chips and Critical Mass counters — they do not grant inherent buffs or abilities.
- **Abilities section is the mechanics source of truth** per piece: adjacency auras, fight-start passives, pause actives, and command actions are authored explicitly on each unit.
- **Hybrid ability authoring:** reusable catalog assets for common effects + inline one-off rows for legendaries.
- **Critical Mass unchanged** as a separate board-wide threshold system (tag counts → tier bonuses).

## Non-Goals (this milestone)

- Removing or redesigning Critical Mass counting or tier rules.
- Changing `Primary`, `CombatRole`, or `SystemTag` structural roles (targeting, HQ, combatant checks).
- Migrating `GrantedAbility` / `CommandActions` enums in the first implementation pass (Phase 2).

---

## 1. Tags (flavor only)

### Purpose

Identity, card chips, tooltips, and **Critical Mass counting**. No automatic combat or run effects.

### Fields

| Field | Role |
|-------|------|
| `Primary`, `CombatRole`, `SystemTag` | Structural identity — targeting, roles, HQ/combatant checks (**unchanged**) |
| `SynergyTags`, `AbilityTags`, `FlavorTags` | **Display-only optional chips** — same UI priority rules as today |

### Removed behavior

- **No engine reads optional tag buckets to apply buffs.** Deprecate tag-driven adjacency in `SynergyRuleCatalog`.
- `SynergyTraitRegistry` descriptions become **design reference** until effects are recreated as ability modules on specific pieces.

### Critical Mass interaction

Critical Mass **still counts** tag presence on the board by category (Primary, CombatRole, Synergy, Ability, Flavor, AttackType, Faction, etc.).

Example: five Medic-tagged units progress Medic CM thresholds but **do not** heal adjacent Infantry unless those units also have an explicit heal ability.

---

## 2. Abilities (hybrid catalog + one-offs)

### Purpose

Single source of truth for what a piece **does** mechanically and what the unit card **Abilities** section displays.

### Shared catalog — `AbilityDefinitionSO`

Location: `Assets/_Project/Data/Resources/DeadManZone/Abilities/` (or Addressables later).

Examples:

| Id | Description (card copy) |
|----|-------------------------|
| `adjacent_allies_move_plus_one` | Adjacent allies gain +1 move step. |
| `adjacent_infantry_max_hp_plus_10` | Adjacent infantry gain +10 max HP. |
| `grant_supplies_fight_start_10` | Grant +10 supplies at fight start. |
| `grenade_lob` | Pause ability: area damage at pause 0. |

Each catalog entry defines:

- **Id**, **display name**, **card description**
- **Trigger:** `AdjacentAura` | `FightStart` | `Pause` | `Command`
- **Target filter:** reuse `NeighborFilter` / `CriticalMassTargetFilter` patterns (adjacent any, adjacent Infantry, self, board-wide from source, etc.)
- **Effect:** stat + mod type + magnitude (HP, supplies, move steps, damage, armor steps, authority, etc.)
- **Stacking rule:** default — same ability id from one source piece applies once; document per-ability overrides if needed

### Per-piece authoring — `PieceDefinitionSO`

```csharp
AbilityReference[] catalogAbilities;   // → AbilityDefinitionSO refs
PieceAbilityInline[] customAbilities;  // one-off rows for legendaries
```

Inline rows use the **same schema** as catalog entries (no second effect language).

### Designer example

| Tags (flavor) | Abilities (mechanics) |
|---------------|------------------------|
| Medic, Logistics | `adjacent_allies_move_plus_one` (catalog) |

No automatic +10 HP or +10 supplies unless those ability rows are explicitly added.

---

## 3. Critical Mass (unchanged scope)

Stays a **separate board-wide system**:

- Count tags on board → tier thresholds → fight/run bonuses.
- Not folded into per-piece abilities.
- Coexists with piece abilities (CM rewards tag diversity; piece abilities define local auras).

No changes to `CriticalMassEngine`, database SO, or buff strip behavior in this milestone.

---

## 4. Runtime architecture

### Engine replacement

| Current | Target |
|---------|--------|
| `SynergyEngine` (tag → adjacency via `SynergyRuleCatalog`) | **`PieceAbilityEngine`** — evaluates catalog + inline abilities on each placed piece |
| `GrantedAbility` + `CommandActions` | **Phase 1:** unchanged. **Phase 2:** migrate into ability catalog |
| `CriticalMassEngine` | Unchanged |
| Card synergy lines from tag-implied rules | **Ability lines** from resolved piece abilities |

### Fight-start evaluation order

1. **`PieceAbilityEngine`** — adjacent auras, fight-start passives from piece ability lists
2. **`CriticalMassEngine`** — board threshold snapshot
3. Merge modifiers into `CombatantState` / run resources (existing apply hooks)

### Board presentation

- Synergy overlay / link lines draw from **ability adjacency links** (`PieceAbilityEngine` output), not from synergy tag ids on the source piece.

---

## 5. UI / unit card

| Section | Source |
|---------|--------|
| Identity tags | Primary, CombatRole, faction, attack type |
| Optional tag chips | Synergy + Ability + Flavor buckets (display only) |
| **Abilities section** | Catalog + inline ability descriptions for this piece |
| Buff strip | Critical Mass tiers (unchanged) |

Remove or stop generating card copy that implies tag-owned combat effects (e.g. trait registry text presented as active mechanics).

---

## 6. Data & editor tooling

### New assets / types

- `AbilityDefinitionSO` — catalog entry
- `AbilityDatabaseSO` (optional) — index of all catalog abilities for editor dropdowns
- `PieceAbilityInline` — serializable struct matching catalog fields for one-offs

### Editor menus (suggested)

- **DeadManZone/Generate Default Ability Catalog** — seed from current `SynergyRuleCatalog` + `SynergyTraitRegistry` intents
- **DeadManZone/Migrate Piece Tag Implied Abilities** — suggest catalog assignments where tags previously implied effects (designer confirms)

### Content migration

1. Seed catalog abilities matching former tag-implied adjacency rules (Medic, Inspiring, Command→Artillery, etc.).
2. Walk existing `PieceDefinitionSO` assets; assign catalog abilities where design intent matches old implicit behavior.
3. Remove or empty tag-driven rules in `SynergyRuleCatalog`; delete dead code paths after tests pass.

---

## 7. Testing strategy (TDD)

| Scope | Path | Focus |
|-------|------|-------|
| Core | `Assets/_Project/Core.Tests/EditMode/PieceAbilityEngineTests.cs` | Port/adapt cases from `SynergyEngineTests`; adjacent filters, stacking, fight-start apply |
| Core | Existing `CriticalMassEngineTests.cs` | Regression — unchanged behavior |
| Presentation | `PieceCardViewModelBuilderTests.cs` | Ability lines on card; no tag-implied synergy bonus copy |
| Integration | Play mode (optional) | Board overlay links from ability engine |

**Iron law:** failing tests before implementation; remove `SynergyRuleCatalog` tag lookup only after `PieceAbilityEngine` covers equivalent cases.

---

## 8. Phased delivery

### Phase 1 (this spec)

- Ability catalog + inline schema
- `PieceAbilityEngine` replaces tag-driven `SynergyEngine`
- Piece content migration + card UI abilities section
- Critical Mass untouched

### Phase 2 (follow-up)

- Unify `GrantedAbility` and `CommandActions` into ability catalog entries
- Deprecate `GrantedAbility` enum on `PieceDefinition`

---

## 9. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Content migration misses implicit tag effects | Editor migrator + checklist against `SynergyTraitRegistry` |
| Duplicate effects (CM + piece ability double-dipping) | Design review per piece; CM targets board totals, abilities target adjacency/self |
| Card lies (tag chip says Medic but no heal ability) | Intentional — tags are flavor; abilities section is authoritative |
| Large one-off inline proliferation | Prefer catalog; inline only for true uniques |

---

## 10. Success criteria

- No combat or run stat changes occur solely because a piece has a synergy/ability/flavor tag.
- A piece with Medic + Logistics tags and only `adjacent_allies_move_plus_one` ability applies move bonus only — not HP or supplies.
- Critical Mass still triggers from tag counts on board as before.
- Edit Mode tests green for ability engine + CM regression.
- Unit card Abilities section lists explicit ability copy for each piece.
