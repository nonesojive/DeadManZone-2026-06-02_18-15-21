> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Iron Vanguard Premium Combat Slice — Design

**Date:** 2026-06-15  
**Status:** Approved (2026-06-15)  
**Branch:** `prettycombat`  
**Builds on:** `docs/combat/prettycombat-visual-scorecard.md`, `2026-06-13-synty-asset-revamp-design.md`, `2026-06-10-deadmanzone-combat-arena-presentation-design.md`  
**Goal:** Upgrade combat presentation from prototype to premium quality for one canonical Iron Vanguard skirmish — fully animated units, dressed battlefield, trench-ring surroundings, Synty Apocalypse HUD, and evidence-based visual scorecard gates.

---

## 1. Summary

This milestone delivers **Approach 1 (Content Wiring Pass)**: no new presentation framework, only curated content, config, animation wiring, environment enablement, and QA evidence on top of the existing replay-driven combat arena architecture.

| Decision | Choice |
|----------|--------|
| Milestone scope | **Vertical slice** — one fight fully polished before roster scale-up |
| Canonical encounter | **Iron Vanguard skirmish** |
| Surroundings | **Trench ring** — perimeter props + fog, no skybox |
| Implementation style | Content wiring pass (not profile SO layer, not cinematic-first) |

---

## 2. Success criteria

| # | Criterion |
|---|-----------|
| 1 | Slice combat is launchable from Run scene via dev menu or fixed test harness with seed `424242` |
| 2 | Player side fields `ironmarch_hq`, 2× `ironmarch_rifle`, `ironmarch_heavy_tank`, `field_gun_nest`; enemy fields `ironmarch_hq` + 2× `ironmarch_rifle` |
| 3 | Infantry plays **idle, walk, shoot, death** synced to replay events (`move`, `damage`, `destroyed`) |
| 4 | Tank uses **cannon** attack profile with muzzle/tracer/explosion VFX and audio |
| 5 | Field gun uses **building artillery** profile with cannon/explosion VFX |
| 6 | All slice pieces render Synty arena prefabs (not cubes/legacy Toon/RTS) |
| 7 | Battlefield uses Synty dirt ground; **no visible void** at camera edges (perimeter props enabled) |
| 8 | Army HUD uses `HUD_Apocalypse_HealthBar_02` at top-center; bars drop during replay |
| 9 | Hit feedback = anim + VFX + SFX + damage pop on same replay tick window |
| 10 | Visual scorecard ≥ **4/5** on all ten rubric rows (see §8) with screenshot evidence in `Assets/_Project/Art/QA/CombatPrettyPass/` |
| 11 | EditMode + PlayMode tests pass (spectacle, replay, health bar, new slice layout tests) |
| 12 | ≥55 FPS @ 1080p during slice replay; no GC spike >1 ms in health-bar hot path |

**Explicitly out of scope for this slice:**
- Full sandbox roster animation coverage
- URP post-processing volume / color grade on arena camera
- Combat music and mixer snapshots
- World-space per-unit health bars
- Cinemachine camera shake
- Trailer-only custom showcase layouts

---

## 3. Architecture

Presentation remains replay-driven. No changes to combat simulation, resolver, or tick logic.

```
CombatDirector (replay events)
    └── CombatArenaPresenter
            ├── CombatUnitActor → CombatArenaUnitVisual → ICombatUnitVisualDriver
            │       ├── HumanoidCombatVisualDriver (infantry)
            │       └── VehicleCombatVisualDriver (tank)
            ├── CombatArenaBuildingSpawner (HQ, field gun)
            ├── CombatArenaVfx
            ├── CombatArenaAudioPresenter  (PlayClipAtPoint only — never moves UI transform)
            └── CombatArenaBootstrap (ground, fog, lights, perimeter props, camera)

CombatFlowPresenter (Run scene overlay)
    └── ArmyHealthBarPresenter → CombatHealthBarUiFactory (Synty Apocalypse HUD)
```

**Data assets (existing pattern, refreshed via editor bootstrap menus):**
- `CombatArenaConfigSO` — slice environment preset
- `CombatArenaAnimationSetSO` — shoot/death clips
- `CombatArenaVfxSetSO` — muzzle/tracer/impact/explosion/death particles
- `CombatArenaAudioSetSO` — weapon/impact/explosion/death clips
- `CombatHudAssetsSO` — `HUD_Apocalypse_HealthBar_02` prefab reference

---

## 4. Encounter specification

### 4.1 Piece roster

