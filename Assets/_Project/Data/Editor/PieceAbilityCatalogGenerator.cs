using System.IO;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class PieceAbilityCatalogGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Resources/DeadManZone/Abilities";

        [InitializeOnLoadMethod]
        private static void EnsureCatalogAssetsExist()
        {
            if (AllAssetsExist())
                return;

            Generate();
        }

        [MenuItem(DeadManZoneEditorMenus.Content + "Generate Piece Ability Catalog")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);

            var defaults = BuildDefaultAbilities();
            for (int i = 0; i < defaults.Length; i++)
                WriteAbility(defaults[i]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Piece ability catalog written to {OutputFolder} ({defaults.Length} abilities).");
        }

        public static void GenerateFromCommandLine() => Generate();

        internal static AbilitySeed[] BuildDefaultAbilities() => new[]
        {
            new AbilitySeed(
                "adjacent_infantry_armor_plus_one",
                "Adjacent infantry gain +1 armor.",
                PieceAbilityTrigger.AdjacentAura,
                new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                SynergyStat.ArmorType,
                SynergyModType.Flat,
                1),
            new AbilitySeed(
                "adjacent_artillery_damage_plus_two",
                "Adjacent artillery gain +2 damage.",
                PieceAbilityTrigger.AdjacentAura,
                new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
                SynergyStat.Damage,
                SynergyModType.Flat,
                2),
            new AbilitySeed(
                "adjacent_stealth_tag_damage_plus_one",
                "Adjacent units with Stealth gain +1 damage.",
                PieceAbilityTrigger.AdjacentAura,
                new NeighborFilter { AbilityTagId = GameTagIds.Stealth },
                SynergyStat.Damage,
                SynergyModType.Flat,
                1),
            new AbilitySeed(
                "adjacent_allies_move_charge_plus_five",
                "Adjacent allies gain +5% move charge.",
                PieceAbilityTrigger.AdjacentAura,
                NeighborFilter.Any,
                SynergyStat.MoveChargePercent,
                SynergyModType.Flat,
                5)
        };

        private static void WriteAbility(AbilitySeed seed)
        {
            var path = $"{OutputFolder}/{seed.Id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDefinitionSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = seed.Id;
            so.FindProperty("cardDescription").stringValue = seed.CardDescription;
            so.FindProperty("trigger").enumValueIndex = (int)seed.Trigger;
            WriteNeighborFilter(so.FindProperty("neighborFilter"), seed.NeighborFilter);
            so.FindProperty("stat").enumValueIndex = (int)seed.Stat;
            so.FindProperty("modType").enumValueIndex = (int)seed.ModType;
            so.FindProperty("magnitude").intValue = seed.Magnitude;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteNeighborFilter(SerializedProperty filter, NeighborFilter value)
        {
            filter.FindPropertyRelative("PrimaryTagId").stringValue = value.PrimaryTagId ?? string.Empty;
            filter.FindPropertyRelative("CombatRoleTagId").stringValue = value.CombatRoleTagId ?? string.Empty;
            filter.FindPropertyRelative("SystemTagId").stringValue = value.SystemTagId ?? string.Empty;
            filter.FindPropertyRelative("SynergyTagId").stringValue = value.SynergyTagId ?? string.Empty;
            filter.FindPropertyRelative("AbilityTagId").stringValue = value.AbilityTagId ?? string.Empty;
        }

        private static bool AllAssetsExist()
        {
            var defaults = BuildDefaultAbilities();
            for (int i = 0; i < defaults.Length; i++)
            {
                var path = $"{OutputFolder}/{defaults[i].Id}.asset";
                if (AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(path) == null)
                    return false;
            }

            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        internal readonly struct AbilitySeed
        {
            public AbilitySeed(
                string id,
                string cardDescription,
                PieceAbilityTrigger trigger,
                NeighborFilter neighborFilter,
                SynergyStat stat,
                SynergyModType modType,
                int magnitude)
            {
                Id = id;
                CardDescription = cardDescription;
                Trigger = trigger;
                NeighborFilter = neighborFilter;
                Stat = stat;
                ModType = modType;
                Magnitude = magnitude;
            }

            public string Id { get; }
            public string CardDescription { get; }
            public PieceAbilityTrigger Trigger { get; }
            public NeighborFilter NeighborFilter { get; }
            public SynergyStat Stat { get; }
            public SynergyModType ModType { get; }
            public int Magnitude { get; }
        }
    }
}
