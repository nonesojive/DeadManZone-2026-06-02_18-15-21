> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# IronMarch Union Visual Pass — Design Spec

**Date:** 2026-07-01  
**Branch:** `contentpassv1`  
**Scope:** Art + data wiring for the 17-piece IronMarch Union roster. **Out of scope:** ShopScene layout, UI chrome, theme, or HUD changes.

---

## Goal

Give every piece in the current roster readable **shop icons**, **board placement art**, **combat battlefield sprites**, plus confirm **battlefield grid / skybox** assets — using project-owned art first (Grok isometric sheets, Combat2D library, existing board tile JPGs). Photoshop / Blender only for gaps; Autosprite last resort.

---

## Asset Audit (what exists today)

### Already on disk and usable

| Category | Location | Status |
|----------|----------|--------|
| Shop isometric roster (5 units + 2 vehicles) | `Assets/Grok Images/Isometric Batch 2/` | High-quality WW1 isometric; importer exists (`GrokBatch2IconImporter`) |
| IronMarch character concepts (9 units) | `Assets/Grok Images/Isometric Batch 1/grok-image-c06f4efa-…jpg` | Maps to faction-specific units (surgeon, marksman, marshal, bulwark, etc.) |
| Vehicle pair (half-track + field gun) | `Isometric Batch 2/grok-image-6f700ac6-…jpg` | armored_transport / ironclad_mortars |
| Neutral shop icons (5) | `Assets/_Project/Art/Neutral/Renders/Icons/` | PNGs exist but **not assigned** on piece `.asset` files |
| Combat2D environment (6) | `Assets/_Project/Art/Combat2D/Environment/` | Wired in `CombatArena2DEnvironmentArt.asset` |
| Combat2D silhouettes (5) | `Combat2D/Units/Silhouettes/` | Wired in `CombatArena2DSilhouetteArt.asset` |
| Combat2D buildings (10) | `Combat2D/Buildings/` | supply_depot, command_bunker, field_gun, ironmarch_hq, etc. |
| Combat2D VFX (3 strips) | `Combat2D/VFX/` | Wired in `CombatArena2DVfxArt.asset` |
| Combat2D unit animations (legacy IDs) | `Combat2D/Units/Animations/` | conscript_rifleman, field_medic, ironmarch_sniper, rifle_squad, shock_trooper, etc. |
| Shop board lane tiles | `Assets/Grok Images/Fronttile*.jpg`, `Supporttile*.jpg`, `Reartile*.jpg` | Already referenced by `BoardTerrainArt.asset` (rear/support/front/reserve) |
| Board terrain cell | `SupporttileB1.jpg` etc. | `BoardTerrainArt.cellSprite` currently **null** |

### Gaps

- All **17 roster piece assets** have `icon: null`, `cellSprites: []`, `combatArenaSprite: null`.
- `SandboxArtCatalog` + `SandboxArtRoster` still list **pre–content-pass piece IDs** (rifle_squad, ironmarch_breacher, …). Synty icon paths point to an **empty** `Art/Synty/` folder.
- Missing Combat2D dedicated sprites for: field_hospital, officer_quarters, recruitment_office, surgical_center, bulwark_squad, enlisted_rifleman, ironmarch_surgeon, ironmarch_iron_horse, ironclad_mortars, ironclad_marksman, ironclad_field_marshal (buildings partially covered).
- `combat2d_sky_gradient.png` on disk reads as a dirt texture, not a sky gradient — needs replacement or re-export.

---

## Art Direction (two layers, intentional)

| Context | Style | Rationale |
|---------|-------|-----------|
| **Shop + board pieces** | Isometric Grok WW1 dieselpunk (matches existing `conscript_rifleman_icon.png`) | ShopScene already built around isometric unit icons on lane tiles |
| **Combat arena** | Top Troops 2D — side-view sprites / silhouettes on dirt grid | `CombatArena2D` brief; silhouettes + role tints already implemented |

Shop and combat **do not** need pixel-identical art; they need **recognizable silhouettes** per piece (scoped rifle = marksman, red-cross armband = medic, etc.).

---

## Piece → Source Mapping (P0)

