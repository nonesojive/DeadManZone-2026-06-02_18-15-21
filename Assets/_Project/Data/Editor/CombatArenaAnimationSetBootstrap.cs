#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CombatArenaAnimationSetBootstrap
    {
        private const string AssetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset";

        private const string RifleShootFbx =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Rifle/HumanM@Rifle_Aim01_Shoot01.fbx";
        private const string GrenadeThrowFbx =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Grenade/HumanM@ThrowGrenade01_L.fbx";
        private const string Death01Fbx =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx";
        private const string Death02Fbx =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death02.fbx";
        private const string Death03Fbx =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death03.fbx";
        private const string SidekickDeathFbx =
            "Assets/Synty/AnimationSwordCombat/Animations/Sidekick/Death/A_MOD_SWD_Death_F_Neut.fbx";

        [MenuItem("DeadManZone/Combat Arena/Create Or Refresh Animation Set")]
        public static void CreateOrRefresh()
        {
            var animationSet = AssetDatabase.LoadAssetAtPath<CombatArenaAnimationSetSO>(AssetPath);
            if (animationSet == null)
            {
                animationSet = ScriptableObject.CreateInstance<CombatArenaAnimationSetSO>();
                AssetDatabase.CreateAsset(animationSet, AssetPath);
            }

            animationSet.rifleShoot = LoadClip(RifleShootFbx, "HumanM@Rifle_Aim01_Shoot01");
            animationSet.grenadeThrow = LoadClip(GrenadeThrowFbx, "HumanM@ThrowGrenade01_L");
            animationSet.death01 = LoadClip(Death01Fbx, "HumanM@Death01");
            animationSet.death02 = LoadClip(Death02Fbx, "HumanM@Death02");
            animationSet.death03 = LoadClip(Death03Fbx, "HumanM@Death03");
            animationSet.sidekickDeathForward = LoadClip(SidekickDeathFbx, "A_MOD_SWD_Death_F_Neut");

            EditorUtility.SetDirty(animationSet);
            AssetDatabase.SaveAssets();
            Debug.Log($"Combat arena animation set refreshed at {AssetPath}");
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning($"Animation FBX missing: {fbxPath}");
                return null;
            }

            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            Debug.LogWarning($"Clip '{clipName}' missing in {fbxPath}");
            return null;
        }
    }
}
#endif
