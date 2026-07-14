> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone Visual Studio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an in-Unity visual hub (`DeadManZone → Visual Studio`) for editing UI palette, board zone colors, menu atmosphere/lighting, and presets with live Edit Mode preview.

**Architecture:** Split ScriptableObjects (`SceneAtmosphereSO`, `MenuLightingSO`, `VisualProfileSO`) bundle the existing `UiThemeSO`. `VisualProfileApplier` applies profile data in scenes. Editor window edits assets and calls `UiThemeSceneRefresher` for immediate UI feedback without scene rebuild.

**Tech Stack:** Unity Built-in RP, uGUI, TextMeshPro, existing Presentation/Editor assemblies, NUnit Edit Mode tests.

**Spec:** [2026-06-06-deadmanzone-visual-studio-design.md](../specs/2026-06-06-deadmanzone-visual-studio-design.md)

---

## File map

| File | Responsibility |
|------|----------------|
| `Presentation/Visual/SceneAtmosphereSO.cs` | Fog + ambient data; `ApplyToRenderSettings()` |
| `Presentation/Visual/MenuLightingSO.cs` | Named light entries; `ApplyToEnvironment(Transform root)` |
| `Presentation/Visual/VisualProfileSO.cs` | Bundle references + display name |
| `Presentation/Visual/VisualProfileProvider.cs` | Resources load + active profile cache |
| `Presentation/Visual/VisualProfileApplier.cs` | Scene component; applies profile on load/validate |
| `Presentation/Editor/VisualProfilePresetFactory.cs` | Create default + starter presets |
| `Presentation/Editor/VisualProfileEditorUtility.cs` | Apply/save/sync helpers shared by window |
| `Presentation/Editor/UiThemeSceneRefresher.cs` | Live UI refresh in open scene |
| `Presentation/Editor/VisualStudioWindow.cs` | Menu item + window shell |
| `Presentation/Editor/VisualStudioUiTab.cs` | UI & Board tab drawer |
| `Presentation/Editor/VisualStudioAtmosphereTab.cs` | Main Menu + Run atmosphere tabs |
| `Presentation/Editor/VisualStudioPresetsTab.cs` | Preset picker/duplicate/apply |
| `Presentation/Board/BoardView.cs` | Add `RefreshZoneColors()` |
| `Presentation/Visual/UiThemeProvider.cs` | Delegate to active profile |
| `Presentation/Editor/CinematicMenuEnvironmentBuilder.cs` | Read profile instead of hardcoded values |
| `Core.Tests/EditMode/SceneAtmosphereTests.cs` | Unit tests for atmosphere apply |
| `Data/Visual/*` | Default profile + child assets |

---

### Task 1: Scene atmosphere data + tests

**Files:**
- Create: `Assets/_Project/Presentation/Visual/SceneAtmosphereSO.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/SceneAtmosphereTests.cs`
- Modify: `Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef` (add Presentation reference)

- [ ] **Step 1: Write failing test**

