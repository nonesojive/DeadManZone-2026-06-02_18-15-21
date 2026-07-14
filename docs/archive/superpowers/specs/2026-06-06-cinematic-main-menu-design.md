> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Cinematic Main Menu — Design Spec

**Date:** 2026-06-06  
**Engine:** Unity  
**Status:** Approved — implementation in progress  
**Scope:** Replace flat programmatic main menu with SlimUI 3D Modern Menu cinematic shell while preserving all DeadManZone menu logic.  
**Art backlog:** [2026-06-06-cinematic-main-menu-art-requirements.md](./2026-06-06-cinematic-main-menu-art-requirements.md)

---

## Goal

Deliver a **full cinematic main menu**: animated camera, SlimUI panel/button polish, optional 3D diorama backdrop, and async scene loading — without replacing `MainMenuController` game logic or the existing `UiThemeSO` palette used across Run UI.

---

## Non-Goals (this pass)

- Replacing in-run UI (board, shop, combat) with SlimUI
- SlimUI mobile template
- Full key-bindings / difficulty / HUD settings tabs
- Custom trench environment art (placeholder diorama only for MVP)
- Removing `UiThemeSO` (menu SlimUI theme mirrors it; Run UI unchanged)

---

## Current State

| Piece | Location | Role |
|-------|----------|------|
| `MainMenuController` | `_Project/Presentation/MainMenu/` | Continue, New Run, faction select, options stub, meta panels |
| `MenuSceneSetup` | `_Project/Presentation/Editor/` | Regenerates flat overlay canvas scene |
| `UiThemeSO` | `_Project/Presentation/Visual/` | Grimdark brass palette for all UI |
| SlimUI 3D Modern Menu | `Assets/SlimUI/Modern Menu 1/` | Camera animator, canvas template, settings scaffold, SFX |
| Play Mode test | `MainMenuPlayModeTests` | Continue hidden when no save |

SlimUI is **not referenced** anywhere in `_Project` today.

---

## Architecture

```
MainMenu Scene
├── MenuCamera          Camera + MainMenuCam Animator + AudioListener
├── MenuEnvironment     3D backdrop (lighting, fog, optional unit props)
├── EventSystem
├── RunManager
└── MenuCanvas          Screen Space – Camera (renderCamera = MenuCamera)
    ├── MainMenuController      (game logic — kept)
    ├── MainMenuCameraDirector  (NEW — camera + load transitions)
    └── UI panels (SlimUI-styled)
        ├── MainPanel           camera Animate = 0
        ├── FactionPanel        camera Animate = 1
        ├── OptionsPanel        camera Animate = 1
        ├── AchievementsPanel   camera Animate = 1
        └── LeaderboardPanel    camera Animate = 1
```

### Component responsibilities

**`MainMenuController`** (extend, not replace)

- Keeps all existing button handlers, save gating, faction unlock checks, `RunManager` integration.
- On panel transitions, delegates to `MainMenuCameraDirector`:
  - `ShowMainPanel()` → camera Position 1
  - `ShowFactionPanel()` / `ShowOptionsPanel()` / meta panels → camera Position 2
- Scene exit (Continue / Start Run) calls director async load instead of direct `GameScenes.LoadRun()`.

**`MainMenuCameraDirector`** (new)

- Holds reference to camera `Animator` (`Animate` float: 0 = hero/main, 1 = sub-panel).
- Methods: `FocusMain()`, `FocusSubPanel()`, `LoadRunSceneAsync()`.
- `LoadRunSceneAsync()` uses SlimUI-style loading overlay (slider + optional key prompt disabled for MVP).
- Plays SlimUI swoosh SFX on sub-panel entry (serialized AudioSource refs).

**`MainMenuSceneSetup`** (rename/refactor from editor portion of `MenuSceneSetup`)

- Instantiates SlimUI prefabs instead of building flat buttons from scratch.
- Wires serialized refs on controller + director.
- Applies DeadManZone theme asset to `ThemedUIElement` components.

**`DeadManZoneMenuTheme`** (new ScriptableObject instance)

- `ThemedUIData` asset with colors copied from `UiThemeSO`:
  - Graphic/accent: brass `accentColor`
  - Text: `textPrimary` / `textSecondary`
- Single preset (`custom1`); ignore SlimUI custom2/custom3.

---

## Camera & panel mapping

SlimUI `MainMenuCam` controller uses one bool-like float **`Animate`**:

| Value | Animation | Visible panel |
|-------|-----------|---------------|
| `0` | `MenuCamIdle` / Position 1 | Main menu (title + primary buttons) |
| `1` | `MenuCamPos2` | Faction select, Options, Achievements, Leaderboard |

All sub-panels share Position 2. Panel swap is instant UI toggle after camera starts moving (~0.6s blend). Returning to main calls `FocusMain()` then hides sub-panels.

---

## Visual design

### UI chrome (from SlimUI)

- `Btn_MainMenu.prefab` for all menu buttons
- Panel frame sprites (`Panel Frame 512px`, corner details)
- `WindowPopUpBig` animator on faction/options/meta sheets
- `POST_ModernMenu.asset` post-processing on MenuCamera (tuned warmer/darker)
- Fonts: keep SlimUI Rubik/Poppins or swap to project TMP default — decision at implementation (default: Rubik Bold for title, Poppins for body)

