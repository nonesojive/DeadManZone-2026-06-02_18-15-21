# Meshy conscript bake-off — job tracking (2026-07-14)

Phase 0 style spike: 4 reference variants of conscript_rifleman through the full pipeline.
Pipeline per variant: image3d @12k → remesh @12k → rig (height 1.8, walk/run free) → animate (0=Idle, 8=Dead).
Client: `tools/meshy/meshy_client.py`; key via `[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')`.
Refs: `tools/meshy/units/conscript_rifleman/refs/<variant>.png`. Output: `tools/meshy/units/conscript_rifleman/spike/<variant>/{idle,walk,die}.glb`.

## Jobs (queued 2026-07-14 ~19:15 ET)

| Variant | image3d task id | remesh | rig | anim idle | anim die |
|---|---|---|---|---|---|
| cel_stocky | 019f62e8-2727-7ded-9655-0459d7e98015 ✅ | 019f62ea-dd08-7064-9841-2f06858c2f2a ✅ | 019f62ec-ddc1-77c2-ab5c-a2fc08b3dbb1 ✅ | 019f62ee-b92f-7274-a45d-9fd8bb095e01 ✅ | 019f62ee-d9b5-70bd-93c9-936f264da609 ✅ |
| cel_real | 019f62e8-4daf-7dfd-965a-ed7f0efe4d6d ✅ | 019f62eb-242f-718c-b558-ecb78b4923d5 ✅ | 019f62ed-3501-721e-990e-d80d1f51efb9 ✅ | 019f62ef-13dc-70c2-bd0e-6e200583c6ff ✅ | 019f62ef-2f17-70c3-9772-0a6b4fc4c715 ✅ |
| neutral_stocky | 019f62e8-6cda-70f4-b7ba-81ce7678d98e ✅ | 019f62ec-4c01-71f5-a87b-0f73f3ad41d4 ✅ | 019f62ed-cf3a-70a4-ac9d-5e95981c30ba ✅ | 019f62ef-67cc-7293-b97c-58ec18e5fbb3 ✅ | 019f62ef-8414-70e5-9b9f-8c84b70de973 ✅ |
| neutral_real | 019f62e8-8c74-7e05-aa80-fd646d16e7ce ✅ | 019f62ec-8310-77bf-b738-321b9dca1e5c ✅ | 019f62ee-290f-70ac-8703-da351ca0b683 ✅ | 019f62ef-bbc0-7f5e-824e-37202967b3c4 ✅ | 019f62ef-d851-782e-a506-4b51efb9ed5a ✅ |

Gotchas (from roster run): texture color varies per gen (accept it — no re-gens for color); do NOT blunt-decimate in Blender; Meshy shoot anims are bow-and-arrow (unusable — never order one).

**All 20 jobs complete 2026-07-14 (~19:23 ET), zero failures/retries.** GLBs downloaded and normalized to `tools/meshy/units/conscript_rifleman/spike/<variant>/{idle,walk,die}.glb` (idle = anim action 0, die = anim action 8, walk = rig `basic_animations.walking_glb`). All 12 files 6.5–7.4 MB.
