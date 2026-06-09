# DeadManZone — Neutral Faction Art & Asset Pipeline Design

**Date:** 2026-06-05 (camera updated 2026-06-07)  
**Engine:** Unity 6  
**Status:** Approved — isometric tokens + top-down terrain locked  
**Scope:** Art pipeline and asset specs for the 5 neutral demo pieces  
**Builds on:** `2026-05-31-deadmanzone-autobattler-design.md`, `2026-06-04-deadmanzone-combat-units-demo-design.md`  
**Visual commitment:** `2026-06-06-deadmanzone-top-down-visual-commitment.md`

---

## Summary

Create neutral faction piece art using a **SuperGrok Imagine → 2D sprite** pipeline (primary), with optional **Blender → PNG** fallback for vehicles. Unit tokens use a locked **orthographic 3/4 isometric** camera; terrain tiles use **true top-down**. Same unit sprites serve shop icons, board cells, and combat replay. Delivery is phased: shop icons first (plugs into existing `PieceDefinitionSO.icon` today), per-cell board tiles later (requires a separate code implementation plan).

**Neutral roster (5 pieces):** Conscript Rifleman, Grenade Thrower, Field Medic, Armored Transport, Mobile Cannon.

---

## Section 1 — Visual Direction & Camera Standard

### Faction read: Neutral forces

Neutrals are generic trench militia — not faction-proud like Iron Vanguard.

| Attribute | Direction |
|-----------|-----------|
| **Palette** | Worn olive drab, mud-brown leather, dull gunmetal, off-white bandages, faded markings (no brass hero accents) |
| **Wear** | Beat-up kit, patched coats, scuffed vehicles, practical not ornate |
| **Retro-futurist** | Coil-rifle hints, welded armor plates, gas-mask silhouettes — subtle, not sci-fi clean |
| **vs Iron Vanguard** | IV = brass, diesel glow, industrial precision. Neutral = mud, canvas, field-expedient |

### Two-layer camera standard (locked)

**Unit tokens** — isometric 3/4 (infantry readability at grid scale).  
**Terrain tiles** — true top-down (trenches, mud, bunker walls).

#### Unit token camera (shop + board + combat)

| Setting | Value |
|---------|--------|
| **Projection** | Orthographic |
| **Elevation** | **~35°** (classic 3/4 isometric) |
| **Azimuth** | **~225°** (token faces bottom-right of frame) |
| **Lighting** | Key upper-left, cool fill — grim, not studio-bright |
| **Background** | Transparent PNG (remove white from AI exports) |
| **Shadow** | Soft oval drop shadow baked in (consistent across roster) |

**AI style anchor:** `Assets/Grok Images/Isometric Batch 2/grok-image-2eb75a93-e52d-4847-ae43-03394588e5fd.jpg`  
**Blender fallback:** `Assets/_Project/Art/Neutral/Source/neutral_token_camera.py`

#### Terrain tile camera

| Setting | Value |
|---------|--------|
| **Projection** | Orthographic top-down (90°) |
| **Use** | Zone cell backgrounds only — not unit tokens |
| **Examples** | `Assets/Grok Images/FronttileA*.jpg`, `ReartileA.jpg` |

### Deliverable sizes

| Asset | Resolution | Use |
|-------|------------|-----|
| Shop icon | 256×256 px | `PieceDefinitionSO.icon` — works in current build |
| Board / combat token | 128×128 px | Per-cell modular art (Phase 3); combat replay reuses same sprites |
| Style reference sheet | 1920×1080 px | Camera, lighting, palette documentation |

### Board rotation

Pieces rotate 0° / 90° / 180° / 270° on the grid. Phase 3 uses **modular per-cell tiles** that compose into footprints (handles L-shaped Armored Transport without four full footprint variants per piece).

---

## Section 2 — Pipeline, Piece Briefs & Budgets

### AI → Unity pipeline (primary)

