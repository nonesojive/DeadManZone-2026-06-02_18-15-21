> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Sandbox Art & Asset Pass Design

**Date:** 2026-06-13  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-12-mechanics-sandbox-prototype-design.md`, `2026-06-05-deadmanzone-neutral-faction-art-design.md`  
**Goal:** Wire **temporary** 2D shop/board icons and 3D combat-arena prefabs for the **sandbox core roster** (25 pieces) using assets already in the project.

---

## 1. Summary

The mechanics sandbox prototype is complete (295 EditMode tests green). All `PieceDefinitionSO` assets currently have **null** `icon` and `combatArenaPrefab` fields after content regeneration. Combat arena units render as billboards with no sprite; buildings use **colored cube placeholders**.

This pass connects existing third-party and in-repo art to the **10 neutral + 15 IronMarch** sandbox roster. Tone and lore accuracy are explicitly **not** goals — assets are placeholders until a future grimdark art pass.

| Decision | Choice |
|----------|--------|
| Visual layers | 2D icons (shop/board/drag) **and** 3D arena prefabs |
| Infantry style | **Toon_Soldiers** (`Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/`) |
| Vehicles | **RTS_Modern_Combat_Vehicle_Pack_Free** (`ATV_N1`, `FA_N26`, `MSH_N2`) |
| Roster scope | Sandbox core only — **25 pieces** (no Dust/Cartel/Crimson/Ash enemy units) |
| Buildings (arena) | Keep existing **cube placeholders** for 3D; assign **BunkerSurvivalUI** sprites for 2D icons |

---

## 2. Success criteria

| # | Criterion |
|---|-----------|
| 1 | All **25** sandbox piece assets have a non-null `PieceDefinitionSO.icon` |
| 2 | All **units and hybrids** in the roster have a non-null `combatArenaPrefab` |
| 3 | All **buildings** retain cube arena prefabs (existing or regenerated placeholders) with proper 2D icons |
| 4 | `DeadManZone/Art/Validate Sandbox Art Coverage` reports zero gaps for the 25-piece list |
| 5 | EditMode test `SandboxArtCoverageTests` enforces criteria 1–2 in CI |
| 6 | Existing `CombatArenaPlayModeTests` still pass after prefab assignment |
| 7 | Assignment survives re-run of **Apply Sandbox Art Pass** without manual per-piece work |

---

## 3. Architecture

### 3.1 Data-driven catalog

Introduce a **`SandboxArtCatalog`** ScriptableObject listing one entry per sandbox piece:

```csharp
// Conceptual fields per entry
string pieceId;
string iconAssetPath;           // PNG or sprite asset path
string combatArenaPrefabPath;   // optional — null for buildings using shared cube prefabs
float combatArenaModelScale;    // default 1f
float combatArenaModelHeight;   // default 1.6f infantry, 0f buildings
```

Catalog lives at:

`Assets/_Project/Data/Art/SandboxArtCatalog.asset`

### 3.2 Editor assigner

**`SandboxArtAssigner`** reads the catalog and writes:

- `piece.icon` ← sprite loaded from `iconAssetPath`
- `piece.combatArenaPrefab` ← prefab (when specified)
- `piece.combatArenaModelScale` / `combatArenaModelHeight`

**Menus:**

| Menu | Action |
|------|--------|
| `DeadManZone/Art/Apply Sandbox Art Pass` | Apply catalog to all 25 pieces |
| `DeadManZone/Art/Validate Sandbox Art Coverage` | Log missing icon/prefab for roster |
| `DeadManZone/Art/Snapshot Missing Icons From Prefabs` | Optional: render prefab → PNG for entries flagged `useSnapshotIcon` |

Reuses existing tooling where possible:

- `DeadManZone/Art/Assign Neutral Icons From Renders` (5 Grok neutrals)
- `DeadManZone/Art/Import Grok Batch 2 Icons` (regenerate PNGs from Grok sheets)
- `DeadManZone/Combat Arena/Generate Building Placeholder Prefabs`

### 3.3 Icon sources (priority order)

1. **Existing Grok renders** — `Assets/_Project/Art/Neutral/Renders/Icons/{pieceId}_icon.png` (5 neutrals)
2. **BunkerSurvivalUI sprites** — `Assets/BunkerSurvivalUI/Sprites/Icons/icon_*.png` (buildings + utility)
3. **Prefab snapshot** — orthographic render of assigned toon/vehicle prefab → `Assets/_Project/Art/Sandbox/Renders/Icons/{pieceId}_icon.png`
4. **Colored placeholder** — fallback via extended `NeutralArtPipelineEditor.GeneratePlaceholderIcons` pattern (unique hue per piece)

### 3.4 Arena prefab sources

| Category | Source | Notes |
|----------|--------|-------|
| Infantry / role variants | `Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_*` | Role-based mapping (see §4) |
| Light transport | `RTS_Modern_Combat_Vehicle_Pack_Free/ATV_N1/0_Prefabs/ATV_N1_Color_0_Prefab.prefab` | Scale ~0.8–1.2 per footprint |
| Heavy tank / walker | `FA_N26/0_Prefabs/FA_N26_Color_*_Prefab.prefab` | Color variant differentiates pieces |
| Artillery / gunship | `MSH_N2/0_Prefabs/MSH_N2_Color_*_Prefab.prefab` | Mobile cannon, artillery hybrids |
| Buildings | `Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_*.prefab` | Existing cubes — **no change** this pass |

Arena presentation already reads `combatArenaPrefab`, `combatArenaModelScale`, and `combatArenaModelHeight` from `PieceDefinitionSO` via `CombatArenaPresenter` and `CombatArenaBuildingSpawner`.

---

## 4. Piece mapping (25 entries)

### 4.1 Neutral (10)

| pieceId | Arena prefab | Icon source |
|---------|--------------|-------------|
| `conscript_rifleman` | `TSww2_German_infantry` | Grok render |
| `grenade_thrower` | `TSww2_German_support` | Grok render |
| `field_medic` | `TSww2_German_medic` | Grok render |
| `armored_transport` | `ATV_N1_Color_0_Prefab` | Grok vehicle sheet |
| `mobile_cannon` | `MSH_N2_Color_0_Prefab` | Grok vehicle sheet |
| `neutral_supply_depot` | `ArenaBuilding_SupplyDepot` | `icon_fuel_canister` |
| `neutral_field_gun` | `ArenaBuilding_FieldGun` | `icon_generator_part` |
| `shock_trooper` | `TSww2_German_officer` | Prefab snapshot |
| `neutral_mortar_team` | `TSww2_German_support` | Prefab snapshot |
| `marksman_squad` | `TSww2_German_sniper` | Prefab snapshot |

### 4.2 IronMarch / iron_vanguard (15)

| pieceId | Arena prefab | Icon source |
|---------|--------------|-------------|
| `ironmarch_hq` | `ArenaBuilding_Hq` | `icon_bunker_map` |
| `rifle_squad` | `TSww2_German_infantry` | Prefab snapshot |
| `diesel_walker` | `FA_N26_Color_0_Prefab` | Prefab snapshot |
| `radio_array` | *(none — non-combat building uses spawner only)* | `icon_emergency_radio` |
| `mg_team` | `TSww2_German_support` | Prefab snapshot |
| `field_gun_nest` | `ArenaBuilding_FieldGun` | `icon_generator_part` |
| `supply_depot` | `ArenaBuilding_SupplyDepot` | `icon_fuel_canister` |
| `field_workshop` | `ArenaBuilding_SupplyDepot` *(reuse cube)* | `icon_toolbox` |
| `mobile_artillery` | `MSH_N2_Color_1_Prefab` | Prefab snapshot |
| `ironmarch_heavy_tank` | `FA_N26_Color_1_Prefab` | Prefab snapshot |
| `ironmarch_mortar` | `TSww2_German_support` | Prefab snapshot |
| `ironmarch_engineer` | `TSww2_German_medic` | Prefab snapshot |
| `ironmarch_breacher` | `TSww2_German_officer` | Prefab snapshot |
| `ironmarch_sniper` | `TSww2_German_sniper` | Prefab snapshot |
| `ironmarch_defender` | `TSww2_German_infantry` | Prefab snapshot |

**Notes:**

- `radio_array` is a utility building; assign icon only unless arena spawner requires a prefab — use smallest cube or omit prefab if spawner handles null gracefully.
- Multi-cell footprints use a **single representative prefab** centered on anchor; scale tuned per piece in catalog.
- German toon variants used for **both** neutral and IronMarch intentionally (temporary cohesion).

---

## 5. Implementation phases

### Phase A — Catalog + assigner (code)

- Add `SandboxArtCatalog`, `SandboxArtEntry`, `SandboxArtAssigner`
- Seed catalog asset with §4 mappings (hard-coded defaults in editor `CreateDefaultCatalog` menu)
- Menus: Apply, Validate

### Phase B — Icon pipeline

- Run Grok import + assign for 5 neutrals (existing menus)
- Configure BunkerSurvivalUI PNGs as sprites (if not already)
- Implement optional `SandboxIconSnapshotter` for IronMarch + remaining neutrals
- Write snapshots to `Assets/_Project/Art/Sandbox/Renders/Icons/`

### Phase C — Apply + verify

- Run **Apply Sandbox Art Pass** in Unity
- Run **Validate Sandbox Art Coverage**
- Add `SandboxArtCoverageTests` (EditMode)
- Manual: enter Run scene — shop cards show icons; combat arena shows toon infantry + vehicles

---

## 6. Testing

### EditMode

`SandboxArtCoverageTests.cs`:

- Loads `SandboxArtCatalog` and ContentDatabase
- For each catalog entry: `piece.icon != null`
- For each unit/hybrid entry: `piece.combatArenaPrefab != null`
- Buildings: icon required; prefab optional per building spawner rules

### PlayMode

Re-run existing `CombatArenaPlayModeTests.InitializeArena_ShowsHqFieldGunAndSupplyDepot` — should still pass (buildings unchanged).

Optional new test: spawn gauntlet board in arena — at least one toon unit visible (manual QA if PlayMode setup is heavy).

---

## 7. Explicitly deferred

- Enemy faction pieces (Crimson, Ash, Dust, Cartel)
- Per-cell board sprites (`cellSprites` on `PieceDefinitionSO`)
- Replacing arena building cubes with `_Creepy_Cat` modular geometry
- Grimdark / lore-accurate style pass
- WW2 realistic infantry (`WW2_German_soilders`) — rejected for this pass in favor of toon
- Audio (`PostApocalypseGunsDemo`, battle music demos)
- FOV mapping integration

---

## 8. File layout (new)

```
Assets/_Project/
  Data/
    Art/
      SandboxArtCatalog.asset
    Editor/
      SandboxArt/
        SandboxArtCatalogSO.cs
        SandboxArtAssigner.cs
        SandboxIconSnapshotter.cs
  Art/
    Sandbox/
      Renders/
        Icons/
  Core.Tests/
    EditMode/
      SandboxArtCoverageTests.cs
docs/superpowers/
  plans/
    2026-06-13-sandbox-art-asset-pass.md   # created by writing-plans (next step)
```

---

## 9. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Content regen clears `icon` / `combatArenaPrefab` | Catalog + one-click **Apply** menu; do not store mappings only on SO assets |
| Toon prefab scale wrong for multi-cell footprints | Per-entry `combatArenaModelScale` in catalog; tune in Play Mode |
| Grok PNG files missing on disk | Placeholder generator + snapshot fallback |
| Mixed toon + modern RTS vehicles look incoherent | Acceptable for temporary pass; document in catalog |
| `Generate Demo Content` overwrites piece fields | Run **Apply Sandbox Art Pass** after content generation (document in plan) |

---

## 10. Self-review checklist

- [x] No TBD placeholders in mapping table
- [x] Scope bounded to 25 sandbox pieces
- [x] Consistent with approved brainstorming decisions (C + B + A)
- [x] Reuses existing editor menus and arena infrastructure
- [x] Clear deferred list separates this pass from future art work
