using System;
using System.IO;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Editor
{
    public static class SyntyUiKitSetup
    {
        public const string KitRoot = "Assets/Synty/InterfaceMilitaryCombatHUD";
        public const string FramesRoot = KitRoot + "/Sprites/Frames";
        public const string ButtonsRoot = KitRoot + "/Sprites/Buttons";
        public const string BannersRoot = KitRoot + "/Sprites/Banners";
        public const string MilitaryCombatRoot = KitRoot + "/Sprites/MilitaryCombat";
        public const string HudSpritesRoot = KitRoot + "/Sprites/HUD";
        public const string ModernMenusBackgroundRoot = "Assets/Synty/InterfaceModernMenus/Sprites/General";

        public const string ThemeAssetPath = "Assets/_Project/Data/Visual/Presets/SyntyTrenchUiTheme.asset";
        public const string RunAtmosphereAssetPath = "Assets/_Project/Data/Visual/Presets/SyntyTrenchRunAtmosphere.asset";
        public const string PresetProfilePath = "Assets/_Project/Data/Visual/Presets/SyntyTrenchVisualProfile.asset";

        private static readonly string[] FrameSearchFolders =
        {
            FramesRoot,
            MilitaryCombatRoot,
            HudSpritesRoot
        };

        private static readonly string[] ButtonSearchFolders =
        {
            ButtonsRoot,
            MilitaryCombatRoot,
            HudSpritesRoot
        };

        private static readonly string[] BannerSearchFolders =
        {
            BannersRoot,
            HudSpritesRoot,
            MilitaryCombatRoot
        };

        [MenuItem("DeadManZone/UI Kit/Import Synty Trench Theme")]
        public static void ImportSyntyTrenchTheme()
        {
            if (!AssetDatabase.IsValidFolder(KitRoot))
            {
                Debug.LogError(
                    $"Synty Military Combat HUD kit not found at {KitRoot}. " +
                    "Import InterfaceMilitaryCombatHUD into Assets/Synty/.");
                return;
            }

            EnsureSyntyTrenchTheme();
            EnsureSyntyTrenchRunAtmosphere();
            EnsureSyntyTrenchProfile(EnsureSyntyTrenchTheme());
            VisualProfilePresetFactory.EnsureDefaultProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Synty Trench UI theme ready.\n" +
                $"- Theme: {ThemeAssetPath}\n" +
                $"- Run atmosphere: {RunAtmosphereAssetPath}\n" +
                $"- Preset profile: {PresetProfilePath}\n" +
                $"Use DeadManZone/UI Kit/Apply Synty Theme To Active Profile to wire runtime assets.");
        }

        [MenuItem("DeadManZone/UI Kit/Apply Synty Theme To Active Profile")]
        public static void ApplySyntyThemeToActiveProfile()
        {
            var theme = EnsureSyntyTrenchTheme();
            var runAtmosphere = EnsureSyntyTrenchRunAtmosphere();
            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath);

            if (runtimeProfile == null)
                runtimeProfile = VisualProfilePresetFactory.EnsureDefaultProfile();

            runtimeProfile.displayName = "Synty Trench";
            runtimeProfile.uiTheme = theme;
            runtimeProfile.runAtmosphere = runAtmosphere;
            EditorUtility.SetDirty(runtimeProfile);

            var defaultProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.DefaultProfilePath);
            if (defaultProfile != null)
            {
                defaultProfile.displayName = "Synty Trench";
                defaultProfile.uiTheme = theme;
                defaultProfile.runAtmosphere = runAtmosphere;
                EditorUtility.SetDirty(defaultProfile);
            }

            UiThemeProvider.InvalidateCache();
            VisualProfileProvider.InvalidateCache();
            UiThemeSceneRefresher.RefreshOpenScene(runtimeProfile);

            AssetDatabase.SaveAssets();
            Debug.Log("Active visual profile now uses Synty Trench UI sprites, colors, and run atmosphere.");
        }

        public static UiThemeSO EnsureSyntyTrenchTheme()
        {
            EnsureFolder("Assets/_Project/Data/Visual/Presets");

            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(ThemeAssetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeSO>();
                theme.name = "SyntyTrenchUiTheme";
                AssetDatabase.CreateAsset(theme, ThemeAssetPath);
            }

            ApplySyntyTrenchPalette(theme);
            CopySemanticColorsFromBunker(theme);

            theme.panelSprite = LoadSprite(
                new[] { "Frame_Medium_01", "Frame_Rounded_01", "Frame_Base_01" },
                FrameSearchFolders);
            theme.cardSprite = LoadSprite(
                new[] { "Box_Medium_01", "Frame_Small_01", "Frame_Base_02" },
                FrameSearchFolders);
            theme.modalFrameSprite = LoadSprite(
                new[] { "Frame_Medium_12", "Frame_Medium_11", "Frame_Techy_01" },
                FrameSearchFolders);
            theme.sidebarPanelSprite = LoadSprite(
                new[] { "Frame_Medium_03", "Frame_Medium_02", "Frame_Small_03" },
                FrameSearchFolders);
            theme.inventoryPanelSprite = LoadSprite(
                new[] { "Frame_Medium_20", "Frame_Medium_17", "Frame_Medium_16" },
                FrameSearchFolders);
            theme.securityTerminalFrameSprite = LoadSprite(
                new[] { "Frame_Techy_01", "Frame_Techy_02", "Frame_Medium_13" },
                FrameSearchFolders);
            theme.bannerSprite = LoadSprite(
                new[] { "Background_HazardStripes_01", "Bar_DogEar_01_Clean", "Badge_Shield_07" },
                BannerSearchFolders);

            theme.buttonNormalSprite = LoadSprite(
                new[] { "Button_01", "Box_Medium_02", "Bar_Angled_01_Clean" },
                ButtonSearchFolders);
            theme.buttonHighlightedSprite = LoadSprite(
                new[] { "Button_02", "Box_Medium_03", "Bar_Angled_02_Clean" },
                ButtonSearchFolders);
            theme.buttonPressedSprite = LoadSprite(
                new[] { "Button_03", "Box_Medium_04", "Bar_Angled_01_Gradient" },
                ButtonSearchFolders);
            theme.buttonDisabledSprite = LoadSprite(
                new[] { "Button_04", "Box_Small_01", "Box_Small_02" },
                ButtonSearchFolders);
            theme.accentButtonSprite = LoadSprite(
                new[] { "Bar_01", "Bar_DogEar_01_Gradient", "Box_Medium_01" },
                ButtonSearchFolders);
            theme.secondaryButtonSprite = LoadSprite(
                new[] { "Box_Medium_01", "Frame_Small_02", "Bar_02" },
                ButtonSearchFolders);
            theme.dangerButtonSprite = LoadSprite(
                new[] { "Background_HazardStripes_01", "Bar_08", "Box_Medium_04" },
                ButtonSearchFolders);
            theme.warningButtonSprite = LoadSprite(
                new[] { "Background_HazardStripes_01", "Bar_07", "Bar_06" },
                ButtonSearchFolders);

            theme.slotEmptySprite = LoadSprite(
                new[] { "Socket_Background_01", "Box_Small_01", "Frame_Small_01" },
                FrameSearchFolders);
            theme.slotSelectedSprite = LoadSprite(
                new[] { "Socket_Frame_01", "Frame_Small_04", "Frame_Small_05" },
                FrameSearchFolders);
            theme.storageSlotEmptySprite = theme.slotEmptySprite;
            theme.storageSlotSelectedSprite = theme.slotSelectedSprite;

            theme.menuBackgroundSprite = LoadSprite(
                new[] { "Menu_Background_Spiky_Large_01", "Menu_Background_Vignette_01", "Vignette_Background_01" },
                new[] { ModernMenusBackgroundRoot, HudSpritesRoot });
            theme.runBackgroundSprite = LoadSprite(
                new[] { "Background_Angled_01", "Background_DogEar_01", "Background_Octagon_01" },
                new[] { HudSpritesRoot, MilitaryCombatRoot });
            theme.combatBackgroundSprite = null;

            EditorUtility.SetDirty(theme);
            return theme;
        }

        public static SceneAtmosphereSO EnsureSyntyTrenchRunAtmosphere()
        {
            EnsureFolder("Assets/_Project/Data/Visual/Presets");

            var atmosphere = AssetDatabase.LoadAssetAtPath<SceneAtmosphereSO>(RunAtmosphereAssetPath);
            if (atmosphere == null)
            {
                atmosphere = ScriptableObject.CreateInstance<SceneAtmosphereSO>();
                atmosphere.name = "SyntyTrenchRunAtmosphere";
                AssetDatabase.CreateAsset(atmosphere, RunAtmosphereAssetPath);
            }

            atmosphere.fogEnabled = true;
            atmosphere.fogColor = new Color(0.45f, 0.42f, 0.38f, 1f);
            atmosphere.fogMode = FogMode.Exponential;
            atmosphere.fogDensity = 0.025f;
            atmosphere.linearFogStart = 0f;
            atmosphere.linearFogEnd = 300f;
            atmosphere.ambientMode = AmbientMode.Trilight;
            atmosphere.ambientSkyColor = new Color(0.42f, 0.40f, 0.36f);
            atmosphere.ambientEquatorColor = new Color(0.38f, 0.35f, 0.30f);
            atmosphere.ambientGroundColor = new Color(0.28f, 0.26f, 0.22f);

            EditorUtility.SetDirty(atmosphere);
            return atmosphere;
        }

        private static VisualProfileSO EnsureSyntyTrenchProfile(UiThemeSO theme)
        {
            var defaultProfile = VisualProfilePresetFactory.EnsureDefaultProfile();
            var profile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(PresetProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VisualProfileSO>();
                profile.name = "SyntyTrenchVisualProfile";
                AssetDatabase.CreateAsset(profile, PresetProfilePath);
            }

            profile.displayName = "Synty Trench";
            profile.uiTheme = theme;
            profile.mainMenuAtmosphere = defaultProfile.mainMenuAtmosphere;
            profile.mainMenuLighting = defaultProfile.mainMenuLighting;
            profile.runAtmosphere = EnsureSyntyTrenchRunAtmosphere();
            profile.postProcessProfile = defaultProfile.postProcessProfile;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ApplySyntyTrenchPalette(UiThemeSO theme)
        {
            theme.backgroundColor = new Color(0.10f, 0.11f, 0.09f, 1f);
            theme.panelColor = new Color(0.16f, 0.17f, 0.14f, 0.96f);
            theme.cardColor = new Color(0.20f, 0.21f, 0.18f, 0.98f);
            theme.accentColor = new Color(0.72f, 0.62f, 0.32f, 1f);
            theme.accentMutedColor = new Color(0.48f, 0.42f, 0.22f, 1f);
            theme.dangerColor = new Color(0.70f, 0.24f, 0.18f, 1f);
            theme.textPrimary = new Color(0.90f, 0.88f, 0.80f, 1f);
            theme.textSecondary = new Color(0.58f, 0.56f, 0.48f, 1f);
            theme.textOnAccent = new Color(0.10f, 0.09f, 0.07f, 1f);
            theme.buttonNormal = new Color(0.24f, 0.25f, 0.21f, 0.98f);
            theme.buttonHighlighted = new Color(0.34f, 0.35f, 0.30f, 1f);
            theme.buttonPressed = new Color(0.16f, 0.17f, 0.14f, 1f);
            theme.combatOverlayColor = new Color(0.03f, 0.04f, 0.03f, 0.58f);
            theme.combatBannerColor = new Color(0.12f, 0.11f, 0.09f, 0.90f);
        }

        private static void CopySemanticColorsFromBunker(UiThemeSO theme)
        {
            var bunker = AssetDatabase.LoadAssetAtPath<UiThemeSO>(BunkerSurvivalUiKitSetup.ThemeAssetPath);
            if (bunker == null)
                return;

            const float desaturation = 0.10f;
            theme.rearZoneColor = Desaturate(bunker.rearZoneColor, desaturation);
            theme.supportZoneColor = Desaturate(bunker.supportZoneColor, desaturation);
            theme.frontZoneColor = Desaturate(bunker.frontZoneColor, desaturation);
            theme.neutralZoneColor = Desaturate(bunker.neutralZoneColor, desaturation);
            theme.specialTileColor = Desaturate(bunker.specialTileColor, desaturation);
            theme.generalLaneTint = Desaturate(bunker.generalLaneTint, desaturation);
            theme.engineersLaneTint = Desaturate(bunker.engineersLaneTint, desaturation);
            theme.requisitionLaneTint = Desaturate(bunker.requisitionLaneTint, desaturation);
            theme.sellZoneColor = Desaturate(bunker.sellZoneColor, desaturation);
        }

        private static Sprite LoadSprite(string[] nameFragments, string[] folders)
        {
            foreach (var fragment in nameFragments)
            {
                var sprite = FindSpriteByNameFragment(fragment, folders);
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        private static Sprite FindSpriteByNameFragment(string nameFragment, string[] folders)
        {
            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
                Sprite best = null;
                var bestNameLength = int.MaxValue;

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    if (!fileName.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                        continue;

                    if (fileName.Length < bestNameLength)
                    {
                        best = sprite;
                        bestNameLength = fileName.Length;
                    }
                }

                if (best != null)
                    return best;
            }

            return null;
        }

        private static Color Desaturate(Color color, float amount)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            s = Mathf.Clamp01(s - amount);
            var desaturated = Color.HSVToRGB(h, s, v);
            desaturated.a = color.a;
            return desaturated;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
