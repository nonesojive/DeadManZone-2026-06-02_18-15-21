# Unit Art Readability — Design

**Date:** 2026-07-14 · **Status:** approved in brainstorming session, pending implementation plan
**Sibling to:** `2026-07-14-art-direction-readability-audit-design.md` (same audit lens, applied to
unit art specifically)
**Amends:** `docs/art/style-bible/00-style-bible.md` (§2), `docs/art/style-bible/50-combat-arena-spec.md`
(§1 board scale, §4 mesh sourcing, §8 Phase 0)
**Reconfirms:** ADR-0002 (cel-shaded 3D + ink) — the 3D move stands; this spec defines the
differentiation requirements the models must hit.
**Corrects:** ADR-0003 (kitbash backbone) is **superseded by practice** — the roster is produced by
the character pipeline: reference image → Meshy image3d @12k → remesh → rig → animate
(`docs/meshy-roster-jobs-2026-07-11.md`; ten roster GLBs already live in
`Assets/_Project/Combat3D/Models/`). **Companion item: write the superseding ADR** so the ADR layer
stops describing a pipeline that no longer exists.

## 0. The problem, diagnosed from the live assets

The current per-piece sprite strips (`Art/Combat2D/Units/Animations/`) and painterly shop icons
(`Art/Combat2D/Icons/ShopV2/`) are good *at card size* and fail *at combat size*. Inspected
side-by-side (conscript_rifleman vs ironclad_marksman idle strips): at battle scale both collapse
into the same olive smudge, because **all differentiation lives in surface detail** (texture, gear
painting) — the first thing lost when shrunk — while the structural channels (pose, proportion,
value blocking) are identical across the humanoid roster. The art was made "good," not made
"readable at 40 pixels."

## 1. The requirement (bible §2 amendment)

At battle distance, every unit answers three questions in under one second:

1. **Whose?** — side channel (existing law, unchanged).
2. **What kind?** — archetype, carried by **silhouette + stance**.
3. **Which one?** — piece within archetype, carried by **exactly one secondary cue**.

**Surface detail (texture, interior ink, gear painting) is not allowed to carry identity.** It is
mood only. Any identity that only exists in surface detail does not exist.

## 2. Combat models — the identity system

### 2.1 Scale law (arena spec §1/§2 amendment)
Units render at **120–140% of cell footprint** (toy-soldier oversized, Top Troops precedent);
slight overlap between neighbours is tolerated. Playback gets the shape; punch-ins get the detail.
Proportions: bible §1 mandates stocky (heads/hands/weapons oversized enough to read small, never
chibi-cute), but whether stocky or realistic survives the Meshy pipeline better is decided by the
§7.4 bake-off — the verdict is committed roster-wide.

**(Phase 0 verdict, 2026-07-14): MID-STOCKY, pending rerun confirmation.** Target ~5
head-heights — between current stocky (~4.5) and realistic. Slightly oversized head/hands but
not full toy-soldier chunk (`Screenshots/phase0/battle_distance.png`, `blackshape_sim.png`).
Commit roster-wide (locked with style template 2026-07-15). Do not use full stocky or realistic
as the default.

### 2.2 Stance joins silhouette (bible §2 amendment)
Each archetype's idle **stance is part of its silhouette signature** — the black-shape test is run
on the **posed idle at final combat screen size**, not on a T-pose or a zoomed view:

| Archetype | Stance signature |
|---|---|
| Rifle infantry | Upright, long-gun line across the body |
| Marksman | Kneeling brace, longest gun line |
| Mortar/artillery crew | Low cluster + steep tube angle |
| Assault/melee | Forward lean, bulk at shoulders |
| Support | Upright, **no long-gun line**, satchel reach |
| Command | Greatcoat mass, raised baton arm |
| Vehicle | Wider than tall, track/wheel base |
| Structure | Rigid, architectural, taller than any unit |

### 2.3 Two-level identity key — current roster mapping
**(Phase 0 verdict, 2026-07-14): UPGRADED — body-mass cue rule.** Piece-within-archetype
identity at battle distance comes from **body-level silhouette mass** (pack, cape, armor bulk).
Headgear alone does not resolve at ~60 px silhouette height (`Screenshots/phase0/blackshape_sim.png`)
— it is a punch-in/icon cue only. The identity key **splits across the pipeline** (Meshy's
auto-rig wants a neutral standing character, so stance cannot live in the base mesh):

- **Geometry cues live in the reference image** — silhouette-shaping dressing at **body mass**
  (pack bulk, greatcoat mass, shield-plate shoulders, ghillie hood volume). Headgear is
  secondary — legible at punch-in/card size, not at battle distance. See §7 for the ref spec.
- **Stance lives in the idle animation** — Meshy preset where it fits, Humanoid retarget where it
  doesn't (§5.1 risk; Phase 0's marksman case answers it).

