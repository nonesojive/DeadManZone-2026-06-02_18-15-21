using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    public static class PieceCardTooltipFormatter
    {
        public static IReadOnlyList<string> BuildSynergyLines(
            SynergyEngine.FightStartSynergySnapshot snapshot,
            BoardState board,
            string targetInstanceId)
        {
            if (snapshot == null || board == null || string.IsNullOrWhiteSpace(targetInstanceId))
                return Array.Empty<string>();

            var piecesById = new Dictionary<string, PlacedPiece>(StringComparer.Ordinal);
            foreach (var piece in board.Pieces)
                piecesById[piece.InstanceId] = piece;

            var lines = new List<string>();
            foreach (var link in snapshot.Links)
            {
                if (!string.Equals(link.TargetInstanceId, targetInstanceId, StringComparison.Ordinal))
                    continue;

                if (!piecesById.TryGetValue(link.SourceInstanceId, out var sourcePiece))
                    continue;

                string sourceName = sourcePiece.Definition?.DisplayName ?? link.SourceInstanceId;
                string statLine = FormatSynergyStatLine(link);
                if (string.IsNullOrEmpty(statLine))
                    continue;

                lines.Add($"{statLine} from adjacent {sourceName}");
            }

            return lines;
        }

        public static string BuildCriticalMassHint(BoardState board, PieceDefinition piece)
        {
            if (board == null || piece == null)
                return string.Empty;

            var hints = new List<string>();
            var rules = CriticalMassRuleCatalog.GetRules();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (!PieceMatchesRule(piece, rule))
                    continue;

                int count = CountMatchingPieces(board, rule);
                string tagName = TagRegistry.TryGet(rule.TagId, out var tag)
                    ? tag.DisplayName
                    : rule.TagId;
                string bonus = FormatCriticalMassBonus(rule);
                if (string.IsNullOrEmpty(bonus))
                    continue;

                if (count >= rule.Threshold)
                    hints.Add($"Critical mass active: {tagName} ({bonus})");
                else
                    hints.Add($"Critical mass: {count}/{rule.Threshold} {tagName} ({bonus})");
            }

            return hints.Count == 0 ? string.Empty : string.Join("\n", hints);
        }

        public static string BuildSalvageContext(
            bool isSalvaged,
            string lastEnemyFactionId,
            string lastEnemyFactionDisplayName)
        {
            if (!isSalvaged)
                return string.Empty;

            string factionLabel = !string.IsNullOrWhiteSpace(lastEnemyFactionDisplayName)
                ? lastEnemyFactionDisplayName.Trim()
                : lastEnemyFactionId?.Trim();

            return string.IsNullOrEmpty(factionLabel)
                ? "Salvaged offer"
                : $"Salvaged from {factionLabel}";
        }

        public static string BuildAbilityText(GrantedAbility ability) =>
            ability switch
            {
                GrantedAbility.GrenadeLob => "Grenade Lob — Area damage at pause 0.",
                GrantedAbility.ShieldAllies => "Shield Allies — Protect nearby allies at pause.",
                GrantedAbility.CannonBlast => "Cannon Blast — Heavy blast at pause 1.",
                _ => string.Empty
            };

        private static string FormatSynergyStatLine(SynergyEngine.SynergyLink link)
        {
            int magnitude = ResolveSynergyMagnitude(link.SourceTagId, link.Stat);
            if (magnitude == 0)
                return string.Empty;

            return link.Stat switch
            {
                SynergyStat.Damage => $"+{magnitude} Damage",
                SynergyStat.ArmorType => $"+{magnitude} Armor",
                SynergyStat.MoveChargePercent => $"+{magnitude}% Move charge",
                _ => string.Empty
            };
        }

        private static int ResolveSynergyMagnitude(string sourceTagId, SynergyStat stat)
        {
            var rules = SynergyRuleCatalog.GetRulesForSourceTag(sourceTagId);
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule.Stat == stat)
                    return rule.Magnitude;
            }

            return 0;
        }

        private static bool PieceMatchesRule(PieceDefinition piece, CriticalMassRuleDefinition rule)
        {
            return rule.CountCategory switch
            {
                CriticalMassCountCategory.Primary => PieceTagQueries.HasPrimaryTag(piece, rule.TagId),
                CriticalMassCountCategory.CombatRole => PieceTagQueries.HasCombatRoleTag(piece, rule.TagId),
                CriticalMassCountCategory.Synergy => PieceTagQueries.HasSynergyTag(piece, rule.TagId),
                _ => false
            };
        }

        private static int CountMatchingPieces(BoardState board, CriticalMassRuleDefinition rule)
        {
            int count = 0;
            foreach (var placed in board.Pieces)
            {
                if (placed.Definition == null)
                    continue;

                if (PieceMatchesRule(placed.Definition, rule))
                    count++;
            }

            return count;
        }

        private static string FormatCriticalMassBonus(CriticalMassRuleDefinition rule)
        {
            var parts = new List<string>();
            if (rule.DamageBonus > 0)
                parts.Add($"+{rule.DamageBonus} Damage");
            if (rule.ArmorShredSteps > 0)
                parts.Add($"+{rule.ArmorShredSteps} Armor shred");
            if (rule.MoveChargePercentBonus > 0)
                parts.Add($"+{rule.MoveChargePercentBonus}% Move charge");

            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }
    }
}
