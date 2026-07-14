> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Combat Arena Presentation Design

**Date:** 2026-06-10  
**Branch:** `combat-rework`  
**Engine:** Unity 6  
**Status:** Draft — pending user review  
**Supersedes (presentation only):** Flat grid combat replay sections in `2026-06-06-deadmanzone-top-down-visual-commitment.md` (build phase unchanged)  
**Builds on:** `2026-06-06-deadmanzone-master-design.md`, `2026-06-04-deadmanzone-combat-units-demo-design.md`

---

## Summary

Rework combat **presentation** to feel like Top Troops idle battles: enter a **3D angled arena**, watch units **move and fight automatically**, with richer motion and VFX. Build phase stays the flat UI grid puzzle. Combat sim, event log, segments, and tactic pauses are **unchanged** — only how replay is rendered changes.

**Reference:** Top Troops — pre-placed army, continuous auto-battle spectacle, angled 3D battlefield with animated squads. DeadManZone differs by keeping the Backpack Battles build grid and mirroring placements directly (no separate deployment step).

---

## Locked decisions

| Area | Choice |
|------|--------|
| Primary focus | Visual presentation (not sim pacing rework) |
| Approach | Hybrid 2.5D — additive 3D combat sub-scene |
| Build vs combat | Mode switch: flat UI grid → 3D arena on fight start |
| Unit placement | Direct mirror from build board; fight starts after transition |
| Unit art (v1) | Camera-facing billboards from existing `PieceDefinitionSO.icon` |
| Unit art (later) | Low-poly rigged models per unit (`combatModel` on SO) |
| Buildings (v1) | Not rendered; sim logic unchanged |
| Buildings (later) | Same staged pipeline: decals → 3D props |
| Tactic pauses | **Full visual freeze** — tweens, animators, particles stop; overlay UI on frozen scene |
| Sim / Core | No changes to `TickCombatRun`, damage, movement rules, save schema |

---

## Section 1 — Architecture & flow

### Core principle

**Sim stays pure C#; presentation gets a second skin for combat.**

```
TickCombatRun → CombatEventLog → CombatDirector replay
                                      ↓
                            CombatArenaPresenter (new)
                            CombatBoardPresenter (disabled in arena mode)
```

### Player flow

```
Build Phase (Run scene — existing)
  Shop, reserves, drag-drop on flat grid
  Player finalizes board
        ↓
  Start Fight
        ↓
Transition (~1–2s — extend existing CombatFlowPresenter loading overlay)
  Hide: board, shop, reserves (resource HUD optional)
  Load/show: CombatArena additive sub-scene
  Spawn combatant billboards from board snapshot (direct mirror)
        ↓
Combat Phase (3D arena + screen-space overlay UI)
  CombatDirector replays event log → arena actors
  Segment ends → full visual freeze → TacticPausePanel
  Fight ends → battle report
        ↓
Return to Build
  Unload/hide arena, restore flat build UI
```

### New presentation components

| Component | Role |
|-----------|------|
| `CombatArenaBootstrap` | Arena scene setup: camera, ground, lighting |
| `CombatGridMapper` | `GridCoord` (full battlefield layout) → world `(x, z)` |
| `CombatUnitActor` | One per living combatant; billboard or SkinnedMeshRenderer |
| `CombatUnitActorPool` | Reuse actors across fights |
| `CombatArenaPresenter` | Handles `CombatDirector.EventReplayed`; drives actors |
| `CombatArenaFreezeController` | Pause/resume all motion on tactic pause |
| `CombatArenaVfx` | World-space impacts, damage numbers, death bursts |
| `CombatBillboard` | Camera-facing quad + icon material |
| `CombatArenaConfigSO` | Cell size, camera angles, transition timing |

### Build board → arena mapping

- Uses existing `BattlefieldLayout`: player half | neutral (7 cols) | enemy half.
- Positions mirror 1:1 from build snapshot — no deployment step.
- **Combatants only in v1** — pieces without combat movement may exist in sim but have no arena actor.
- Enemy X mirror stays in Core; mapper consumes post-mirror world coords from `BattlefieldState`.

### Presentation routing

```
CombatDirector.EventReplayed
        ↓
   IsArenaActive?
    /         \
  yes          no (dev fallback only)
   ↓              ↓
CombatArenaPresenter   CombatBoardPresenter → BoardView
```

During normal play, build UI is hidden; board replay does not run in parallel.

### Unchanged for this rework

- Tick sim, segments, gas phase, tactic/ability rules
- Build-phase grid, shop, reserves, synergies
- Save/resume JSON schema
- Determinism requirements for event log

