> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone Visual Studio — Design Spec

**Date:** 2026-06-06  
**Engine:** Unity (Built-in Render Pipeline)  
**Status:** Approved  
**Scope:** In-editor visual hub for UI palette, board look, scene atmosphere, menu lighting, and preset save/load — with live Edit Mode preview.

---

## Problem

Visual settings are scattered across `UiThemeSO`, hardcoded builder scripts (`CinematicMenuEnvironmentBuilder`), scene `RenderSettings`, and SlimUI theme assets. Tweaking the look requires chat-driven code changes or full scene rebuilds, with no unified preview loop.

## Goal

Deliver **DeadManZone Visual Studio**: a dockable Unity Editor window that lets designers create, edit, and save visual presets with immediate feedback in open scenes (MainMenu and Run).

## Non-Goals (v1)

- Piece sprite/material assignment (keep `DeadManZone/Art/*` pipeline)
- Menu diorama prop placement or camera animation editing
- Full post-processing UI (optional asset slot only until a volume exists in scenes)
- URP/HDRP migration
- JSON export/import of presets (deferred to v1.1)

---

## Architecture

### Data model

```
VisualProfileSO (bundle + preset identity)
├── uiTheme              → UiThemeSO (existing fields, unchanged)
├── mainMenuAtmosphere   → SceneAtmosphereSO
├── mainMenuLighting     → MenuLightingSO
├── runAtmosphere        → SceneAtmosphereSO (optional)
└── postProcessProfile   → PostProcessProfile reference (optional)
```

**`SceneAtmosphereSO`** — ScriptableObject holding:

- `fogEnabled`, `fogColor`, `fogMode`, `fogDensity` (and linear start/end when applicable)
- `ambientMode`, `ambientSkyColor`, `ambientEquatorColor`, `ambientGroundColor`

**`MenuLightingSO`** — ScriptableObject holding a list of `MenuLightEntry` structs:

- `lightName` (matched to child GameObject name under `MenuEnvironment`)
- `lightType` (Directional / Point / Spot)
- `color`, `intensity`, `range` (point/spot)
- `localPosition`, `eulerRotation`

**`VisualProfileSO`** — ScriptableObject bundle with display name and references to the assets above.

### Runtime loading

- **`VisualProfileProvider`** — loads active profile from `Resources.Load<VisualProfileSO>("DeadManZone/VisualProfile")`, same pattern as `UiThemeProvider`.
- **`UiThemeProvider.Current`** — delegates to `VisualProfileProvider.Current.uiTheme` when a profile exists; falls back to direct Resources load for backward compatibility.
- **`VisualProfileApplier`** — `[ExecuteAlways]` MonoBehaviour placed in MainMenu and Run scenes; applies atmosphere and menu lighting from the active profile on validate and when invoked by the editor.

### Editor hub

**Menu:** `DeadManZone → Visual Studio`  
**Window:** dockable `EditorWindow` with tabs:

| Tab | Purpose |
|-----|---------|
| **Presets** | Active profile picker; duplicate, rename, delete; built-in starter presets |
| **UI & Board** | All `UiThemeSO` fields with grouped headers and swatch previews |
| **Main Menu** | Atmosphere controls; editable light list with “Select in scene” |
| **Run Scene** | Optional run atmosphere; “Refresh UI theme in scene” |
| **Preview** | Target scene (MainMenu / Run); auto-apply toggle; optional screenshot |

Toolbar: **Apply to Scene**, **Save Assets**, **Revert Unsaved**, **Sync SlimUI Menu Theme**.

When **Auto-apply** is enabled, color and atmosphere changes push to the open scene immediately via `VisualProfileApplier` and `UiThemeSceneRefresher`.

### Live preview workflow

**`UiThemeSceneRefresher`** (editor-only static helper):

