using System;
using System.Collections.Generic;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class PieceAbilityContentMigrator
    {
        private const string PieceRoot = "Assets/_Project/Data";
        private const string AbilityRoot = "Assets/_Project/Data/Resources/DeadManZone/Abilities";

        private const string MedicAbilityPath = AbilityRoot + "/adjacent_infantry_armor_plus_one.asset";
        private const string InspiringAbilityPath = AbilityRoot + "/adjacent_allies_move_charge_plus_five.asset";
        private const string CommandAbilityPath = AbilityRoot + "/adjacent_artillery_damage_plus_two.asset";
        private const string EchoAbilityPath = AbilityRoot + "/adjacent_stealth_tag_damage_plus_one.asset";

        [MenuItem("DeadManZone/Migrate Piece Tag-Implied Abilities")]
        public static void MigratePieceTagImpliedAbilities()
        {
            var medicAbility = LoadAbilityOrWarn(MedicAbilityPath, "adjacent_infantry_armor_plus_one");
            var inspiringAbility = LoadAbilityOrWarn(InspiringAbilityPath, "adjacent_allies_move_charge_plus_five");
            var commandAbility = LoadAbilityOrWarn(CommandAbilityPath, "adjacent_artillery_damage_plus_two");
            var echoAbility = LoadAbilityOrWarn(EchoAbilityPath, "adjacent_stealth_tag_damage_plus_one");

            string[] guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PieceRoot });
            int updated = 0;
            int unchanged = 0;

            foreach (string guid in guids)
            {
                string piecePath = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(piecePath);
                if (piece == null)
                    continue;

                if (ApplyMappedAbilities(piece, medicAbility, inspiringAbility, commandAbility, echoAbility))
                {
                    updated++;
                    EditorUtility.SetDirty(piece);
                }
                else
                {
                    unchanged++;
                }
            }

            if (updated > 0)
                AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            Debug.Log($"[PieceAbilityContentMigrator] Piece ability migration complete. Updated={updated}, Unchanged={unchanged}.");
        }

        private static bool ApplyMappedAbilities(
            PieceDefinitionSO piece,
            AbilityDefinitionSO medicAbility,
            AbilityDefinitionSO inspiringAbility,
            AbilityDefinitionSO commandAbility,
            AbilityDefinitionSO echoAbility)
        {
            var abilityList = new List<AbilityDefinitionSO>(piece.catalogAbilities ?? Array.Empty<AbilityDefinitionSO>());
            bool changed = false;

            changed |= TryAddTagMappedAbility(piece, abilityList, GameTagIds.Medic, medicAbility);
            changed |= TryAddTagMappedAbility(piece, abilityList, GameTagIds.Inspiring, inspiringAbility);
            changed |= TryAddTagMappedAbility(piece, abilityList, GameTagIds.Command, commandAbility);
            changed |= TryAddTagMappedAbility(piece, abilityList, GameTagIds.Echo, echoAbility);

            if (!changed)
                return false;

            piece.catalogAbilities = abilityList.ToArray();
            return true;
        }

        private static bool TryAddTagMappedAbility(
            PieceDefinitionSO piece,
            List<AbilityDefinitionSO> abilityList,
            string tagId,
            AbilityDefinitionSO mappedAbility)
        {
            if (mappedAbility == null)
                return false;
            if (!HasTag(piece.synergyTags, tagId))
                return false;
            if (ContainsAbility(abilityList, mappedAbility))
                return false;

            abilityList.Add(mappedAbility);
            return true;
        }

        private static bool HasTag(string[] tags, string tagId)
        {
            if (tags == null || tags.Length == 0 || string.IsNullOrWhiteSpace(tagId))
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], tagId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool ContainsAbility(List<AbilityDefinitionSO> abilities, AbilityDefinitionSO target)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                var candidate = abilities[i];
                if (candidate == null)
                    continue;

                if (candidate == target)
                    return true;

                if (string.Equals(candidate.id, target.id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static AbilityDefinitionSO LoadAbilityOrWarn(string assetPath, string resourceName)
        {
            var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(assetPath);
            if (ability != null)
                return ability;

            ability = Resources.Load<AbilityDefinitionSO>($"DeadManZone/Abilities/{resourceName}");
            if (ability != null)
                return ability;

            Debug.LogWarning($"[PieceAbilityContentMigrator] Missing ability asset '{assetPath}' (resource '{resourceName}').");
            return null;
        }
    }
}