---

## Section 2 — Arena layout, camera, movement & pause freeze

### 3D arena layout

```
                    ENEMY HALF
              ←─────────────────→
    ┌─────────┬───────────────┬─────────┐
    │ Player  │   Neutral     │ Enemy   │
    │  half   │ no-man's-land │  half   │
    └─────────┴───────────────┴─────────┘
         ↑ camera from player side
```

| Element | v1 spec |
|---------|---------|
| Ground | Single trench/mud mesh or tiled plane; neutral zone = cratered no-man's-land |
| Grid mapping | `CombatGridMapper`: cell `(x,y)` → world position on ground plane |
| Cell size | Tunable via `CombatArenaConfigSO` (~1.5–2m world units) |
| Depth | Real 3D Z-order on ground; billboards face camera |
| Buildings | Not rendered in v1 |
| Combatants | One `CombatUnitActor` per living unit with combat movement |

### Camera

| Setting | Value |
|---------|--------|
| Projection | Perspective (recommended for Top Troops feel) |
| Elevation | ~30–35° |
| Azimuth | ~225° (consistent with isometric token art) |
| Framing | Auto-fit full battlefield width |
| During combat | Fixed in v1 |
| On pause | Optional subtle zoom toward neutral zone (polish, not v1) |

### Unit movement & combat feel (billboard v1)

| Event | Visual behavior |
|-------|-----------------|
| `move` | Smooth lerp to target cell over ~0.3–0.5s |
| `damage` | Hit flash, floating damage number, screen shake on large hits |
| attack (implicit) | Forward lunge ~0.15s toward target, then return |
| `destroyed` | Death burst VFX, fade/scale-down, remove actor |
| ability events | Colored flash + particle burst at target (expand per ability later) |

If a new `move` arrives mid-lerp, blend to the new target. Multi-cell sim jumps use faster lerp or stepped motion.

### Tactic pause — full visual freeze

When `CombatDirector` finishes a segment and invokes `PausedForCommands`:

| System | Behavior |
|--------|----------|
| Event replay | Stopped (coroutine finished) |
| Movement tweens | **Paused** — hold mid-lerp position |
| Attack animations | **Frozen** on current frame |
| Particles / VFX | **Paused** |
| Ambient arena | Optional desaturate/vignette (polish) |
| UI | `TacticPausePanel` overlays frozen 3D scene |
| Resume | Unpause all motion; next segment playback begins |

`CombatArenaFreezeController` listens to `PausedForCommands` and resume from `CombatFlowPresenter`.

### Transition (build → arena)

1. **0.0s** — Loading overlay ("Entering combat…"); hide board/shop/reserves  
2. **0.3s** — Load/show `CombatArena` sub-scene  
3. **0.5s** — Spawn unit billboards at mirrored positions (fade-in)  
4. **~1.0s** — Overlay fades; `CombatDirector` begins segment 1  

Return to build: fade out arena, restore build UI, unload arena.

### On-screen during combat

| Visible | Hidden |
|---------|--------|
| 3D arena + unit billboards | Build grid, shop, reserves |
| Resource HUD (optional) | Drag-drop, synergy overlays |
| Tactic pause panel (when paused) | |
| Battle report (after fight) | |

---

## Section 3 — Art pipeline & component boundaries

### Staged art pipeline

**Phase 1 — Billboards (v1)**

```
PieceDefinitionSO.icon → Quad + material → CombatBillboard (face camera)
→ code-driven Idle / MoveLerp / AttackLunge / Death
```

**Phase 2 — Billboard juice**

- Per-unit lunge tuning, muzzle flashes, footstep dust.

**Phase 3 — 3D model swap (per unit)**

```
Blender/Meshy → rig → FBX → PieceDefinitionSO.combatModel (optional)
→ CombatUnitActor uses SkinnedMeshRenderer if set, else billboard
```

Existing pipeline: `Assets/_Project/Art/Neutral/Source/*.blend`, `neutral_token_camera.py`.

**Phase 4 — Buildings (deferred)**

```
v2: ground decal (icon on cell)
v3: simple 3D prop for large footprints
```

### New files

| Path | Purpose |
|------|---------|
| `Assets/_Project/Presentation/Combat/Arena/*.cs` | Arena presentation layer |
| `Assets/Scenes/CombatArena.unity` | Additive sub-scene |
| `Assets/_Project/Data/CombatArenaConfigSO.cs` | Tunable arena settings |

### Modified files

