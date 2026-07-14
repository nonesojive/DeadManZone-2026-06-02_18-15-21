> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Cinematic Main Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the flat MainMenu scene with a SlimUI-backed cinematic menu (camera sweep, fog backdrop, async load) while preserving `MainMenuController` game logic.

**Architecture:** `MainMenuCameraDirector` owns camera animator + loading overlay. `CinematicMenuSceneBuilder` (editor) generates the scene with MenuCamera, fog/lights environment, Screen Space Camera canvas, SlimUI buttons, and wired controllers. `UiThemeSO` drives colors; optional `DeadManZoneMenuTheme` (`ThemedUIData`) tints SlimUI elements.

**Tech Stack:** Unity uGUI, TextMeshPro, SlimUI 3D Modern Menu assets, existing DeadManZone Presentation/Game assemblies.

---

### Task 1: MainMenuCameraDirector

**Files:**
- Create: `Assets/_Project/Presentation/MainMenu/MainMenuCameraDirector.cs`

- [ ] Add director with `FocusMain()`, `FocusSubPanel()`, `LoadRunScene()` coroutine
- [ ] Wire Animator float `Animate` (0 = main, 1 = sub-panel)

### Task 2: Extend MainMenuController

**Files:**
- Modify: `Assets/_Project/Presentation/MainMenu/MainMenuController.cs`

- [ ] Inject optional `MainMenuCameraDirector`
- [ ] Call camera focus on panel transitions
- [ ] Route Continue / faction start through async load

### Task 3: Menu theme asset

**Files:**
- Create: `Assets/_Project/Presentation/Editor/MenuThemeEditor.cs`
- Create: `Assets/_Project/Data/Menu/DeadManZoneMenuTheme.asset` (via editor)

- [ ] Create `ThemedUIData` mirroring `UiThemeSO` brass palette via reflection

### Task 4: Cinematic scene builder

**Files:**
- Create: `Assets/_Project/Presentation/Editor/CinematicMenuSceneBuilder.cs`
- Create: `Assets/_Project/Presentation/Editor/CinematicMenuEnvironmentBuilder.cs`
- Modify: `Assets/_Project/Presentation/Editor/MenuSceneSetup.cs`

- [ ] Build fog + lights + ground plane environment
- [ ] Build MenuCamera with `MainMenuCam` animator
- [ ] Build Screen Space Camera canvas with SlimUI buttons + all panels
- [ ] Wire controller + director + RunManager

### Task 5: Tests & verification

**Files:**
- Modify: `Assets/_Project/Tests.PlayMode/MainMenuPlayModeTests.cs`

- [ ] Ensure Continue-hidden test still passes after scene refresh
- [ ] Run Unity batch refresh if available