```csharp
using DeadManZone.Presentation.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Core.Tests
{
    public class SceneAtmosphereTests
    {
        [Test]
        public void ApplyToRenderSettings_SetsFogAndAmbient()
        {
            var atmosphere = ScriptableObject.CreateInstance<SceneAtmosphereSO>();
            atmosphere.fogEnabled = true;
            atmosphere.fogColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            atmosphere.fogDensity = 0.04f;
            atmosphere.fogMode = FogMode.Exponential;
            atmosphere.ambientSkyColor = Color.red;
            atmosphere.ambientEquatorColor = Color.green;
            atmosphere.ambientGroundColor = Color.blue;
            atmosphere.ambientMode = AmbientMode.Trilight;

            atmosphere.ApplyToRenderSettings();

            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(atmosphere.fogColor, RenderSettings.fogColor);
            Assert.AreEqual(atmosphere.fogDensity, RenderSettings.fogDensity, 0.0001f);
            Assert.AreEqual(atmosphere.ambientSkyColor, RenderSettings.ambientSkyColor);

            Object.DestroyImmediate(atmosphere);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

Run: Unity Edit Mode tests or `dotnet test` if wired; expected: type `SceneAtmosphereSO` not found.

- [ ] **Step 3: Implement `SceneAtmosphereSO`**

```csharp
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Visual
{
    [CreateAssetMenu(menuName = "DeadManZone/Visual/Scene Atmosphere")]
    public sealed class SceneAtmosphereSO : ScriptableObject
    {
        public bool fogEnabled = true;
        public Color fogColor = new(0.12f, 0.08f, 0.05f, 1f);
        public FogMode fogMode = FogMode.Exponential;
        public float fogDensity = 0.035f;
        public float linearFogStart;
        public float linearFogEnd = 300f;
        public AmbientMode ambientMode = AmbientMode.Trilight;
        public Color ambientSkyColor = new(0.08f, 0.09f, 0.11f);
        public Color ambientEquatorColor = new(0.06f, 0.05f, 0.04f);
        public Color ambientGroundColor = new(0.03f, 0.025f, 0.02f);

        public void ApplyToRenderSettings()
        {
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.linearFogStart = linearFogStart;
            RenderSettings.linearFogEnd = linearFogEnd;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
        }

        public void CopyFromCurrentRenderSettings()
        {
            fogEnabled = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = RenderSettings.fogMode;
            fogDensity = RenderSettings.fogDensity;
            linearFogStart = RenderSettings.linearFogStart;
            linearFogEnd = RenderSettings.linearFogEnd;
            ambientMode = RenderSettings.ambientMode;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
        }
    }
}
```

- [ ] **Step 4: Add Presentation reference to Core.Tests asmdef; run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Presentation/Visual/SceneAtmosphereSO.cs Assets/_Project/Core.Tests/EditMode/SceneAtmosphereTests.cs Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef
git commit -m "feat: add SceneAtmosphereSO with render settings apply"
```

---

### Task 2: Menu lighting data

**Files:**
- Create: `Assets/_Project/Presentation/Visual/MenuLightingSO.cs`

