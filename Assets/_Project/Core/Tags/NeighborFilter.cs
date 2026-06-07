using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public readonly struct NeighborFilter
    {
        public static NeighborFilter Any => default;

        public string PrimaryTagId { get; init; }
        public string CombatRoleTagId { get; init; }
        public string SystemTagId { get; init; }
        public string SynergyTagId { get; init; }
        public string AbilityTagId { get; init; }

        public bool Matches(PieceDefinition piece)
        {
            if (piece == null)
                return false;

            return MatchesSingleTag(piece.Primary, PrimaryTagId)
                && MatchesSingleTag(piece.CombatRole, CombatRoleTagId)
                && MatchesSingleTag(piece.SystemTag, SystemTagId)
                && MatchesTagList(piece.SynergyTags, SynergyTagId)
                && MatchesTagList(piece.AbilityTags, AbilityTagId);
        }

        private static bool MatchesSingleTag(string candidate, string required)
        {
            string requiredKey = NormalizeTagKey(required);
            if (requiredKey.Length == 0)
                return true;

            return NormalizeTagKey(candidate) == requiredKey;
        }

        private static bool MatchesTagList(IReadOnlyList<string> candidates, string required)
        {
            string requiredKey = NormalizeTagKey(required);
            if (requiredKey.Length == 0)
                return true;

            if (candidates == null)
                return false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (NormalizeTagKey(candidates[i]) == requiredKey)
                    return true;
            }

            return false;
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
                    normalized[count++] = char.ToLowerInvariant(c);
            }

            return count == 0 ? string.Empty : new string(normalized, 0, count);
        }
    }
}
