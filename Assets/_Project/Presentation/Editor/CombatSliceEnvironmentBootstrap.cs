#if UNITY_EDITOR
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatSliceEnvironmentBootstrap
    {
        private const string ConfigPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";

        [MenuItem("DeadManZone/Combat Arena/Apply Iron Vanguard Slice Environment")]
        public static void ApplySliceEnvironment()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"Missing config at {ConfigPath}");
                return;
            }

            config.spawnPerimeterProps = false;
            config.groundPadding = 1.4f;
            config.enableArenaFog = true;
            config.fogDensity = 0.038f;
            config.useSyntySkybox = false;
            config.useFlatTexturedGround = true;
            config.atmosphereProfile = AssetDatabase.LoadAssetAtPath<CombatArenaAtmosphereProfileSO>(
                "Assets/_Project/Data/Resources/DeadManZone/CombatArenaAtmosphereProfile.asset");

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Iron Vanguard slice environment preset applied to CombatArenaConfig.");
        }
    }
}
#endif
