---
name: meshy-unit-pipeline
description: Take a DeadManZone unit from a reference image to a scene-ready 3D roster unit via the Meshy pipeline (image3d -> remesh -> rig -> animate -> Unity). Use when asked to "add a new unit", "generate a unit model", "image to unit", "new roster unit", or anything involving the Meshy pipeline.
---

# Meshy unit pipeline: reference image -> scene-ready roster unit

NOTE: this doc lives in `docs/skills/` (tracked) because `.gitignore` excludes
`.claude/skills/` (that folder is regenerated locally, not portable).

## Prerequisites

- `MESHY_API_KEY` in the **User registry** (set via `setx`). Fresh shells don't
  inherit it — always prefix commands with:
  `$env:MESHY_API_KEY=[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User'); `
- Reference image at `tools/meshy/units/<unit>/ref.png` (or pass `--ref`).
- Unity open with the Unity MCP bridge connected (needed only for the final
  rebuild + Play verification steps).
- Run commands on the **real Windows machine** (Desktop Commander), not the
  Linux sandbox — OneDrive placeholders make sandbox file access unreliable.
  Repo file edits: file tools (Read/Write/Edit), never sandbox bash.
- **Credits**: a full unit run costs ~30-40 Meshy credits (image3d + remesh +
  rig + 2 animations). `--dry-run` is free and side-effect free.

## The one command

```powershell
cd "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\tools\meshy"
$env:MESHY_API_KEY=[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User'); python generate_unit.py <unit_name>
```

Options: `--ref <path>` `--polycount 12000` `--height 1.8`
`--resume <stage:task_id>` `--dry-run` `--no-unity-copy`.

## What it does (stages + typical timings, from the 2026-07-11 roster runs)

| Stage | What | Typical wait |
|---|---|---|
| image3d | ref.png -> textured 3D gen (raw ~500k faces) | ~3-4 min |
| remesh | enforce the 12k poly budget (**image3d's polycount is IGNORED at gen — remesh is what enforces it**) | ~1-2 min |
| rig | humanoid rig @ 1.8 m; walking/running GLBs come free | ~1.5-2 min |
| animate x2 | action 0 = Idle, action 8 = Dead (created in parallel) | ~1 min each |
| download | `idle.glb`, `walk.glb` (from the rig's walking anim), `die.glb` -> `tools/meshy/units/<unit>/glb12k/`; prunes fbx/running/armature extras | seconds |
| unity copy | the 3 GLBs -> `Assets/_Project/Combat3D/Models/<unit>/` | seconds |

Total wall time ~10-15 min. Task ids print as they're created and are saved to
`tools/meshy/units/<unit>/pipeline_state.json` after every stage.

## Resume after interruption

- Rerun the same command — completed stages are skipped via the state file.
- State file lost? Inject a known id:
  `python generate_unit.py <unit> --resume rig:019f5254-...`
  (stage: `image3d|remesh|rig|anim_idle|anim_die`; ids are in the console log,
  `pipeline_state.json`, `docs/meshy-roster-jobs-*.md`, or the Meshy dashboard).

## Manual Unity steps (script prints this checklist too)

1. **RosterUnits**: add `("<unit_folder>", "<piece_id>")` to the `RosterUnits`
   array in `Assets/_Project/Presentation/Editor/Combat3DDemoSceneBootstrap.cs`.
   The piece id MUST exist in ContentDatabase (proxy check:
   `Assets/_Project/Data/Resources/DeadManZone/Pieces/<id>.asset`). No matching
   piece? The model can be "worn" by an existing piece — e.g. the
   grenade_thrower model was worn by `ironclad_mortars`.
2. **Rebuild**: `assets-refresh` (if Unity was open during the copy), then menu
   `DeadManZone -> Combat3D -> Build Combat3D Demo Scene` — generates looped
   clips + AnimatorController per unit automatically. Units with broken/missing
   GLBs are skipped with a warning and fall back to rifleman visuals.
3. **Verify in Play** (put the piece id in a roster on `Combat3DDemoDriver`):
   - proportions sane (the retired grenade_thrower had an oversized head);
   - texture color acceptable — **color varies per gen (roulette)**; re-run
     image3d if it's way off the ref;
   - feet planted ON the base ring (not hovering);
   - rifle attached (right-hand bone found; missing hand logs one error);
   - idle / walk / die all read.

## Gotchas (all confirmed the hard way)

- **Polycount**: image3d ignores `target_polycount` — remesh enforces the
  budget. Never skip remesh.
- **NEVER blunt-decimate in Blender** — it mangles UVs (blue legs) and
  skinning. Safe cleanup = weld coincident verts + recalc normals ONLY. Real
  reduction = remesh-at-polycount.
- **Download collision**: both anim tasks expose the same remote filename
  (`animation_glb.glb`). `generate_unit.py` avoids it by downloading straight
  to `idle.glb`/`die.glb`. If using `meshy_client.py download` manually,
  download into separate dirs or rename between sequential downloads.
- **Shoot anims unusable**: Meshy's shoot library is bow-and-arrow. Riflemen
  use the code-driven aim/recoil layer in `CombatUnitVisual3D`; custom shoot
  clips are still an open item.
- **Texture roulette**: color varies per gen (rifleman came out blue vs olive
  ref). Accept or re-gen; there is no seed control.
- **Rig bones**: all Meshy rigs so far share
  `Armature/Hips/Spine02/.../RightHand`, but **dump bones on a new rig before
  assuming** — don't guess.
- **Credits**: ~30-40 per unit. Don't rerun paid stages to "check" anything —
  use `status`/`--dry-run`/`--resume`.

## Troubleshooting

- **Unit invisible / falls back to rifleman**: RosterUnits entry missing, GLBs
  missing under `Assets/_Project/Combat3D/Models/<unit>/`, or the piece id
  isn't in ContentDatabase — check the rebuild's console warning.
- **Piece won't place in test boards**: the Rear zone rejects Unit pieces —
  `TestBoards.Layout` placements at column 3 silently no-op; place via
  `SupportLineAnchor` and assert `PlacementResult.Success`.
- **Files look missing / git looks clean when it shouldn't**: OneDrive
  placeholders — use file tools (not sandbox bash) for repo files and Desktop
  Commander for real-machine shell/git.
- **Task FAILED**: fix the input (usually the ref image) and rerun; the state
  file keeps every other stage's id.
