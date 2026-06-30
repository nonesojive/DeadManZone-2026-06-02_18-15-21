using System.IO;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class ShopConfigEditorGenerator
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone/Shop";
        private const string ConfigPath = "Assets/_Project/Data/Resources/DeadManZone/ShopConfig.asset";

        [MenuItem("DeadManZone/Shop/Create Default Shop Profiles")]
        public static void CreateDefaultShopProfiles()
        {
            Directory.CreateDirectory(Root);

            var baseline = new ShopSlotProfileSO[ShopSlotLayoutResolver.BaselineSlotCount];
            for (int i = 0; i < baseline.Length; i++)
            {
                ShopSlotKind kind;
                ShopPoolBias bias;
                string[] preferredRoles;

                if (i < 3)
                {
                    kind = ShopSlotKind.BaselineOffensive;
                    bias = ShopPoolBias.Offensive;
                    preferredRoles = new[] { GameTagIds.Assault, GameTagIds.Sniper, GameTagIds.Tank };
                }
                else if (i < ShopSlotLayoutResolver.ReservedSlotStartIndex)
                {
                    kind = ShopSlotKind.BaselineDefensive;
                    bias = ShopPoolBias.Defensive;
                    preferredRoles = new[] { GameTagIds.Support, GameTagIds.Utility, GameTagIds.Defender, GameTagIds.Headquarters };
                }
                else
                {
                    kind = ShopSlotKind.ReservedAbility;
                    bias = ShopPoolBias.Defensive;
                    preferredRoles = System.Array.Empty<string>();
                }

                baseline[i] = CreateProfileAsset(
                    $"{Root}/slot_{i}.asset",
                    i,
                    kind,
                    bias,
                    preferredRoles);
            }

            var bonus = new ShopSlotProfileSO[ShopSlotLayoutResolver.BonusSlotCount];
            for (int i = 0; i < bonus.Length; i++)
            {
                int slotIndex = ShopSlotLayoutResolver.BaselineSlotCount + i;
                bool offensive = i % 2 == 0;
                bonus[i] = CreateProfileAsset(
                    $"{Root}/slot_{slotIndex}.asset",
                    slotIndex,
                    ShopSlotKind.Bonus,
                    offensive ? ShopPoolBias.Offensive : ShopPoolBias.Defensive,
                    offensive
                        ? new[] { GameTagIds.Assault, GameTagIds.Sniper, GameTagIds.Tank }
                        : new[] { GameTagIds.Support, GameTagIds.Utility, GameTagIds.Defender, GameTagIds.Headquarters });
            }

            var config = AssetDatabase.LoadAssetAtPath<ShopConfigSO>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ShopConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("baselineProfiles").arraySize = baseline.Length;
            for (int i = 0; i < baseline.Length; i++)
                serialized.FindProperty("baselineProfiles").GetArrayElementAtIndex(i).objectReferenceValue = baseline[i];

            serialized.FindProperty("bonusProfiles").arraySize = bonus.Length;
            for (int i = 0; i < bonus.Length; i++)
                serialized.FindProperty("bonusProfiles").GetArrayElementAtIndex(i).objectReferenceValue = bonus[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Shop config written to {ConfigPath} with {baseline.Length + bonus.Length} slot profiles.");
        }

        private static ShopSlotProfileSO CreateProfileAsset(
            string path,
            int slotIndex,
            ShopSlotKind kind,
            ShopPoolBias bias,
            string[] preferredRoles)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ShopSlotProfileSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ShopSlotProfileSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.slotIndex = slotIndex;
            asset.slotKind = kind;
            asset.poolBias = bias;
            asset.neutralPercent = 10;
            asset.factionPercent = 80;
            asset.salvagePercent = 10;
            asset.preferredRoleWeight = 2f;
            asset.preferredCombatRoles = preferredRoles;
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