| piece_id | Shop icon source | Board cells | Combat `combatArenaSprite` |
|----------|------------------|-------------|---------------------------|
| conscript_rifleman | Batch2 roster [0] | icon per 1×1 cell | silhouette_assault + rifle_squad idle sheet |
| field_medic | Batch2 roster [2] | icon per cell | `combat2d_unit_field_medic.png` or medic anims |
| armored_transport | Batch2 vehicle left | icon stretched on L-shape cells | silhouette_vehicle |
| supply_depot | crop building / existing icon pipeline | per-cell from building sprite | `combat2d_building_supply_depot` |
| field_hospital | Batch1 medic tent concept or new crop | 2×2 cell tiles from icon | new `combat2d_building_field_hospital` (crop from grok or photoshop) |
| officer_quarters | Batch1 scout/officer (binoculars) | 2×2 cells | `combat2d_building_ironmarch_hq` |
| command_outpost | command_bunker crop | 2-cell horizontal | `combat2d_building_command_bunker` |
| surgical_center | Batch1 surgeon variant | 1×1 | hospital sprite tint |
| recruitment_office | supply_depot recolor crop | 1×1 | supply_depot variant |
| ironmarch_surgeon | Batch1 surgeon (bone saw) | 1×1 | ironmarch_engineer anim set → rename wire |
| bulwark_squad | Batch1 anchor melee | 1×1 | ironmarch_breacher / assault silhouette |
| enlisted_rifleman | Batch2 rifleman or rifle_squad icon | 1×1 | rifle_squad animations |
| ironmarch_iron_horse | Batch1 heavy tank top-left | 6-cell footprint | silhouette_vehicle + scale |
| ironclad_mortars | Batch2 artillery / 6f700ac6 right | 2-cell vertical | silhouette_artillery |
| ironclad_marksman | Batch1 slouch-hat marksman | 1×1 | ironmarch_sniper animations |
| ironclad_field_marshal | Batch1 peaked-cap officer | 1×1 | shock_trooper animations |
| machine_gun_nest | field_gun nest crop | 2-cell horizontal | `combat2d_building_field_gun` |

Legacy animation folders are **renamed/wired by ID alias table**, not duplicated on disk.

---

## Architecture

### New editor pipeline (minimal)

1. **`IronmarchArtPaths`** — extends `PieceArtPaths` pattern with `IronmarchIcons`, `IronmarchCells` folders under `Art/Ironmarch/`.
2. **`IronmarchUnionIconImporter`** — crops Batch 1 + Batch 2 sheets (same math as `GrokBatch2IconImporter`).
3. **`IronmarchArtAssigner`** — assigns `icon`, `cellSprites` (1:1 sprite per shape cell from icon for MVP), `combatArenaSprite` from Combat2D folder.
4. **`CombatArena2DAnimationAliasTable`** — maps new piece IDs → existing animation subfolders.
5. Update **`CombatArena2DSceneBootstrap.Wire2DBuildingArt`** roster to all building/structure pieces.
6. **`BoardTerrainArt`**: assign `cellSprite` from `SupporttileB1.jpg` (existing GrokTerrainArtEditor pattern); leave lane tile arrays untouched.

### Data flow

```
Grok sheets → PNG icons (256px) → PieceDefinitionSO.icon
              └→ cellSprites[] (same sprite per occupied cell)
Combat2D PNGs → PieceDefinitionSO.combatArenaSprite
Animation folders → CombatArena2DAnimationSetSO (aliased IDs)
BoardTerrainArtSO → shop board grid (unchanged lane arrays + cellSprite fill)
CombatArena2DEnvironmentArtSO → combat grid + sky (re-export sky if needed)
```

### ShopScene constraint

Only touch **piece data** and **terrain ScriptableObjects**. No prefab/scene edits under `Presentation/Shop/` except what auto-resolves from data.

---

## Approaches Considered

| Approach | Pros | Cons |
|----------|------|------|
| **A. Remap existing Grok + Combat2D (recommended)** | Fast, style-consistent with conscript icons, zero new gen cost | Combat side-view ≠ shop isometric |
| **B. Blender isometric re-render all 17** | Perfect cell control for multi-cell shapes | High time cost; duplicates Grok quality |
| **C. Autosprite combat sprites** | Fast animated combat | Style drift from shop; user listed as last resort |

**Recommendation:** A for this milestone. B only for 2×2 buildings if crops look bad in Play mode.

---

## Implementation Phases

### Phase 1 — Wiring pass (same session)
- Run icon importer for all 17 IDs
- Assign icons + cell sprites on piece assets
- Wire combat building sprites (bootstrap)
- Alias animation sets for units
- Set `BoardTerrainArt.cellSprite`
- Fix sky gradient asset

### Phase 2 — Gap fills (Photoshop)
- `combat2d_building_field_hospital.png`
- Any icon crop that fails silhouette read at 64px

### Phase 3 — Verification
- Play mode: shop offers show icons, board placement shows cells, combat shows sprites not grey placeholders
- EditMode: extend `CombatArena2DHelpersTests` with alias coverage assert
- Update `combat-arena-2d-asset-tracker.csv` status columns

---

## Testing

| Check | Method |
|-------|--------|
| All 17 pieces have `icon != null` | Editor validation menu + EditMode test |
| Combat resolver returns non-placeholder for each role | `CombatArena2DHelpersTests` |
| No ShopScene file changes | `git diff -- Assets/_Project/Scenes/Shop*` empty |
| BoardTerrainArt.cellSprite assigned | Asset inspection |

---

## Risks

1. **Style mismatch** shop isometric vs combat side-view — mitigated by consistent silhouettes per piece.
2. **Multi-cell board art** — MVP uses same icon per cell; upgrade path is Blender per-cell renders.
3. **Sky texture wrong file** — must re-export before calling environment done.
4. **SandboxArtCatalog drift** — defer Synty catalog update; IronMarch pass uses Neutral/Ironmarch paths only.

---

## Out of Scope

- ShopScene UI layout, fonts, colors, button art
- New VFX beyond existing P0 strips
- Synty 3D arena prefab restoration
- Autosprite generation (unless Phase 2 gaps remain after Photoshop)
