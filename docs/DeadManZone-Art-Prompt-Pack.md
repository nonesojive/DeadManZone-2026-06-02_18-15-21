# DeadManZone — Ready-to-Use Art Prompt Pack

**Version:** 1.0  
**Date:** 2026-06-06  
**Purpose:** Copy-paste prompts for generating the 5 neutral demo pieces (shop icons + per-cell modular sprites) with maximum consistency.

---

## Style Bible (Lock This In)

Use this exact description at the start of **every** prompt:

```
grimdark retro-futurist WW1 trench warfare militia, worn olive drab uniforms with mud-brown leather straps and patches, dull gunmetal weapons and armor plates, off-white bandages, faded markings, beat-up practical field-expedient kit, no brass or heroic accents, muddy and weathered, orthographic 3/4 isometric view, single key light from upper left, cool fill light, soft ground shadow, transparent background, high readability at small sizes
```

**Camera/Lighting Lock (for Blender or consistent AI):**
- Projection: Orthographic
- Elevation: ~35°
- Azimuth: ~45° (classic 3/4)
- Facing: Piece front angled toward camera-right
- Background: Transparent PNG
- Resolution: 256×256 for shop icons, 128×128 for per-cell tiles

**Palette Reference (do not deviate):**
- Primary: Worn olive drab `#5C6B4F`
- Secondary: Mud brown `#4A3F2F`
- Metal: Dull gunmetal `#5A5F66`
- Accents: Off-white bandages `#D4D0C0`, faded red cross on medic (desaturated)

---

## Shop Icons (256×256) – Milestone 1 Priority

### 1. Conscript Rifleman (Style Anchor – Do This First)

**Prompt:**
```
[STYLE BIBLE]

Single hunched human soldier, 1x1 footprint, trench helmet, gas mask, patched olive drab greatcoat, carrying coil-carbine rifle diagonally across body, small backpack, practical and tired pose, strong clear silhouette with diagonal rifle read, grim and weathered, no base or diorama, centered on transparent background, 256x256 shop icon style
```

**File name:** `conscript_rifleman_icon.png`

**Notes:** This is your style anchor. Get this one looking good before moving to the others. Strong head + rifle silhouette is critical for readability at 48px.

---

### 2. Grenade Thrower (1×2 Vertical)

**Prompt:**
```
[STYLE BIBLE]

Human soldier in throwing pose, 1x2 vertical footprint, trench helmet, bandolier of grenades across chest, holding grenade in right hand ready to throw, patched olive drab coat, gas mask, upper cell = torso and arms, lower cell = legs and boots, reads as one cohesive unit when stacked vertically, grim and weathered, no base, centered, 256x256 shop icon style
```

**File name:** `grenade_thrower_icon.png`

---

### 3. Field Medic (1×1)

**Prompt:**
```
[STYLE BIBLE]

Human medic soldier, 1x1 footprint, softer less armored silhouette than rifleman, muted desaturated red cross armband on left arm, medic satchel, sidearm or no primary rifle, trench helmet, gas mask, patched olive drab coat, off-white bandages visible, supportive and slightly less aggressive pose, grim and practical, no base, centered, 256x256 shop icon style
```

**File name:** `field_medic_icon.png`

---

### 4. Armored Transport (2×3 L-shape)

**Prompt:**
```
[STYLE BIBLE]

WW1-era armored half-track truck, 2x3 L-footprint (cab front, cargo bed back), riveted dull gunmetal armor plates, muddy olive drab canvas covers, heavy tracks, practical field-expedient look, not sleek or modern, weathered and beaten, no base or crew visible, strong clear L silhouette, centered on transparent background, 256x256 shop icon style
```

**File name:** `armored_transport_icon.png`

---

### 5. Mobile Cannon (3×2)

**Prompt:**
```
[STYLE BIBLE]

Field gun on wheeled carriage, 3x2 rectangular footprint, long barrel pointing along the long axis, ammunition crates and shells visible, dull gunmetal and muddy olive tones, heavy practical WW1 artillery look, weathered, strong long silhouette, no base or crew, centered, 256x256 shop icon style
```

