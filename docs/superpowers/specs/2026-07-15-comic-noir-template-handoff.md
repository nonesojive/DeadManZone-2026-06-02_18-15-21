# Handoff — Comic-Noir Unit Art Template (locked 2026-07-15)

**Point a future session here.** This is the entry doc for unit 3D art after Phase 0 + the multi-style bake-off.

**Branch context when locked:** `shopvisualrefreshv1` (may have unrelated WIP — do not `git add .`).

---

## What was decided (do not re-litigate)

| Lock | Value | Evidence / doc |
|---|---|---|
| **Ref style (§7.2)** | **Heavy-ink comic** (`s09_comic_noir`) | Tone + combat-view readability. `Screenshots/phase0/combat_pick_9_comic.png`, lineup |
| **Proportions** | **Mid-stocky ~5 head-heights**, slightly oversized head/hands | Phase 0 verdict 6 + combat pick |
| **Unit outlines** | **Black only** — no blue/red side tint on outline | Side = base rings only. `DMZ/UnitCelInk` |
| **Interior ink** | Two-tier: cards/portraits/punch-ins get interior ink; battlefield = outline + cel bands | Phase 0 verdict 1 |
| **Body-mass cues** | Battle distance = silhouette mass (pack/cape/armor bulk). Headgear = punch-in/icon only after Phase 0 | Phase 0 verdict 3 |
| **Scale** | Unit height **1.3×CELL**; rings shrink to **~0.9×CELL** (impl still open) | Phase 0 verdict 4 |
| **Morale gutter** | Pass as-is (`_Gutter` on ring fill; subtle 0.35 OK) | Phase 0 verdict 2 — runtime MPB driver still open |
| **Refs** | Geometry + identity, not mood. **Single figure only** (no turnaround sheets) | Multi-view sheets → Meshy 3-body meshes (s03/s04 lesson) |

Authoritative design:
- `docs/superpowers/specs/2026-07-14-unit-art-readability-design.md` (§7 template)
- `docs/superpowers/specs/2026-07-15-multi-style-bakeoff.md` (style lock)
- `docs/superpowers/specs/2026-07-phase0-verdicts.md` (six judgments)
- `docs/GDD.md` (game rules — update if any rule changes)
- `docs/art/style-bible/` + ADR-0002 (cel/ink) — still stand; outline side-tint removed

**Do not design from `docs/archive/`.**

---

## Ref template (copy into every piece prompt)

```
ONE single character only — no turnaround, no side/back view sheet, no duplicate clones.
Full-body, ¾ front, neutral A-pose standing (rig-friendly), rifle or gear at side as appropriate.
Mid-stocky ~5 head-heights, slightly oversized head and hands.
Style: heavy-ink comic — thick black contour, crosshatch / hatched shadow, high contrast,
limited olive / bone / leather / metal palette.
Flat even studio lighting, plain solid light-grey background, no ground shadow, no atmosphere.
Hard clear material boundaries (cloth / leather / metal / wood).
Piece cue must change body-mass silhouette at battle distance (not headgear-only).
No backpack unless the piece cue IS pack mass.
```

Then: `python tools/refcheck.py <ref.png>` — archetype + cue must read at ~40px black-shape before Meshy.

Canonical example ref/model: `tools/meshy/units/conscript_rifleman/refs/styles/s09_comic_noir.png`  
Canonical spike GLB: `Assets/_Project/Combat3D/Models/_Spike/style_s09_comic_noir/idle.glb`

---

## Pipeline (live practice)

```
ref.png → refcheck.py → Meshy image3d@12k → remesh@12k → rig(height 1.8) → animate(idle=0, die=8)
       → import GLB → DMZ/UnitCelInk (black outline, assign texture_0) → height-normalize 1.3×CELL
```

- Client: `tools/meshy/meshy_client.py` (`MESHY_API_KEY` user env)
- Style walker (reference): `tools/meshy/walk_style_chains.ps1` / `walk_style_v2_fix.ps1`
- Job logs: `docs/meshy-styles-jobs-2026-07.md`, earlier spike job docs under `docs/meshy-*-jobs-*.md`
- Units dir (mostly gitignored except refs): `tools/meshy/units/<piece>/`

---

## Shader / presentation facts

| Asset | Notes |
|---|---|
| `DMZ/UnitCelInk` | Cel bands + **black** outline. Path under `Assets/_Project/Presentation/Combat/Arena/Shaders/` |
| `CombatRingFill` | `_Fill` = HP; `_Gutter` = morale achromatic rim flicker (not replacing fill) |
| Combat camera | ArenaCamera ≈ `(0, 10, -14)`, Euler `(29,0,0)`, FOV `42` — see `Combat3DDemoSceneBootstrap` |
| Spike scene | `Assets/_Project/Scenes/StyleSpike_Phase0.unity` — CombatPick roots for 1·9·8·6·3v2·4v2 |
| CELL | `1.8` (`CombatArena3DDemoConfig`) |

---

## Done vs open

### Done
- [x] Readability audit + unit-art design specs
- [x] `tools/refcheck.py` black-shape gate
- [x] Phase 0 cel/neutral spike + owner verdicts
- [x] Multi-style 10-way bake-off + combat-view pick
- [x] Lock comic noir + mid-stocky
- [x] Fix s03/s04 as single-figure v2 (DQ originals were turnaround sheets)

### Still open (engineering / art)
1. **Ring diameter shrink** to ~0.9×CELL (presentation) — verdict 4
2. **Enlisted baseline blue-patch** material/albedo fix on pack/helmet submeshes
3. **Runtime `_Gutter` MPB driver** for morale ring flicker
4. **Roster regen** in confusion-priority order under comic-noir §7.2 template (conscript → enlisted → bulwark rifle cluster first; mortars/vehicles last; only where scale check fails)
5. Optional: promote `s09` into live combat roster slot (replace enlisted/conscript path) once regen starts

Screenshots (judgment evidence): `Screenshots/phase0/` — especially `combat_pick_*.png`, `styles_*.png`, earlier `battle_distance.png` / `closeup_*.png`.

---

## Suggested next session prompts

**Art / roster:**  
> Read `docs/superpowers/specs/2026-07-15-comic-noir-template-handoff.md`. Author comic-noir mid-stocky refs for conscript + enlisted, gate with refcheck, run Meshy, stage under ArenaCamera combat framing, compare to `style_s09_comic_noir`.

**Engineering:**  
> Read the handoff. Implement ring shrink to 0.9×CELL, enlisted blue-patch fix, and `_Gutter` MPB runtime driver. Leave Core rules alone unless GDD says otherwise.

**Do not:** re-open style bake-off; reintroduce outline side-tint; use turnaround sheet refs; design from `docs/archive/`.
