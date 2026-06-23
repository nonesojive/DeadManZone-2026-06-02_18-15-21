# Combat Arena 2D (Top Troops Hybrid) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a parallel Top Troops–style 2.5D combat presentation backend (ortho camera, sprite units, arced VFX) without changing combat sim logic.

**Architecture:** `CombatArenaVisualMode` on config selects Legacy3D vs TopTroops2D. Shared `CombatGridMapper` + `CombatArenaPresenter` event replay; 2D-specific battlefield, unit visual, VFX, and ortho framer.

**Tech Stack:** Unity 6, URP, NUnit EditMode/PlayMode, existing CombatArena scene + new CombatArena2D scene.

**Spec:** `docs/superpowers/specs/2026-06-23-combat-arena-2d-design.md`

---

### Task 1: Core helpers + tests

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaVisualMode.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresentationMode.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DSortOrder.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DProjectileArc.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatUnitSpriteResolver.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaOrthographicFramer.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/CombatArena2DHelpersTests.cs`

- [ ] Write failing tests for sort order, arc midpoint, sprite priority, ortho size
- [ ] Implement helpers until green

### Task 2: 2D battlefield + placeholders

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DPlaceholderSprites.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DBattlefieldView.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs`

### Task 3: Unit visual 2D

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`

### Task 4: 2D VFX

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/ICombatArenaVfxPresenter.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DVfx.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`

### Task 5: Bootstrap + scene routing

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaSceneLoader.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaBuildingSpawner.cs`
- Modify: `Assets/_Project/Game/GameScenes.cs`
- Create: `Assets/_Project/Presentation/Editor/CombatArena2DSceneBootstrap.cs`
- Create: `Assets/_Project/Tests.PlayMode/CombatArena2DLoadPlayModeTests.cs`
