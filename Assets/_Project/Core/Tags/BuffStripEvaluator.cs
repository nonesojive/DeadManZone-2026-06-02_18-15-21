using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Tags
{
    public sealed class BuffStripEntry
    {
        public string TagId { get; init; } = string.Empty;
        public string RuleId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int CurrentCount { get; init; }
        public int ActiveThreshold { get; init; }
        public int NextThreshold { get; init; }
        public string DetailText { get; init; } = string.Empty;
    }

    public static class BuffStripEvaluator
    {
        public static List<BuffStripEntry> Evaluate(BoardState board)
        {
            var entries = new List<BuffStripEntry>();
            if (board == null)
                return entries;

            var snapshot = CriticalMassEngine.Evaluate(board);
            for (int i = 0; i < snapshot.Rules.Count; i++)
            {
                var evaluated = snapshot.Rules[i];
                var rule = evaluated.Rule;
                if (rule.Tiers == null || rule.Tiers.Length == 0)
                    continue;

                int firstThreshold = rule.Tiers[0].Threshold;
                bool isNearMiss = !evaluated.IsActive
                    && evaluated.Count >= firstThreshold - 1
                    && evaluated.Count > 0;
                if (!evaluated.IsActive && !isNearMiss)
                    continue;

                string displayName = ResolveDisplayName(rule);
                entries.Add(new BuffStripEntry
                {
                    TagId = rule.CountTagId,
                    RuleId = rule.Id,
                    DisplayName = displayName,
                    IsActive = evaluated.IsActive,
                    CurrentCount = evaluated.Count,
                    ActiveThreshold = evaluated.IsActive ? evaluated.ActiveTier.Threshold : 0,
                    NextThreshold = evaluated.IsActive ? 0 : firstThreshold,
                    DetailText = BuildDetailText(displayName, evaluated)
                });
            }

            AppendActiveAbilityAuras(board, entries);
            return entries;
        }

        /// <summary>Card copy for an ability id on the board (not synergy tag display names).</summary>
        public static string ResolveAbilityDescription(
            BoardState board,
            string abilityId,
            string sourceInstanceId = null)
        {
            if (TryFindAbility(board, abilityId, sourceInstanceId, out var ability)
                && !string.IsNullOrWhiteSpace(ability.CardDescription))
            {
                return ability.CardDescription.Trim();
            }

            return abilityId ?? string.Empty;
        }

        private static void AppendActiveAbilityAuras(BoardState board, List<BuffStripEntry> entries)
        {
            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            var seenAbilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
                seenAbilityIds.Add(entry.TagId);

            foreach (var link in snapshot.Links)
            {
                string abilityId = GetSourceAbilityId(link);
                if (string.IsNullOrWhiteSpace(abilityId) || seenAbilityIds.Contains(abilityId))
                    continue;

                seenAbilityIds.Add(abilityId);
                string description = ResolveAbilityDescription(board, abilityId, link.SourceInstanceId);
                entries.Add(new BuffStripEntry
                {
                    TagId = abilityId,
                    RuleId = abilityId,
                    DisplayName = description,
                    IsActive = true,
                    CurrentCount = 0,
                    DetailText = $"Active ability: {description}"
                });
            }
        }

        private static string GetSourceAbilityId(PieceAbilityEngine.SynergyLink link) => link.SourceTagId;

        private static bool TryFindAbility(
            BoardState board,
            string abilityId,
            string sourceInstanceId,
            out PieceAbilityDefinition ability)
        {
            ability = default;
            if (board == null || string.IsNullOrWhiteSpace(abilityId))
                return false;

            if (!string.IsNullOrWhiteSpace(sourceInstanceId))
            {
                foreach (var piece in board.Pieces)
                {
                    if (!string.Equals(piece.InstanceId, sourceInstanceId, StringComparison.Ordinal))
                        continue;

                    if (TryFindOnPiece(piece, abilityId, out ability))
                        return true;
                }
            }

            foreach (var piece in board.Pieces)
            {
                if (TryFindOnPiece(piece, abilityId, out ability))
                    return true;
            }

            return false;
        }

        private static bool TryFindOnPiece(PlacedPiece piece, string abilityId, out PieceAbilityDefinition ability)
        {
            ability = default;
            var abilities = piece.Definition?.Abilities;
            if (abilities == null)
                return false;

            for (int i = 0; i < abilities.Count; i++)
            {
                if (!string.Equals(abilities[i].Id, abilityId, StringComparison.Ordinal))
                    continue;

                ability = abilities[i];
                return true;
            }

            return false;
        }

        private static string BuildDetailText(string displayName, EvaluatedCriticalMassRule evaluated)
        {
            var rule = evaluated.Rule;
            string bonus = FormatBonus(rule, evaluated.ActiveTier.Magnitude);
            if (evaluated.IsActive)
            {
                return string.IsNullOrEmpty(bonus)
                    ? $"{displayName} critical mass active ({evaluated.Count}/{evaluated.ActiveTier.Threshold})"
                    : $"{displayName} {evaluated.Count}/{evaluated.ActiveTier.Threshold}: {bonus}";
            }

            int nextThreshold = rule.Tiers[0].Threshold;
            for (int i = 0; i < rule.Tiers.Length; i++)
            {
                if (evaluated.Count < rule.Tiers[i].Threshold)
                {
                    nextThreshold = rule.Tiers[i].Threshold;
                    bonus = FormatBonus(rule, rule.Tiers[i].Magnitude);
                    break;
                }
            }

            return $"{displayName} {evaluated.Count}/{nextThreshold} — {bonus}";
        }

        private static string FormatBonus(CriticalMassRuleDefinition rule, int magnitude)
        {
            if (magnitude == 0)
                return string.Empty;

            return rule.ModType switch
            {
                SynergyModType.Percent => $"+{magnitude}% {HumanizeStat(rule.Stat)}",
                SynergyModType.TierStep => $"+{magnitude} {HumanizeStat(rule.Stat)}",
                _ => $"+{magnitude} {HumanizeStat(rule.Stat)}"
            };
        }

        private static string HumanizeStat(CriticalMassStat stat) => stat switch
        {
            CriticalMassStat.MaxHp => "HP",
            CriticalMassStat.Damage => "Damage",
            CriticalMassStat.Accuracy => "Accuracy",
            CriticalMassStat.AttackSpeed => "Attack Speed",
            CriticalMassStat.MovementSpeed => "Move Speed",
            CriticalMassStat.AttackRange => "Attack Range",
            CriticalMassStat.Authority => "Authority",
            CriticalMassStat.Supplies => "Supplies",
            _ => stat.ToString()
        };

        private static string ResolveDisplayName(CriticalMassRuleDefinition rule)
        {
            if (!string.IsNullOrWhiteSpace(rule.Id)
                && TagRegistry.TryGet(rule.Id, out var idTag)
                && !string.IsNullOrWhiteSpace(idTag.DisplayName))
            {
                return idTag.DisplayName;
            }

            return ResolveTagDisplayName(rule.CountTagId);
        }

        private static string ResolveTagDisplayName(string tagId)
        {
            if (TagRegistry.TryGet(tagId, out var tag) && !string.IsNullOrWhiteSpace(tag.DisplayName))
                return tag.DisplayName;

            if (tagId == "iron_vanguard")
                return "IronMarch Union";

            return tagId;
        }
    }
}
