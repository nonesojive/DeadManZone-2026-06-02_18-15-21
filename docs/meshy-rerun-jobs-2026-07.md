# Meshy conscript cel rerun — job tracking (2026-07-14)

Phase 0 focused bake-off rerun after verdict 5 (INCONCLUSIVE) + verdict 6 (mid-stocky).
Scope: **inked flat-cel style only**, 3 proportion variants. Neutral column dropped (junk geometry contaminated the first bake-off).

Pipeline per variant: image3d @12k → remesh @12k → rig (height 1.8, walk/run free) → animate (0=Idle, 8=Dead).
Client: `tools/meshy/meshy_client.py`; key via `[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')`.
Refs: `tools/meshy/units/conscript_rifleman/refs/rerun/<variant>.png`. Output: `tools/meshy/units/conscript_rifleman/rerun/<variant>/{idle,walk,die}.glb`.

## Ref variants (black-shape gated 2026-07-14)

| Variant | Proportion target | Shape gate |
|---|---|---|
| `cel_mid` | ~5 head-heights, between stocky and realistic | ✅ `cel_mid_shape.png` |
| `cel_stocky_v2` | ~4.5H stocky, cleaner silhouette / less chunky face | ✅ `cel_stocky_v2_shape.png` |
| `cel_real_v2` | Realistic military proportions, hard gear edges | ✅ `cel_real_v2_shape.png` |

Shared ref prompt base: WW1 grimdark conscript, soft field cap (no helmet), no backpack, rifle vertical at side, A-pose, 3/4 front, flat studio lighting, light-grey background, flat cel + bold black ink outlines.

## Jobs

| Variant | image3d task id | remesh | rig | anim idle | anim die |
|---|---|---|---|---|---|
| cel_mid | 019f6331-e7bd-717e-a4e8-cb68fcadd3fe ⏳ | | | | |
| cel_stocky_v2 | 019f6331-eb83-7e1b-b39e-86502aa3d89a ⏳ | | | | |
| cel_real_v2 | 019f6331-e498-7e1a-a8d2-a375906b00f3 ⏳ | | | | |

Queued 2026-07-14 ~20:35 ET. Chains walking via `tools/meshy/walk_rerun_chains.ps1`.

Gotchas (from spike run): texture color varies per gen; do NOT blunt-decimate in Blender; Meshy shoot anims unusable.
