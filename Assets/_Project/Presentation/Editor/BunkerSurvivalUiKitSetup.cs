using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class BunkerSurvivalUiKitSetup
    {
        public const string KitRoot = "Assets/BunkerSurvivalUI";
        public const string NineSliceRoot = KitRoot + "/Sprites/NineSlice";
        public const string BackgroundRoot = KitRoot + "/Sprites/Backgrounds";
        public const string ThemeAssetPath = "Assets/_Project/Data/Visual/Presets/BunkerSurvivalUiTheme.asset";
        public const string PresetProfilePath = "Assets/_Project/Data/Visual/Presets/BunkerSurvivalVisualProfile.asset";

        [MenuItem("DeadManZone/UI Kit/Import Bunker Survival Theme")]
        public static void ImportBunkerSurvivalTheme()
        {
            if (!AssetDatabase.IsValidFolder(KitRoot))
            {
                Debug.LogError(
                    $"Bunker Survival UI kit not found at {KitRoot}. " +
                    "Copy Engine/Unity/Assets/BunkerSurvivalUI from the zip into Assets/.");
                return;
            }

            EnsureBunkerSurvivalTheme();
            EnsureBunkerSurvivalProfile(EnsureBunkerSurvivalTheme());
            VisualProfilePresetFactory.EnsureDefaultProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Bunker Survival UI theme ready.\n" +
                $"- Theme: {ThemeAssetPath}\n" +
                $"- Preset profile: {PresetProfilePath}\n" +
                $"Use DeadManZone/UI Kit/Restyle All Scenes With Bunker Kit to rebuild scenes.");
        }

        [MenuItem("DeadManZone/UI Kit/Apply Bunker Survival Theme To Active Profile")]
        public static void ApplyBunkerSurvivalThemeToActiveProfile()
        {
            var theme = EnsureBunkerSurvivalTheme();
            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath);

            if (runtimeProfile == null)
                runtimeProfile = VisualProfilePresetFactory.EnsureDefaultProfile();

            runtimeProfile.displayName = "Bunker Survival";
            runtimeProfile.uiTheme = theme;
            EditorUtility.SetDirty(runtimeProfile);

            var defaultProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.DefaultProfilePath);
            if (defaultProfile != null)
            {
                defaultProfile.uiTheme = theme;
                EditorUtility.SetDirty(defaultProfile);
            }

            UiThemeProvider.InvalidateCache();
            VisualProfileProvider.InvalidateCache();
            UiThemeSceneRefresher.RefreshOpenScene(runtimeProfile);

            AssetDatabase.SaveAssets();
            Debug.Log("Active visual profile now uses Bunker Survival UI sprites and colors.");
        }

        [MenuItem("DeadManZone/UI Kit/Restyle All Scenes With Bunker Kit")]
        public static void RestyleAllScenesWithBunkerKit()
        {
            if (!AssetDatabase.IsValidFolder(KitRoot))
            {
                Debug.LogError($"Bunker Survival UI kit not found at {KitRoot}.");
                return;
            }

            ApplyBunkerSurvivalThemeToActiveProfile();
            MenuSceneSetup.RefreshMainMenuScene();
            MenuSceneSetup.RefreshRunScene();
            Debug.Log("MainMenu and Run scenes rebuilt with Bunker Survival UI styling.");
        }

        [MenuItem("DeadManZone/UI Kit/Open Bunker Survival Screen Prefabs")]
        public static void PingScreenPrefabs()
        {
            var prefabFolder = KitRoot + "/Prefabs/UI_Screens";
            var folder = AssetDatabase.LoadAssetAtPath<Object>(prefabFolder);
            if (folder == null)
            {
                Debug.LogError($"Prefab folder not found: {prefabFolder}");
                return;
            }

            EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        public static UiThemeSO EnsureBunkerSurvivalTheme()
        {
            EnsureFolder("Assets/_Project/Data/Visual/Presets");

            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(ThemeAssetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeSO>();
                theme.name = "BunkerSurvivalUiTheme";
                AssetDatabase.CreateAsset(theme, ThemeAssetPath);
            }

            theme.ApplyBunkerSurvivalDefaults();
            theme.panelSprite = LoadNineSlice("panel_industrial_main_normal_9slice.png");
            theme.cardSprite = LoadNineSlice("card_item_industrial_normal_9slice.png");
            theme.modalFrameSprite = LoadNineSlice("frame_modal_window_normal_9slice.png");
            theme.sidebarPanelSprite = LoadNineSlice("panel_industrial_sidebar_normal_9slice.png");
            theme.inventoryPanelSprite = LoadNineSlice("panel_main_inventory_normal_9slice.png");
            theme.securityTerminalFrameSprite = LoadNineSlice("frame_security_terminal_normal_9slice.png");
            theme.bannerSprite = LoadNineSlice("tooltip_frame_alert_normal_9slice.png");
            theme.buttonNormalSprite = LoadNineSlice("button_primary_normal_9slice.png");
            theme.buttonHighlightedSprite = LoadNineSlice("button_primary_hover_9slice.png");
            theme.buttonPressedSprite = LoadNineSlice("button_primary_pressed_9slice.png");
            theme.buttonDisabledSprite = LoadNineSlice("button_primary_disabled_9slice.png");
            theme.accentButtonSprite = LoadNineSlice("btn_industrial_primary_normal_9slice.png");
            theme.secondaryButtonSprite = LoadNineSlice("button_secondary_normal_9slice.png");
            theme.dangerButtonSprite = LoadNineSlice("button_danger_normal_9slice.png");
            theme.warningButtonSprite = LoadNineSlice("btn_warning_emergency_normal_9slice.png");
            theme.slotEmptySprite = LoadNineSlice("inventory_slot_01_empty_9slice.png");
            theme.slotSelectedSprite = LoadNineSlice("inventory_slot_01_selected_9slice.png");
            theme.storageSlotEmptySprite = LoadNineSlice("slot_storage_empty_normal_9slice.png");
            theme.storageSlotSelectedSprite = LoadNineSlice("slot_storage_selected_normal_9slice.png");
            theme.menuBackgroundSprite = LoadBackground("07_shelter_dashboard_background_plate.png");
            theme.runBackgroundSprite = LoadBackground("04_storage_inventory_background_plate.png");
            theme.combatBackgroundSprite = LoadBackground("03_security_terminal_background_plate.png");

            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static VisualProfileSO EnsureBunkerSurvivalProfile(UiThemeSO theme)
        {
            var defaultProfile = VisualProfilePresetFactory.EnsureDefaultProfile();
            var profile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(PresetProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VisualProfileSO>();
                profile.name = "BunkerSurvivalVisualProfile";
                AssetDatabase.CreateAsset(profile, PresetProfilePath);
            }

            profile.displayName = "Bunker Survival";
            profile.uiTheme = theme;
            profile.mainMenuAtmosphere = defaultProfile.mainMenuAtmosphere;
            profile.mainMenuLighting = defaultProfile.mainMenuLighting;
            profile.runAtmosphere = defaultProfile.runAtmosphere;
            profile.postProcessProfile = defaultProfile.postProcessProfile;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Sprite LoadNineSlice(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{NineSliceRoot}/{fileName}");

        private static Sprite LoadBackground(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BackgroundRoot}/{fileName}");

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