```
SuperGrok Imagine (style-locked isometric sprite sheet)
    ↓
Crop units + remove background → PNG
    ↓
Assets/_Project/Art/Neutral/Renders/Icons/
    ↓
Unity import (Sprite 2D/UI, sRGB, no mips)
    ↓
Assign on PieceDefinitionSO.icon (Phase 1–2)
    ↓
[Phase 3] Per-cell board tiles → PieceShapeVisual
```

### Blender → Unity pipeline (optional — vehicles / consistency)

```
Blender or Meshy model → neutral_token_camera.py isometric render
    ↓
Same PNG folders and Unity import path as AI pipeline
```

### Template scene rules

- One master file: `neutral_render_template.blend`
- Fixed camera, lights, and resolution — never eyeball per piece
- Ground plane at Y=0; pieces centered on origin before framing
- Export naming: `{piece_id}_icon.png` (e.g. `conscript_rifleman_icon.png`)

### Folder structure

```
Assets/_Project/Art/
  Neutral/
    Source/              # .blend per piece
    Renders/
      Icons/             # 256×256 shop sprites
      Cells/             # 128×128 modular tiles (Phase 3)
    StyleSheet/          # palette + camera reference
  Shared/
    neutral_render_template.blend
```

### Unity import defaults

| Setting | Value |
|---------|--------|
| Texture Type | Sprite (2D and UI) |
| sRGB | On |
| Mip Maps | Off |
| Filter Mode | Bilinear |
| Pixels Per Unit | 100 |
| Max Size | 256 (icons), 128 (cells) |

### Per-piece modeling briefs

All five use `categoryTint` ≈ muted olive `(0.45, 0.48, 0.42)` on their ScriptableObjects — materials should harmonize.

| Piece ID | Grid (demo build) | Silhouette hook | Art notes |
|----------|-------------------|-----------------|-----------|
| `conscript_rifleman` | 1×1 | **Style anchor** | Hunched rifleman, rifle, trench helmet, gas mask, small pack. |
| `grenade_thrower` | 1×1 | Throw pose | Grenade in raised hand, bandolier. Shop icon can show full figure; board uses single cell. |
| `field_medic` | 1×1 | Support identity | Faded red-cross armband, medic satchel, no primary rifle. |
| `armored_transport` | **1×2** horizontal | Half-track vehicle | Side-view half-track from Grok fits 1×2. *(Design docs previously said 2×3 L — not in demo.)* |
| `mobile_cannon` | **1×2** horizontal | Towed field gun | Carriage + barrel from Grok fits 1×2. *(Design docs previously said 3×2 — not in demo.)* |

Shapes may expand post-demo; art pipeline targets **current `PieceDefinitionSO.shapeCells`** first.

**Optional ability hints on icons:** grenade in hand (Grenade Thrower), slab armor (Armored Transport), prominent barrel + ammo crate (Mobile Cannon).

### Poly & texture budgets (MVP)

| Category | Tris | Texture |
|----------|------|---------|
| Infantry 1×1 | 2,000–4,000 | 1024² or shared atlas |
| Tall infantry 1×2 | 3,000–5,000 | 1024² |
| Armored Transport | 8,000–12,000 | 1024² (2048² if needed) |
| Mobile Cannon | 8,000–15,000 | 1024²–2048² |

**PBR maps:** Base Color, Roughness, Normal (optional at icon scale), Metallic. Weathering primarily in base color.

**Render framing:** Icons fill ~70–80% of 256×256 frame. Cells centered in 128×128 with ~4px transparent gutter for grid spacing.

### Shared material library

| Material | Use |
|----------|-----|
| `MUD_CANVAS` | Uniforms, tarps |
| `GUNMETAL` | Weapons, rivets |
| `WORN_LEATHER` | Straps, boots |
| `DULL_OLIVE` | Vehicle hulls |
| `MEDIC_WHITE` | Armband, bandages (desaturated) |
| `RUST_PATCH` | Weathering accent |

### Phased delivery

