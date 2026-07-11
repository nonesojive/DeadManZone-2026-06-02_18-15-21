# Meshy roster generation — job tracking (2026-07-11)

Pipeline per unit: image3d @12k → remesh @12k → rig (walk/run free) → animate (0=Idle, 8=Dead).
Client: `tools/meshy/meshy_client.py`; key via `[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')`.

## image3d jobs (queued 2026-07-11 ~13:20 ET)

| Unit | image3d task id | remesh | rig | anim idle | anim die |
|---|---|---|---|---|---|
| bulwark_squad | 019f51ef-090f-76d8-89c8-bc92d5f21597 ✅ | 019f51fe-6e33-70c8-9d15-33dbb8f0223c ✅ | 019f5205-c733-77c1-a405-166e858e4263 ✅ | 019f5207-9115-7330-8c13-b0fa74964749 ✅ | 019f5207-9468-7877-b5fc-d8e9a28ee0ab ✅ |
| field_medic | 019f51ef-0c63-76d9-88bd-390968a2885b ✅ | 019f51fe-7013-758c-bdab-8a14397a790b ✅ | 019f5205-e50f-77d6-a266-fc24df881be9 ✅ | 019f5207-975f-7878-b136-b43b44d8c870 ✅ | 019f5207-9ab1-7378-873e-6c54cb90c8c9 ✅ |
| grenade_thrower | 019f51ef-0f5a-76d9-9a27-5293ff224a76 ✅ | 019f51fe-71dd-7ffd-b46c-ab83a2cfe891 ✅ | 019f5206-0004-7803-9246-43c00d694fbe ✅ | 019f5207-9d70-7379-9f14-5c24329bf2ba ✅ | 019f5207-a0de-737a-98fc-2d0a6afff390 ✅ |

Next: `python meshy_client.py status image3d <id>` → when done, `remesh <id> --polycount 12000` → `rig <remesh_id> --height 1.8` → `animate <rig_id> 0` / `animate <rig_id> 8` → `download`.

Gotchas (from v5 handoff): texture color varies per gen (accept or re-gen); do NOT blunt-decimate in Blender (weld+recalc normals only); Meshy shoot anims are bow-and-arrow (unusable).

**All jobs complete 2026-07-11.** GLBs downloaded to `tools/meshy/units/<unit>/glb12k/{idle,walk,die}.glb` and copied into `Assets/_Project/Combat3D/Models/`. grenade_thrower model is worn by the `ironclad_mortars` piece (no grenade_thrower content piece exists).