1. Invalidate `UiThemeProvider` cache.
2. Find and call `ApplyTheme(UiThemeSO)` on all components that expose it (`RunHudView`, `PauseMenuView`, `RunEndOverlayView`, `AchievementsPanelView`, `LeaderboardPanelView`, etc.).
3. Refresh `BoardView` zone tile colors via public refresh API (new method on `BoardView`).
4. Re-apply `UiThemeApplicator` conventions on canvas roots (`MenuCanvas`, `RunScene` hierarchy) for `Image`, `Button`, and `TMP_Text` where patterns are unambiguous.

**`VisualProfileApplier`**:

- Applies `SceneAtmosphereSO` to `RenderSettings` for the active scene context.
- Applies `MenuLightingSO` entries to lights under `MenuEnvironment` by name match; creates missing lights only when explicitly requested (avoid polluting scenes).
- Invoked from applier validate, editor window apply, and preset switch.

### Preset system

- Default active profile: `Assets/_Project/Data/Visual/DeadManZoneDefaultVisualProfile.asset`
- Resources copy: `Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset` (referenced at runtime)
- **Duplicate preset** — copies profile and child atmosphere/lighting assets (UI theme duplicated unless user chooses “share UI theme”)
- **Apply preset** — sets active profile, writes Resources reference, applies to open scene, marks dirty
- Built-in starters (factory-created, read-only originals): **Iron Vanguard** (current defaults), **High Contrast**, **Bleached Trench**

SlimUI `DeadManZoneMenuTheme.asset` sync uses existing `MenuThemeEditor.ApplyUiThemeColors` when user clicks **Sync SlimUI Menu Theme** or on save when auto-sync is enabled.

### Refactor existing builders

Replace hardcoded values in `CinematicMenuEnvironmentBuilder` with reads from `VisualProfileSO.mainMenuAtmosphere` and `mainMenuLighting`. Scene setup menus continue to work; they seed from the active profile instead of inline constants.

---

## File layout

```
Assets/_Project/
├── Presentation/
│   ├── Visual/
│   │   ├── SceneAtmosphereSO.cs
│   │   ├── MenuLightingSO.cs
│   │   ├── VisualProfileSO.cs
│   │   ├── VisualProfileProvider.cs
│   │   └── VisualProfileApplier.cs
│   └── Editor/
│       ├── VisualStudioWindow.cs
│       ├── VisualProfileEditorUtility.cs
│       ├── UiThemeSceneRefresher.cs
│       └── VisualProfilePresetFactory.cs
└── Data/
    └── Visual/
        ├── DeadManZoneDefaultVisualProfile.asset
        ├── Atmosphere/
        └── Presets/
```

Editor assembly: extend existing `_Project` editor scripts; add `VisualStudioWindow` under `Presentation/Editor/` (split sub-editors if any file exceeds ~300 lines).

---

## Integration points

| Existing system | Change |
|-----------------|--------|
| `UiThemeSO` | No field changes; edited through hub |
| `UiThemeProvider` | Delegate to active `VisualProfileSO` |
| `MenuThemeEditor` | Called from hub on sync |
| `CinematicMenuEnvironmentBuilder` | Read profile assets |
| `BoardView` | Add `RefreshZoneColors()` for live tile update |
| MainMenu / Run scenes | Add `VisualProfileApplier` component |

---

## Success criteria

1. Open **DeadManZone → Visual Studio** with MainMenu scene open.
2. Change accent color, fog density, and key light warmth — see updates live without scene rebuild.
3. Save as a new preset and re-apply it.
4. Enter Play Mode on Run scene — UI uses the same palette from Resources.
5. SlimUI menu buttons reflect synced theme after **Sync SlimUI Menu Theme**.

---

## Testing

- Edit Mode: tweak each tab field; confirm MainMenu atmosphere and Run UI refresh.
- Preset duplicate/apply restores prior look.
- Play Mode smoke: MainMenu → Run retains palette (existing play mode tests unaffected).
- No new per-frame allocations in `VisualProfileApplier` at runtime (apply on load only).

---

## Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Partial UI refresh misses custom-styled elements | Document known roots; extend refresher incrementally |
| Light name drift vs scene hierarchy | “Select in scene” + validation warnings in hub |
| Large monolithic editor window file | Split tab drawers into separate static classes |
