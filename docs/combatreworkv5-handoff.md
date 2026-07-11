# DeadManZone — combatreworkv5 handoff (3D toon-ink pipeline proven; next: wire the Core combat sim)

**Purpose:** context dump so a fresh session (switching to the Fable model) can start **scoping the Core-sim hookup** without re-deriving anything. The previous handoff (`docs/combatreworkv4-handoff.md`) planned the greenfield 3D art rework on paper. This session **built and proved it end-to-end** — toon-ink shader, interior ink, Meshy character pipeline, side channel, combat scale, and a scripted 3v3 skirmish that runs in Play mode. All of it lives in a throwaway spike; the real Core combat sim is **not yet wired to the 3D actors** — that is the next task.

- **Repo:** `C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone`
- **Branch:** `combatvisualv5` (switched onto it this session from the stale `combatreworkv4`; the 2026-07-10 planning docs are committed here as `065924ab`).
- **Engine:** Unity 6 (6000.3.8f1), URP 17.3. Windows 11.
- **Tooling live this session:** Unity MCP (`com.ivanmurzak.unity.mcp`, port 20242) — prefer it. Blender MCP (Hyper3D Rodin free-trial ok; Hunyuan off; Sketchfab NOT wired in this addon). Desktop Commander for real-machine shell/git. The Meshy pipeline is a **local Python client** (`tools/meshy/meshy_client.py`), not an MCP.
- **Read first, in order:** `CLAUDE.md` (layering: Core has NO UnityEngine dependency; Presentation = visuals only), `CONTEXT.md` (glossary — side vs faction), `docs/adr/0001–0003`, `docs/art/style-bible/50-combat-arena-spec.md`, then this file.

---

## What this session proved (the whole arc)

1. **Toon-ink 3D shader pipeline works.** Hand-written URP shader `DMZ/ToonInk` (2-band cel + ink-family shadow + inverted-hull screen-constant outline + fresnel/normal-derivative interior ink + MPB status hooks) plus a **fullscreen depth+normals edge-detect** for real interior ink. The breakthrough: the toon shader needed **DepthOnly + DepthNormals passes** so characters actually populate URP's prepass textures — once added, the edge-detect inks part boundaries and facets. Reads as inked illustration, not generic toon.
2. **Art direction pass.** Warm-key/cool-shadow lighting, desaturated split-tone grade (ACES/bloom/vignette/grain), SSAO, atmospheric fog, and a sandbag-trench environment. Took it from "Synty with a filter" to intentional direction.
3. **Character pipeline pivoted to Meshy.** The generic Synty body and free-trial Rodin were both dead ends (Rodin produced a blob). The user's **existing Meshy pipeline** (`tools/meshy/`) is the character source. Proved the full loop: `image3d → remesh-to-budget → rig → animate → Blender cleanup → Unity → toon-ink`, landing a **12,213-tri** clean rigged animated rifleman from a 526k raw gen.
4. **Side channel + combat scale.** Player-left / enemy-right, side conveyed by **muted base rings** (blue/red) — colored outlines were tried and removed (redundant, too loud). Toon-ink holds up on multiple units.
5. **Scripted 3v3 skirmish runs in Play mode.** Two squads advance (walk), clash, and take casualties (die), driven by an AnimatorController + a `SkirmishDriver` MonoBehaviour, using Meshy idle/walk/die clips on the budgeted rig. This is a *scripted* fight — **not** the real Core sim.

Captures of each milestone are in `Assets/_Phase0Spike/Captures/` (`phase0_iter1..10`).

## Current state — what's in the project (all throwaway spike unless noted)

