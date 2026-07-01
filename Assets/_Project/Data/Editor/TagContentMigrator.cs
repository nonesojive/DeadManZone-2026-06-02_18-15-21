using System;
using System.Collections.Generic;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class TagContentMigrator
    {
        private const string PieceRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

        private static readonly Dictionary<string, PieceTagMapping> PieceMappings =
            new(StringComparer.Ordinal)
            {
                ["ironmarch_hq"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Headquarters, GameTagIds.Hq),
                ["rifle_squad"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["diesel_walker"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, GameTagIds.Combatant),
                ["radio_array"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, GameTagIds.NonCombatant),
                ["mg_team"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["field_gun_nest"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Artillery, GameTagIds.Combatant),
                ["supply_depot"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, GameTagIds.NonCombatant),
                ["field_workshop"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, GameTagIds.NonCombatant),
                ["mobile_artillery"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Artillery, GameTagIds.Combatant),
                ["ironmarch_heavy_tank"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, GameTagIds.Combatant),
                ["ironmarch_mortar"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Artillery, GameTagIds.Combatant),
                ["ironmarch_engineer"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Support, GameTagIds.Combatant),
                ["ironmarch_breacher"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),

                ["conscript_rifleman"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["grenade_thrower"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Artillery, GameTagIds.Combatant),
                ["field_medic"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Support, GameTagIds.Combatant,
                    synergyTags: new[] { GameTagIds.Medic }),
                ["armored_transport"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, GameTagIds.Combatant),
                ["mobile_cannon"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Artillery, GameTagIds.Combatant),
                ["neutral_supply_depot"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, GameTagIds.NonCombatant),
                ["neutral_field_gun"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Artillery, GameTagIds.Combatant),
                ["shock_trooper"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),

                ["dust_hq"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Headquarters, GameTagIds.Hq),
                ["sand_raider"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["scrap_rig"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, GameTagIds.Combatant),
                ["toxin_launcher"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Artillery, GameTagIds.Combatant),

                ["echo_hq"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Headquarters, GameTagIds.Hq),
                ["phantom_agent"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant),
                ["signal_relay"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, GameTagIds.NonCombatant),
                ["resonance_cannon"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Artillery, GameTagIds.Combatant),

                ["crimson_elite"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["crimson_tank"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, GameTagIds.Combatant),
                ["crimson_artillery"] = new PieceTagMapping(GameTagIds.Building, GameTagIds.Artillery, GameTagIds.Combatant),

                ["wraith_stalker"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant),
                ["wraith_phantom"] = new PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, GameTagIds.Combatant),
                ["wraith_bombard"] = new PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Artillery, GameTagIds.Combatant)
            };

        private static readonly HashSet<string> KnownLegacyTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "Infantry",
            "Vehicle",
            "Building",
            "Structure",
            "Combatant",
            "NonCombatant",
            "HQ",
            "Artillery",
            "Assault",
            "Tank",
            "Support",
            "Utility",
            "Headquarters",
            "Sniper"
        };

        public static void MigratePieceTags()
        {
            string[] guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PieceRoot });
            int updated = 0;
            int unchanged = 0;
            int warnings = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
                if (piece == null)
                    continue;

                warnings += LogUnknownLegacyTags(piece, path);

                if (!TryGetMapping(piece.id, out var mapping))
                {
                    warnings++;
                    Debug.LogWarning($"[TagContentMigrator] No mapping defined for piece id '{piece.id}' at '{path}'.");
                    unchanged++;
                    continue;
                }

                if (ApplyMapping(piece, mapping))
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
            {
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TagContentMigrator] Piece tag migration complete. Updated={updated}, Unchanged={unchanged}, Warnings={warnings}.");
        }

        internal static void ClearLegacySynergyTags()
        {
            string[] guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PieceRoot });
            int cleared = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
                if (piece == null || piece.synergyTags == null || piece.synergyTags.Length == 0)
                    continue;

                piece.synergyTags = Array.Empty<string>();
                EditorUtility.SetDirty(piece);
                cleared++;
            }

            if (cleared > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"[TagContentMigrator] Cleared synergy tags on {cleared} pieces.");
        }

        internal static bool TryGetMapping(string pieceId, out PieceTagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                mapping = default;
                return false;
            }

            return PieceMappings.TryGetValue(pieceId.Trim(), out mapping);
        }

        internal static PieceTagMapping GetMappingOrThrow(string pieceId)
        {
            if (TryGetMapping(pieceId, out var mapping))
                return mapping;

            throw new InvalidOperationException($"No piece tag mapping configured for '{pieceId}'.");
        }

        private static int LogUnknownLegacyTags(PieceDefinitionSO piece, string path)
        {
            if (piece.tags == null || piece.tags.Length == 0)
                return 0;

            int warnings = 0;
            for (int i = 0; i < piece.tags.Length; i++)
            {
                string legacyTag = piece.tags[i];
                if (string.IsNullOrWhiteSpace(legacyTag))
                    continue;

                string trimmed = legacyTag.Trim();
                if (KnownLegacyTags.Contains(trimmed) || TagRegistry.TryGet(trimmed, out _))
                    continue;

                warnings++;
                Debug.LogWarning($"[TagContentMigrator] Unknown legacy tag '{legacyTag}' on piece '{piece.id}' ({path}).");
            }

            return warnings;
        }

        private static bool ApplyMapping(PieceDefinitionSO piece, PieceTagMapping mapping)
        {
            bool changed = false;
            changed |= SetIfChanged(ref piece.primary, mapping.Primary);
            changed |= SetIfChanged(ref piece.combatRole, mapping.CombatRole);
            changed |= SetIfChanged(ref piece.systemTag, mapping.SystemTag);

            if (mapping.SynergyTags != null && mapping.SynergyTags.Length > 0)
            {
                if (!AreArraysEqual(piece.synergyTags, mapping.SynergyTags))
                {
                    piece.synergyTags = mapping.SynergyTags;
                    changed = true;
                }
            }
            else if (piece.synergyTags != null && piece.synergyTags.Length > 0)
            {
                piece.synergyTags = Array.Empty<string>();
                changed = true;
            }

            string[] rebuiltLegacyTags = PieceTagQueries.BuildLegacyTags(
                piece.category,
                piece.baseDamage,
                piece.primary,
                piece.combatRole,
                piece.systemTag,
                piece.synergyTags,
                piece.abilityTags ?? Array.Empty<string>(),
                piece.flavorTags ?? Array.Empty<string>());

            if (!AreArraysEqual(piece.tags, rebuiltLegacyTags))
            {
                piece.tags = rebuiltLegacyTags;
                changed = true;
            }

            return changed;
        }

        private static bool SetIfChanged(ref string field, string value)
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
                return false;

            field = value;
            return true;
        }

        private static bool AreArraysEqual(string[] left, string[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        internal readonly struct PieceTagMapping
        {
            public PieceTagMapping(
                string primary,
                string combatRole,
                string systemTag,
                string[] synergyTags = null)
            {
                Primary = primary;
                CombatRole = combatRole;
                SystemTag = systemTag;
                SynergyTags = synergyTags ?? Array.Empty<string>();
            }

            public string Primary { get; }
            public string CombatRole { get; }
            public string SystemTag { get; }
            public string[] SynergyTags { get; }
        }
    }
}