| Side | Piece ID | Role |
|------|----------|------|
| Player | `ironmarch_hq` | HQ building |
| Player | `ironmarch_rifle` ×2 | Infantry |
| Player | `ironmarch_heavy_tank` | Vehicle |
| Player | `field_gun_nest` | Building artillery |
| Enemy | `ironmarch_hq` | HQ building |
| Enemy | `ironmarch_rifle` ×2 | Infantry |

### 4.2 Layout

Layouts are defined in code (`CombatSliceLayouts.IronVanguardSkirmish` or extended `CombatArenaTestBoards`) using Iron Vanguard faction board dimensions. Exact grid anchors are chosen to keep units visible in the oblique camera frustum (HQ rear, rifles support/front, tank front line, field gun support flank).

### 4.3 Determinism

- **Seed:** `424242` for slice PlayMode tests and screenshot capture
- Same seed + layout must produce identical opening board state across runs

### 4.4 Dev entry points

- Editor menu: `DeadManZone → Combat Arena → Launch Iron Vanguard Slice` (loads Run combat with slice board)
- PlayMode test harness builds the same layout programmatically

---

## 5. Unit animation

### 5.1 Infantry (`HumanoidCombatVisualDriver`)

| Replay event | Visual response |
|--------------|-----------------|
| `move` | `SetWalking(true)` during lerp; `false` on arrival |
| `damage` (as attacker) | Face target → `PlayAttackToward` → Shoot or GrenadeThrow trigger → muzzle callback → impact callback |
| `destroyed` | Death trigger → release to pool after clip duration |

**Animator:** `AC_CombatArena_Infantry`  
**Parameters:** `MoveSpeed`, `IsWalking`, `CurrentGait`, `IsGrounded`, `Shoot`, `GrenadeThrow`, `Death`  
**Clips (`CombatArenaAnimationSetSO`):** Kevin Iglesias rifle shoot + grenade throw; death01–03; Synty sidekick death fallback

### 5.2 Vehicle (`VehicleCombatVisualDriver`)

| Replay event | Visual response |
|--------------|-----------------|
| `move` | Optional tread nudge or snap (no full path animation required) |
| `damage` (as attacker) | Lunge/recoil toward target; cannon muzzle VFX profile |
| `destroyed` | Death VFX at position; pool release |

### 5.3 Buildings

| Piece | Presentation |
|-------|----------------|
| `ironmarch_hq` | Static Synty mesh; damage VFX at mapped world position |
| `field_gun_nest` | `BuildingArtillery` attack profile; cannon shot + explosion impact |

### 5.4 Validation

PlayMode test: spawn slice board, replay until first `move` and first `damage` event; assert infantry animator bool/trigger state changes.

---

## 6. Battlefield and trench ring

### 6.1 Config preset (`CombatArenaConfigSO`)

| Field | Slice value |
|-------|-------------|
| `useFlatTexturedGround` | `true` |
| `syntyGroundMaterial` | Assigned (Generic_Dirt or project preset) |
| `groundPadding` | `1.4` |
| `spawnPerimeterProps` | **`true`** |
| `enableArenaFog` | `true` |
| `fogDensity` | `0.024` |
| `useSyntySkybox` | `false` |

### 6.2 Perimeter props

Reuse `CombatArenaBootstrap.SpawnPerimeterProps`:
- Prefab: `SM_Bld_Bunker_Wall_01` (PolygonWar)
- Count: 8, rotated around board edge at `PerimeterPropOffset` (1.8 m)

### 6.3 Lighting

Existing three-point setup in `CombatArenaEnvironment`:
- Key (soft shadows), fill, rim
- Dark fog horizon hides remaining edge cases

### 6.4 Deferred

- Zone tint decals on ground cells
- Skybox and distant silhouettes (milestone B)

---

## 7. HUD, VFX, and audio

### 7.1 HUD

- Army bars: `HUD_Apocalypse_HealthBar_02` via `CombatHealthBarUiFactory`
- Labels: ALLIED / HOSTILE
- Checkpoint notches at 75% and 30%
- Fill driven by manual `fillRect` anchor (Slider component disabled to avoid layout explosions)

### 7.2 VFX

| Profile | VFX chain |
|---------|-----------|
| Infantry rifle | Muzzle + smoke + tracer → impact burst + damage pop |
| Tank / field gun | Cannon muzzle + tracer → explosion + damage pop |
| Death | Dust burst + smoke |

Source: `CombatArenaVfxSetSO` (Synty PolygonWar / PolygonParticleFX prefabs)

### 7.3 Audio

| Event | Clip source |
|-------|-------------|
| Rifle shot | PostApocalypseGunsDemo AutoGun |
| Cannon | AssaultCanon |
| Impact | Zapper |
| Explosion | HeavyLaserLauncher |
| Death | JackHammer |