| File | Change |
|------|--------|
| `CombatFlowPresenter.cs` | Arena vs build UI toggle; freeze wiring |
| `GameScenes.cs` | `CombatArena` constant |
| `PieceDefinitionSO.cs` | Optional `combatModel` prefab (Phase 3) |

### Untouched

- All of `DeadManZone.Core` combat sim
- Build-phase `BoardView`, shop, reserves
- Save/resume schema

### v1 art requirements

| Asset | Required | Source |
|-------|----------|--------|
| Ground mesh + trench texture | Yes | New or evolved from `trench_battlefield_backdrop` |
| Unit billboards | Yes | Existing icons in `Art/Neutral/Renders/Icons/` |
| Building props | No | Deferred |
| Rigged models | No | Phase 3 |

### Performance

- Pool `CombatUnitActor` instances (~20–30 max).
- Pool particle bursts; cap simultaneous VFX.
- Target 60 FPS on desktop; profile after first arena scene.

---

## Section 4 — Save/resume, testing & scope

### Save & resume (mid-combat)

No schema changes. On load into `RunPhase.Combat`:

1. `CombatFlowPresenter.BeginCombatPresentation()` runs (existing).
2. Arena bootstraps from saved player board + `Combat.EnemyBoard` snapshot.
3. `CombatArenaPresenter` restores actor positions from saved `EventLog` (same logic as `CombatReplayVisuals.RestoreFromBattlefieldAndEvents`).
4. If `AwaitingCommand`: arena **frozen** at end of completed segment; show tactic panel.
5. If mid-segment replay needed on load: replay from segment start or restore state from log (match current `CombatBoardPresenter` behavior).

Arena state is always **reconstructible** from saved board + event log — no separate arena save fields.

### Testing

| Layer | Tests |
|-------|-------|
| **Core** | No new tests required (sim unchanged) |
| **Edit Mode** | `CombatGridMapperTests` — coord → world → coord round-trip; mirror enemy half |
| **Edit Mode** | `CombatArenaPresenterTests` — mock events drive actor state (no Unity scene) |
| **Play Mode** | Enter fight → arena visible, build UI hidden |
| **Play Mode** | Unit moves on `move` event (lerp, not snap) |
| **Play Mode** | Tactic pause freezes actors; resume continues |
| **Play Mode** | Save mid-combat → reload → arena restores positions |
| **Play Mode** | Fight end → arena hides, build UI returns |
| **Regression** | Existing EditMode combat sim tests must still pass unchanged |

### combat-rework scope (v1)

**In scope**

- `CombatArena` additive scene + bootstrap
- `CombatGridMapper`, `CombatUnitActor`, billboard rendering
- `CombatArenaPresenter` wired to `CombatDirector`
- `CombatArenaFreezeController` on tactic pause
- Basic world-space VFX (impact, death, damage numbers)
- Build ↔ arena transition in `CombatFlowPresenter`
- `CombatArenaConfigSO` for tuning
- Play Mode smoke tests

**Out of scope (follow-up phases)**

- 3D rigged unit models (Phase 3)
- Building rendering (Phase 4)
- Fog-of-war gas reveal intro
- Camera pan/zoom during fight
- Tactical ground overlay grid on pause
- Combat pacing / segment structure changes
- Removing tactic pauses
- Separate Top Troops–style deployment step

### Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Dual replay paths (board vs arena) drift | Single `EventReplayed` bus; disable `CombatBoardPresenter` when arena active |
| Mid-lerp save/resume looks wrong | On restore, snap actors to log-derived positions (skip in-flight tween) |
| Scene load hitch | Prewarm arena during loading overlay; keep scene lightweight |
| Billboard + perspective camera mismatch | Lock camera angles to match icon art azimuth (~225°) |

---

## Relationship to prior specs

| Spec | Relationship |
|------|--------------|
| `2026-06-06-deadmanzone-top-down-visual-commitment.md` | **Build phase unchanged.** Combat replay moves from "same 2D grid" to 3D arena. Deferred 3D combat skin is now partial 3D arena with billboards. |
| `2026-06-04-deadmanzone-combat-units-demo-design.md` | Sim segments, tactics, abilities unchanged. Pause UI overlays 3D scene. |
| `2026-05-31-deadmanzone-autobattler-design.md` | Total War *tempo* (phases/pauses) kept; Top Troops *spectacle* (arena, motion) added to presentation. |

---

## Next step

After user approval: invoke **writing-plans** to produce `docs/superpowers/plans/2026-06-10-deadmanzone-combat-arena-presentation.md`.
