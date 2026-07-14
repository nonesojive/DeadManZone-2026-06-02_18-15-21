> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Tag / Keyword System Design

**Date:** 2026-06-07  
**Status:** Approved — ready for implementation plan  
**Goal:** One tag vocabulary powers authoring, backend systems, and player-facing piece cards so new content can be dropped in with correct behavior.

---

## 1. Core philosophy

Tags are the **identity and behavior layer** for pieces. Stats and enums are the **numbers and combat math layer**.

| Layer | Answers | Examples |
|-------|---------|----------|
| **Tags** | What kind of thing is this? How should systems treat it? | `infantry`, `assault`, `neutral`, `inspiring` |
| **Stats / enums** | How strong, fast, or durable is it? | `MaxHp`, `BaseDamage`, `AttackType.Fire`, `ArmorType.Heavy` |
| **GrantedAbility** | What special pause-window action does it get? | `Flamethrower`, `GrenadeLob` |

A single **Tag Registry** backs:

- Designer authoring (unified tag picker)
- Backend consumers (AI, synergies, critical mass, zones, faction rules)
- Player piece cards (chips + tooltips)

**Not everything is a tag.** Shape, costs, shop lane, cooldown ticks, and numeric balance stay as typed fields on `PieceDefinition`.

---

## 2. Tag categories and rules

### 2.1 Primary (exactly one, player-visible)

Drives zone placement and combat movement eligibility.

| Tag | Build zones | Combat movement |
|-----|-------------|-----------------|
| `infantry` | Front, Support | Yes (`MovementSpeed` applies) |
| `vehicle` | Front, Support | Yes |
| `building` | Rear only | No (`MovementSpeed.None`) |
| `structure` | Front, Support, Rear | No (emplacement; exceptions via abilities only) |

**Rule:** Every piece must have exactly one Primary tag. Primary is authoritative for placement/movement rules (supersedes legacy `PieceCategory` over time).

### 2.2 Combat Role (exactly one, player-visible)

Drives AI behavior profile lookup.

| Tag | Player tooltip (example) | AI behavior (v1) |
|-----|--------------------------|------------------|
| `assault` | Pushes toward enemies | Nearest enemy, prefer front zone, +10% move charge |
| `tank` | Holds line, absorbs hits | Nearest front enemy, low reposition rate |
| `artillery` | Strikes from range | Prefer furthest target in range |
| `support` | Stays back, helps allies | Prefer rear; low aggression |
| `utility` | Non-combat specialist | No attack targeting; economy/ability focus |
| `headquarters` | Command center | No movement; passive |
| `sniper` | Picks high-value targets | Prefer highest HP or back-line target |

Profiles live in data assets (`CombatRoleProfile`) keyed by role tag ID.

### 2.3 System (exactly one, hidden from player card)

| Tag | Purpose |
|-----|---------|
| `combatant` | Win condition, manpower, movement eligibility |
| `noncombatant` | Excluded from “kill all enemy combatants” win |
| `hq` | Instant loss on destroy; immovable; not sellable |

Auto-rules on import/validate:

- Units/Hybrids with combat capability → `combatant`
- Pure economic buildings → `noncombatant`
- HQ pieces → `hq`

### 2.4 Faction (exactly one, player-visible)

- **Canonical field:** `FactionId` (e.g. `neutral`, `iron_vanguard`, `dust_scourge`)
- **Card display:** registry lookup (e.g. chip label “Neutral”)
- **Buff logic:** faction buffs match `FactionId` only (neutrals do not receive faction-specific buffs)
- **Synergy logic:** rules may filter neighbors by faction tag / `FactionId` (e.g. “adjacent `neutral`”)

### 2.5 Synergy (0+, player-visible with overflow)

Tag-owned adjacency effects. See §4.

### 2.6 Ability (0+, player-visible with overflow)

- **Mechanics:** `GrantedAbility` enum drives pause-window behavior (coded once per ability)
- **Card:** ability tag chip (e.g. `Flamethrower`) with tooltip; links to enum for UI copy

### 2.7 Combat stats (not tag chips)

`AttackType` and `ArmorType` remain **enums** on `PieceDefinition`. They are **not** shown as word tags on the card.

| Enum | Card display |
|------|--------------|
| `AttackType` | Icon only (bullet, flame, explosion, piercing bolt) |
| `ArmorType` | Shield icon, color/shape = tier (light / medium / heavy) |

Registry may hold display metadata (icon, tooltip) for enum values without duplicating them as synergy tags.

---

## 3. Tag Registry

Central catalog: one entry per valid tag ID.

### 3.1 Registry entry fields

