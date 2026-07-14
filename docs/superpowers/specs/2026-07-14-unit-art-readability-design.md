# Unit Art Readability — Design

**Date:** 2026-07-14 · **Status:** approved in brainstorming session, pending implementation plan
**Sibling to:** `2026-07-14-art-direction-readability-audit-design.md` (same audit lens, applied to
unit art specifically)
**Amends:** `docs/art/style-bible/00-style-bible.md` (§2), `docs/art/style-bible/50-combat-arena-spec.md`
(§1 board scale, §4 mesh sourcing, §8 Phase 0)
**Reconfirms:** ADR-0002 (cel-shaded 3D + ink) and ADR-0003 (kitbash backbone + AI heroes) — the 3D
move stands; this spec defines the differentiation requirements those models must hit.

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
Proportions stay stocky per bible §1 — heads/hands/weapons oversized enough to read small, never
chibi-cute.

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
Piece-within-archetype identity comes from **one head-zone cue** (heads read best at distance).
**A new piece must declare its row in this table before modeling starts** — that is the production
gate.

| Piece | Archetype signature | Piece cue |
|---|---|---|
| conscript_rifleman | rifle, upright | soft cap, no pack |
| enlisted_rifleman | rifle, upright | steel helmet + full pack |
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

The style spike now includes **two units of the same archetype** — conscript (soft cap) vs enlisted
(helmet + pack) — posed side by side and judged at final combat screen size with the oversized
scale law applied. If cap-vs-helmet cannot be told apart at battle distance, the cue system needs a
bigger lever **before any roster work**.

One spike, four judgments (consolidated from both 2026-07-14 specs):
1. Interior ink: full pass / close-camera pass (two-tier surface) / fail.
2. Morale ring guttering legibility at 20+ units.
3. Within-archetype cue legibility (this spec).
4. Oversized-scale grid read (does 120–140% break the board?).

## 5. Build cost summary

| Item | Layer | Cost |
|---|---|---|
| Stance-posed idle set (per archetype, retargeted) | Art/Anim | Rides existing Phase 2 plan |
| Head-zone kitbash cues (per piece) | Art | Small per piece (kitbash parts) |
| Oversized scale + overlap tolerance | Presentation | Small |
| Icon render tool (pose/camera/frame presets, batch) | Editor tooling | Medium, one-time |
| Portrait re-slotting (hovercard/front report/aftermath) | Presentation | Small |
| Phase 0 second unit | Art | Small (one kitbash variant) |

## 6. Explicit non-goals

- No interim sprite rework (§2.4).
- No per-piece bespoke silhouettes beyond the cue system (two-level key was chosen over full
  piece-level uniqueness — cost scales to future factions).
- No reopening of ADR-0002/0003.
