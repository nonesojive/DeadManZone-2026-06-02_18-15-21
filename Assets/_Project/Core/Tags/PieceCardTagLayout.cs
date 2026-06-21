using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public static class PieceCardTagLayout
    {
        public static bool IsDedicatedSlotCategory(TagCategory category) =>
            category is TagCategory.Primary
                or TagCategory.CombatRole
                or TagCategory.AttackType;

        public static TagDefinition ResolvePrimaryTag(PieceTagQueries.PlayerVisibleTagsResult visible) =>
            FindFirstCategory(visible?.IdentityTags, TagCategory.Primary);

        public static TagDefinition ResolveCombatRoleTag(PieceTagQueries.PlayerVisibleTagsResult visible) =>
            FindFirstCategory(visible?.IdentityTags, TagCategory.CombatRole);

        public static IReadOnlyList<TagDefinition> BuildChipTags(PieceTagQueries.PlayerVisibleTagsResult visible)
        {
            if (visible == null)
                return Array.Empty<TagDefinition>();

            var chips = new List<TagDefinition>();
            for (int i = 0; i < visible.IdentityTags.Count; i++)
            {
                TagDefinition tag = visible.IdentityTags[i];
                if (tag != null && !IsDedicatedSlotCategory(tag.Category))
                    chips.Add(tag);
            }

            chips.AddRange(visible.OptionalTags);
            return chips;
        }

        private static TagDefinition FindFirstCategory(IReadOnlyList<TagDefinition> tags, TagCategory category)
        {
            if (tags == null)
                return null;

            for (int i = 0; i < tags.Count; i++)
            {
                TagDefinition tag = tags[i];
                if (tag != null && tag.Category == category)
                    return tag;
            }

            return null;
        }
    }
}