| Field | Purpose |
|-------|---------|
| `id` | Canonical string (used in code, assets, saves) |
| `displayName` | Player-facing chip label |
| `category` | Primary, CombatRole, System, Faction, Synergy, Ability, Flavor |
| `playerVisible` | Show on piece card? (System tags: false) |
| `tooltip` | Hover text on card and in editor |
| `chipColor` | Optional UI styling |
| `displayPriority` | Sort order when overflow collapses to `+N` |
| `enumBinding` | Optional link to `AttackType`, `ArmorType`, or `GrantedAbility` |
| `aiProfileId` | Combat Role → profile asset |
| `synergyEffectId` | Synergy tag → effect rule asset |

### 3.2 Naming convention

Use **lowercase snake_case** for new tag IDs (`iron_vanguard`, `small_arms` deprecated in favor of enums). Migrate existing PascalCase constants (`Infantry` → `infantry`) during content migration.

### 3.3 Validation

Editor and load-time checks:

- Exactly one tag per required category (Primary, Combat Role, System)
- Unknown tag IDs → error in editor, warning/fail in CI
- Synergy/ability tags must reference valid effect definitions when they claim mechanical behavior

---

## 4. Adjacency synergies (tag-owned)

### 4.1 Core rules

1. **Only pieces with synergy tags activate synergy behavior** as sources (they run their tag’s rules).
2. Pieces without synergy tags do not run rules; they may still **receive** outbound auras from neighbors.
3. **Each synergy tag has its own effect definition** — not a single global pair table applied to all pieces.
4. **Fight-start snapshot only** — evaluated once from **starting adjacency** before combat movement. Buffs are **fixed for the entire fight**. Deaths and movement do **not** recalculate synergies mid-fight.
5. Build-phase positioning determines synergy outcomes.

### 4.2 Effect directions

| Direction | Name | Tag on | Behavior |
|-----------|------|--------|----------|
| **Inbound** | Self-scan | This piece | Count/filter adjacent neighbors; **buff self** |
| **Outbound** | Aura | This piece | Filter adjacent pieces; **buff neighbors** |

Examples:

| Tag | Direction | Rule |
|-----|-----------|------|
| `climbing` | Inbound | +1 attack range (flat) per adjacent `structure` |
| `inspiring` | Outbound | +1 movement tier (tier step) to each adjacent `infantry` |
| `supply` | Outbound | +1 damage (flat) to each adjacent piece (any) |
| `medic` | Outbound | +1 armor tier (tier step) to each adjacent `infantry` |

Multiple synergy tags on one piece evaluate independently. Inbound effects stack per matching neighbor.

### 4.3 Effect mod types

Per-tag configuration in `SynergyEffect` data:

| Mod type | Example | Status |
|----------|---------|--------|
| **Flat** | +1 damage, +1 range (tiles) | Demo |
| **TierStep** | +1 movement tier, +1 armor tier | Demo |
| **Percent** | +10% damage | Reserved for later content |

Each effect specifies: `modType`, `stat`, `value`, `direction`, `neighborFilter` (Primary, Role, Faction, tag ID, or any).

### 4.4 Demo synergy migration

Legacy global pair rules become tag-owned outbound/inbound effects:

| Legacy behavior | New owner |
|-----------------|-----------|
| Any piece adjacent to Supply → +1 damage | `supply` on depot → outbound to any adjacent |
| Medic adjacent to Infantry → +1 armor | `medic` on medic → outbound to adjacent `infantry` |
| Command adjacent to Artillery → +2 damage | `command` on radio → outbound to adjacent `artillery` role |
| Echo adjacent to Stealth → +1 damage | Define owner tag explicitly in content (outbound or inbound) |

---

## 5. Critical mass (board-wide thresholds)

### 5.1 Core rules

1. Counts **friendly** pieces on the board at **fight start**.
2. **Snapshot only** — bonuses fixed for the entire fight (same timing as synergies).
3. Rules are **data assets** (`CriticalMassRuleSO`), not hardcoded strings.

### 5.2 Demo rules

| Threshold | Tag counted | Category | Team bonus |
|-----------|-------------|----------|------------|
| ≥3 | `infantry` | Primary | +2 damage (all friendly combatants) |
| ≥2 | `vehicle` | Primary | +1 armor shred step |
| ≥2 | `artillery` | Combat Role | +3 damage |
| ≥3 | `assault` | Combat Role | +10% move charge |
| ≥2 | `vanguard` | Synergy | +1 damage |

Fight UI may show a banner when active (e.g. “Infantry Mass — +2 damage”).

---

## 6. Player piece card

Hover card (not yet implemented). Two layers.

### 6.1 Stats row (icons + numbers)

| Stat | Display |
|------|---------|
| HP | Health icon + number |
| Damage | Weapon icon + `BaseDamage` |
| Movement | Boot/track icon + tier |
| Attack speed | Clock/bolt icon + tier |
| Attack type | Enum icon (no text chip) |
| Armor type | Shield icon by tier (no text chip) |

