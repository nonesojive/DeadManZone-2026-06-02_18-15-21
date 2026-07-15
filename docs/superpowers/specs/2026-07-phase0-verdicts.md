# Phase 0 Style-Spike Verdicts — FINAL (owner-ruled)

> **FINAL.** Owner verdicts locked 2026-07-14 from `Assets/_Project/Scenes/StyleSpike_Phase0.unity`.
> Evidence captures live in `Screenshots/phase0/` (untracked by convention). Spec amendments
> marked **(Phase 0 verdict, 2026-07-14)** in the sibling design docs.

## Capture setup

**ArenaCamera (scene camera):** position (12.60, 27.81, -19.80), rotation (50°, 0, 0),
perspective, FOV 36, near 0.3, far 200.

**Caveat:** positioned captures (`battle_distance`, closeups, `crowd_gutter`) were taken
through the Unity MCP screenshot tool's temp camera, which renders at its default FOV ≈ 60,
not the ArenaCamera's 36. Distances were chosen so the on-screen unit size approximates the
real combat framing (player row ≈ 37% of frame width in `battle_distance`), but perspective
compression is milder than the real camera. Re-verify borderline calls in-editor.

**Per-model scale factors** (local scale on each lineup unit's `idle` model child; ring
child at 1.98 uniform on all):

| Unit | Scale |
|---|---|
| Lineup_1_enlisted_baseline | 1.03 |
| Lineup_2_cel_stocky | 1.01 |
| Lineup_3_cel_real | 1.11 |
| Lineup_4_neutral_stocky | 0.80 |
| Lineup_5_neutral_real | 1.08 |

**Idle-pose sampling outcome: SUCCESS.** All ten lineup/enemy units sampled their own GLB
sub-asset clip `Armature|Idle|baselayer` at t≈2.0s (mid-clip) via `clip.SampleAnimation` in
edit mode — no retargeting failures. Caveat: mid-clip frames are not uniformly neutral
(cel_stocky holds an arm out, neutral_real leans into a step), which adds pose noise to the
silhouette comparison. Crowd stayed A-pose by design. Scene NOT saved; poses were
judgment-only.

## Evidence captures (`Screenshots/phase0/`)

| File | What |
|---|---|
| `full_lineup.png` | Default ArenaCamera frame, whole stage |
| `battle_distance.png` | Player row at ≈1/3 frame width (combat zoom approximation) |
| `closeup_cel_stocky.png`, `closeup_cel_real.png`, `closeup_neutral_stocky.png`, `closeup_neutral_real.png` | Punch-in per variant (~3.5 units, eye height) |
| `crowd_gutter.png` | 24-unit crowd, four gutter rows visible (gutter set via MPB, see notes) |
| `crowd_gutter_play_1.png` / `crowd_gutter_play_2.png` | Play mode, ~2s apart — flicker check |
| `blackshape_sim.png` | Five lineup silhouettes at real rendered scale (diff vs `battle_distance_bgplate2.png`, built by `make_blackshape.py`) |

## Owner verdicts

### 1. Interior ink — **CLOSE-CAMERA PASS (two-tier surface)**

**Evidence:** At punch-in the cel textures read as inked illustration — cel_stocky has clear
browline/crease linework and flat color planes, cel_real reads painted-with-line; both neutrals
read as soft matte CG with no ink language (`closeup_cel_stocky.png`, `closeup_cel_real.png`
vs `closeup_neutral_stocky.png`, `closeup_neutral_real.png`). At battle distance the interior
ink is sub-pixel: cel vs neutral is barely distinguishable and the look is carried by the
outline pass and value grouping, not interior line (`battle_distance.png`).

**Owner ruling:** **Close-camera pass** — adopt the two-tier surface from the art-direction
spec §5 outcome 2. Interior ink lives on cards, portraits, and punch-ins; battlefield models
carry exterior outline + 2–3 band cel only. Not a failure; the DD skin is delivered at the
distances the eye can receive it.

### 2. Morale ring gutter legibility at 24 units — **PASS AS-IS**

**Evidence:** Rows 0.7 and 1.0 are clearly broken/sputtering and read at a glance; row 0.35
IS distinguishable from the solid row 1 but only on inspection — it is too subtle for
peripheral reading at crowd scale (`crowd_gutter.png`). Flicker animates: play captures ~2s
apart differ by 13,257 px (>30 channel-sum) concentrated on rows 2–4 rims — `_Time`
animation confirmed (`crowd_gutter_play_1.png`, `crowd_gutter_play_2.png`).

**Owner ruling:** **Pass as-is.** The subtle shaken band (0.35) is acceptable; do not steepen
the gutter threshold curve. Guttering reads at 20+ units without becoming screen noise.

### 3. Within-archetype cue (cap vs helmet+pack) at battle distance — **UPGRADE (body-mass cue rule)**

**Evidence:** In `blackshape_sim.png` the enlisted baseline (leftmost) reads bulkier through
torso mass (pack) than the four conscripts, but the headgear itself does not resolve: cap brim
vs helmet dome is not legible at ~60 px silhouette height. The differentiating signal is body
mass, not the intended headgear cue. Pose variance from idle-sampling contaminates this
comparison.

