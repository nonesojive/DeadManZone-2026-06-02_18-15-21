# Phase 0 Style-Spike Verdicts — DRAFT (owner ruling pending)

> **DRAFT.** These are agent judgments with evidence citations, produced 2026-07-14 from
> `Assets/_Project/Scenes/StyleSpike_Phase0.unity`. The six verdicts are OWNER calls; no
> spec has been updated. Evidence captures live in `Screenshots/phase0/` (untracked by
> convention).

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

## Draft verdicts

### 1. Interior ink

**DRAFT: PASS at punch-in, COLLAPSES at battle distance.** At punch-in the cel textures
read as inked illustration — cel_stocky has clear browline/crease linework and flat color
planes, cel_real reads painted-with-line; both neutrals read as soft matte CG with no ink
language (`closeup_cel_stocky` / `closeup_cel_real` vs `closeup_neutral_stocky` /
`closeup_neutral_real`). At battle distance the interior ink is sub-pixel: cel vs neutral is
barely distinguishable and the look is carried by the outline pass and value grouping, not
interior line (`battle_distance.png`). Implication: interior ink is a closeup/UI-card
asset, not a battlefield differentiator.

### 2. Morale ring gutter legibility at 24 units

**DRAFT: PASS with one tuning flag.** Rows 0.7 and 1.0 are clearly broken/sputtering and
read at a glance; row 0.35 IS distinguishable from the solid row 1 but only on inspection —
it is too subtle for peripheral reading at crowd scale (`crowd_gutter.png`). Flicker
animates: play captures ~2s apart differ by 13,257 px (>30 channel-sum) concentrated on
rows 2–4 rims — `_Time` animation confirmed (`crowd_gutter_play_1/2.png`). Consider
steepening the low end of the `_Gutter * 1.2 - 1.0` threshold curve so 0.35 notches earlier.

### 3. Within-archetype cue (cap vs helmet+pack) at battle distance

**DRAFT: MARGINAL.** In `blackshape_sim.png` the enlisted baseline (leftmost) reads bulkier
through torso mass (pack) than the four conscripts, but the headgear itself does not
resolve: cap brim vs helmet dome is not legible at ~60 px silhouette height. The
differentiating signal is body mass, not the intended headgear cue. Pose variance from
idle-sampling contaminates this comparison — recommend the owner confirm on matched poses
before ruling; if it holds, the within-archetype cue needs a bigger silhouette element than
headgear.

### 4. Oversized scale (1.3×CELL)

**DRAFT: PASS for bodies, WATCH the rings.** In the 24-unit crowd the grid stays readable —
each unit is traceable to its ring and rows/columns hold (`crowd_gutter.png`). But the ring
quads (1.98 scale ≈ 1.9-unit rim outer diameter) overlap at 1.8-unit crowd spacing: rims
kiss and cut into neighbors, which is where the "bad overlap" lives — not in the bodies.
A-pose arms cross neighbor cells but posed units at battle distance don't visually collide
(`battle_distance.png`). If crowd spacing is representative of real board density, shrink
rim outer radius or ring scale.

### 5. Ref style column: cel vs neutral

**DRAFT: CEL wins on both axes.** Geometry: cel_stocky and cel_real came back as clean
single figures; neutral_stocky shipped with a cluster of extra rifles fused beside the
figure (Meshy junk geometry, visible in `closeup_neutral_stocky.png` and photobombing
`closeup_neutral_real.png`). Final look through `DMZ/UnitCelInk`: the cel textures' baked
line + flat planes cooperate with the shader (reads stylized at punch-in), while the neutral
textures' soft gradients read as generic CG through the same shader — the shader does not
stylize them by itself (`closeup_cel_*` vs `closeup_neutral_*`).

### 6. Proportions: stocky vs realistic at battle distance

**DRAFT: STOCKY.** At battle distance the stocky variants hold chunkier, more solid masses
with larger head reads; realistic proportions go spindly — thin limbs start to break up
into noise at rendered scale (`battle_distance.png`, silhouette solidity in
`blackshape_sim.png`: stocky tiles ~1.72k silhouette px vs ~1.4k for realistic at equal
height). Stocky also matches the tabletop-miniature framing of the ring bases.

## Tuning notes / issues found while capturing

- **Gutter materials are inert on disk:** all four `Assets/_Project/Combat3D/RingFill_Gutter_{0,35,70,100}.mat`
  assets have `_Gutter=0` — the per-row values existed only as MaterialPropertyBlocks that a
  prior session applied and that do not survive scene reload (the first crowd capture came
  out all-solid). For these captures the MPBs were reapplied (0 / 0.35 / 0.7 / 1.0 by row) in
  edit mode and again inside play mode. Real gameplay needs a runtime driver setting `_Gutter`
  per unit via MPB (matches the shader's stated design); the material assets alone will not do it.
- Gutter threshold constant `1.2` (`CombatRingFill.shader` line 79): fine at 0.7/1.0, too
  shy at 0.35 — see verdict 2.
- **Enlisted rifleman material artifact:** the enlisted_baseline model shows saturated blue
  patches on pack/helmet submeshes at grazing angles (`closeup_cel_stocky.png` right figure,
  also visible in crowd shots) — looks like a submesh missing its albedo binding on the
  UnitCelInk material rather than a shader fault. Worth a fix before any final art call
  that involves the enlisted model.
- Positioned-capture FOV caveat (see Capture setup) applies to any framing-sensitive
  re-review.