| Phase | Deliverable | Requires code? |
|-------|-------------|----------------|
| **1** | Conscript Rifleman icon + template scene + style sheet | No |
| **2** | Icons for remaining 4 neutrals | No |
| **3** | Per-cell tiles + board sprite hook | Yes — separate implementation plan |
| **4** | Drag-ghost sprites, faction material variants | Optional |

**Phase 1 exit criteria:** Conscript icon in Unity, readable at 256px and ~48px shop scale, neutral palette locked, visually distinct from future IV aesthetic.

---

## Section 3 — QA, Modular Tiles & Code Handoff

### Readability QA checklist

| Test | Pass criteria |
|------|---------------|
| Shop @ 256px | Silhouette identifiable within 2 seconds |
| Shop @ ~48px | Distinguishable from other 4 neutrals without relying on color alone |
| Dark panel | Readable on `cardColor` (~`#292B35`) |
| Zone backgrounds | Acceptable on rear / support / front zone tints |
| Side-by-side | All 5 in one row — no duplicates |
| vs Iron Vanguard | Field militia, not brass industrial |

**Review artifact:** `neutral_roster_review.png` — all 5 icons at full and 50% scale.

### Modular cell rules (Phase 3)

| Piece | Cells to export | Notes |
|-------|-----------------|-------|
| Conscript Rifleman | 1 `infantry_cell` | Full figure in one cell |
| Grenade Thrower | `grenade_upper`, `grenade_lower` | Vertical stack |
| Field Medic | 1 `medic_cell` | Distinct from rifleman at cell scale |
| Armored Transport | `0_0`, `1_0` | One sprite per horizontal cell (demo 1×2) |
| Mobile Cannon | `0_0`, `1_0` | Barrel cell + carriage/wheel cell |

**Rotation:** Isometric tokens rotate on the grid; prefer mostly symmetric silhouettes or accept mild rotation artifacts. Token front faces bottom-right of source art at rotation 0.

**Demo footprints (R0):**
```
Armored Transport:  [cell][cell]
Mobile Cannon:      [cell][cell]
```

### Unity assignment (Phase 1–2)

| `PieceDefinitionSO` field | Asset path |
|---------------------------|------------|
| `icon` | `Neutral/Renders/Icons/{piece_id}_icon.png` |
| `categoryTint` | Keep `(0.45, 0.48, 0.42)` or tune after icon review |

### Implementation boundary (Phase 3)

Board sprites are not wired today. Separate implementation plan will cover:

| Component | Change |
|-----------|--------|
| `PieceDefinitionSO` | Optional `cellSprites` array keyed by local `shapeCells` offset |
| `PieceShapeVisual` | Sprite cells instead of tinted `Image` blocks |
| `DragGhost` | Optional sprite reuse |
| `ContentDatabase` | No change — manual SO assignment |

**Recommended approach:** Per-cell sprites indexed by shape offset; rotation via existing `ShapeTransforms.RotateOffset` without pre-baking four orientations per piece.

### Out of scope

- Rigging / animation
- 3D in-engine combat rendering (deferred — see top-down visual commitment spec)
- Iron Vanguard pieces (separate spec)
- Combat VFX (separate presentation pass; uses same top-down tokens)

### Success criteria

**Phase 1–2 done when:**
- [ ] `neutral_render_template.blend` in repo or documented external path
- [ ] Style sheet PNG committed
- [ ] 5 shop icons at 256×256, correctly named
- [ ] Icons assigned on all neutral `PieceDefinitionSO` assets
- [ ] `neutral_roster_review.png` passes QA checklist

**Phase 3 done when:**
- [ ] Per-cell tiles for all footprints
- [ ] Assembly maps documented
- [ ] Board sprite implementation plan approved and executed

---

## References

- Piece data: `Assets/_Project/Data/Resources/DeadManZone/Pieces/`
- Visual hook: `PieceDefinitionSO.icon`, `categoryTint`
- Board renderer (today): `Assets/_Project/Presentation/Board/PieceShapeVisual.cs`
- UI palette: `Assets/_Project/Presentation/Visual/UiThemeSO.cs`
