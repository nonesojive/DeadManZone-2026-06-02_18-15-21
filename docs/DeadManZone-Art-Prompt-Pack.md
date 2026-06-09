# DeadManZone — Ready-to-Use Art Prompt Pack

**Version:** 1.1  
**Date:** 2026-06-07  
**Purpose:** Copy-paste prompts for generating neutral demo art — **isometric unit tokens** on **top-down terrain**.  
**Visual commitment:** `docs/superpowers/specs/2026-06-06-deadmanzone-top-down-visual-commitment.md`

---

## Two-layer visual standard

| Asset type | Camera | Tool |
|------------|--------|------|
| **Unit tokens** (shop, board, combat) | Orthographic **3/4 isometric** (~35°) | SuperGrok Imagine (primary) or Blender |
| **Terrain tiles** (zone backgrounds) | True **top-down** (90°) | SuperGrok / existing `Assets/Grok Images/` |

**Why split?** Infantry is unreadable as pure top-down helmet blobs. Terrain reads best birds-eye. This is the locked demo standard.

**Style anchor (approved Batch 2 roster):**  
`Assets/Grok Images/Isometric Batch 2/grok-image-2eb75a93-e52d-4847-ae43-03394588e5fd.jpg`

**Legacy reference sheet:**  
`Assets/Grok Images/Isometric/grok-image-0211da6d-2b71-444a-ad30-4781dae097e0.jpg`

**Grenade thrower single reference:**  
`Assets/Grok Images/Isometric/gtvnM.jpg`

---

## Style Bible (unit tokens — use at start of every prompt)

```
grimdark retro-futurist WW1 trench warfare militia, worn olive drab uniforms with mud-brown leather straps and patches, dull gunmetal weapons and armor plates, off-white bandages, faded markings, beat-up practical field-expedient kit, no brass or heroic accents, muddy and weathered, orthographic 3/4 isometric view facing bottom-right, thick clean outlines, single key light from upper left, cool fill light, soft oval drop shadow, high readability at small sizes, game token sprite style
```

**Camera lock (units only):**
- Projection: Orthographic
- Elevation: ~35° (3/4 isometric — NOT birds-eye top-down)
- Facing: Token faces **bottom-right** of frame
- Background: Gray or transparent (remove white before Unity import)
- Resolution: 256×256 shop icons, 128×128 board tokens

**Palette (do not deviate):**
- Primary: Worn olive drab `#5C6B4F`
- Secondary: Mud brown `#4A3F2F`
- Metal: Dull gunmetal `#5A5F66`
- Accents: Off-white bandages `#D4D0C0`, faded red cross on medic (desaturated)

**Neutral tone — DO NOT include:**
- Glowing eyes / neon accent colors as primary identity
- Purple magic, demon faces, horns, energy beams
- Baked flames, sparks, lightning in idle sprites
- Spiky post-apocalyptic Mad Max vehicles

---

## SuperGrok — full neutral roster sheet (primary prompt)

Attach style anchor `grok-image-2eb75a93` (Batch 2 roster) as reference image if Grok supports it.

```
Using the exact art style of the attached reference sheet (thick outlines, olive drab WW1 gas-mask soldiers, muddy weathered, orthographic 3/4 isometric view facing bottom-right, soft drop shadow):

Generate these 5 neutral trench militia units in one horizontal row, same scale and style:

1. Conscript rifleman — hunched, bolt-action rifle, small pack (1×1)
2. Grenade thrower — throwing pose, bandolier, pineapple grenade raised (1×2 tall feel)
3. Field medic — medic satchel, faded desaturated red cross armband, no rifle (1×1)
4. Armored transport — WW1 riveted half-track truck, olive drab canvas, muddy side view, NO spikes (demo grid: **1×2** horizontal)
5. Mobile cannon — field gun on wheeled carriage, barrel along long axis, ammo crates (demo grid: **1×2** horizontal)

RULES: field-expedient militia NOT superheroes. No purple magic, no glowing eyes, no flames, no lightning. Dull gunmetal and worn canvas. Readable game tokens for a dark UI autobattler. Gray background between units.
```

---

## SuperGrok — infantry-only sheet (warm-up)

Generate this first if the full roster sheet is too messy.

```
[STYLE BIBLE]

Top-down tactical game sprite sheet, grimdark WW1 trench militia tokens, orthographic 3/4 isometric view facing bottom-right, olive drab weathered kit, gray background, 1×3 horizontal row:

1. Conscript rifleman — hunched, bolt-action rifle, gas mask, small pack
2. Field medic — red cross armband, medic satchel, softer pose
3. Grenade thrower — tall vertical pose, grenade in raised hand, bandolier

Same faction, same lighting, strong distinct silhouettes, thick outlines, game-ready 2D tokens, readable at 48px. No glowing eyes, no magic effects.
```

---

## SuperGrok — follow-up prompts

**Clean row after messy sheet:**
```
Take the soldier designs from the previous image only. Re-draw as a clean sprite row: 5 separate isometric game tokens on dark gray background, evenly spaced, consistent scale, same grimdark WW1 olive drab palette, each facing bottom-right, thick outlines, soft drop shadows.
```

**Single unit refinement:**
```
Isolate and redraw only the [CONSCRIPT RIFLEMAN / FIELD MEDIC / etc.] from the previous sprite sheet as a single centered 256×256 isometric game token. Same style as reference, gray background, stronger silhouette, facing bottom-right, no base, no VFX.
```

**Vehicles only (after infantry locked):**
```
[STYLE BIBLE]

Two isometric vehicle game tokens on gray background, facing bottom-right, same line weight and palette as WW1 infantry reference:

1. Armored transport — WW1 half-track truck, riveted plates, olive drab canvas, L-shaped silhouette from 3/4 view, muddy, practical, NO spikes, NO glow effects
2. Mobile cannon — field gun on wheeled carriage, long barrel, ammo crates, dull gunmetal

WW1 field-expedient, not sci-fi, not Mad Max. Game-ready sprites, readable silhouettes.
```