### Copy & layout

| Element | Text / behavior |
|---------|-----------------|
| Title | **Until The Trenches Fall** |
| Subtitle | Dynamic (existing `subtitleText` logic) |
| Buttons | Continue (save-gated), New Run, Achievements, Leaderboard, Options, Exit |
| Faction panel | Ironmarch Vanguard, Dust Scourge, Cartel of Echoes + lock labels |
| Options | Audio (Music/SFX sliders) + Video (Fullscreen) only for MVP |

### 3D backdrop (MVP placeholder)

No dedicated environment art exists yet. **MVP is lights and fog only** — no unit meshes or environment props until dedicated menu art is produced.

- Dark ground plane + exponential fog (amber-tinted)
- 2–3 warm point/spot lights (arc-lamp feel)
- Skybox: solid dark or existing Unity default dark

Future art pass replaces `MenuEnvironment` contents per `docs/superpowers/specs/2026-06-06-cinematic-main-menu-art-requirements.md` without touching controller code.

---

## Options panel (MVP subset)

Strip SlimUI settings tabs down to:

| Tab | Controls | Storage |
|-----|----------|---------|
| Game | Music volume, SFX volume | `PlayerPrefs` (SlimUI keys OK) |
| Video | Fullscreen toggle | `Screen.fullScreen` |

Remove: key bindings, difficulty, motion blur, AA toggles, tooltip/HUD toggles.

`UISettingsManager` can remain on Options panel root with unused UI hidden/disabled in scene setup rather than forked source.

---

## Scene loading flow

```
User clicks Continue or Faction
  → MainMenuController validates (save / unlock)
  → MainMenuCameraDirector.LoadRunSceneAsync("Run")
      → show loading overlay, hide main canvas
      → AsyncOperation progress bar
      → allowSceneActivation = true (no key prompt in MVP)
  → Run scene active
```

`GameScenes.LoadMainMenu()` from pause/run-end stays synchronous (no loading screen required returning to menu).

---

## Editor workflow

| Menu item | Action |
|-----------|--------|
| `DeadManZone/Refresh Main Menu Scene` | Rebuild cinematic MainMenu from prefabs + theme |
| `DeadManZone/Setup Main Menu & Run Scenes` | Unchanged entry point; MainMenu uses new composer |

Scene is **regenerated** by editor script (same pattern as today). Manual edits to `MainMenu.unity` get overwritten on refresh — document in script header comment.

---

## Files to add / modify

| Action | Path |
|--------|------|
| **Create** | `Assets/_Project/Presentation/MainMenu/MainMenuCameraDirector.cs` |
| **Modify** | `Assets/_Project/Presentation/MainMenu/MainMenuController.cs` |
| **Modify** | `Assets/_Project/Presentation/Editor/MenuSceneSetup.cs` (cinematic generation) |
| **Create** | `Assets/_Project/Data/Menu/DeadManZoneMenuTheme.asset` |
| **Create** | `Assets/_Project/Presentation/MainMenu/Prefabs/` (optional composed sub-prefabs) |
| **Modify** | `Assets/_Project/Scenes/MainMenu.unity` (via refresh) |
| **Modify** | `Assets/_Project/Tests.PlayMode/MainMenuPlayModeTests.cs` (optional: wait for camera setup) |

No changes to `DeadManZone.Core` or sim assemblies.

---

## Testing

| Test | Method |
|------|--------|
| Continue hidden without save | Existing Play Mode test — must still pass |
| Continue visible with save | Add Play Mode test |
| Faction lock labels | Manual + optional unit test on `MetaProgressionService` integration |
| Camera transition | Manual: New Run opens faction panel with camera sweep |
| Load Run | Manual: Continue and New Run reach Run scene without error |
| Refresh scene | Editor: `Refresh Main Menu Scene` produces valid wired scene |

---

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| SlimUI `UIMenuManager` conflicts with custom flow | Do **not** use `UIMenuManager`; only borrow prefabs, animator, SFX |
| `ThemedUIElement` uses legacy `TextMeshPro` type | Verify TMP compatibility; replace component refs in prefab variants if needed |
| Scene regen wipes manual tweaks | Single composer script; optional sub-prefabs for hand-tuned layout |
| No 3D props ready | **Confirmed:** MVP uses lights + fog only; props deferred to art pass |
| Two theme systems (UiThemeSO + ThemedUIData) | Menu-only SlimUI theme; document mapping in theme asset |

---

## Success criteria

- Main menu feels cinematic: camera moves on sub-panel entry, polished SlimUI buttons/frames, post-process grade
- All existing menu **functionality** preserved (save continue, 3 factions with locks, exit, options stub upgraded to audio/video)
- Play Mode test suite passes
- Run scene load uses loading overlay (no hard cut)

---

## Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Integration depth | SlimUI shell + own logic | `UIMenuManager` Play/Campaign model doesn't fit autobattler flow |
| Camera positions | 2 positions shared by all sub-panels | Matches SlimUI asset; avoids custom animation work |
| Theme | Mirror `UiThemeSO` into `ThemedUIData` | Keeps grimdark brass identity |
| 3D backdrop | Lights + fog only (MVP) | No environment art yet; full list in art-requirements doc |
| Settings scope | Audio + fullscreen only | Relevant controls; avoid misleading FPS/key-binding UI |
