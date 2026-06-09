using System.Linq;
using DeadManZone.Core.Shop;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class UnitCreatorMigrationMenu
    {
        [MenuItem("DeadManZone/Migrate Shop Pool Flags From Demo List")]
        public static void MigrateShopPoolFlags()
        {
            var guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[]
            {
                "Assets/_Project/Data/Resources/DeadManZone/Pieces"
            });

            int updated = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
                if (piece == null)
                    continue;

                bool inDemo = ContentDatabase.DemoShopPieceIds.Contains(piece.id);
                if (piece.includeInShopPool == inDemo)
                    continue;

                piece.includeInShopPool = inDemo;
                EditorUtility.SetDirty(piece);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Migrated shop pool flags on {updated} piece assets.");
        }

        [MenuItem("DeadManZone/Migrate Shop Lanes From Combat Roles")]
        public static void MigrateShopLanes()
        {
            var guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[]
            {
                "Assets/_Project/Data/Resources/DeadManZone/Pieces"
            });

            int updated = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
                if (piece == null)
                    continue;

                var lane = ShopLaneResolver.ResolveLane(piece.combatRole);
                if (piece.shopLane == lane)
                    continue;

                piece.shopLane = lane;
                EditorUtility.SetDirty(piece);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Migrated shop lanes on {updated} piece assets.");
        }
    }
}