**Negative prompt (if supported):**
```
modern military, sleek sci-fi, clean uniforms, brass gold accents, diesel glow, bright neon, cartoon chibi, true birds-eye top-down view, side view, purple magic, demon eyes, horns, flames, lightning, blood, gore, text, watermark, blurry edges
```

---

## SuperGrok — terrain tiles (top-down)

Use for zone backgrounds — **different camera from units**.

```
[STYLE BIBLE adapted for terrain]

True orthographic top-down view (birds-eye, 90 degrees), seamless game tile texture, grimdark WW1 trench battlefield ground, [rear support zone / front line mud / bunker wall / barbed wire], rust-orange muddy earth, boot prints, scattered barbed wire, desaturated, high detail, tileable edges, no characters, no vehicles, 512x512
```

Existing terrain references: `Assets/Grok Images/FronttileA1.jpg`, `ReartileA.jpg`, `Bunkerwall1.jpg`

---

## Shop icons (256×256) — per-piece prompts

### 1. Conscript Rifleman (style anchor)

```
[STYLE BIBLE]

Single hunched human soldier, 1x1 footprint, trench helmet, gas mask, patched olive drab greatcoat, bolt-action rifle, small backpack, practical tired pose, strong silhouette, isometric 3/4 facing bottom-right, grim and weathered, soft drop shadow, centered, 256x256 game icon
```

**File:** `conscript_rifleman_icon.png`

### 2. Grenade Thrower (1×2)

```
[STYLE BIBLE]

Human soldier in throwing pose, 1x2 vertical footprint feel, trench helmet, bandolier, pineapple grenade in raised hand, patched olive drab coat, gas mask, reads as one cohesive unit, isometric 3/4 facing bottom-right, 256x256 game icon
```

**File:** `grenade_thrower_icon.png` — see also `gtvnM.jpg`

### 3. Field Medic (1×1)

```
[STYLE BIBLE]

Human medic soldier, 1x1 footprint, softer silhouette than rifleman, muted desaturated red cross armband, medic satchel, no primary rifle, trench helmet, gas mask, supportive pose, isometric 3/4 facing bottom-right, 256x256 game icon
```

**File:** `field_medic_icon.png`

### 4. Armored Transport (1×2 demo)

```
[STYLE BIBLE]

WW1-era armored half-track truck from isometric side view, riveted dull gunmetal, muddy olive drab canvas, heavy tracks, practical field-expedient, NOT modern tank, NOT spiky, isometric 3/4 facing bottom-right, 256x256 game icon
```

**File:** `armored_transport_icon.png` — vehicle reference: `Isometric Batch 2/grok-image-129be410`

### 5. Mobile Cannon (1×2 demo)

```
[STYLE BIBLE]

Field gun on wheeled carriage from isometric view, long barrel, ammunition crates, dull gunmetal and muddy olive, heavy WW1 artillery, weathered, isometric 3/4 facing bottom-right, 256x256 game icon
```

**File:** `mobile_cannon_icon.png`

---

## Per-cell modular sprites (128×128) — Phase 3

Add to every cell prompt:
`128x128 per-cell modular sprite, isometric 3/4 facing bottom-right, centered with 4px transparent gutter, clean edges for grid tiling`

See neutral art spec for cell breakdown: `conscript` (1 cell), `grenade_upper/lower`, `medic_cell`, `vehicle_cab/hull/track/rear`, `cannon_barrel/carriage/wheel`.

---

## Post-processing checklist

1. **Remove backgrounds** — AI white/gray → transparent PNG (GIMP color-to-alpha or remove.bg)
2. **Crop** — 256×256 icons; ~70–80% frame fill
3. **Test at 48px** — shop card readability on dark panel `#292B35`
4. **Test on terrain** — tokens must pop against orange mud tiles (`FronttileA1.jpg`)
5. **Assign in Unity** — `DeadManZone → Art → Assign Neutral Icons From Renders`
6. **Avoid** — picking units from high-fantasy sheets (`grok-image-e6caa845` purple tanks, glowing-eye hero portraits) for neutral demo

---

## File naming & folders

**Shop icons** → `Assets/_Project/Art/Neutral/Renders/Icons/`
- `conscript_rifleman_icon.png`
- `grenade_thrower_icon.png`
- `field_medic_icon.png`
- `armored_transport_icon.png`
- `mobile_cannon_icon.png`

**Board cells** → `Assets/_Project/Art/Neutral/Renders/Cells/`

**Grok mood board** → `Assets/Grok Images/Isometric/` (not imported as sprites — reference only)

---

## Quick tips

1. Lock style with `grok-image-0211da6d` before generating the full roster.
2. Generate 4–6 variations per piece; pick strongest **silhouette** at 48px, not prettiest at full size.
3. Meshy/Blender is optional — use only if AI vehicles fail L-shape / 3×2 readability.
4. Infantry first, vehicles second with infantry sheet attached as style reference.
5. Combat VFX (muzzle flash, gas) are added in Unity — do not bake into idle token art.

---

**Import cropped Batch 2 icons:**
- PowerShell: `Assets/_Project/Art/Neutral/Source/import_grok_batch2_icons.ps1`
- Unity: `DeadManZone → Art → Import Grok Batch 2 Icons` then `Assign Neutral Icons From Renders`

*Use alongside `docs/DeadManZone-Demo-Completion-Roadmap.md` and `docs/superpowers/specs/2026-06-05-deadmanzone-neutral-faction-art-design.md`.*
