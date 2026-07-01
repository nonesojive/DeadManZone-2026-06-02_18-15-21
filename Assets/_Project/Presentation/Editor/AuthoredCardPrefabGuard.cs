using DeadManZone.Presentation.UI;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>Blocks programmatic overwrites of manually authored card prefab assets. Runtime instance mutation during Play is allowed.</summary>
    public static class AuthoredCardPrefabGuard
    {
        public static bool IsProtectedPath(string assetPath) =>
            assetPath == CardPrefabPaths.UnitDetailCard
            || assetPath == CardPrefabPaths.BuildingPrefab
            || assetPath == CardPrefabPaths.ShopOfferCard;

        public static bool TrySavePrefab(GameObject root, string assetPath, out bool success)
        {
            success = false;
            if (IsProtectedPath(assetPath))
            {
                Debug.LogError(
                    $"{assetPath} is manually authored. Edit the prefab in the Project window — bake menus cannot overwrite it.");
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, assetPath, out success);
            return true;
        }
    }
}
