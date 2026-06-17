#if UNITY_EDITOR
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatArenaBackdropRingBootstrap
    {
        private const string RingsFolder = "Assets/_Project/Data/Resources/DeadManZone/BackdropRings";

        public static CombatArenaBackdropRingSO[] EnsureDefaultRings()
        {
            EnsureFolder(RingsFolder);

            return new[]
            {
                EnsureRing(
                    $"{RingsFolder}/TrenchDressingRing.asset",
                    CombatArenaBackdropRing.TrenchDressing,
                    "TrenchDressing",
                    CombatArenaBackdropCatalog.TrenchDressingPaths),
                EnsureRing(
                    $"{RingsFolder}/SkylineRing.asset",
                    CombatArenaBackdropRing.Skyline,
                    "SkylineBackdrop",
                    CombatArenaBackdropCatalog.SkylinePaths),
                EnsureRing(
                    $"{RingsFolder}/AtmosphereFxRing.asset",
                    CombatArenaBackdropRing.AtmosphereFx,
                    "AtmosphereFx",
                    CombatArenaBackdropCatalog.AtmosphereFxPaths)
            };
        }

        private static CombatArenaBackdropRingSO EnsureRing(
            string assetPath,
            CombatArenaBackdropRing ring,
            string childRootName,
            string[] prefabPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<CombatArenaBackdropRingSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.ring = ring;
            asset.enabled = ring != CombatArenaBackdropRing.AtmosphereFx;
            asset.childRootName = childRootName;
            asset.prefabPaths = prefabPaths;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            const string parent = "Assets/_Project/Data/Resources/DeadManZone";
            if (!AssetDatabase.IsValidFolder(parent))
                return;

            AssetDatabase.CreateFolder(parent, "BackdropRings");
        }
    }
}
#endif