Example: blue heavy shield = Heavy armor tier; `10` beside heart icon = 10 HP (armor icon does not represent HP).

### 6.2 Tag chips (identity only)

**Always show:** Primary, Combat Role, Faction.

**Optional (synergy + ability combined):** up to **4** chips, then **`+N`** with tooltip listing remainder.

**Overflow priority:** ability tags first, then synergy by `displayPriority`.

**Never show:** System tags (`combatant`, `hq`, `noncombatant`).

Chip hover → registry tooltip.

### 6.3 Example card

```
┌─────────────────────────────────────┐
│  Flamethrower Trooper               │
├─────────────────────────────────────┤
│  ♥ 10   ⚔ 4   👟 Med   ⏱ Slow      │
│  🔥 Fire              🛡 Light       │
├─────────────────────────────────────┤
│  [Infantry] [Assault] [Neutral]     │
│  [Flamethrower]                     │
└─────────────────────────────────────┘
```

---

## 7. Authoring UX

**Unified tag picker (Option B):** one visual picker grouped by category with exactly-one enforcement on Primary, Combat Role, and System.

Separate inspector fields (or picker sections) for:

- Numeric stats (HP, damage, costs, speeds)
- `AttackType`, `ArmorType`, `GrantedAbility` enums
- `FactionId`

Picking display-linked values (e.g. Fire attack) sets the enum and uses registry metadata for card icons.

---

## 8. System consumers summary

| System | Tags / fields used | Timing |
|--------|-------------------|--------|
| Zone placement | Primary | Build phase |
| Combat movement | Primary, System (`combatant`), `MovementSpeed` | Combat |
| Combat AI | Combat Role → profile asset | Combat |
| Win conditions | System (`combatant`, `hq`) | Combat |
| Manpower / authority | System, Synergy (`command`) | Build / pause |
| Damage resolver | `AttackType`, `ArmorType` enums; Primary for building bonus | Combat |
| Synergies | Synergy tags → effect assets | **Fight-start snapshot** |
| Critical mass | Primary, Role, Synergy counts | **Fight-start snapshot** |
| Faction buffs | `FactionId` | Build / fight per rule |
| Shop pools (future) | Faction, Primary, Synergy | Build phase |
| Piece card | Registry + enums | UI |

---

## 9. Migration and deprecation

| Current | Target |
|---------|--------|
| Flat `Tags` list on `PieceDefinition` | Categorized fields + registry IDs |
| `GameTags` + `GameKeywords` constants | Single registry; constants become registry IDs |
| PascalCase tag strings (`Infantry`, `Combatant`) | snake_case IDs with display names in registry |
| Global synergy pair checks | Tag-owned `SynergyEffect` assets |
| `PieceCategory` for placement | Primary tag (category retained temporarily for migration) |

Content migration: editor script maps old flat tags → categorized fields and validates against registry.

---

## 10. Out of scope (this spec)

- Shop tag filtering implementation
- Percent-based synergy mods (schema reserved only)
- In-combat synergy UI feedback (buffs are implicit from build layout)
- `TagRegistry` ScriptableObject editor UI polish beyond basic validation

---

## 11. Testing expectations

| Area | Tests |
|------|-------|
| Registry | Unknown ID rejection; category validation |
| Synergies | Inbound/outbound stacking; snapshot immutability after simulated move |
| Critical mass | Threshold at fight start; no change after combatant death |
| Piece card | Chip visibility rules; `+N` overflow; system tags hidden |
| Migration | Demo pieces map to new taxonomy without behavior regression |

---

## 12. Implementation order (recommended)

1. Tag Registry data model + demo tag set
2. Categorized fields on `PieceDefinition` / `PieceDefinitionSO`
3. Content migration for demo pieces
4. Refactor `SynergyEngine` → tag-owned effects, fight-start snapshot
5. Refactor `CriticalMassRules` → data-driven rules, fight-start snapshot
6. Combat Role → AI profile wiring
7. Primary → zone validator
8. Piece card UI (stats row + tag chips)
9. Editor validation + unified tag picker

---

## Decision log

| Date | Decision |
|------|----------|
| 2026-06-07 | Tags for identity/behavior; stats/enums for numbers (Option A) |
| 2026-06-07 | Unified tag picker authoring (Option B) |
| 2026-06-07 | Card shows identity tags only; attack/armor as stat icons |
| 2026-06-07 | Max 4 synergy/ability chips, then `+N` overflow |
| 2026-06-07 | Synergies: tag-owned, inbound/outbound, flat+tier+% mod types |
| 2026-06-07 | Synergies + critical mass: fight-start snapshot, no mid-fight recalc |
| 2026-06-07 | Faction: `FactionId` canonical; display via registry |