**A new piece must declare its row in this table before its reference image is authored** — that
is the production gate.

| Piece | Archetype signature | Piece cue |
|---|---|---|
| conscript_rifleman | rifle, upright | no pack (lean torso mass) |
| enlisted_rifleman | rifle, upright | full pack bulk (headgear = punch-in only) |
| bulwark_squad | assault lean, multi-body | shield-plate bulk at shoulders |
| ironclad_marksman | kneel, longest gun | ghillie hood |
| ironclad_mortars | low crew + tube | — (silhouette unique) |
| ironmarch_surgeon / field_medic | support, satchel | bone-white armband/case |
| ironclad_field_marshal | command greatcoat | brass-trim cap + baton |
| ironmarch_iron_horse | vehicle | turret mass |
| armored_transport | vehicle | flat hull, no turret |
| machine_gun_nest | structure (Combat board) | sandbag ring + gun barrel |
| HQ buildings | structure, architectural | per-building massing (existing plan) |

Notes:
- Support pieces are the **only** units that may use the bone-white value as a cue — it doubles as
  the universal "medic" read.
- The cue system scales to new factions: same archetype stances, new dressing + faction accent.
- Squads (manpowerCost > 1) remain N staggered bodies per bible §2; the cue must survive the
  multi-body read.

### 2.4 Transition
Current sprites stay in the live game until arena-spec Phase 2 replaces them. **No interim 2D
rework** — all effort goes to the pipeline that fixes this permanently.

## 3. Icons and board representation

### 3.1 Render from the models (shop slots + board cells)
Shop slot icons and board cell sprites are **automated renders of the actual 3D model through the
ink shader**: fixed per-archetype 3/4 pose, fixed camera and framing, faction-accent trim on the
frame. Properties:
- Always match the battlefield — no icon↔model drift, ever.
- Batch-regenerate when a model changes (editor tool; `screenshot-isolated`-style capture).
- Every new piece gets its icons for free — icon cost is removed from the per-piece budget.
- Cell sprites are judged **at final cell size**, silhouette-first.

### 3.2 Portraits stay painterly (for now)
Existing painterly art moves up-format to where its detail can breathe: **hovercard portraits,
front report, aftermath**. The binding requirement is *mood + readability*, not painterly-specifically
(owner statement 2026-07-14). This tier is deliberately reversible: if the ink renders read strongly
at card size, retiring the painterly tier is a one-line decision, not a rework.

### 3.3 Frame grammar
Icon frames follow bible §6: square badge = unit ("who"), round = ability ("trigger"); category and
faction read from frame trim, not from repainting the render.

## 4. Phase 0 gate extension (arena spec §8 amendment)

The style spike is now an **experiment, not just a look-check**. It includes the **reference-image
bake-off** (§7.4): conscript generated from a 2×2 matrix of refs — (inked flat-cel vs neutral
geometry) × (stocky toy-soldier vs realistic proportions) — four full Meshy chains, screenshotted
beside the existing enlisted model (the painterly-realistic baseline) at final combat screen size
under the oversized scale law.

One spike, six judgments (consolidated from both 2026-07-14 specs):
1. Interior ink: full pass / close-camera pass (two-tier surface) / fail.
2. Morale ring guttering legibility at 20+ units.
3. Within-archetype cue legibility: body-mass differentiation at battle distance (conscript
   lean torso vs enlisted pack bulk). If the cue cannot be told apart, the cue system needs a
   bigger lever **before any roster work**.
4. Oversized-scale grid read (does 120–140% break the board?).
5. **Ref style verdict** — where the ink lives: baked in texture (inked-cel refs), shader-only
   (neutral refs), or both.
6. **Proportion verdict** — stocky vs realistic, committed roster-wide.

Verdicts 5–6 lock the §7 reference template. The marksman stance-coverage question (§5.1) rides
the same spike.

**(Phase 0 verdict, 2026-07-14) — judgments locked:**

| # | Judgment | Owner verdict | Evidence |
|---|---|---|---|
| 1 | Interior ink | **Close-camera pass** (two-tier surface) | `closeup_cel_*.png` vs `battle_distance.png` |
| 2 | Morale guttering | **Pass as-is** (0.35 subtle OK) | `crowd_gutter.png`, `crowd_gutter_play_*.png` |
| 3 | Within-archetype cue | **Upgrade** — body-mass cue rule | `blackshape_sim.png` |
| 4 | Oversized scale | **Pass** — ring shrink to ~0.9×CELL | `crowd_gutter.png`, `battle_distance.png` |
| 5 | Ref style | **Locked — heavy-ink comic (`s09_comic_noir`)** | `combat_pick_9_comic.png`, multi-style bake-off 2026-07-15 |
| 6 | Proportions | **Mid-stocky** (~5 head-heights) | `battle_distance.png`, `blackshape_sim.png`, combat pick |


