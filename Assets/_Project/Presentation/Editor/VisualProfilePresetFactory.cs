using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Editor
{
    public static class VisualProfilePresetFactory
    {
        public const string MainMenuAtmospherePath = "Assets/_Project/Data/Visual/Atmosphere/MainMenuAtmosphere.asset";
        public const string RunAtmospherePath = "Assets/_Project/Data/Visual/Atmosphere/RunAtmosphere.asset";
        public const string MainMenuLightingPath = "Assets/_Project/Data/Visual/Lighting/MainMenuLighting.asset";
        public const string DefaultProfilePath = "Assets/_Project/Data/Visual/DeadManZoneDefaultVisualProfile.asset";
        public const string RuntimeProfilePath = "Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset";
        public const string PresetsFolder = "Assets/_Project/Data/Visual/Presets";

        [MenuItem(DeadManZoneEditorMenus.VisualStudio + "Create Default Profile")]
        public static void CreateDefaultProfileMenu()
        {
            EnsureDefaultProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Default visual profile ready at {DefaultProfilePath}");
        }

        [MenuItem(DeadManZoneEditorMenus.VisualStudio + "Create Starter Presets")]
        public static void CreateStarterPresetsMenu()
        {
            var defaultProfile = EnsureDefaultProfile();
            CreateStarterPresets(defaultProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Starter presets ready under {PresetsFolder}");
        }

        public static VisualProfileSO EnsureDefaultProfile()
        {
            EnsureFolder("Assets/_Project/Data/Visual");
            EnsureFolder("Assets/_Project/Data/Visual/Atmosphere");
            EnsureFolder("Assets/_Project/Data/Visual/Lighting");
            EnsureFolder("Assets/_Project/Data/Resources/DeadManZone");

            var uiTheme = ResolvePreferredUiTheme();
            var mainMenuAtmosphere = EnsureMainMenuAtmosphere();
            var runAtmosphere = EnsureRunAtmosphere();
            var mainMenuLighting = EnsureMainMenuLighting();

            var profile = LoadOrCreateAsset(DefaultProfilePath, () =>
            {
                var created = ScriptableObject.CreateInstance<VisualProfileSO>();
                created.displayName = "IronMarch Union";
                return created;
            });

            profile.displayName = ResolveProfileDisplayName(uiTheme);
            profile.uiTheme = uiTheme;
            profile.mainMenuAtmosphere = mainMenuAtmosphere;
            profile.mainMenuLighting = mainMenuLighting;
            profile.runAtmosphere = runAtmosphere;
            EditorUtility.SetDirty(profile);

            EnsureRuntimeProfileCopy(profile);
            return profile;
        }

        public static void CreateStarterPresets(VisualProfileSO sourceProfile)
        {
            if (sourceProfile == null)
                sourceProfile = EnsureDefaultProfile();

            EnsureFolder(PresetsFolder);
            CreateHighContrastPreset(sourceProfile);
            CreateBleachedTrenchPreset(sourceProfile);
        }

        private static SceneAtmosphereSO EnsureMainMenuAtmosphere()
        {
            var atmosphere = LoadOrCreateAsset(MainMenuAtmospherePath, () =>
            {
                var created = ScriptableObject.CreateInstance<SceneAtmosphereSO>();
                SeedMainMenuAtmosphere(created);
                return created;
            });

            if (IsAtmosphereUninitialized(atmosphere))
                SeedMainMenuAtmosphere(atmosphere);

            EditorUtility.SetDirty(atmosphere);
            return atmosphere;
        }

        private static SceneAtmosphereSO EnsureRunAtmosphere()
        {
            var atmosphere = LoadOrCreateAsset(RunAtmospherePath, () =>
            {
                var created = ScriptableObject.CreateInstance<SceneAtmosphereSO>();
                SeedRunAtmosphere(created);
                return created;
            });

            if (IsAtmosphereUninitialized(atmosphere))
                SeedRunAtmosphere(atmosphere);

            EditorUtility.SetDirty(atmosphere);
            return atmosphere;
        }

        private static MenuLightingSO EnsureMainMenuLighting()
        {
            var lighting = LoadOrCreateAsset(MainMenuLightingPath, () =>
            {
                var created = ScriptableObject.CreateInstance<MenuLightingSO>();
                SeedMainMenuLighting(created);
                return created;
            });

            if (lighting.lights == null || lighting.lights.Count == 0)
                SeedMainMenuLighting(lighting);

            EditorUtility.SetDirty(lighting);
            return lighting;
        }

        private static void EnsureRuntimeProfileCopy(VisualProfileSO sourceProfile)
        {
            var runtimeProfile = LoadOrCreateAsset(RuntimeProfilePath, () =>
                ScriptableObject.CreateInstance<VisualProfileSO>());

            runtimeProfile.displayName = sourceProfile.displayName;
            runtimeProfile.uiTheme = sourceProfile.uiTheme;
            runtimeProfile.mainMenuAtmosphere = sourceProfile.mainMenuAtmosphere;
            runtimeProfile.mainMenuLighting = sourceProfile.mainMenuLighting;
            runtimeProfile.runAtmosphere = sourceProfile.runAtmosphere;
            runtimeProfile.postProcessProfile = sourceProfile.postProcessProfile;
            EditorUtility.SetDirty(runtimeProfile);
        }

        private static void CreateHighContrastPreset(VisualProfileSO sourceProfile)
        {
            var preset = DuplicateProfile(
                sourceProfile,
                $"{PresetsFolder}/HighContrastVisualProfile.asset",
                "High Contrast");

            var theme = preset.uiTheme;
            theme.textPrimary = new Color(0.98f, 0.98f, 0.96f, 1f);
            theme.textSecondary = new Color(0.82f, 0.8f, 0.74f, 1f);
            theme.rearZoneColor = BoostSaturation(theme.rearZoneColor, 0.12f);
            theme.supportZoneColor = BoostSaturation(theme.supportZoneColor, 0.12f);
            theme.frontZoneColor = BoostSaturation(theme.frontZoneColor, 0.12f);
            theme.neutralZoneColor = BoostSaturation(theme.neutralZoneColor, 0.12f);
            EditorUtility.SetDirty(theme);
            EditorUtility.SetDirty(preset);
        }

        private static void CreateBleachedTrenchPreset(VisualProfileSO sourceProfile)
        {
            var preset = DuplicateProfile(
                sourceProfile,
                $"{PresetsFolder}/BleachedTrenchVisualProfile.asset",
                "Bleached Trench");

            var atmosphere = preset.mainMenuAtmosphere;
            atmosphere.fogDensity = 0.02f;
            atmosphere.ambientSkyColor = LiftColor(atmosphere.ambientSkyColor, 0.06f);
            atmosphere.ambientEquatorColor = LiftColor(atmosphere.ambientEquatorColor, 0.05f);
            atmosphere.ambientGroundColor = LiftColor(atmosphere.ambientGroundColor, 0.04f);
            EditorUtility.SetDirty(atmosphere);

            WarmKeyLight(preset.mainMenuLighting, new Color(1f, 0.9f, 0.62f));
            EditorUtility.SetDirty(preset.mainMenuLighting);
            EditorUtility.SetDirty(preset);
        }

        private static VisualProfileSO DuplicateProfile(VisualProfileSO source, string profilePath, string displayName)
        {
            var prefix = displayName.Replace(" ", string.Empty);
            var theme = CopySubAsset(source.uiTheme, $"{PresetsFolder}/{prefix}UiTheme.asset");
            var mainMenuAtmosphere = CopySubAsset(
                source.mainMenuAtmosphere,
                $"{PresetsFolder}/{prefix}MainMenuAtmosphere.asset");
            var runAtmosphere = CopySubAsset(
                source.runAtmosphere,
                $"{PresetsFolder}/{prefix}RunAtmosphere.asset");
            var mainMenuLighting = CopySubAsset(
                source.mainMenuLighting,
                $"{PresetsFolder}/{prefix}MainMenuLighting.asset");

            var existing = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(profilePath);
            if (existing != null)
            {
                existing.displayName = displayName;
                existing.uiTheme = theme;
                existing.mainMenuAtmosphere = mainMenuAtmosphere;
                existing.mainMenuLighting = mainMenuLighting;
                existing.runAtmosphere = runAtmosphere;
                existing.postProcessProfile = source.postProcessProfile;
                return existing;
            }

            var profile = ScriptableObject.CreateInstance<VisualProfileSO>();
            profile.displayName = displayName;
            profile.uiTheme = theme;
            profile.mainMenuAtmosphere = mainMenuAtmosphere;
            profile.mainMenuLighting = mainMenuLighting;
            profile.runAtmosphere = runAtmosphere;
            profile.postProcessProfile = source.postProcessProfile;
            AssetDatabase.CreateAsset(profile, profilePath);
            return profile;
        }

        private static T CopySubAsset<T>(T source, string destinationPath) where T : Object
        {
            if (source == null)
                return null;

            var existing = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            if (existing != null)
                return existing;

            var copy = Object.Instantiate(source);
            copy.name = System.IO.Path.GetFileNameWithoutExtension(destinationPath);
            AssetDatabase.CreateAsset(copy, destinationPath);
            return copy;
        }

        private static void SeedMainMenuAtmosphere(SceneAtmosphereSO atmosphere)
        {
            atmosphere.fogEnabled = true;
            atmosphere.fogColor = new Color(0.12f, 0.08f, 0.05f, 1f);
            atmosphere.fogMode = FogMode.Exponential;
            atmosphere.fogDensity = 0.035f;
            atmosphere.linearFogStart = 0f;
            atmosphere.linearFogEnd = 300f;
            atmosphere.ambientMode = AmbientMode.Trilight;
            atmosphere.ambientSkyColor = new Color(0.08f, 0.09f, 0.11f);
            atmosphere.ambientEquatorColor = new Color(0.06f, 0.05f, 0.04f);
            atmosphere.ambientGroundColor = new Color(0.03f, 0.025f, 0.02f);
        }

        private static void SeedRunAtmosphere(SceneAtmosphereSO atmosphere)
        {
            atmosphere.fogEnabled = false;
            atmosphere.fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            atmosphere.fogMode = FogMode.ExponentialSquared;
            atmosphere.fogDensity = 0.01f;
            atmosphere.linearFogStart = 0f;
            atmosphere.linearFogEnd = 300f;
            atmosphere.ambientMode = AmbientMode.Skybox;
            atmosphere.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
            atmosphere.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            atmosphere.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
        }

        private static void SeedMainMenuLighting(MenuLightingSO lighting)
        {
            lighting.lights = new System.Collections.Generic.List<MenuLightEntry>
            {
                new()
                {
                    lightName = "KeyLight",
                    lightType = LightType.Directional,
                    color = new Color(1f, 0.82f, 0.58f),
                    intensity = 0.55f,
                    range = 10f,
                    localPosition = Vector3.zero,
                    eulerRotation = new Vector3(38f, -35f, 0f)
                },
                new()
                {
                    lightName = "FillLight",
                    lightType = LightType.Point,
                    color = new Color(0.95f, 0.65f, 0.35f),
                    intensity = 1.4f,
                    range = 12f,
                    localPosition = new Vector3(-2.5f, 1.8f, 1f),
                    eulerRotation = Vector3.zero
                },
                new()
                {
                    lightName = "RimLight",
                    lightType = LightType.Point,
                    color = new Color(0.55f, 0.45f, 0.32f),
                    intensity = 0.9f,
                    range = 10f,
                    localPosition = new Vector3(2.5f, 2f, -1f),
                    eulerRotation = Vector3.zero
                }
            };
        }

        private static bool IsAtmosphereUninitialized(SceneAtmosphereSO atmosphere)
        {
            return atmosphere.fogDensity <= 0f
                && !atmosphere.fogEnabled
                && atmosphere.ambientSkyColor == Color.black;
        }

        private static void WarmKeyLight(MenuLightingSO lighting, Color warmColor)
        {
            if (lighting?.lights == null)
                return;

            for (var i = 0; i < lighting.lights.Count; i++)
            {
                if (lighting.lights[i].lightName != "KeyLight")
                    continue;

                var entry = lighting.lights[i];
                entry.color = warmColor;
                lighting.lights[i] = entry;
                return;
            }
        }

        private static Color BoostSaturation(Color color, float amount)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            s = Mathf.Clamp01(s + amount);
            var boosted = Color.HSVToRGB(h, s, v);
            boosted.a = color.a;
            return boosted;
        }

        private static Color LiftColor(Color color, float amount)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                color.a);
        }

        private static T LoadOrCreateAsset<T>(string path, System.Func<T> create) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = create();
            created.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static UiThemeSO ResolvePreferredUiTheme()
        {
            if (AssetDatabase.IsValidFolder(GrittyPostApocalypticUiKitSetup.KitRoot))
                return GrittyPostApocalypticUiKitSetup.EnsureTheme();

            return UiThemeEditor.EnsureThemeAsset();
        }

        private static string ResolveProfileDisplayName(UiThemeSO uiTheme)
        {
            if (AssetDatabase.IsValidFolder(GrittyPostApocalypticUiKitSetup.KitRoot) && uiTheme.UsesButtonSprites)
                return "Gritty Post-Apocalyptic";

            return "IronMarch Union";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
