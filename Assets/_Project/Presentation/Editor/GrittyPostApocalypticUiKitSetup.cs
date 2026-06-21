using System.Collections.Generic;
using System.IO;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Nexa Visuals — Gritty Post-Apocalyptic Survival UI (itch.io premium pack).
    /// Raw 4K screens live under Sprites/; sliced components under Components/, Icons/, Screens/.
    /// </summary>
    public static class GrittyPostApocalypticUiKitSetup
    {
        public const string KitRoot = "Assets/_Project/Art/UI/GrittyPostApocalyptic";
        public const string SpritesRoot = KitRoot + "/Sprites";
        public const string ComponentsRoot = KitRoot + "/Components";
        public const string IconsRoot = KitRoot + "/Icons";
        public const string ScreensRoot = KitRoot + "/Screens";
        public const string ThemeAssetPath = "Assets/_Project/Data/Visual/Presets/GrittyPostApocalypticUiTheme.asset";
        public const string PresetProfilePath = "Assets/_Project/Data/Visual/Presets/GrittyPostApocalypticVisualProfile.asset";

        private static readonly Dictionary<string, Vector4> SliceBorders = new()
        {
            ["btn_normal"] = new Vector4(48, 32, 48, 32),
            ["btn_accent"] = new Vector4(48, 32, 48, 32),
            ["btn_secondary"] = new Vector4(48, 32, 48, 32),
            ["btn_tab"] = new Vector4(48, 32, 48, 32),
            ["panel_wide"] = new Vector4(120, 96, 120, 96),
            ["panel_square"] = new Vector4(96, 96, 96, 96),
            ["card_frame_01"] = new Vector4(96, 80, 96, 80),
            ["modal_frame_01"] = new Vector4(128, 104, 128, 104),
            ["sidebar_panel"] = new Vector4(64, 96, 64, 96),
            ["slot_empty"] = new Vector4(64, 64, 64, 64),
        };

        [MenuItem("DeadManZone/UI Kit/Gritty Post-Apocalyptic/Slice Sheets (Python)")]
        public static void SliceSheetsFromPython()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var scriptPath = Path.Combine(projectRoot, "Tools", "slice_gritty_ui_pack.py");
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"Slicer not found: {scriptPath}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Debug.LogError("Failed to start Python slicer.");
                return;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(stdout))
                Debug.Log(stdout.TrimEnd());
            if (!string.IsNullOrEmpty(stderr))
                Debug.LogWarning(stderr.TrimEnd());

            if (process.ExitCode != 0)
            {
                Debug.LogError($"Gritty UI slicer failed with exit code {process.ExitCode}.");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log("Gritty Post-Apocalyptic UI sheets sliced. Run Configure All Sprites next.");
        }

        [MenuItem("DeadManZone/UI Kit/Gritty Post-Apocalyptic/Configure All Sprites")]
        public static void ConfigureAllSprites()
        {
            ConfigureFolder(SpritesRoot, applyNineSlice: false);
            ConfigureFolder(ComponentsRoot, applyNineSlice: true);
            ConfigureFolder(IconsRoot, applyNineSlice: false);
            ConfigureFolder(ScreensRoot, applyNineSlice: false);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured Gritty Post-Apocalyptic sprites (raw + sliced).");
        }

        [MenuItem("DeadManZone/UI Kit/Gritty Post-Apocalyptic/Import Theme")]
        public static void ImportTheme()
        {
            if (!AssetDatabase.IsValidFolder(ComponentsRoot))
            {
                Debug.LogError(
                    $"Sliced components missing at {ComponentsRoot}. " +
                    "Run Slice Sheets first, or copy assets/ from the itch.io pack into Sprites/.");
                return;
            }

            ConfigureAllSprites();
            var theme = EnsureTheme();
            EnsureProfile(theme);
            VisualProfilePresetFactory.EnsureDefaultProfile();

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Gritty Post-Apocalyptic UI theme ready.\n" +
                $"- Theme: {ThemeAssetPath}\n" +
                $"- Preset profile: {PresetProfilePath}");
        }

        [MenuItem("DeadManZone/UI Kit/Gritty Post-Apocalyptic/Apply Theme To Active Profile")]
        public static void ApplyThemeToActiveProfile()
        {
            var theme = EnsureTheme();
            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath)
                ?? VisualProfilePresetFactory.EnsureDefaultProfile();

            runtimeProfile.displayName = "Gritty Post-Apocalyptic";
            runtimeProfile.uiTheme = theme;
            EditorUtility.SetDirty(runtimeProfile);

            UiThemeProvider.InvalidateCache();
            VisualProfileProvider.InvalidateCache();
            UiThemeSceneRefresher.RefreshOpenScene(runtimeProfile);

            AssetDatabase.SaveAssets();
            Debug.Log("Active visual profile now uses Gritty Post-Apocalyptic UI.");
        }

        public static UiThemeSO EnsureTheme()
        {
            EnsureFolder("Assets/_Project/Data/Visual/Presets");

            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(ThemeAssetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeSO>();
                theme.name = "GrittyPostApocalypticUiTheme";
                AssetDatabase.CreateAsset(theme, ThemeAssetPath);
            }

            theme.ApplyGrittyPostApocalypticDefaults();
            theme.panelSprite = LoadComponent("panel_wide");
            theme.cardSprite = LoadComponent("card_frame_01");
            theme.modalFrameSprite = LoadComponent("modal_frame_01");
            theme.sidebarPanelSprite = LoadComponent("sidebar_panel");
            theme.inventoryPanelSprite = LoadComponent("panel_square");
            theme.bannerSprite = LoadComponent("panel_square_list");
            theme.buttonNormalSprite = LoadComponent("btn_normal");
            theme.buttonHighlightedSprite = LoadComponent("btn_normal_alt");
            theme.buttonPressedSprite = LoadComponent("btn_tab");
            theme.buttonDisabledSprite = LoadComponent("btn_secondary");
            theme.accentButtonSprite = LoadComponent("btn_accent");
            theme.secondaryButtonSprite = LoadComponent("btn_secondary");
            theme.dangerButtonSprite = LoadComponent("btn_secondary_alt");
            theme.slotEmptySprite = LoadComponent("slot_empty");
            theme.slotSelectedSprite = LoadComponent("slot_progress_50");
            theme.storageSlotEmptySprite = LoadComponent("slot_empty_02");
            theme.storageSlotSelectedSprite = LoadComponent("slot_progress_35");
            theme.menuBackgroundSprite = LoadScreen("screen_main_menu_title");
            theme.runBackgroundSprite = LoadScreen("screen_inventory_grid");
            theme.combatBackgroundSprite = LoadScreen("hud_status_bars");
            theme.shopBackgroundSprite = LoadScreen("screen_crafting_bench");

            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static VisualProfileSO EnsureProfile(UiThemeSO theme)
        {
            var defaultProfile = VisualProfilePresetFactory.EnsureDefaultProfile();
            var profile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(PresetProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VisualProfileSO>();
                profile.name = "GrittyPostApocalypticVisualProfile";
                AssetDatabase.CreateAsset(profile, PresetProfilePath);
            }

            profile.displayName = "Gritty Post-Apocalyptic";
            profile.uiTheme = theme;
            profile.mainMenuAtmosphere = defaultProfile.mainMenuAtmosphere;
            profile.mainMenuLighting = defaultProfile.mainMenuLighting;
            profile.runAtmosphere = defaultProfile.runAtmosphere;
            profile.postProcessProfile = defaultProfile.postProcessProfile;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureFolder(string folder, bool applyNineSlice)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;

                if (applyNineSlice)
                {
                    var stem = Path.GetFileNameWithoutExtension(path);
                    if (SliceBorders.TryGetValue(stem, out var border))
                        importer.spriteBorder = border;
                }

                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadComponent(string stem) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ComponentsRoot}/{stem}.png");

        private static Sprite LoadScreen(string stem) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ScreensRoot}/{stem}.png");

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
