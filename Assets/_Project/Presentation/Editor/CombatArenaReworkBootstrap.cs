#if UNITY_EDITOR
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatArenaReworkBootstrap
    {
        private const string ConfigPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";
        private const string ProfilePath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaAtmosphereProfile.asset";
        private const string PostFxPath =
            "Assets/_Project/Data/Resources/DeadManZone/CombatArenaVolumeProfile.asset";

        [MenuItem("DeadManZone/Combat Arena/Apply Combat Rework v2 — Grim Arena Shell")]
        public static void ApplyGrimArenaShell()
        {
            var profile = EnsureAtmosphereProfile();
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"Missing config at {ConfigPath}");
                return;
            }

            config.atmosphereProfile = profile;
            config.spawnPerimeterProps = false;
            config.useSyntySkybox = false;
            config.enableArenaFog = true;
            config.fogDensity = profile.fogDensity;
            config.groundPadding = 1.4f;
            config.useFlatTexturedGround = true;
            config.boardVerticalViewportCenter = 0.44f;

            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("Combat Rework v2 grim arena shell preset applied.");
        }

        private static CombatArenaAtmosphereProfileSO EnsureAtmosphereProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatArenaAtmosphereProfileSO>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CombatArenaAtmosphereProfileSO>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ApplyGrimDefaults();
            profile.backdropRings = CombatArenaBackdropRingBootstrap.EnsureDefaultRings();
            if (profile.enablePostProcessing)
                profile.postVolumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath);
            else
                profile.postVolumeProfile = null;
            return profile;
        }
    }
}
#endif