Full owner calls and follow-up queue: `2026-07-phase0-verdicts.md`.

## 5. Build cost summary

| Item | Layer | Cost |
|---|---|---|
| Archetype stance idles (Meshy anim preset per archetype, or Humanoid retarget where the preset library falls short) | Art/Anim | Medium — see §5.1 risk |
| Head-zone cues authored into reference images (per piece) | Art (prompt/reference pass) | Small per piece |
| Roster re-generation from new template refs | Art (Meshy re-runs) | Per-piece, confusion-priority order, only where Phase 0 says the current model fails at scale (§7.5) |
| Oversized scale + overlap tolerance | Presentation | Small |
| Icon render tool (pose/camera/frame presets, batch) | Editor tooling | Medium, one-time |
| Portrait re-slotting (hovercard/front report/aftermath) | Presentation | Small |
| Phase 0 bake-off | Art | 4 ref images + 4 Meshy chains (conscript 2×2; enlisted GLB is the baseline, already exists) |

### 5.1 Known risk — stance coverage in the Meshy animation library
The stance law (§2.2) depends on per-archetype idle poses. Meshy's preset animations cover
idle/walk/die (and its shoot presets are known-unusable — bow-and-arrow, per the 2026-07-11 job
notes). If the preset library cannot deliver a kneeling-brace idle (marksman) or low-crew idle
(mortars), those archetypes need Unity Humanoid retargeting of external clips onto the Meshy rigs.
Phase 0 should answer this for one hard case (marksman) before the roster pass.

## 6. Explicit non-goals

- No interim sprite rework (§2.4).
- No per-piece bespoke silhouettes beyond the cue system (two-level key was chosen over full
  piece-level uniqueness — cost scales to future factions).
- No reopening of ADR-0002.

## 7. Reference Image Spec — the pipeline's control surface

**Root cause, stated plainly (owner, 2026-07-14):** the current painterly refs were authored as
shop art, not geometry sources. Painterly refs hurt Meshy twice — soft edges and texture noise
give it mushy forms to reconstruct, and baked painterly lighting fights the cel/ink shader
afterward. The refs are the root of the readability failure; every law in this spec is ultimately
a reference-image requirement.

### 7.1 Principle
The reference image's job is **geometry and identity, not mood**. The shader owns mood (final
split pending §7.4). Ref style and game style are allowed to diverge. A ref that looks beautiful
but generates a mushy model is a failed ref.

### 7.2 The template — every piece ref must have
- Single character, ¾ front view, **neutral standing pose** (rig-friendly).
- Flat, even lighting; solid clean background; no atmosphere, no ground shadow, no painterly
  softness.
- **Hard part boundaries** — armor vs cloth vs weapon separations Meshy can reconstruct.
- The piece's **cue and archetype dressing at silhouette-affecting size** (§2.3).
- Proportions and style keywords per the §7.4 verdicts, then locked as template defaults.

**(Locked 2026-07-15):** Style = **heavy-ink comic** (`s09_comic_noir` — thick contour,
crosshatch shadow, high contrast). Proportions = **mid-stocky** (~5 head-heights, slightly
oversized head/hands). See `2026-07-15-multi-style-bakeoff.md`. Refs must be **single-figure**
only (no turnaround sheets — Meshy extrudes multi-view sheets into multi-body meshes).


### 7.3 Pre-Meshy gate — black-shape the ref itself
Before a ref spends a Meshy chain: threshold to flat black, shrink to combat size (~40px), check
archetype + cue still read. A failed ref costs a re-prompt; a failure discovered after
generate→remesh→rig→animate costs the whole chain plus cleanup. Cheapest-point enforcement of the
silhouette law.

### 7.4 Phase 0 bake-off (see §4)
Conscript ×4: (inked flat-cel | neutral geometry) × (stocky | realistic), full chain each, judged
through the ink shader at combat scale beside the enlisted baseline. Outputs: ref style verdict
(where the ink lives) and proportion verdict. Both lock the template.

**(Locked 2026-07-15):** Ref style = heavy-ink comic (`s09`); proportions = mid-stocky
(~5 head-heights). Multi-style bake-off superseded the cel-vs-neutral 2×2. Roster refs follow
§7.2 keywords.


### 7.5 Rollout
After the template locks, regenerate the roster in **confusion-priority order**: most-confused
pairs first (the conscript/enlisted/bulwark rifle cluster), silhouette-unique pieces (mortars,
vehicles, structures) last — and only where the Phase 0 scale check says the existing model
actually fails.

### 7.6 Storage
Refs are a per-piece artifact: `tools/meshy/units/<piece>/ref.png` beside the GLBs. The painterly
portraits are unaffected (§3.2) — refs are a new artifact class, not a replacement.
