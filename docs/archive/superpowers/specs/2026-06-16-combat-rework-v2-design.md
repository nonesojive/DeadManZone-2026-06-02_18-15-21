> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Combat Rework v2 Design

**Date:** 2026-06-16  
**Branch:** `combatreworkv2`  
**Status:** Approved  
**Supersedes (presentation):** Incremental upgrade over Iron Vanguard / spectacle passes  
**Reference:** Top Troops idle combat tempo + formation readability; grim trench-war presentation (not bright cartoon)

---

## Summary

Rework combat **presentation and feel** on `combatreworkv2` in two sequential approaches:

| Phase | Approach | Deliverable |
|-------|----------|-------------|
| **Wave 1 (now)** | **A — Shell upgrade pass** | Extend existing arena bootstrap with diorama-grade atmosphere, hybrid trench backdrop, grim HUD/VFX palette |
| **Wave 2 (after A is good)** | **B — Battlefield assembly system** | Modular backdrop rings, dedicated atmosphere controller, data-driven art iteration |
| **Wave 3 (later)** | Feel tuning | Role engagement pacing, juice timing, camera punch (sim unchanged) |

**Scope locked:** Presentation + combat feel. No sim rule changes, no deployment/targeting preview, no left/right-only layout change.

---

## Locked decisions

| Area | Choice |
|------|--------|
| Art direction | **Hybrid** — Synty Apocalypse units/buildings on trench-diorama battlefield + grim URP post |
| Camera | Keep Top Troops oblique framer (`CombatArenaCameraFramer`) |
| Sky | **No bright skydome** — fog + solid/muted horizon |
| Atmosphere source | Port `DioramaAtmosphere` mood into runtime via `CombatArenaAtmosphereProfileSO` |
| Backdrop | `CombatArenaBackdrop` — trench dressing + distant ruins + low-count atmosphere FX |
| Legacy perimeter | Disabled when new backdrop is enabled |
| Tests | TDD — EditMode layout/atmosphere tests + PlayMode backdrop spawn gate |

---

## Wave 1 — Approach A architecture

```
CombatArenaBootstrap.FrameBattlefield
    ├── CombatArenaAtmosphereApplicator (fog, lights, URP volume)
    ├── CombatArenaBackdrop (trench ring + skyline + FX)
    └── existing ground / camera / units
```

### New types

| Type | Assembly | Role |
|------|----------|------|
| `CombatArenaAtmosphereProfileSO` | Data | Grim tuning + post volume ref |
| `CombatArenaBackdropLayout` | Presentation | Deterministic spawn-point math (testable) |
| `CombatArenaBackdropCatalog` | Presentation | SimpleMilitary / SimpleFX prefab paths |
| `CombatArenaBackdrop` | Presentation | Spawns dressing from layout |
| `CombatArenaAtmosphereApplicator` | Presentation | Applies profile to scene |

### Success criteria (Wave 1)

1. Iron Vanguard slice loads with backdrop root and ≥8 dressing props
2. Fog density ≥ 0.035; bright skydome off in `CombatArenaConfig`
3. URP post volume active on arena camera (Diorama-grade desaturation/vignette)
4. Army HUD uses muted olive/rust fills (not saturated green/red)
5. EditMode + PlayMode tests green

---

## Wave 2 — Approach B (deferred)

Replace monolithic `CombatArenaBackdrop` with ring assembler:

- `CombatArenaBackdropAssembler` + per-ring `ICombatArenaBackdropRing`
- `CombatArenaAtmosphereController` owning volume/lights/FX lifecycle
- Ring assets as ScriptableObjects for designer iteration

Approach A types remain; B refactors internals without changing bootstrap API.

---

## Out of scope (v1)

- Pre-battle targeting arrows
- Full roster per-unit animation pass
- Combat music / mixer snapshots
- `TickCombatRun` / pause threshold changes
