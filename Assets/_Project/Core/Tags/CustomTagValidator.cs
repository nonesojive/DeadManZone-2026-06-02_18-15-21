using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeadManZone.Core.Tags
{
    public static class CustomTagValidator
    {
        private static readonly Regex IdPattern = new("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);

        public static bool TryValidate(CustomTagRecord record, IReadOnlyList<CustomTagRecord> existing, out string error)
        {
            error = null;
            if (record == null)
            {
                error = "Tag record is required.";
                return false;
            }

            string id = record.Id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                error = "Tag id is required.";
                return false;
            }

            if (!IdPattern.IsMatch(id))
            {
                error = "Tag id must be lowercase snake_case starting with a letter.";
                return false;
            }

            if (IsBuiltInId(id))
            {
                error = $"Tag id '{id}' is reserved by the built-in catalog.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(record.DisplayName))
            {
                error = "Display name is required.";
                return false;
            }

            if (!IsSupportedCategory(record.Category))
            {
                error = $"Category '{record.Category}' cannot be created via Tag Creator.";
                return false;
            }

            for (int i = 0; i < existing?.Count; i++)
            {
                var other = existing[i];
                if (other == null || ReferenceEquals(other, record))
                    continue;

                if (string.Equals(other.Id, id, StringComparison.Ordinal))
                {
                    error = $"Tag id '{id}' already exists in custom tags.";
                    return false;
                }
            }

            return true;
        }

        public static bool IsBuiltInId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            foreach (var field in typeof(GameTagIds).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType != typeof(string))
                    continue;

                var value = field.GetValue(null) as string;
                if (string.Equals(value, id, StringComparison.Ordinal))
                    return true;
            }

            for (int i = 0; i < KeywordTagCatalog.All.Count; i++)
            {
                if (string.Equals(KeywordTagCatalog.All[i].Id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static bool IsSupportedCategory(TagCategory category) =>
            category is TagCategory.Primary
                or TagCategory.CombatRole
                or TagCategory.System
                or TagCategory.Faction
                or TagCategory.Synergy
                or TagCategory.Ability
                or TagCategory.Flavor;
    }
}
