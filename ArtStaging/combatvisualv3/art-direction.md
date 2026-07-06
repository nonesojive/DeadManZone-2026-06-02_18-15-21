# CombatArena2D Unit Redo — Art Direction (combatvisualv3)

**Goal:** close the gap to Top Troops-class unit presence. The current roster
(see `ArtStaging/combatvisualv2/*_base.png`) is realistic-proportioned,
fine-lined, low-contrast — it disappears against the dirt. The original art
brief (`docs/art/combat-arena-2d-art-brief.md`) already called for "cartoon
military, bold shapes, outline"; this pass delivers it.

## Locked style fragment (prepend to every unit prompt)

> Stylized chunky military game character, mobile strategy game style,
> exaggerated proportions: 2.5 heads tall, oversized helmet, hands and weapon,
> short sturdy legs, big boots. Bold dark outline around the whole silhouette.
> Soft 3D-render shading like a modern mobile RTS (Top Troops / Clash-style),
> NOT flat pixel art. Grimdark WW1 trench aesthetic: mud-green greatcoat,
> brass fittings, gas mask, muted khaki-and-rust palette but strong value
> contrast. Full body, standing, facing right, side view, single character,
> plain white background, no text.

## Pilot units (validate direction before batching the 17)

| Piece id | Prompt suffix (after the locked fragment) |
|---|---|
| `enlisted_rifleman` | Rifleman holding a long bolt-action rifle across the body, small backpack, canvas pouches. |
| `bulwark_squad` | Heavy shield-bearer: massive riveted tower shield in the left hand, short trench mace in the right, extra armor plating on the shoulders. |
| `ironmarch_iron_horse` | Small steampunk assault tank, riveted iron hull, side profile, single stubby cannon, brass exhaust stacks, no crew visible. |

## Pilot results (2026-07-06) — direction VALIDATED

Three archetypes generated via Blender MCP + Hyper3D Rodin, rendered
orthographic side-view, staged in this folder:

- `enlisted_rifleman_base.png` — chibi rifleman, gas mask, greatcoat.
- `bulwark_squad_base.png` — shield-bearer, tower shield forward, mace back.
- `ironmarch_iron_horse_base.png` — steampunk assault tank, brass stacks.

All read as premium chunky mobile-strategy units — a decisive jump over the
realistic v2 roster. **Facing:** render from the -X camera (looking +X) for
right-facing bases; do NOT rotate the model (Blender render ignores object
rotation in this MCP context — camera moves work, object transforms don't).

## AutoSprite pipeline — PROVEN end-to-end (rifleman pilot, 2026-07-06)

Real key supplied (`vspk_…`), registered as a local-scope MCP server
(`autosprite`) and used directly via JSON-RPC this session. Full loop:

1. `request_upload_url` → `curl -X PUT` the Hyper3D base render → `upload_character`
   (uploading your own image is FREE, keeps the chunky style).
2. `generate_spritesheet` with animations `[idle, walk, custom(shoot),
   custom(die)]`, `spritesheet:{frameCount:25, frameSize:256}`. **5 credits per
   animation** (20/unit). "shoot"/"die" are `kind:"custom"` with a prompt +
   `loop:false`; idle/walk are their own kinds with `loop:true`.
3. Poll `list_spritesheets latestOnly:false` until all `succeeded` (30–120s;
   wait ≥30s between checks). The two customs come back with `name:null` —
   identify shoot vs die by eye.
4. Download: signed `?sig=` URLs expire in SECONDS — fetch a fresh URL via
   `get_spritesheet` and download it IN THE SAME PROCESS with `-L` (redirects
   to R2). Shell pipelines are too slow; use the in-process python in
   scratchpad `dl_sheets.py`.
5. Place as `Animations/<piece>/<piece>_{idle,walk,shoot,die}.png` (overwrite,
   keep `.meta` for GUID), `assets-refresh`, run
   `Combat2DAnimationSetBuilder.BuildAll` (auto-detects the 5×5 grid).

Result verified in-arena: the chunky v3 rifleman POPS against the old v2
roster — direction validated at gameplay scale. Cost so far: 20/260 credits.

Remaining roster (14 units × ~20 credits ≈ 280) exceeds the 240 balance —
budget top-up or a smaller state set (idle+walk+shoot, skip die) needed to
finish all 17.

## Pipeline (agent-driven once Blender is up)

1. **Base generation — Blender MCP + Hyper3D/Hunyuan** (preferred): text-to-3D
   from the prompts above → orthographic side camera, 3-point soft light,
   toon/outline look via freestyle or solidify-flip outline → render 1024×1024
   PNG on white → `ArtStaging/combatvisualv3/<piece_id>_base.png`.
   - Fallback A: Unity AI Generators / Muse (user drives the generate click
     with these prompts; agent handles import).
   - Fallback B: Photoshop Generative Fill by hand (bridge cannot script it).
2. **Photoshop post (agent-driven):** Select Subject → mask → trim → verify
   silhouette reads at 128 px tall; punch contrast +10 if muddy.
3. **Animation — AutoSprite API** (`AUTOSPRITE_API_KEY` is set): upload base,
   request idle / walk / shoot / die, download strips as
   `<piece_id>_<state>.png` (7×7 grid convention).
4. **Import:** copy under `Assets/_Project/Art/Combat2D/Units/Animations/<piece_id>/`,
   run `DeadManZone → Combat Arena → Build All Unit 2D Anim Sets`, play-verify.

## Acceptance per unit (vs current roster)

- Reads clearly at gameplay scale (≈130 px on screen) — silhouette test.
- Outline visible; unit separates from the dirt without squinting.
- Proportions match the other redone units (no style drift).
- All four states animate without background halos.