**Throwaway spike folder: `Assets/_Phase0Spike/`**
- `Shaders/DMZ_ToonInk.shader` — the unit uber-shader. Passes: `ToonForward` (UniversalForward), `Outline` (SRPDefaultUnlit, Cull Front inverted hull, `_SideTint`/`_SideTintAmount`), `DepthOnly`, `DepthNormals`, `ShadowCaster`. Props include cel ramp, `_InkColor/_InkStrength/_InkPower`, crease-ink (`_CreaseStrength/Threshold/Sharp` — inert on smooth Meshy normals), MPB status hooks (`_HitFlash/_Desat/_DissolveAmount`), masked `_AccentColor/_AccentEmission`.
- `Shaders/DMZ_InteriorInkFullscreen.shader` — Roberts-cross edge detect on `_CameraDepthTexture` + `_CameraNormalsTexture`. Includes core RP `Blit.hlsl` (`Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl` — NOT the universal path).
- `Scenes/Phase0_Spike.unity` — the trench scene: `Graybox_Ground`, `Key Light`, `Global Volume` (uses `P0_Grade.asset`), `Environment` (sandbag walls/pile), `Skirmish` → `Players`/`Enemies` (3 units each), `SkirmishManager` (has `SkirmishDriver`).
- `Materials/` — `ToonInk_Meshy`, `Unit_Player`/`Unit_Enemy` (side-tint OFF, pure dark ink outline), `RingBlue`/`RingRed` (muted URP-Unlit discs), `InteriorInk` (fullscreen), `P0_Grade` (VolumeProfile).
- `Models/` — `enlisted_rifleman_12k.glb` (idle), `enlisted_rifleman_12k_walk.glb`, `enlisted_rifleman_12k_die.glb`. Same rig (`Armature/Hips`, 24 bones). Clips: `Armature|Idle|baselayer` (4s), `Armature|walking_man|baselayer` (1.07s), `Armature|Dead|baselayer` (3s) — cross-compatible. Also a superseded `enlisted_rifleman_clean.glb` (31k) and the failed `DMZ_DieselSoldier.glb` (Rodin blob — delete-worthy).
- `RiflemanFight.controller` — Idle(default)/Walk/Die; params `Moving` (bool), `Die` (trigger).
- `SkirmishDriver.cs` — finds `Players`/`Enemies` GameObjects, advances both, drops enemy casualties, pushes players. Throwaway test harness.
- `Captures/` — milestone PNGs.

**NON-throwaway change to be aware of:** the interior-ink + SSAO renderer features were added to the **shared** `Assets/_Project/Settings/Rendering/DeadManZone_ForwardRenderer.asset` (features `DMZ_InteriorInk` = FullScreenPassRendererFeature @ BeforeRenderingPostProcessing, requirements Depth|Normal; and `DMZ_SSAO`). **These affect every camera/scene in the game, including the Run scene and menus.** Decide whether to scope interior ink to combat units (e.g., a layer mask / separate renderer) or keep it global.

