#if UNITY_EDITOR
using DeadManZone.Data;
using DeadManZone.Data.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatArena2DSceneBootstrap
    {
        private const string ConfigPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";
        private const string ScenePath = "Assets/_Project/Scenes/CombatArena2D.unity";

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Wire All 2D Combat Art", priority = 0)]
        public static void WireAll2DCombatArt()
        {
            EnableTopTroops2DMode();
        }

        public static void EnableTopTroops2DMode()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"Missing config at {ConfigPath}");
                return;
            }

            config.visualMode = CombatArenaVisualMode.TopTroops2D;
            config.useTopTroopsProceduralBattlefield = false;
            config.useTopTroopsBrightSky = true;
            config.topTroopsSkyColor = new Color(0.45f, 0.72f, 0.95f);
            config.projectileArcHeight = 0.65f;
            config.orthoCameraElevationDegrees = 52f;
            config.orthoCameraAzimuthDegrees = 270f;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Wire2DEnvironmentArt();
            Wire2DSilhouetteArt();
            Wire2DVfxArt();
            Wire2DBuildingArt();
            EnsureSceneInBuildSettings();
            Debug.Log("Top Troops 2D mode enabled on CombatArenaConfig. Combat fights load CombatArena2D scene.");
        }

        public static void Wire2DEnvironmentArt()
        {
            const string artFolder = "Assets/_Project/Art/Combat2D/Environment";
            const string assetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArena2DEnvironmentArt.asset";

            var art = AssetDatabase.LoadAssetAtPath<CombatArena2DEnvironmentArtSO>(assetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CombatArena2DEnvironmentArtSO>();
                AssetDatabase.CreateAsset(art, assetPath);
            }

            art.gridCellLight = LoadEnvSprite(artFolder, "combat2d_grid_cell_light.png");
            art.gridCellDark = LoadEnvSprite(artFolder, "combat2d_grid_cell_dark.png");
            art.gridBackdrop = LoadEnvSprite(artFolder, "combat2d_grid_backdrop.png");
            art.skyGradient = LoadEnvSprite(artFolder, "combat2d_sky_gradient.png");
            art.shadowUnit = LoadEnvSprite(artFolder, "combat2d_shadow_unit.png");
            art.shadowBuilding = LoadEnvSprite(artFolder, "combat2d_shadow_building.png");

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Wired 2D environment sprites into {assetPath}");
        }

        public static void Wire2DSilhouetteArt()
        {
            const string artFolder = "Assets/_Project/Art/Combat2D/Units/Silhouettes";
            const string assetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArena2DSilhouetteArt.asset";

            var art = AssetDatabase.LoadAssetAtPath<CombatArena2DSilhouetteArtSO>(assetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CombatArena2DSilhouetteArtSO>();
                AssetDatabase.CreateAsset(art, assetPath);
            }

            art.assault = LoadEnvSprite(artFolder, "combat2d_silhouette_assault.png");
            art.ranged = LoadEnvSprite(artFolder, "combat2d_silhouette_ranged.png");
            art.artillery = LoadEnvSprite(artFolder, "combat2d_silhouette_artillery.png");
            art.vehicle = LoadEnvSprite(artFolder, "combat2d_silhouette_vehicle.png");
            art.generic = LoadEnvSprite(artFolder, "combat2d_silhouette_generic.png");

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Wired 2D silhouette sprites into {assetPath}");
        }

        public static void Wire2DVfxArt()
        {
            const string artFolder = "Assets/_Project/Art/Combat2D/VFX";
            const string assetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArena2DVfxArt.asset";

            var art = AssetDatabase.LoadAssetAtPath<CombatArena2DVfxArtSO>(assetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CombatArena2DVfxArtSO>();
                AssetDatabase.CreateAsset(art, assetPath);
            }

            art.rifleImpactStrip = LoadEnvSprite(artFolder, "combat2d_vfx_impact_rifle.png");
            art.explosionSmallStrip = LoadEnvSprite(artFolder, "combat2d_vfx_explosion_small.png");
            art.deathPuffStrip = LoadEnvSprite(artFolder, "combat2d_vfx_death_puff.png");

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Wired 2D VFX strips into {assetPath}");
        }

        public static void Wire2DBuildingArt()
        {
            const string artFolder = "Assets/_Project/Art/Combat2D/Buildings";
            const string piecesFolder = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

            (string pieceId, string spriteFile)[] assignments =
            {
                ("supply_depot", "combat2d_building_supply_depot.png"),
                ("neutral_supply_depot", "combat2d_building_supply_depot.png"),
                ("field_gun_nest", "combat2d_building_field_gun.png"),
                ("neutral_field_gun", "combat2d_building_field_gun.png"),
                ("command_bunker", "combat2d_building_command_bunker.png"),
                ("crimson_artillery", "combat2d_building_crimson_battery.png"),
                ("field_workshop", "combat2d_building_workshop.png"),
                ("radio_array", "combat2d_building_radio_array.png"),
                ("signal_relay", "combat2d_building_signal_relay.png")
            };

            int wired = 0;
            foreach (var (pieceId, spriteFile) in assignments)
            {
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{piecesFolder}/{pieceId}.asset");
                var sprite = LoadEnvSprite(artFolder, spriteFile);
                if (piece == null || sprite == null)
                    continue;

                piece.combatArenaSprite = sprite;
                EditorUtility.SetDirty(piece);
                wired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Wired combatArenaSprite on {wired} building pieces from {artFolder}");
        }

        private static Sprite LoadEnvSprite(string folder, string fileName)
        {
            string path = $"{folder}/{fileName}";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Add CombatArena2D To Build Settings")]
        public static void EnsureSceneInBuildSettings()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogWarning($"Scene missing at {ScenePath}. Copy CombatArena.unity first.");
                return;
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var entry in scenes)
            {
                if (entry.path == ScenePath)
                    return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("CombatArena2D added to Build Settings.");
        }
    }
}
#endif
