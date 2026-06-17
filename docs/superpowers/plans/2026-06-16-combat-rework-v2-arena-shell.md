# Combat Rework v2 — Arena Shell (Approach A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or executing-plans for Wave 2 feel pass and later Approach B.

**Goal:** Ship grim Top Troops–style arena shell — diorama atmosphere, hybrid trench backdrop, muted HUD — without changing combat sim.

**Architecture:** Extend `CombatArenaBootstrap` with `CombatArenaAtmosphereApplicator` + `CombatArenaBackdrop`. Data-driven via `CombatArenaAtmosphereProfileSO`. Approach B later refactors backdrop into ring assembler.

**Tech Stack:** Unity 6, URP, SimpleMilitary + SimpleFX props, Diorama_PostFX volume

---

## Wave 1 — Completed (Approach A)

- [x] `CombatArenaAtmosphereProfileSO` + grim defaults
- [x] `CombatArenaAtmosphereApplicator` (fog, lights, post volume)
- [x] `CombatArenaBackdrop` + layout + catalog
- [x] `CombatArenaConfig` wired to atmosphere profile; skybox off
- [x] Grim HUD fill/label colors
- [x] EditMode tests (layout + atmosphere)
- [x] PlayMode test scaffold (`CombatArenaBackdropPlayModeTests`)
- [x] Editor menu: `DeadManZone → Combat Arena → Apply Combat Rework v2 — Grim Arena Shell`

## Wave 2 — Combat feel (next)

- [ ] Tune `RoleEngagement` / movement goal anchors for formation clash
- [ ] `CombatArenaConfig` pacing (move lerp, attack timing)
- [ ] `CombatArenaJuicePresenter` — camera punch + micro hit-stop on heavy damage
- [ ] Spectacle timing tests against Iron Vanguard slice

## Wave 3 — Approach B (modular battlefield assembly)

- [x] `CombatArenaBackdropAssembler` + `ICombatArenaBackdropRing`
- [x] Per-ring `CombatArenaBackdropRingSO` ScriptableObjects
- [x] `CombatArenaAtmosphereController` lifecycle split from applicator
- [x] Editor bootstrap creates default ring assets under `BackdropRings/`

## Editor validation

1. Run `DeadManZone → Combat Arena → Apply Combat Rework v2 — Grim Arena Shell`
2. Open `Run` scene → enter combat (Iron Vanguard slice menu or normal fight)
3. Confirm: cold fog horizon, ruined skyline in distance, trench props at board edge, desaturated post, muted army bars
