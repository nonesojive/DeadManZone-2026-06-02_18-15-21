using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
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
        public int ProgressThreshold { get; init; }
        public bool IsAtMaxTier { get; init; }
        public string DetailText { get; init; } = string.Empty;
    }

    public static class BuffStripEvaluator
    {
        public static List<BuffStripEntry> Evaluate(BuildBoardSet boards) =>
            Evaluate(boards?.ToAggregateBoard());

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

                ResolveProgress(evaluated, rule, out int progressThreshold, out bool isAtMaxTier);
                string displayName = ResolveDisplayName(rule);
                entries.Add(new BuffStripEntry
                {
                    TagId = rule.CountTagId,
                    RuleId = rule.Id,
                    DisplayName = displayName,
                    IsActive = evaluated.IsActive,
                    CurrentCount = evaluated.Count,
                    ProgressThreshold = progressThreshold,
                    IsAtMaxTier = isAtMaxTier,
                    DetailText = BuildDetailText(displayName, evaluated, progressThreshold, isAtMaxTier)
                });
            }

            return entries;
        }

        public static int CountActive(BuildBoardSet boards) =>
            Evaluate(boards).Count(entry => entry.IsActive);

        public static string FormatProgressLabel(BuffStripEntry entry)
        {
            if (entry == null)
                return string.Empty;

            if (entry.IsAtMaxTier)
                return $"{entry.CurrentCount}/{entry.ProgressThreshold}";

            if (entry.ProgressThreshold > 0)
                return $"{entry.CurrentCount}/{entry.ProgressThreshold}";

            return entry.CurrentCount.ToString();
        }

        private static void ResolveProgress(
            EvaluatedCriticalMassRule evaluated,
            CriticalMassRuleDefinition rule,
            out int progressThreshold,
            out bool isAtMaxTier)
        {
            isAtMaxTier = false;
            if (!evaluated.IsActive)
            {
                progressThreshold = ResolveNextUnreachedThreshold(evaluated.Count, rule.Tiers);
                return;
            }

            int nextIndex = evaluated.ActiveTierIndex + 1;
            if (nextIndex < rule.Tiers.Length)
            {
                progressThreshold = rule.Tiers[nextIndex].Threshold;
                return;
            }

            isAtMaxTier = true;
            progressThreshold = evaluated.ActiveTier.Threshold;
        }

        private static int ResolveNextUnreachedThreshold(int count, CriticalMassTier[] tiers)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                if (count < tiers[i].Threshold)
                    return tiers[i].Threshold;
            }

            return tiers[^1].Threshold;
        }

        private static string BuildDetailText(
            string displayName,
            EvaluatedCriticalMassRule evaluated,
            int progressThreshold,
            bool isAtMaxTier)
        {
            var rule = evaluated.Rule;
            if (evaluated.IsActive)
            {
                string bonus = FormatBonus(rule, evaluated.ActiveTier.Magnitude);
                string progress = isAtMaxTier
                    ? $"{evaluated.Count}/{progressThreshold} (max)"
                    : $"{evaluated.Count}/{progressThreshold}";
                return string.IsNullOrEmpty(bonus)
                    ? $"{displayName} critical mass active ({progress})"
                    : $"{displayName} {progress}: {bonus}";
            }

            string nextBonus = FormatBonus(rule, ResolveTierMagnitude(rule, progressThreshold));
            return $"{displayName} {evaluated.Count}/{progressThreshold} — {nextBonus}";
        }

        private static int ResolveTierMagnitude(CriticalMassRuleDefinition rule, int threshold)
        {
            for (int i = 0; i < rule.Tiers.Length; i++)
            {
                if (rule.Tiers[i].Threshold == threshold)
                    return rule.Tiers[i].Magnitude;
            }

            return rule.Tiers[0].Magnitude;
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

            if (tagId == FactionIds.IronmarchUnion)
                return "IronMarch Union";

            return tagId;
        }
    }
}
