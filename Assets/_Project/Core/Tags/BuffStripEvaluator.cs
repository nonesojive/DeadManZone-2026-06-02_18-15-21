using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Tags
{
    public sealed class BuffStripEntry
    {
        public string TagId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int CurrentCount { get; init; }
        public int Threshold { get; init; }
        public string DetailText { get; init; } = string.Empty;
    }

    public static class BuffStripEvaluator
    {
        public static List<BuffStripEntry> Evaluate(BoardState board)
        {
            var entries = new List<BuffStripEntry>();
            if (board == null)
                return entries;

            var rules = CriticalMassRuleCatalog.GetRules();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule.Threshold <= 0 || string.IsNullOrWhiteSpace(rule.TagId))
                    continue;

                int count = CountMatchingPieces(board, rule);
                bool isActive = count >= rule.Threshold;
                bool isNearMiss = !isActive && count >= rule.Threshold - 1 && count > 0;
                if (!isActive && !isNearMiss)
                    continue;

                string displayName = ResolveTagDisplayName(rule.TagId);
                entries.Add(new BuffStripEntry
                {
                    TagId = rule.TagId,
                    DisplayName = displayName,
                    IsActive = isActive,
                    CurrentCount = count,
                    Threshold = rule.Threshold,
                    DetailText = BuildDetailText(displayName, rule, count, isActive)
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
                        Threshold = 0,
                        DetailText = $"Active synergy: {displayName}"
                    });
                }
            }
        }

        private static int CountMatchingPieces(BoardState board, CriticalMassRuleDefinition rule)
        {
            int count = 0;
            foreach (var piece in board.Pieces)
            {
                if (piece.Definition == null)
                    continue;

                if (rule.CountCategory == CriticalMassCountCategory.Primary
                    && PieceTagQueries.HasPrimaryTag(piece.Definition, rule.TagId))
                    count++;
                else if (rule.CountCategory == CriticalMassCountCategory.CombatRole
                    && PieceTagQueries.HasCombatRoleTag(piece.Definition, rule.TagId))
                    count++;
                else if (rule.CountCategory == CriticalMassCountCategory.Synergy
                    && PieceTagQueries.HasSynergyTag(piece.Definition, rule.TagId))
                    count++;
            }

            return count;
        }

        private static string BuildDetailText(
            string displayName,
            CriticalMassRuleDefinition rule,
            int count,
            bool isActive)
        {
            if (isActive)
            {
                string bonus = FormatBonus(rule);
                return string.IsNullOrEmpty(bonus)
                    ? $"{displayName} critical mass active ({count}/{rule.Threshold})"
                    : $"{displayName} critical mass ({count}/{rule.Threshold}): {bonus}";
            }

            return $"{displayName} {count}/{rule.Threshold} — {FormatBonus(rule)}";
        }

        private static string ResolveTagDisplayName(string tagId)
        {
            if (TagRegistry.TryGet(tagId, out var tag) && !string.IsNullOrWhiteSpace(tag.DisplayName))
                return tag.DisplayName;

            return tagId;
        }

        private static string FormatBonus(CriticalMassRuleDefinition rule)
        {
            if (rule.DamageBonus > 0)
                return $"+{rule.DamageBonus} damage";
            if (rule.ArmorShredSteps > 0)
                return $"{rule.ArmorShredSteps} armor shred";
            if (rule.MoveChargePercentBonus > 0)
                return $"+{rule.MoveChargePercentBonus}% move charge";
            return string.Empty;
        }
    }
}
