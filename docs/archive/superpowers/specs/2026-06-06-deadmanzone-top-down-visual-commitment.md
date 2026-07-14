> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — 2D Visual Commitment

**Date:** 2026-06-06 (revised 2026-06-07)  
**Status:** Approved  
**Supersedes:** Pure top-down token camera (88° elevation) — infantry readability insufficient at grid scale

---

## Decision

**Commit to unified 2D presentation** for build phase, shop, board, and combat replay:

| Layer | Camera | Source |
|-------|--------|--------|
| **Terrain tiles** | True top-down (90°) | Grok / hand-painted (`Assets/Grok Images/`) |
| **Unit tokens** | Orthographic **3/4 isometric** (~35°) | SuperGrok Imagine or Blender → PNG |
| **Combat replay** | Same 2D grid + isometric tokens + VFX | `CombatDirector` event log |

**Defer full 3D combat replay** to a possible post-demo / v2 presentation skin on the same event log.

---

## Rationale

| Factor | 2D isometric tokens + top-down terrain | Isometric board + 3D combat |
|--------|----------------------------------------|-----------------------------|
| Infantry readability | Faces, weapons, packs visible — distinct silhouettes | N/A (3D separate stack) |
| Matches current code | Board, shop, reserves, combat replay on same grid UI | Second presentation stack |
| Solo scope | AI sprites or Blender renders; no in-engine 3D | 5–10× art, 3–5× engineering |
| Meshy | **Optional** — AI 2D primary; Blender for hard footprints | Required for 3D path |
| Genre fit | Autobattler / TFT clarity + trench theme | Total War fantasy (deferred) |

**Why not pure top-down tokens?** At shop (~48px) and board scale, infantry becomes helmet blobs. Grok isometric sheets validated that 3/4 view is the readability sweet spot for units while terrain stays birds-eye.

The deterministic combat sim and event log remain **presentation-agnostic**.

---

## Locked visual standard

| Layer | Presentation |
|-------|----------------|
| Main menu / run HUD | 2D UI (unchanged) |
| Shop | 2D cards with **isometric token** icons |
| Board / reserves | Isometric unit sprites on top-down terrain tiles |
| Combat | Same grid; tokens move/attack via replay + VFX |
| Post-demo optional | Fog-of-war intro, richer VFX, **optional 3D combat skin** |

### Camera — unit tokens (locked)

| Setting | Value |
|---------|--------|
| Projection | Orthographic |
| Elevation | **~35°** (classic 3/4 isometric) |
| Azimuth | **~225°** (token faces **bottom-right** of frame — matches Grok style anchor) |
| Lighting | Key upper-left, cool fill, soft oval drop shadow |
| Background | Transparent PNG (remove white from AI exports) |
| Shop icon | 256×256 px |
| Board / combat token | 128×128 px (Phase 3; same art pass as icons) |

**Style anchor (AI):** `Assets/Grok Images/Isometric/grok-image-0211da6d-2b71-444a-ad30-4781dae097e0.jpg`  
**Grenade thrower reference:** `Assets/Grok Images/Isometric/gtvnM.jpg`

**Blender fallback:** `Assets/_Project/Art/Neutral/Source/neutral_token_camera.py` (isometric preset)  
**Batch 2 import:** `import_grok_batch2_icons.ps1` or Unity menu `Import Grok Batch 2 Icons`

### Camera — terrain tiles (locked)

| Setting | Value |
|---------|--------|
| Projection | Orthographic top-down (90°) |
| Use | Zone backgrounds, trenches, mud, bunker walls |
| Examples | `Assets/Grok Images/FronttileA*.jpg`, `ReartileA.jpg`, `Bunkerwall*.jpg` |

### Depth without 3D

- Isometric tokens on top-down terrain (natural depth separation)
- Y-sort draw order on grid rows
- Baked drop shadows under tokens
- Terrain parallax / zone tint overlays
- Combat VFX: muzzle flash, tracers, gas overlay (not baked into idle sprites)
- Hit flash, damage numbers, brief screen shake

### Neutral tone guardrails (AI prompts)

Neutrals are **field militia**, not elite heroes. Avoid in demo roster art:

- Glowing eyes / neon accent colors as primary identity
- Purple magic, demon tank faces, horns, energy beams
- Baked flames, sparks, lightning in idle token sprites
- Spiky Mad Max vehicles (save for future factions)

---

## Art pipeline (primary: AI 2D)

```
SuperGrok Imagine (style-locked isometric sheet)
    ↓
Crop + background removal (GIMP / Photoshop)
    ↓
PNG → Assets/_Project/Art/Neutral/Renders/Icons/
    ↓
Unity Sprite import → PieceDefinitionSO.icon
    ↓
[Phase 3] Per-cell board tokens → PieceShapeVisual
```

**Optional Blender path** (vehicles or style consistency): Meshy/procedural model → `neutral_token_camera.py` isometric render → same PNG folders.

---

## Out of scope (unchanged)

- In-engine 3D models for combat (deferred)
- Rigging / animation for battle replay
- Iron Vanguard pieces (separate spec — can use bolder glow/VFX)

---

## References

- Master GDD: `docs/superpowers/specs/2026-06-06-deadmanzone-master-design.md`
- Neutral art spec: `docs/superpowers/specs/2026-06-05-deadmanzone-neutral-faction-art-design.md`
- Prompt pack: `docs/DeadManZone-Art-Prompt-Pack.md`
- Grok mood board: `Assets/Grok Images/Isometric/`
