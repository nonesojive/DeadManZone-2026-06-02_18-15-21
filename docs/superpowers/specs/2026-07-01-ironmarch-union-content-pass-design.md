# IronMarch Union Content Pass — Design

**Date:** 2026-07-01  
**Status:** Approved (2026-07-01)  
**Goal:** Full game roster wipe and rebuild from author content sheet; rename faction to IronMarch Union; implement faction baselines, numbered movement, starting tactic gating, and full bespoke piece abilities.

---

## 1. Summary

| Decision | Choice |
|----------|--------|
| Roster scope | **Full wipe** — delete all ~40 existing piece assets; create **17** pieces from content sheet only |
| Other factions | **IronMarch Union only** — hide Dust Scourge & Cartel in faction select; stub enemy fights from new pool |
| Faction ID | Rename `iron_vanguard` → **`ironmarch_union`** (breaking change for saves/tests) |
| Display name | **IronMarch Union** everywhere (no "Vanguard" in player-facing or lore strings) |
| Tactics | Rename **Stand Ground → Hold the Line** in UI; start with Hold the Line + Advance + Disciplined Fire; Protect Support locked |
| Abilities | **Full implementation** of bespoke building and unit effects |
| Accuracy | **Engine defaults only** — no per-piece `accuracyOverride` |
| Movement | **Numbered 0–4** (higher = faster); replaces tier enum for authoring |
| Balancing | Remove/ignore `CombatContentBalancePass` heuristics, legacy balance goals, and `FightRewardTable` |
| Implementation | **Approach 1:** content factory editor script + focused engine deltas |

---

## 2. Faction & run economy

### 2.1 Rename

- `FactionIds.IronVanguard` → `FactionIds.IronmarchUnion` (`"ironmarch_union"`)
- Asset: `iron_vanguard.asset` → `ironmarch_union.asset`
- Update all code, tests, UI, enemy templates, art catalogs, achievements (`win_ironmarch` may keep id or alias)
- Remove player-facing "Iron Vanguard" / "Vanguard" strings

### 2.2 FactionSO values

| Field | Value |
|-------|-------|
| `displayName` | IronMarch Union |
| `hqBoardWidth` × `hqBoardHeight` | 3 × 6 |
| `startingSupplies` | 50 |
| `startingManpower` | 15 |
| `startingAuthority` | 2 |
| `baseSuppliesPerRound` | **10** (new field) |
| `baseMusterPerShop` | **1** |
| `baseSalvageChancePercent` | 1 |

`RoundIncomeCalculator.ComputeSuppliesIncome` uses `FactionSO.baseSuppliesPerRound` plus board bonuses only (`BuildingIncomeRules`, critical mass). No per-fight reward table.

### 2.3 Playable factions

`ContentDatabase.PlayableFactionIds` = `[ironmarch_union]` only. Main menu hides Dust Scourge and Cartel buttons.

### 2.4 Starting tactics

- New `FactionSO.startingTactics`: Hold the Line, Advance, Disciplined Fire
- `TacticType.StandGround` display string → **"Hold the Line"**
- `ProtectSupport` not in starting set; locked until a future unlock path exists
- Combat UI and tactic validator respect faction unlocked-tactic list

---

## 3. Movement rework (0–4)

Replace `MovementSpeedTier` enum authoring with **`int movementSpeed` (0–4)** on `PieceDefinition` / `PieceDefinitionSO`.

| Speed | Behavior |
|-------|----------|
| 0 | Immobile (buildings, Machine Gun Nest) |
| 1 | Slowest (Ironclad Mortars) |
| 2 | Standard infantry / vehicles |
| 3 | Fast infantry (Bulwark, Marksman, Field Marshal) |
| 4 | Fastest (Armored Transport) |

**Charge-per-tick mapping** (initial tuning — higher speed = more charge per tick):

```
chargePerTick = movementSpeed == 0 ? 0 : movementSpeed + 1
// 1→2, 2→3, 3→4, 4→5
```

Update `CombatMovementSpeed`, combat tests, and unit creator UI. Deprecate or remove `MovementSpeedTier` from piece data path once migrated.