**File name:** `mobile_cannon_icon.png`

---

## Per-Cell Modular Sprites (128×128) – Milestone 2 Priority

These are used for the actual board grid. They must work when rotated and when assembled into larger shapes.

**General rules for all cell prompts:**
- Add at the end: `128x128 per-cell modular sprite, centered with 4px transparent gutter, clean edges for grid tiling, orthographic 3/4 isometric`
- Keep the same [STYLE BIBLE] + camera lock.
- Avoid hard one-sided details (text, asymmetric markings) so rotation works.

### Conscript Rifleman Cell

**Prompt:**
```
[STYLE BIBLE] + camera lock

Single hunched soldier filling one cell, same as shop icon but tighter framing for 128x128 modular use, strong silhouette, centered with small transparent gutter, clean edges
```

**File name:** `infantry_cell.png` (or `conscript_cell.png`)

---

### Grenade Thrower Cells (Upper + Lower)

**Upper cell prompt:**
```
[STYLE BIBLE] + camera lock

Upper half of throwing soldier (torso, arms, head, grenade in hand), designed to stack vertically with lower cell, clean bottom edge for seamless join, 128x128 modular sprite
```

**Lower cell prompt:**
```
[STYLE BIBLE] + camera lock

Lower half of soldier (legs, boots, lower coat), designed to stack vertically with upper cell, clean top edge for seamless join, 128x128 modular sprite
```

**File names:** `grenade_upper.png`, `grenade_lower.png`

---

### Field Medic Cell

**Prompt:**
```
[STYLE BIBLE] + camera lock

Medic soldier filling one cell, slightly softer silhouette, visible desaturated red cross armband, centered, clean edges, 128x128 modular sprite
```

**File name:** `medic_cell.png`

---

### Armored Transport Cells (L-shape assembly)

You will need multiple modular pieces. Recommended set:

- `vehicle_cab.png` (front cab section)
- `vehicle_hull.png` (main body)
- `vehicle_track.png` (track sections – make symmetric)
- `vehicle_rear.png` (cargo bed / rear gate)

**Example prompt for cab:**
```
[STYLE BIBLE] + camera lock

Front cab section of armored half-track, riveted plates, muddy canvas, designed to connect to hull section on the right, clean right edge, 128x128 modular sprite
```

Create the others similarly with clear connection edges.

---

### Mobile Cannon Cells

Recommended modular pieces:

- `cannon_barrel.png` (long barrel section – spans multiple cells visually)
- `cannon_carriage.png`
- `cannon_wheel.png`

**Example prompt:**
```
[STYLE BIBLE] + camera lock

Section of long field gun barrel on carriage, dull gunmetal, weathered, designed to tile horizontally with other barrel/carriage sections, clean edges, 128x128 modular sprite
```

---

## Quick Tips for Best Results

1. **Generate the shop icons first** (Milestone 1). They have the biggest immediate impact.
2. **Generate per-cell tiles second** (Milestone 2). Start with Conscript single cell — it's the simplest and most used.
3. If using pure AI image tools, generate 4–6 variations per piece and pick the one with strongest silhouette and best small-size readability.
4. If using Blender: Create one master `neutral_render_template.blend` with locked camera/lights, then duplicate and swap models.
5. After generating, always test at **shop card size (~48 px)** and on the actual zone background colors.

---

## File Naming Convention (Recommended)

Shop icons:
- `conscript_rifleman_icon.png`
- `grenade_thrower_icon.png`
- `field_medic_icon.png`
- `armored_transport_icon.png`
- `mobile_cannon_icon.png`

Per-cell / modular:
- `infantry_cell.png`
- `grenade_upper.png` / `grenade_lower.png`
- `medic_cell.png`
- `vehicle_cab.png`, `vehicle_hull.png`, `vehicle_track.png`, `vehicle_rear.png`
- `cannon_barrel.png`, `cannon_carriage.png`, `cannon_wheel.png`

Place them in:
`Assets/_Project/Art/Neutral/Renders/Icons/` and `.../Cells/`

---

*Use this pack alongside the main Demo Completion Roadmap. Generate in the order shown for maximum parallel efficiency.*