**Critical guardrail:** `CombatArenaAudioPresenter` uses `AudioSource.PlayClipAtPoint` only. It must not move any transform on the combat UI hierarchy.

### 7.4 Bootstrap menus

Run once per fresh clone:
1. `DeadManZone → Combat Arena → Pretty Combat Pass — Import Apocalypse HUD`
2. `DeadManZone → Combat Arena → Create Or Refresh VFX Set`
3. `DeadManZone → Combat Arena → Create Or Refresh Audio Set`
4. `DeadManZone → Combat Arena → Create Or Refresh Animation Set` (if present)

---

## 8. Visual scorecard

Stored at `docs/combat/prettycombat-visual-scorecard.md` (updated when slice completes).  
Screenshots at `Assets/_Project/Art/QA/CombatPrettyPass/combat_prettypass_*.png`.

| # | Criterion | 4/5 bar | Evidence artifact |
|---|-----------|---------|-------------------|
| 1 | Unit idle/walk/attack/death | All four visible in one fight | Play screenshot or short capture |
| 2 | Vehicle combat read | Tank fires with cannon VFX + audio | Mid-fight screenshot |
| 3 | Building presence | HQ + field gun are Synty meshes | Scene + play screenshot |
| 4 | Battlefield ground | Synty dirt, no missing materials | Play screenshot |
| 5 | Trench ring | No void at camera edges | Wide play screenshot |
| 6 | HUD clarity | Synty bars top-center; drop on damage | Before/after damage screenshot |
| 7 | Hit feedback | VFX + SFX + damage pop same tick window | Frame-step or slow replay |
| 8 | Performance | ≥55 FPS @ 1080p; no bar-path GC spike | Profiler screenshot |
| 9 | Automated tests | Spectacle + replay + slice tests green | Test Runner log |
| 10 | Reproducibility | Seed 424242 → same layout | EditMode layout test |

Capture menu: `DeadManZone → Combat Arena → Pretty Combat Pass — Capture Screenshot`

---

## 9. Testing

### 9.1 EditMode

| Test | Purpose |
|------|---------|
| `CombatArenaArtCoverageTests` | Slice piece IDs have arena prefabs in catalog |
| `CombatHealthBarUiFactoryTests` | Synty HUD factory + fill binding |
| `IronVanguardSliceLayoutTests` *(new)* | Layout builds; all placements succeed |

### 9.2 PlayMode

| Test | Purpose |
|------|---------|
| `CombatArenaSpectaclePlayModeTests` | Resources assets load (VFX, animation, HUD, audio) |
| `CombatArenaReplayPlayModeTests` | Replay events drive presenter without null refs |
| `ArmyHealthBarPlayModeTests` | Bars descend during replay |
| `IronVanguardSlicePlayModeTests` *(new)* | Full slice board; first move + damage fire animator triggers |

### 9.3 Manual Director gate

1. Bootstrap all combat asset menus
2. Open Run scene → launch Iron Vanguard slice combat
3. Play through at least one damage exchange and one death
4. Capture 3 screenshots (wide field, HUD damage drop, tank/artillery moment)
5. Complete scorecard rubric; all rows ≥4/5

---

## 10. Implementation phases

| Phase | Deliverable |
|-------|-------------|
| **P1 — Slice layout** | `CombatSliceLayouts` + dev menu launcher + seed constant |
| **P2 — Animation** | Controller states wired; move/attack/death PlayMode assertions |
| **P3 — Environment** | Config preset with `spawnPerimeterProps: true`; fog/light tune |
| **P4 — Integration** | Audio/HUD hardening; building spawner verified for HQ/field gun |
| **P5 — Scorecard & QA** | Tests, screenshot folder, updated scorecard doc |

Estimated risk: animation clip/controller mismatch (Sidekick vs Kevin Iglesias parameter names). Mitigation: single test scene with one rifle unit before full slice.

---

## 11. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Animator parameter mismatch | Validate against `AC_CombatArena_Infantry` in PlayMode; fallback to timed lunge if trigger ignored |
| Perimeter props block camera | Props disabled by default globally; only enabled for slice config preset; tune offset |
| UI layout regression on damage | Disable Synty Slider; manual fillRect; audio via PlayClipAtPoint |
| Missing Resources refs in player builds | Direct references on ScriptableObjects; bootstrap menus document required assignments |
| Performance from 8 perimeter props + particles | Object pooling already used for units/VFX; profile before adding skybox (deferred) |

---

## 12. Next milestone (after slice sign-off)

**Roster pass B:** Extract `CombatSliceProfileSO` if second encounter is added; animate remaining catalog entries; enable URP post volume for color grade; optional combat music sting.