---

## 4. Ability scope rules

| Pattern | Tag count scope | Effect target scope |
|---------|-----------------|---------------------|
| **Per [tag]** / **for each [tag]** | Count on **HQ + combat** via `BuildBoardSet.ToAggregateBoard()` or `AllPieces` | Per ability definition below |
| **Adjacent** | Adjacency on **combat board** at fight start (units) or **HQ board** during build (HQ-only building synergies) | Adjacent pieces only |

Examples:
- Officer Quarters: +1 Authority **per `command` tag** counted on both boards
- Ironmarch Surgeon: +2% max HP **per `medic` tag** counted on both boards
- Field Medic: +10 HP to **adjacent** infantry on combat board

---

## 5. Accuracy

No `accuracyOverride` on any piece. Engine derives from attack type + combat role:

| Source | Base % |
|--------|--------|
| Ballistic | 78 |
| Piercing | 80 |
| Shredding | 68 |
| Sniper role | 88 |
| Artillery role | max(type, 72) |

Outcomes: hit (100% dmg), graze (33%, min 1), miss (0, cooldown spent).

---

## 6. Piece roster (17)

Delete all existing `Assets/_Project/Data/Resources/DeadManZone/Pieces/*.asset` except meta cleanup. Regenerate via content factory.

### 6.1 New tags

Add to `GameTagIds` / `KeywordTagCatalog`:
- `small_arms` (flavor)
- `shells` (flavor)

Ironmarch-tagged pieces: `factionId: ironmarch_union`. Neutral pieces: `factionId: neutral`.

### 6.2 Buildings (6)

| id | displayName | cost | shape | tags | effect |
|----|-------------|------|-------|------|--------|
| `supply_depot` | Supply Depot | 15 | 1×2 | neutral, logistics, supply_line | +5 supplies income/round |
| `field_hospital` | Field Hospital | 20 | 2×2 | neutral, medic, support, utility | +10 max HP to all infantry |
| `officer_quarters` | Officer Quarters | 25 | 2×2 | ironmarch_union, command, utility | +1 Authority per `command` tag (both boards) |
| `command_outpost` | Command Outpost | 15 | 1×2 | ironmarch_union, command, support | +1 Authority |
| `surgical_center` | Surgical Center | 20 | 1×1 | ironmarch_union, medic, support | +5% max HP to all infantry |
| `recruitment_office` | Recruitment Office | 15 | 1×1 | neutral, logistics, utility | +1 manpower/round (`musterPerShop: 1`) |

### 6.3 Units & structures (11)

