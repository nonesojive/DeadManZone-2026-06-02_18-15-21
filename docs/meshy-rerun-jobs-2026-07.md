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
| cel_mid | 019f6331-e7bd-717e-a4e8-cb68fcadd3fe ✅ | 019f6334-6df1-7d25-bbdb-b47e6295c5ee ✅ | 019f6335-d7e5-7675-bad7-daaf5a0aaced ✅ | 019f6336-6f92-7eb8-a4f7-fee966648acb ✅ | 019f6336-7279-7d9e-81be-0935268a77ed ✅ |
| cel_stocky_v2 | 019f6331-eb83-7e1b-b39e-86502aa3d89a ✅ | 019f6336-c5a4-7ec0-8daa-f30288f3506f ✅ | 019f6338-3155-72d7-ba9b-67413a50f2a1 ✅ | 019f6338-c98c-76f7-938f-2293e4404f1c ✅ | 019f6338-cc6c-72ed-ab12-3ce68d8ba64d ✅ |
| cel_real_v2 | 019f6331-e498-7e1a-a8d2-a375906b00f3 ✅ | 019f6339-1cb3-72fa-a358-27b14b38a085 ✅ | 019f633a-869f-7747-8a08-a087883bd5a6 ✅ | 019f633b-1dac-7f90-9722-ce62ee381d76 ✅ | 019f633b-2091-737f-83ef-67469657c5bb ✅ |

**All 15 jobs complete 2026-07-14 (~20:44 ET), zero failures.** GLBs downloaded to `tools/meshy/units/conscript_rifleman/rerun/<variant>/{idle,walk,die}.glb` (gitignored spike output — not force-added).

Walker script: `tools/meshy/walk_rerun_chains.ps1`; log: `tools/meshy/walk_rerun_chains.log`.

Gotchas (from spike run): texture color varies per gen; do NOT blunt-decimate in Blender; Meshy shoot anims unusable.
