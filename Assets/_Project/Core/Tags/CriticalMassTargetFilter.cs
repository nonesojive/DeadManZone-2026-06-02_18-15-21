using System;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public readonly struct CriticalMassTargetFilter
    {
        public static CriticalMassTargetFilter Any => default;

        /// <summary>OR-match: piece primary equals any listed tag.</summary>
        public string[] PrimaryTagIds { get; init; }

        public string CombatRoleTagId { get; init; }
        public string SynergyTagId { get; init; }
        public string AbilityTagId { get; init; }
        public string FlavorTagId { get; init; }
        public AttackType? AttackType { get; init; }
        public AttackRangeTier? AttackRange { get; init; }
        public string FactionId { get; init; }

        public bool Matches(PieceDefinition piece)
        {
            if (piece == null)
                return false;

            if (!MatchesPrimaryOr(piece))
                return false;
            if (!MatchesSingleTag(piece.CombatRole, CombatRoleTagId))
                return false;
            if (!MatchesTagList(piece.SynergyTags, SynergyTagId))
                return false;
            if (!MatchesTagList(piece.AbilityTags, AbilityTagId))
                return false;
            if (!MatchesTagList(piece.FlavorTags, FlavorTagId))
                return false;
            if (AttackType.HasValue && piece.AttackType != AttackType.Value)
                return false;
            if (AttackRange.HasValue && piece.AttackRange != AttackRange.Value)
                return false;
            if (!MatchesSingleTag(piece.FactionId, FactionId))
                return false;

            return true;
        }

        private bool MatchesPrimaryOr(PieceDefinition piece)
        {
            if (PrimaryTagIds == null || PrimaryTagIds.Length == 0)
                return true;

            for (int i = 0; i < PrimaryTagIds.Length; i++)
            {
                if (PieceTagQueries.HasPrimaryTag(piece, PrimaryTagIds[i]))
                    return true;
            }

            return false;
        }

        private static bool MatchesSingleTag(string candidate, string required)
        {
            string requiredKey = NormalizeTagKey(required);
            if (requiredKey.Length == 0)
                return true;

            return NormalizeTagKey(candidate) == requiredKey;
        }

        private static bool MatchesTagList(System.Collections.Generic.IReadOnlyList<string> candidates, string required)
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