**Owner ruling:** **Upgrade the cue system.** Piece cues must alter **body-level silhouette
mass** (pack, cape, armor bulk). Headgear alone is a punch-in/icon cue only — not sufficient
at battle distance. Amend unit-art spec §2.3 accordingly; enlisted vs conscript
differentiation must be authored through pack/bulk geometry, not helmet-vs-cap head-zone detail.

### 4. Oversized scale (1.3×CELL) — **PASS with ring shrink**

**Evidence:** In the 24-unit crowd the grid stays readable — each unit is traceable to its ring
and rows/columns hold (`crowd_gutter.png`). But the ring quads (1.98 scale ≈ 1.9-unit rim
outer diameter) overlap at 1.8-unit crowd spacing: rims kiss and cut into neighbors, which is
where the "bad overlap" lives — not in the bodies. A-pose arms cross neighbor cells but posed
units at battle distance don't visually collide (`battle_distance.png`).

**Owner ruling:** **Pass the 1.3× unit height.** Shrink ring outer diameter to ~**0.9×CELL**
so rims don't touch at 1-cell spacing. Bodies and grid read are good; only the ring scale
needs implementation.

### 5. Ref style column: cel vs neutral — **INCONCLUSIVE → RERUN**

**Evidence:** Geometry: cel_stocky and cel_real came back as clean single figures;
neutral_stocky shipped with a cluster of extra rifles fused beside the figure (Meshy junk
geometry, visible in `closeup_neutral_stocky.png` and photobombing `closeup_neutral_real.png`).
Final look through `DMZ/UnitCelInk`: the cel textures' baked line + flat planes cooperate with
the shader (reads stylized at punch-in), while the neutral textures' soft gradients read as
generic CG through the same shader — the shader does not stylize them by itself
(`closeup_cel_*.png` vs `closeup_neutral_*.png`).

**Owner ruling:** **Inconclusive — do not lock cel yet.** A focused cel rerun is queued (see
follow-up queue). The neutral column was contaminated by Meshy junk geometry; the cel column
showed promise but the bake-off matrix was not decisive enough to commit template defaults.
§7.2 template defaults remain **TBD pending rerun**.

### 6. Proportions: stocky vs realistic at battle distance — **MID-STOCKY (pending rerun confirmation)**

**Evidence:** At battle distance the stocky variants hold chunkier, more solid masses with
larger head reads; realistic proportions go spindly — thin limbs start to break up into noise
at rendered scale (`battle_distance.png`, silhouette solidity in `blackshape_sim.png`: stocky
tiles ~1.72k silhouette px vs ~1.4k for realistic at equal height). Stocky also matches the
tabletop-miniature framing of the ring bases.

**Owner ruling:** **Mid-stocky** — between current stocky (~4.5 head-heights) and realistic.
Target **~5 head-heights**, slightly oversized head/hands but not full toy-soldier chunk.
Commit roster-wide once the focused rerun confirms. Do not lock full stocky or realistic until
then.

## Tuning notes / issues found while capturing

- **Outline side-tint SCRAPPED (owner ruling 2026-07-14):** `DMZ/UnitCelInk` ink outline pass is pure black (`_OutlineColor` only). Side allegiance reads via base ring materials only — `_SideColor` / `_SideTint` removed from the shader. Arena spec §3 and style bible §4 updated accordingly.
- **Gutter materials are inert on disk:** all four `Assets/_Project/Combat3D/RingFill_Gutter_{0,35,70,100}.mat`
  assets have `_Gutter=0` — the per-row values existed only as MaterialPropertyBlocks that a
  prior session applied and that do not survive scene reload (the first crowd capture came
  out all-solid). For these captures the MPBs were reapplied (0 / 0.35 / 0.7 / 1.0 by row) in
  edit mode and again inside play mode. Real gameplay needs a runtime driver setting `_Gutter`
  per unit via MPB (matches the shader's stated design); the material assets alone will not do it.
- **Enlisted rifleman material artifact:** the enlisted_baseline model shows saturated blue
  patches on pack/helmet submeshes at grazing angles (`closeup_cel_stocky.png` right figure,
  also visible in crowd shots) — looks like a submesh missing its albedo binding on the
  UnitCelInk material rather than a shader fault. Fix before any final art call involving the
  enlisted model.
- Positioned-capture FOV caveat (see Capture setup) applies to any framing-sensitive
  re-review.

## Phase 0 follow-up queue

| # | Item | Owner verdict driver | Notes |
|---|---|---|---|
| a | **Focused cel rerun matrix** | Verdict 5 inconclusive | Re-run conscript 2×2 bake-off with clean neutral refs (no junk geometry); judge ink-at-combat-scale before locking §7.2 template defaults. Sibling worker owns execution. |
| b | **Ring diameter shrink implementation** | Verdict 4 pass-with-shrink | Shrink ring outer diameter to ~0.9×CELL; keep 1.3× unit body height. Presentation-layer shader/ring prefab change. |
| c | **Enlisted baseline blue-patch material fix** | Capture tuning note | Submesh albedo binding on enlisted_baseline pack/helmet — saturated blue at grazing angles (`closeup_cel_stocky.png`). |
| d | **Gutter runtime driver for `_Gutter`** | Capture tuning note | Material assets on disk have `_Gutter=0`; gameplay must set per-unit `_Gutter` via MPB at runtime. Matches shader design intent. |
