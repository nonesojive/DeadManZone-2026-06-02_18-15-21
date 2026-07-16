# Phase 0 — Multi-Style Reference Bake-off

**Date:** 2026-07-15 · **Status:** LOCKED — future sessions: `docs/superpowers/specs/2026-07-15-comic-noir-template-handoff.md`
**Piece:** `conscript_rifleman` — soft field cap, **no backpack**, bolt-action rifle at side
**Proportions (all):** mid-stocky (~5 head-heights), slightly oversized head/hands — owner target from Phase 0 verdict 6
**Shared framing:** full-body, ¾ front, neutral A-pose standing, flat even studio lighting, plain solid light-grey background, no ground shadow, no atmosphere, hard clear material boundaries (cloth / leather / metal)

## Owner lock (2026-07-15)

**§7.2 style template = `s09_comic_noir` (heavy-ink comic).**

Owner: fits tone/feel and reads decently at combat camera (ArenaCamera 10 / 29° / FOV 42).
Evidence: `Screenshots/phase0/combat_pick_lineup.png`, `combat_pick_closer.png`, `combat_pick_9_comic.png`.
Proportions remain **mid-stocky** (~5 head-heights) from Phase 0 verdict 6.

Template keywords for future piece refs: thick black contour, crosshatch / hatched shadow, high contrast, limited olive/bone/leather palette, hard part boundaries, single figure only (no turnaround sheets).

## Style matrix (10)

| Id | Style | What Meshy should get | Why it's in the bake-off |
|---|---|---|---|
| `s01_cel` | Inked flat-cel | Bold black outlines, 2–3 hard shade bands | Control — previous favorite |
| `s02_vector` | Flat vector graphic | Hard geometric shapes, no gradients, poster-flat fills | Max hard part boundaries |
| `s03_lowpoly2d` | Low-poly 2D | Faceted angular planes, visible triangle edges as design | Geometry-first silhouette |
| `s04_cutout` | Paper cutout | Layered flat silhouettes, slight depth between layers, craft-paper feel | Strong silhouette, simple volumes |
| `s05_stylized3d` | Stylized 3D clay render | Soft matte clay, no outlines, clean volumes | Neutral-geo column (prior bake-off) |
| `s06_plastic_toy` | Hard plastic toy | Glossy injection-molded look, chunky joints, toy soldier | Toy-soldier readability bet |
| `s07_voxel` | Voxel / blocky | Distinct cubic volumes, Minecraft-adjacent but military | Extreme silhouette test |
| `s08_woodblock` | Woodblock / ink print | Heavy carved black shapes, limited olive/bone palette | DD-adjacent ink without cel |
| `s09_comic_noir` | Heavy-ink comic | Thick contour + crosshatch shadow, high contrast | **WINNER** |
| `s10_stopmo` | Stop-motion claymation | Soft clay seams, fingerprint texture, handmade | Soft surface vs hard outline |

## Pipeline
1. Generate ref → `tools/meshy/units/conscript_rifleman/refs/styles/<id>.png`
2. `python tools/refcheck.py` — pass = rifle-infantry + soft-cap at 40px
3. Meshy: image3d@12k → remesh@12k → rig@1.8 → idle(0)+die(8)
4. Import idle GLB → spike scene → UnitCelInk (black outline) → height-normalized 1.3×CELL
5. Owner judges: which style produces best geometry + readability through the shader at battle distance

## Job tracking
See `docs/meshy-styles-jobs-2026-07.md` — all 10 chains complete; s03/s04 single-figure v2 complete.

## Staging
- Idle GLBs: `Assets/_Project/Combat3D/Models/_Spike/style_<id>/idle.glb`
- Combat pick (order 1·9·8·6·3v2·4v2): `Screenshots/phase0/combat_pick_*.png`

## Caveats (fairness)
- **Turnaround contamination (fixed):** original `s03`/`s04` refs were multi-view sheets → 3-body meshes. Replaced by single-figure `s03_lowpoly2d_v2` / `s04_cutout_v2`.
- Through `UnitCelInk`, non-ink styles still tend to read as inked-cel — shader + baked albedo dominate surface intent.
