using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class PieceTagQueries
    {
        public sealed class PlayerVisibleTagsResult
        {
            public IReadOnlyList<TagDefinition> IdentityTags { get; init; } = Array.Empty<TagDefinition>();
            public IReadOnlyList<TagDefinition> OptionalTags { get; init; } = Array.Empty<TagDefinition>();
            public IReadOnlyList<TagDefinition> VisibleTags { get; init; } = Array.Empty<TagDefinition>();
            public int OverflowCount { get; init; }
        }

        public static bool HasTag(PieceDefinition piece, string tagId)
        {
            string targetKey = NormalizeTagKey(tagId);
            if (piece == null || targetKey.Length == 0)
                return false;

            return MatchesTag(piece.Primary, targetKey)
                || MatchesTag(piece.CombatRole, targetKey)
                || MatchesTag(piece.SystemTag, targetKey)
                || ContainsTag(piece.SynergyTags, targetKey)
                || ContainsTag(piece.AbilityTags, targetKey)
                || ContainsTag(piece.Tags, targetKey);
        }

        public static IReadOnlyList<string> GetAllTagIds(PieceDefinition piece)
        {
            if (piece == null)
                return Array.Empty<string>();

            var tags = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddRawTag(tags, seen, piece.Primary);
            AddRawTag(tags, seen, piece.CombatRole);
            AddRawTag(tags, seen, piece.SystemTag);
            AddRawTags(tags, seen, piece.SynergyTags);
            AddRawTags(tags, seen, piece.AbilityTags);
            AddRawTags(tags, seen, piece.Tags);

            return tags;
        }

        public static string[] BuildLegacyTags(
            PieceCategory category,
            int baseDamage,
            string primary,
            string combatRole,
            string systemTag,
            IReadOnlyList<string> synergyTags,
            IReadOnlyList<string> abilityTags,
            IReadOnlyList<string> legacyTags = null)
        {
            var tags = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddLegacyTags(tags, seen, legacyTags);
            AddLegacyTag(tags, seen, primary);
            AddLegacyTag(tags, seen, combatRole);
            AddLegacyTag(tags, seen, systemTag);
            AddLegacyTags(tags, seen, synergyTags);
            AddLegacyTags(tags, seen, abilityTags);

            if (string.IsNullOrWhiteSpace(systemTag) && ShouldAutoAddCombatant(category, baseDamage))
            {
                AddLegacyTag(tags, seen, GameTagIds.Combatant);
            }

            return tags.ToArray();
        }

        public static PlayerVisibleTagsResult GetPlayerVisibleTags(PieceDefinition piece, int maxOptionalChips)
        {
            if (piece == null)
                return new PlayerVisibleTagsResult();

            maxOptionalChips = Math.Max(0, maxOptionalChips);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var identityTags = new List<TagDefinition>();
            AddVisibleTag(identityTags, seen, piece.Primary, TagCategory.Primary, 100, "Primary identity tag.");
            AddVisibleTag(identityTags, seen, piece.CombatRole, TagCategory.CombatRole, 80, "Combat role identity tag.");
            AddFactionTag(identityTags, seen, piece.FactionId);

            var abilityTags = new List<TagDefinition>();
            var synergyTags = new List<TagDefinition>();

            AddOptionalTags(abilityTags, seen, piece.AbilityTags, TagCategory.Synergy, 45, "Ability tag.");
            AddOptionalTags(synergyTags, seen, piece.SynergyTags, TagCategory.Synergy, 35, "Synergy tag.");

            // During migration, legacy flat tags may still be the only populated source.
            AddOptionalTags(synergyTags, seen, piece.Tags, TagCategory.Synergy, 20, "Legacy tag.");

            SortByPriority(abilityTags);
            SortByPriority(synergyTags);

            var orderedOptional = new List<TagDefinition>(abilityTags.Count + synergyTags.Count);
            orderedOptional.AddRange(abilityTags);
            orderedOptional.AddRange(synergyTags);

            int overflowCount = Math.Max(0, orderedOptional.Count - maxOptionalChips);
            int takeCount = Math.Min(maxOptionalChips, orderedOptional.Count);
            var optionalTags = new List<TagDefinition>(takeCount);
            for (int i = 0; i < takeCount; i++)
            {
                optionalTags.Add(orderedOptional[i]);
            }

            var visibleTags = new List<TagDefinition>(identityTags.Count + optionalTags.Count);
            visibleTags.AddRange(identityTags);
            visibleTags.AddRange(optionalTags);

            return new PlayerVisibleTagsResult
            {
                IdentityTags = identityTags,
                OptionalTags = optionalTags,
                VisibleTags = visibleTags,
                OverflowCount = overflowCount
            };
        }

        private static void AddFactionTag(List<TagDefinition> destination, HashSet<string> seen, string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
                return;

            string trimmedId = factionId.Trim();
            if (!seen.Add(trimmedId))
                return;

            if (TryCreateVisibleTag(trimmedId, TagCategory.Primary, 65, "Faction identity tag.", out var tag))
            {
                destination.Add(tag);
            }
        }

        private static void AddVisibleTag(
            List<TagDefinition> destination,
            HashSet<string> seen,
            string tagId,
            TagCategory fallbackCategory,
            int fallbackPriority,
            string fallbackTooltip)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                return;

            string trimmedId = tagId.Trim();
            if (!seen.Add(trimmedId))
                return;

            if (TryCreateVisibleTag(trimmedId, fallbackCategory, fallbackPriority, fallbackTooltip, out var tag))
            {
                destination.Add(tag);
            }
        }

        private static void AddOptionalTags(
            List<TagDefinition> destination,
            HashSet<string> seen,
            IReadOnlyList<string> source,
            TagCategory fallbackCategory,
            int fallbackPriority,
            string fallbackTooltip)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                string tagId = source[i];
                if (string.IsNullOrWhiteSpace(tagId))
                    continue;

                string trimmedId = tagId.Trim();
                if (!seen.Add(trimmedId))
                    continue;

                if (TryCreateVisibleTag(trimmedId, fallbackCategory, fallbackPriority, fallbackTooltip, out var tag))
                {
                    destination.Add(tag);
                }
            }
        }

        private static bool TryCreateVisibleTag(
            string tagId,
            TagCategory fallbackCategory,
            int fallbackPriority,
            string fallbackTooltip,
            out TagDefinition tag)
        {
            if (TagRegistry.TryGet(tagId, out var registryTag))
            {
                if (!registryTag.PlayerVisible || registryTag.Category == TagCategory.System)
                {
                    tag = null;
                    return false;
                }

                tag = registryTag;
                return true;
            }

            tag = new TagDefinition
            {
                Id = tagId,
                DisplayName = HumanizeTag(tagId),
                Category = fallbackCategory,
                Tooltip = fallbackTooltip,
                DisplayPriority = fallbackPriority,
                PlayerVisible = true
            };

            return true;
        }

        private static void SortByPriority(List<TagDefinition> tags)
        {
            tags.Sort((left, right) =>
            {
                int byPriority = right.DisplayPriority.CompareTo(left.DisplayPriority);
                if (byPriority != 0)
                    return byPriority;

                return StringComparer.OrdinalIgnoreCase.Compare(left.DisplayName, right.DisplayName);
            });
        }

        private static bool ContainsTag(IReadOnlyList<string> tags, string targetKey)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (MatchesTag(tags[i], targetKey))
                    return true;
            }

            return false;
        }

        private static bool MatchesTag(string candidate, string targetKey)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            return NormalizeTagKey(candidate) == targetKey;
        }

        private static void AddRawTag(List<string> tags, HashSet<string> seen, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                tags.Add(trimmed);
            }
        }

        private static void AddRawTags(List<string> tags, HashSet<string> seen, IReadOnlyList<string> values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                AddRawTag(tags, seen, values[i]);
            }
        }

        private static void AddLegacyTags(List<string> tags, HashSet<string> seen, IReadOnlyList<string> values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                AddLegacyTag(tags, seen, values[i]);
            }
        }

        private static void AddLegacyTag(List<string> tags, HashSet<string> seen, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string legacyValue = ToLegacyDisplayTag(value.Trim());
            if (seen.Add(legacyValue))
            {
                tags.Add(legacyValue);
            }
        }

        private static string ToLegacyDisplayTag(string tagId)
        {
            if (TagRegistry.TryGet(tagId, out var registryTag))
                return registryTag.DisplayName;

            return HumanizeTag(tagId);
        }

        private static bool ShouldAutoAddCombatant(PieceCategory category, int baseDamage)
        {
            return category is PieceCategory.Unit or PieceCategory.Hybrid
                || (category == PieceCategory.Building && baseDamage > 0);
        }

        private static string HumanizeTag(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim();
            string[] parts = trimmed.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return trimmed;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                    continue;

                parts[i] = char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part.Substring(1).ToLowerInvariant() : string.Empty);
            }

            return string.Join(" ", parts);
        }

        private static string NormalizeTagKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim();
            var normalized = new char[trimmed.Length];
            int count = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsLetterOrDigit(c))
                {
                    normalized[count++] = char.ToLowerInvariant(c);
                }
            }

            return count == 0 ? string.Empty : new string(normalized, 0, count);
        }
    }
}