**Existing pre-made Meshy units (from the user's earlier pipeline runs):** `tools/meshy/units/{enlisted_rifleman, bulwark_squad, field_medic, grenade_thrower, ironclad_field_marshal}/glb/{idle,walk,shoot,die}.glb` — ~31k, olive-textured, rigged+animated. These were rendered to sprite sheets for the *old* 2D combat. They are the roster source; re-budget them via the Meshy remesh flow below when moving to 3D.

## The Meshy pipeline (how to regenerate / animate units)

Client: `tools/meshy/meshy_client.py` (stdlib only). Auth: **`MESHY_API_KEY` env var**. It is saved in the user's **User registry** (via `setx`) but a fresh shell won't inherit it after an app restart — load it per-command without exposing it:
```
$env:MESHY_API_KEY=[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User'); python meshy_client.py <cmd>
```
Correct order (image3d's `--polycount` is ignored at generation — raw gens are ~500k faces, over the 300k rigging limit; **remesh enforces the budget**):
1. `image3d --image <ref.png> --polycount 12000` → image3d task id
2. `remesh <image3d_id> --polycount 12000` → budgeted mesh (~12k tris)
3. `rig <remesh_id> --height 1.8` → rig task id (walking/running GLBs come free — `download rig <rig_id>`)
4. `animate <rig_id> <action_id>` → animated GLB. **action_id: 0=Idle, 8=Dead.** Walk/run come free from step 3.
5. `wait <kind> <id>` / `status <kind> <id>` (kind: image3d|remesh|rig|anim); `download <kind> <id> --out <dir>`.

This session's task IDs (rifleman): image3d `019f4eb7-84dc-7cbc-b5e3-5335b8bfccfe`, remesh `019f4eba-ae8d-7d59-a968-abc7c842cbcb`, rig `019f4ebd-4486-7dca-a316-b5dc279f1119`, anim idle `019f4ebd-fafa-7dc9-827c-a18c90bfc686`, anim die `019f4ed0-090a-7a03-bf54-cb9060401cc9`.

**Meshy gotchas (confirmed):** (a) **shoot animations are all bow-and-arrow** — unusable for riflemen, need custom shoot anims; (b) **texture output varies per gen** — the rifleman regen came out horizon-blue vs the olive reference (`tools/meshy/units/enlisted_rifleman/ref.png`); user accepted blue for now; (c) after remesh, the mesh is clean — **do NOT blunt-decimate in Blender** (it mangled UVs → blue legs, and coarsened skinning). Safe Blender cleanup = weld coincident verts + recalc normals only; real poly reduction = remesh-at-polycount or careful retopo.

## THE NEXT TASK: scope wiring the Core combat sim to the 3D toon-ink actors

Goal: replace the sprite-based `CombatUnitVisual2D` presentation with **3D toon-ink actors driven by the same deterministic replay** the sim already emits — so the fight is real, not scripted.

Start by reading (don't assume): the Core combat sim, `CombatEventLog`/event replay, `CombatDirector` (pacing), and `CombatUnitActor`/free-chase chase controller. Per CLAUDE.md, rules live in `Core` (no UnityEngine), wiring in `Game`, visuals in `Presentation`.

Scoping questions to answer in the new session:
- **Actor mapping:** sim unit/piece → a 3D actor prefab (Meshy GLB + `ToonInk_Meshy`-style material + an Animator like `RiflemanFight.controller`). One prefab family, per-archetype clip sets.
- **Event → presentation mapping:** move/charge → `Moving` bool + position drive (reuse the v4 pacing math: `EmptyTickPaceScale` coupled with `moveSpeedPresentationScale`, and the free-chase SmoothDamp anchor-follow — all presentation-agnostic and already solved); attack → shoot anim (placeholder until custom) + muzzle-flash VFX; hit → `_HitFlash` MPB; death → `Die` trigger + `_DissolveAmount` ink-edge dissolve.
- **Side channel:** base ring per side (blue player / red enemy), driven by sim allegiance (NOT faction — see CONTEXT.md). Neutral units fight for either side.
- **Punch-in director:** consume the same kill/crit/HQ-damage events for the scripted camera beats (arena spec §1/§6) — two feedback tiers only, no full-screen interrupts.
- **Scope decision:** keep the interior-ink/SSAO renderer features global, or restrict interior ink to combat units.
- **Roster:** re-budget the 5 existing Meshy units via the remesh flow; generate idle/walk/die per unit; author custom shoot anims.

## What carries over vs. throwaway

| Keep / reference | Throwaway (spike) |
|---|---|
| `DMZ/ToonInk` + `DMZ/InteriorInkFullscreen` shaders, the DepthOnly/DepthNormals fix | `Phase0_Spike.unity`, `SkirmishDriver.cs`, `RiflemanFight.controller` (harness) |
| The Meshy pipeline (`tools/meshy/`) + the remesh-at-budget flow | The single hero-shot scene setup, ad-hoc materials |
| Renderer features (`DMZ_InteriorInk`, `DMZ_SSAO`) — but re-decide scope | The Rodin blob `DMZ_DieselSoldier.glb` |
| Core sim + event replay + `CombatDirector` pacing + free-chase smoothing (unchanged) | Sprite `CombatUnitVisual2D` pipeline (being replaced) |

## Housekeeping / gotchas

- **Play mode blocks scene edits** — `EditorSceneManager.MarkSceneDirty`/`SaveScene` throw "cannot be used during play mode." Guard scripts with `if(EditorApplication.isPlaying)`. Exiting play mode triggers a domain reload + a brief MCP-bridge disconnect (retry after ~4s).
- **After an app/editor restart Unity may reopen the Run scene, NOT `Phase0_Spike`.** A swap script polluted the Run scene once this session. Always check `SceneManager.GetActiveScene().name` before mutating, and never save units into `Run.unity`.
- **OneDrive placeholders** make the Linux-sandbox `git`/`bash` unreliable (files look absent; `git status` misleadingly "clean"). Use **Desktop Commander** for real-machine git; the repo uses **Git LFS**, so branch switches must happen on the real machine (LFS-aware), not the sandbox.
- **Unity MCP** is the fast path; `screenshot-camera` works even in play mode and even when the Game View window is closed (`screenshot-game-view` needs the window open). It renders via a temp RenderTexture.
- Don't hand-edit `.meta`. After editing `.cs`/`.shader` outside the editor, `assets-refresh`.
- 345/345 EditMode tests were green historically; this spike didn't touch Core, so they should still pass — run them when the Core-sim wiring begins.

## Key decisions made this session
- Base rings (muted) are the side channel; **colored outlines removed** (redundant/loud).
- **Meshy** is the character source (not Synty, not Rodin). Poly budget via **remesh-at-polycount**, not decimation.
- Blue rifleman texture **accepted for now**; olive is the target (per ref) — resolve via re-gen later.
- Interior-ink + SSAO features currently on the **shared** ForwardRenderer (global) — revisit scope.
- Custom **shoot** and **die** animations will be needed (Meshy's shoot is bow-and-arrow).

**First action for the new session:** commit the `_Phase0Spike` work + this handoff to `combatvisualv5` (real-machine git via Desktop Commander), then read the Core combat code and produce the sim-hookup scope/plan before writing integration code.
