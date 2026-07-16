using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    public static class PieceCardTooltipFormatter
    {
        public static IReadOnlyList<string> BuildSynergyLines(
            PieceAbilityEngine.FightStartSynergySnapshot snapshot,
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
                string statLine = FormatSynergyStatLine(link, sourcePiece);
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
                GrantedAbility.MortarShot => "Mortar Shot — Area damage at pause 0.",
                GrantedAbility.ShieldAllies => "Shield Allies — Protect nearby allies at pause.",
                GrantedAbility.CannonBlast => "Cannon Blast — Heavy blast at pause 1.",
                _ => string.Empty
            };

        public static IReadOnlyList<string> BuildAbilityLines(PieceDefinition piece)
        {
            if (piece == null)
                return Array.Empty<string>();

            var lines = new List<string>();
            if (piece.Abilities != null)
            {
                for (int i = 0; i < piece.Abilities.Count; i++)
                {
                    var ability = piece.Abilities[i];
                    string description = !string.IsNullOrWhiteSpace(ability.CardDescription)
                        ? ability.CardDescription.Trim()
                        : PieceAbilityCardDescriptionFormatter.Format(ability);
                    if (!string.IsNullOrWhiteSpace(description))
                        lines.Add(description);
                }
            }

            AppendPassiveEffectLines(piece, lines);

            string grantedText = BuildAbilityText(piece.GrantedAbility);
            if (!string.IsNullOrWhiteSpace(grantedText))
                lines.Add(grantedText);

            return lines;
        }

        private static void AppendPassiveEffectLines(PieceDefinition piece, List<string> lines)
        {
            if (piece == null || lines == null)
                return;

            switch (piece.Id)
            {
                case "supply_depot":
                    lines.Add("+5 supplies income per round.");
                    break;
                case "command_outpost":
                    lines.Add("+1 Authority per round.");
                    break;
                case "marksman_doctrine_officer":
                    lines.Add("Untargetable until after the 2nd tactics checkpoint.");
                    break;
            }

            if (piece.MusterPerShop > 0)
                lines.Add($"+{piece.MusterPerShop} manpower per shop phase.");
        }

        private static string FormatSynergyStatLine(PieceAbilityEngine.SynergyLink link, PlacedPiece sourcePiece)
        {
            int magnitude = ResolveSynergyMagnitude(sourcePiece, link.SourceTagId, link.Stat);
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

        private static int ResolveSynergyMagnitude(PlacedPiece sourcePiece, string sourceAbilityId, SynergyStat stat)
        {
            if (sourcePiece?.Definition.Abilities == null)
                return 0;

            var abilities = sourcePiece.Definition.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                if (!string.Equals(ability.Id, sourceAbilityId, StringComparison.Ordinal))
                    continue;
                if (ability.Stat != stat)
                    continue;
                return ability.Magnitude;
            }

            return 0;
        }
    }
}
