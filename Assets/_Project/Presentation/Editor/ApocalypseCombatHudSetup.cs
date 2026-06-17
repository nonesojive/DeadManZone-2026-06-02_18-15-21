#if UNITY_EDITOR
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class ApocalypseCombatHudSetup
    {
        private const string HudAssetsPath = "Assets/_Project/Data/Resources/DeadManZone/CombatHudAssets.asset";
        private const string AudioSetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaAudioSet.asset";

        [MenuItem("DeadManZone/Combat Arena/Pretty Combat Pass — Import Apocalypse HUD")]
        public static void ImportApocalypseCombatHud()
        {
            EnsureCombatHudAssets();
            EnsureCombatAudioSet();
            WireCombatBackgroundIntoTheme();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Pretty combat pass assets ready.\n" +
                $"- HUD assets: {HudAssetsPath}\n" +
                $"- Audio set: {AudioSetPath}\n" +
                "- SyntyTrenchUiTheme combat background wired.\n" +
                "Enter Play mode → start a combat to verify HUD_Apocalypse_HealthBar_02 bars.");
        }

        [MenuItem("DeadManZone/Combat Arena/Pretty Combat Pass — Upgrade Run Scene Health Bars")]
        public static void UpgradeRunSceneHealthBars()
        {
            EnsureCombatHudAssets();

            var combatPanel = GameObject.Find("CombatPanel");
            if (combatPanel == null)
            {
                Debug.LogError("CombatPanel not found in the open scene. Open Run.unity first.");
                return;
            }

            var presenter = CombatHealthBarUiFactory.CreateUnder(combatPanel.transform);
            var flow = combatPanel.GetComponent<CombatFlowPresenter>();
            if (flow != null)
            {
                var serialized = new SerializedObject(flow);
                serialized.FindProperty("healthBarPresenter").objectReferenceValue = presenter;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(combatPanel.scene);
            Debug.Log("Run scene combat HUD upgraded to Synty Apocalypse health bars.");
        }

        public static CombatHudAssetsSO EnsureCombatHudAssets()
        {
            var assets = AssetDatabase.LoadAssetAtPath<CombatHudAssetsSO>(HudAssetsPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<CombatHudAssetsSO>();
                AssetDatabase.CreateAsset(assets, HudAssetsPath);
            }

            assets.armyHealthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CombatApocalypseHudPaths.PlayerHealthBar02);
            assets.combatBackgroundSprite = LoadSpriteByPath(
                CombatApocalypseHudPaths.CombatBackgroundHazardStripes);

            EditorUtility.SetDirty(assets);
            return assets;
        }

        public static CombatArenaAudioSetSO EnsureCombatAudioSet()
        {
            var audioSet = AssetDatabase.LoadAssetAtPath<CombatArenaAudioSetSO>(AudioSetPath);
            if (audioSet == null)
            {
                audioSet = ScriptableObject.CreateInstance<CombatArenaAudioSetSO>();
                AssetDatabase.CreateAsset(audioSet, AudioSetPath);
            }

            audioSet.rifleShot = LoadClip(
                "Assets/PostApocalypseGunsDemo/AssaultRifles/AutoGun_3p_01.wav");
            audioSet.cannonShot = LoadClip(
                "Assets/PostApocalypseGunsDemo/Miniguns_loop/AssaultCanon_3p.wav");
            audioSet.bulletImpact = LoadClip(
                "Assets/PostApocalypseGunsDemo/Pistols/Zapper_3p_01.wav");
            audioSet.explosion = LoadClip(
                "Assets/PostApocalypseGunsDemo/Z-Extrem/HeavyLaserLauncher1.wav");
            audioSet.unitDeath = LoadClip(
                "Assets/PostApocalypseGunsDemo/Shotguns/JackHammer_3p_01.wav");

            EditorUtility.SetDirty(audioSet);
            return audioSet;
        }

        private static void WireCombatBackgroundIntoTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(SyntyUiKitSetup.ThemeAssetPath);
            if (theme == null)
                return;

            var hudAssets = EnsureCombatHudAssets();
            if (hudAssets.combatBackgroundSprite != null)
                theme.combatBackgroundSprite = hudAssets.combatBackgroundSprite;

            theme.combatOverlayColor = new Color(0.02f, 0.025f, 0.02f, 0.62f);
            theme.combatBannerColor = new Color(0.10f, 0.09f, 0.07f, 0.92f);
            EditorUtility.SetDirty(theme);
        }

        private static Sprite LoadSpriteByPath(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static AudioClip LoadClip(string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
                Debug.LogWarning($"Audio clip missing: {assetPath}");
            return clip;
        }
    }
}
#endif