| id | displayName | category | role | cost | shape | HP | armor | atk type | atk spd | range | mov | dmg | MP | tags | bespoke effect |
|----|-------------|----------|------|------|-------|-----|-------|----------|---------|-------|-----|-----|-----|------|----------------|
| `field_medic` | Field Medic | infantry | support | 10 | 1×1 | 30 | light | ballistic | slow | short | 2 | 3 | 1 | neutral, medic, support, small_arms | +10 HP adjacent infantry |
| `conscript_rifleman` | Conscript Rifleman | infantry | assault | 12 | 1×1 | 50 | light | ballistic | slow | medium | 2 | 5 | 1 | neutral, small_arms, assault | — |
| `armored_transport` | Armored Transport | vehicle | defender | 18 | 1×2 | 75 | light | ballistic | slow | short | 4 | 2 | 3 | neutral, support, convoy | — |
| `ironmarch_surgeon` | Ironmarch Surgeon | infantry | support | 15 | 1×1 | 40 | light | ballistic | slow | short | 2 | 3 | 1 | ironmarch_union, medic, support | +2% max HP per `medic` tag (both boards) → all infantry |
| `bulwark_squad` | Bulwark Squad | infantry | assault | 18 | 1×1 | 55 | medium | ballistic | slow | short | 3 | **3** | 1 | ironmarch_union, veteran, phalanx | +1 dmg, +5 HP per adjacent `phalanx` |
| `enlisted_rifleman` | Enlisted Rifleman | infantry | assault | 15 | 1×1 | 55 | light | ballistic | slow | medium | 2 | 6 | 1 | ironmarch_union, small_arms, assault | +1 attack speed tier if adjacent `command` |
| `ironmarch_iron_horse` | Ironmarch Iron Horse | vehicle | tank | 24 | 3×2 | 75 | medium | piercing | slow | medium | 2 | 6 | 4 | ironmarch_union, ironclad, shells, tank | +10 HP per adjacent infantry |
| `ironclad_mortars` | Ironclad Mortars | infantry | artillery | 20 | 2×1 | 25 | light | piercing | slow | long | 1 | 8 | 3 | ironmarch_union, shells, ironclad, siege | — |
| `ironclad_marksman` | IronClad Marksman | infantry | sniper | 20 | 1×1 | 35 | light | piercing | slow | long | 3 | 6 | 2 | ironmarch_union, ironclad, stealth | untargetable until after 2nd tactics checkpoint |
| `ironclad_field_marshal` | IronClad Field Marshal | infantry | utility | 30 | 1×1 | 50 | medium | ballistic | medium | short | 3 | 3 | 2 | ironmarch_union, command, ironclad, inspiring | +5 HP, +1 movement to adjacent infantry |
| `machine_gun_nest` | Machine Gun Nest | structure | utility | 20 | 2×1 | 100 | heavy | shredding | medium | medium | 0 | 2 | 2 | neutral, fortification, entrenched | immobile; shredding DPS |

*Field Marshal (50 HP) and Machine Gun Nest (100 HP) were not in the author sheet — sensible defaults for factory authoring; rebalance in playtest.*

### 6.4 Shop pool

`ContentDatabase.DemoShopPieceIds` = all 17 ids. All pieces `includeInShopPool: true` unless noted later.

---

## 7. Enemies

Rewrite `EnemyTemplateSO` assets to reference only new piece ids. Progressive difficulty:
- Early fights: neutral conscripts, field medic, MG nest
- Mid: mixed neutral + ironmarch_union units
- Late: ironmarch_union elite compositions

---

## 8. Out of scope

- Dust Scourge / Cartel faction content
- New art assets (reuse placeholders / existing Synty mappings where possible)
- `CombatContentBalancePass` auto-tuning
- `FightRewardTable` (per-fight supply ladder — removed)
- Protect Support unlock progression (locked only; no unlock path this pass)
- Save migration from `iron_vanguard` saves (breaking rename accepted)

---

## 9. Testing & verification

### 9.1 EditMode (TDD)

- Movement 0–4 charge mapping
- Faction baselines: starts, `baseSuppliesPerRound`, `baseMusterPerShop`, salvage 1%
- Tactic gating: only 3 starting tactics selectable
- Per-tag counting uses both HQ + combat boards (Officer Quarters, Surgeon)
- Adjacent abilities on combat board only
- Marksman stealth until checkpoint 2
- Bulwark base damage 3 + phalanx adjacency

### 9.2 Regression

- Update all tests referencing `IronVanguard`, `iron_vanguard`, old piece ids
- Run full EditMode suite
- Play mode smoke: new run → shop shows 17-piece pool → fight 1 completes

---

## 10. Architecture

```
ContentPassFactory (Editor)
    └── generates 17 PieceDefinitionSO assets + updates ContentDatabase

FactionSO + FactionIds (ironmarch_union)
RunOrchestrator / RoundIncomeCalculator (baseSuppliesPerRound)
CombatMovementSpeed (int movementSpeed 0–4)
TacticPauseValidator + FactionSO.startingTactics
PieceAbilityEngine + building income (per-tag = aggregate boards)
```

---

## 11. Risks

| Risk | Mitigation |
|------|------------|
| Breaking saves | Accepted; document in changelog |
| Combat slice / arena tests reference deleted pieces | Update `CombatSliceLayouts` to new canonical units |
| Movement 0–4 tuning feels slow vs old tiers | Charge formula is one constant block; easy to rebalance |
| MG Nest HP unspecified | Pick sensible default; playtest |