- [ ] **Step 1: Implement `MenuLightEntry` + `MenuLightingSO`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [Serializable]
    public struct MenuLightEntry
    {
        public string lightName;
        public LightType lightType;
        public Color color;
        public float intensity;
        public float range;
        public Vector3 localPosition;
        public Vector3 eulerRotation;
    }

    [CreateAssetMenu(menuName = "DeadManZone/Visual/Menu Lighting")]
    public sealed class MenuLightingSO : ScriptableObject
    {
        public List<MenuLightEntry> lights = new();

        public void ApplyToEnvironment(Transform menuEnvironmentRoot)
        {
            if (menuEnvironmentRoot == null)
                return;

            foreach (var entry in lights)
            {
                if (string.IsNullOrEmpty(entry.lightName))
                    continue;

                var lightTransform = menuEnvironmentRoot.Find(entry.lightName);
                if (lightTransform == null)
                    continue;

                var light = lightTransform.GetComponent<Light>();
                if (light == null)
                    continue;

                light.type = entry.lightType;
                light.color = entry.color;
                light.intensity = entry.intensity;
                light.range = entry.range;
                lightTransform.localPosition = entry.localPosition;
                lightTransform.localRotation = Quaternion.Euler(entry.eulerRotation);
            }
        }

        public void CaptureFromEnvironment(Transform menuEnvironmentRoot)
        {
            lights.Clear();
            if (menuEnvironmentRoot == null)
                return;

            for (var i = 0; i < menuEnvironmentRoot.childCount; i++)
            {
                var child = menuEnvironmentRoot.GetChild(i);
                var light = child.GetComponent<Light>();
                if (light == null)
                    continue;

                lights.Add(new MenuLightEntry
                {
                    lightName = child.name,
                    lightType = light.type,
                    color = light.color,
                    intensity = light.intensity,
                    range = light.range,
                    localPosition = child.localPosition,
                    eulerRotation = child.localEulerAngles
                });
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Visual/MenuLightingSO.cs
git commit -m "feat: add MenuLightingSO for menu environment lights"
```

---

### Task 3: Visual profile bundle + provider

**Files:**
- Create: `Assets/_Project/Presentation/Visual/VisualProfileSO.cs`
- Create: `Assets/_Project/Presentation/Visual/VisualProfileProvider.cs`
- Modify: `Assets/_Project/Presentation/Visual/UiThemeProvider.cs`

- [ ] **Step 1: Implement `VisualProfileSO`**

```csharp
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [CreateAssetMenu(menuName = "DeadManZone/Visual/Profile")]
    public sealed class VisualProfileSO : ScriptableObject
    {
        public string displayName = "Default";
        public UiThemeSO uiTheme;
        public SceneAtmosphereSO mainMenuAtmosphere;
        public MenuLightingSO mainMenuLighting;
        public SceneAtmosphereSO runAtmosphere;
        public Object postProcessProfile; // optional PostProcessProfile ref

        public UiThemeSO UiTheme => uiTheme;
    }
}
```

- [ ] **Step 2: Implement `VisualProfileProvider`**

```csharp
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    public static class VisualProfileProvider
    {
        public const string ResourcePath = "DeadManZone/VisualProfile";
        private static VisualProfileSO _cached;

        public static VisualProfileSO Current
        {
            get
            {
                if (_cached != null)
                    return _cached;
                _cached = Resources.Load<VisualProfileSO>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;
    }
}
```

- [ ] **Step 3: Update `UiThemeProvider.Current` to prefer profile UI theme**

```csharp
public static UiThemeSO Current
{
    get
    {
        var profileTheme = VisualProfileProvider.Current?.uiTheme;
        if (profileTheme != null)
            return profileTheme;

        if (_cached != null)
            return _cached;

        _cached = Resources.Load<UiThemeSO>(ResourcePath);
        if (_cached == null)
            _cached = CreateFallback();
        return _cached;
    }
}
```

Also call `VisualProfileProvider.InvalidateCache()` from `InvalidateCache()`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/Visual/VisualProfileSO.cs Assets/_Project/Presentation/Visual/VisualProfileProvider.cs Assets/_Project/Presentation/Visual/UiThemeProvider.cs
git commit -m "feat: add VisualProfileSO bundle and provider"
```

---

### Task 4: VisualProfileApplier + BoardView refresh

**Files:**
- Create: `Assets/_Project/Presentation/Visual/VisualProfileApplier.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`

- [ ] **Step 1: Add `RefreshZoneColors()` to `BoardView`**

```csharp
public void RefreshZoneColors()
{
    if (_layout == null)
        return;

    foreach (var pair in _tiles)
    {
        var coord = pair.Key;
        var tile = pair.Value;
        if (tile == null)
            continue;

        var zone = _layout.GetZone(coord);
        var color = GetZoneColor(zone);
        tile.SetBaseColor(color);
        tile.SetOverlay(color, _layout.IsSpecialTile(coord), false);
    }
}
```

- [ ] **Step 2: Implement `VisualProfileApplier`**

```csharp
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [ExecuteAlways]
    public sealed class VisualProfileApplier : MonoBehaviour
    {
        [SerializeField] private VisualProfileSO profile;
        [SerializeField] private VisualProfileSceneKind sceneKind = VisualProfileSceneKind.MainMenu;
        [SerializeField] private Transform menuEnvironmentRoot;

        public VisualProfileSO Profile
        {
            get => profile;
            set => profile = value;
        }

        public void ApplyNow()
        {
            if (profile == null)
                profile = VisualProfileProvider.Current;
            if (profile == null)
                return;

            switch (sceneKind)
            {
                case VisualProfileSceneKind.MainMenu:
                    profile.mainMenuAtmosphere?.ApplyToRenderSettings();
                    profile.mainMenuLighting?.ApplyToEnvironment(menuEnvironmentRoot);
                    break;
                case VisualProfileSceneKind.Run:
                    profile.runAtmosphere?.ApplyToRenderSettings();
                    break;
            }
        }

        private void OnEnable() => ApplyNow();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyNow();
        }
#endif
    }

    public enum VisualProfileSceneKind
    {
        MainMenu,
        Run
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Visual/VisualProfileApplier.cs Assets/_Project/Presentation/Board/BoardView.cs
git commit -m "feat: add VisualProfileApplier and board zone refresh"
```

---

### Task 5: Default profile assets + preset factory

**Files:**
- Create: `Assets/_Project/Presentation/Editor/VisualProfilePresetFactory.cs`
- Create assets under `Assets/_Project/Data/Visual/` and `Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset`

- [ ] **Step 1: Implement factory with menu item `DeadManZone/Visual Studio/Create Default Profile`**

Factory steps:
1. Ensure `UiTheme.asset` exists via `UiThemeEditor.EnsureThemeAsset()`
2. Create `MainMenuAtmosphere.asset` seeded from current MainMenu scene or `CinematicMenuEnvironmentBuilder` defaults
3. Create `MainMenuLighting.asset` with KeyLight/FillLight/RimLight entries matching builder
4. Create `DeadManZoneDefaultVisualProfile.asset` linking above
5. Copy/symlink active profile to `Resources/DeadManZone/VisualProfile.asset`

- [ ] **Step 2: Add starter preset duplicates: High Contrast (boost text contrast, desaturate zones), Bleached Trench (lift ambient, reduce fog density)**

- [ ] **Step 3: Run menu item once in Unity; verify assets exist**

- [ ] **Step 4: Commit generated `.asset` files + factory script**

```bash
git add Assets/_Project/Presentation/Editor/VisualProfilePresetFactory.cs Assets/_Project/Data/Visual Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset
git commit -m "feat: add default visual profile assets and preset factory"
```

---

### Task 6: Refactor cinematic menu environment builder

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/CinematicMenuEnvironmentBuilder.cs`

- [ ] **Step 1: Replace hardcoded `ApplyRenderSettings` and light colors with reads from active `VisualProfileSO`**

After building light GameObjects, call `profile.mainMenuLighting.CaptureFromEnvironment(root.transform)` if lighting asset has empty list (first-time seed).

- [ ] **Step 2: Refresh Main Menu scene via existing menu item; confirm fog/lights unchanged visually**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Editor/CinematicMenuEnvironmentBuilder.cs
git commit -m "refactor: drive menu environment from visual profile"
```

---

### Task 7: UiThemeSceneRefresher

**Files:**
- Create: `Assets/_Project/Presentation/Editor/UiThemeSceneRefresher.cs`
- Create: `Assets/_Project/Presentation/Editor/VisualProfileEditorUtility.cs`

- [ ] **Step 1: Implement refresher**

```csharp
public static void RefreshOpenScene(VisualProfileSO profile)
{
    UiThemeProvider.InvalidateCache();
    VisualProfileProvider.InvalidateCache();

    var theme = profile?.uiTheme ?? UiThemeEditor.EnsureThemeAsset();

    foreach (var view in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
    {
        switch (view)
        {
            case RunHudView hud: hud.ApplyTheme(theme); break;
            case PauseMenuView pause: pause.ApplyTheme(theme); break;
            case RunEndOverlayView end: end.ApplyTheme(theme); break;
            case AchievementsPanelView achievements: achievements.ApplyTheme(theme); break;
            case LeaderboardPanelView leaderboard: leaderboard.ApplyTheme(theme); break;
            case BoardView board: board.RefreshZoneColors(); break;
        }
    }

    RefreshCanvasBackgrounds(theme);
    foreach (var applier in Object.FindObjectsByType<VisualProfileApplier>(FindObjectsSortMode.None))
        applier.ApplyNow();

    MenuThemeEditor.EnsureMenuTheme(); // sync SlimUI if present
    EditorUtility.SetDirty(profile);
}
```

Implement `RefreshCanvasBackgrounds` to set root canvas `Image` background from `theme.backgroundColor` on objects named `MenuCanvas` or `RunCanvas`.

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Editor/UiThemeSceneRefresher.cs Assets/_Project/Presentation/Editor/VisualProfileEditorUtility.cs
git commit -m "feat: add live UI theme scene refresher for visual studio"
```

---

### Task 8: Visual Studio editor window

**Files:**
- Create: `Assets/_Project/Presentation/Editor/VisualStudioWindow.cs`
- Create: `Assets/_Project/Presentation/Editor/VisualStudioPresetsTab.cs`
- Create: `Assets/_Project/Presentation/Editor/VisualStudioUiTab.cs`
- Create: `Assets/_Project/Presentation/Editor/VisualStudioAtmosphereTab.cs`

- [ ] **Step 1: Window shell with `[MenuItem("DeadManZone/Visual Studio")]`**

State: active profile, selected tab, autoApply bool (EditorPrefs key `DMZ_VisualStudio_AutoApply`).

Toolbar buttons call `VisualProfileEditorUtility.ApplyToOpenScene`, `AssetDatabase.SaveAssets`, `UiThemeSceneRefresher.RefreshOpenScene`.

- [ ] **Step 2: Presets tab** — object field for profile, Duplicate/Rename/Apply, list starter presets from `Assets/_Project/Data/Visual/Presets`

- [ ] **Step 3: UI & Board tab** — `Editor.CreateEditor(profile.uiTheme)` embedded inspector OR manual grouped color fields mirroring `UiThemeSO` headers; on change if autoApply call refresher

- [ ] **Step 4: Main Menu tab** — embedded inspectors for atmosphere + lighting lists; buttons Capture From Scene / Select Light (ping `MenuEnvironment/KeyLight` etc.)

- [ ] **Step 5: Run Scene tab** — run atmosphere fields + Refresh UI button

- [ ] **Step 6: Preview tab** — scene dropdown, auto-apply toggle, Open Scene button

- [ ] **Step 7: Manual test in Unity — tweak accent, fog, key light; confirm live update**

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Presentation/Editor/VisualStudio*.cs
git commit -m "feat: add DeadManZone Visual Studio editor window"
```

---

### Task 9: Scene integration

**Files:**
- Modify: `Assets/_Project/Scenes/MainMenu.unity`
- Modify: `Assets/_Project/Scenes/Run.unity`
- Modify: `Assets/_Project/Presentation/Editor/CinematicMenuSceneBuilder.cs` (add applier when building)
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs` (add applier on Run canvas root)

- [ ] **Step 1: Add `VisualProfileApplier` to MainMenu scene root (or dedicated `VisualProfile` GO)**

Set `sceneKind = MainMenu`, wire `menuEnvironmentRoot` to `MenuEnvironment` transform, assign default profile.

- [ ] **Step 2: Add applier to Run scene with `sceneKind = Run`**

- [ ] **Step 3: Update scene builders to inject applier on refresh**

- [ ] **Step 4: Run existing `MainMenuPlayModeTests` — must still pass**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scenes/MainMenu.unity Assets/_Project/Scenes/Run.unity Assets/_Project/Presentation/Editor/CinematicMenuSceneBuilder.cs Assets/_Project/Presentation/Editor/RunSceneSetup.cs
git commit -m "feat: wire VisualProfileApplier into MainMenu and Run scenes"
```

---

### Task 10: Verification

- [ ] Open Unity → `DeadManZone/Visual Studio`
- [ ] With MainMenu open: change `accentColor`, fog density, KeyLight warmth — confirm live update
- [ ] Duplicate preset → Apply → Save Assets → reload scene → values persist
- [ ] Play Mode Run scene: board zones and HUD use updated palette
- [ ] Click Sync SlimUI Menu Theme — SlimUI buttons update
- [ ] Run Edit Mode tests + Play Mode main menu test

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| SceneAtmosphereSO | Task 1 |
| MenuLightingSO | Task 2 |
| VisualProfileSO bundle | Task 3 |
| VisualProfileProvider + UiThemeProvider | Task 3 |
| VisualProfileApplier | Task 4, 9 |
| BoardView live refresh | Task 4 |
| Preset factory + starters | Task 5 |
| Builder refactor | Task 6 |
| UiThemeSceneRefresher | Task 7 |
| Visual Studio window (all tabs) | Task 8 |
| Scene integration | Task 9 |
| Success criteria verification | Task 10 |
| Post-process optional slot | Task 3 (`Object postProcessProfile`) — no UI beyond object field in Presets tab |
| Out of scope (sprites, camera, full post stack) | Not planned |

## Plan self-review

- No TBD/TODO placeholders in task steps.
- Type names consistent across tasks (`VisualProfileSO`, `MenuLightEntry`, `RefreshZoneColors`).
- Files kept under ~300 lines by splitting tabs into separate editor classes.
