using System.Collections.Generic;
using System.Text;
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

            AppendActiveSynergyTags(board, entries);
            return entries;
        }

        private static void AppendActiveSynergyTags(BoardState board, List<BuffStripEntry> entries)
        {
            var snapshot = SynergyEngine.EvaluateFightStart(board);
            var seenTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
                seenTags.Add(entry.TagId);

            foreach (var piece in board.Pieces)
            {
                if (!snapshot.TryGet(piece.InstanceId, out var synergy))
                    continue;

                if (synergy.DamageBonus == 0 && synergy.ArmorBuffSteps == 0 && synergy.MoveChargeBonus == 0)
                    continue;

                foreach (var tagId in piece.Definition.SynergyTags)
                {
                    if (string.IsNullOrWhiteSpace(tagId) || seenTags.Contains(tagId))
                        continue;

                    seenTags.Add(tagId);
                    string displayName = ResolveTagDisplayName(tagId);
                    entries.Add(new BuffStripEntry
                    {
                        TagId = tagId,
                        DisplayName = displayName,
                        IsActive = true,
                        CurrentCount = 0,
                        DetailText = $"Active synergy: {displayName}"
                    });
                }
            }
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
