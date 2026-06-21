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
    }
}
