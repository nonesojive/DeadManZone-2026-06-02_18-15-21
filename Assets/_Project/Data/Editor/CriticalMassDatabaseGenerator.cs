using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CriticalMassDatabaseGenerator
    {
        private const string AssetPath = "Assets/_Project/Data/Resources/DeadManZone/CriticalMassDatabase.asset";

        [InitializeOnLoadMethod]
        private static void EnsureDatabaseAssetExists()
        {
            if (AssetDatabase.LoadAssetAtPath<CriticalMassDatabaseSO>(AssetPath) != null)
                return;

            WriteDatabase(BuildDefaultRules());
        }

        [MenuItem("DeadManZone/Generate Critical Mass Database")]
        public static void Generate() => WriteDatabase(BuildDefaultRules());

        public static void GenerateFromCommandLine() => Generate();

        internal static CriticalMassRuleEntry[] BuildDefaultRules()
        {
            var definitions = CriticalMassDefaultRules.Build();
            var entries = new CriticalMassRuleEntry[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
                entries[i] = FromDefinition(definitions[i]);

            return entries;
        }

        internal static void WriteDatabase(CriticalMassRuleEntry[] defaults)
        {
            var asset = AssetDatabase.LoadAssetAtPath<CriticalMassDatabaseSO>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CriticalMassDatabaseSO>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var so = new SerializedObject(asset);
            var rulesProp = so.FindProperty("rules");
            rulesProp.arraySize = defaults.Length;
            for (int i = 0; i < defaults.Length; i++)
                WriteRule(rulesProp.GetArrayElementAtIndex(i), defaults[i]);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            asset.RegisterWithCatalog();
            Debug.Log($"Critical Mass Database written to {AssetPath} ({defaults.Length} rules).");
        }

        private static CriticalMassRuleEntry FromDefinition(CriticalMassRuleDefinition definition)
        {
            var tiers = new CriticalMassTierEntry[definition.Tiers?.Length ?? 0];
            for (int i = 0; i < tiers.Length; i++)
            {
                tiers[i] = new CriticalMassTierEntry
                {
                    threshold = definition.Tiers[i].Threshold,
                    magnitude = definition.Tiers[i].Magnitude
                };
            }

            var target = definition.Target;
            return new CriticalMassRuleEntry
            {
                id = definition.Id,
                countTagId = definition.CountTagId,
                countCategory = definition.CountCategory,
                tiers = tiers,
                stat = definition.Stat,
                modType = definition.ModType,
                scope = definition.Scope,
                target = new CriticalMassTargetEntry
                {
                    primaryTagIds = target.PrimaryTagIds ?? System.Array.Empty<string>(),
                    combatRoleTagId = target.CombatRoleTagId,
                    synergyTagId = target.SynergyTagId,
                    abilityTagId = target.AbilityTagId,
                    flavorTagId = target.FlavorTagId,
                    useAttackType = target.AttackType.HasValue,
                    attackType = target.AttackType ?? AttackType.None,
                    useAttackRange = target.AttackRange.HasValue,
                    attackRange = target.AttackRange ?? AttackRangeTier.Medium,
                    factionId = target.FactionId
                }
            };
        }

        private static void WriteRule(SerializedProperty element, CriticalMassRuleEntry entry)
        {
            element.FindPropertyRelative("id").stringValue = entry.id;
            element.FindPropertyRelative("countTagId").stringValue = entry.countTagId;
            element.FindPropertyRelative("countCategory").enumValueIndex = (int)entry.countCategory;
            element.FindPropertyRelative("stat").enumValueIndex = (int)entry.stat;
            element.FindPropertyRelative("modType").enumValueIndex = (int)entry.modType;
            element.FindPropertyRelative("scope").enumValueIndex = (int)entry.scope;

            var tiers = element.FindPropertyRelative("tiers");
            tiers.arraySize = entry.tiers.Length;
            for (int i = 0; i < entry.tiers.Length; i++)
            {
                var tier = tiers.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("threshold").intValue = entry.tiers[i].threshold;
                tier.FindPropertyRelative("magnitude").intValue = entry.tiers[i].magnitude;
            }

            WriteTarget(element.FindPropertyRelative("target"), entry.target);
        }

        private static void WriteTarget(SerializedProperty target, CriticalMassTargetEntry entry)
        {
            var primary = target.FindPropertyRelative("primaryTagIds");
            primary.arraySize = entry.primaryTagIds?.Length ?? 0;
            for (int i = 0; i < primary.arraySize; i++)
                primary.GetArrayElementAtIndex(i).stringValue = entry.primaryTagIds[i];

            target.FindPropertyRelative("combatRoleTagId").stringValue = entry.combatRoleTagId ?? string.Empty;
            target.FindPropertyRelative("synergyTagId").stringValue = entry.synergyTagId ?? string.Empty;
            target.FindPropertyRelative("abilityTagId").stringValue = entry.abilityTagId ?? string.Empty;
            target.FindPropertyRelative("flavorTagId").stringValue = entry.flavorTagId ?? string.Empty;
            target.FindPropertyRelative("useAttackType").boolValue = entry.useAttackType;
            target.FindPropertyRelative("attackType").enumValueIndex = (int)entry.attackType;
            target.FindPropertyRelative("useAttackRange").boolValue = entry.useAttackRange;
            target.FindPropertyRelative("attackRange").enumValueIndex = (int)entry.attackRange;
            target.FindPropertyRelative("factionId").stringValue = entry.factionId ?? string.Empty;
        }
    }
}
