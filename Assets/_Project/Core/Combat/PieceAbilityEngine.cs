using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Applies piece-owned adjacency aura buffs from a fight-start snapshot.</summary>
    public static class PieceAbilityEngine
    {
        public static SynergyEngine.FightStartSynergySnapshot EvaluateFightStart(BoardState board)
        {
            if (board == null)
                return SynergyEngine.FightStartSynergySnapshot.Empty;

            var piecesById = new Dictionary<string, PlacedPiece>(System.StringComparer.Ordinal);
            var resultsById = new Dictionary<string, SynergyEngine.SynergyResult>(System.StringComparer.Ordinal);
            var links = new List<SynergyEngine.SynergyLink>();

            foreach (var piece in board.Pieces)
            {
                piecesById[piece.InstanceId] = piece;
                resultsById[piece.InstanceId] = default;
            }

            foreach (var source in piecesById.Values)
            {
                var abilities = source.Definition.Abilities;
                if (abilities == null || abilities.Count == 0)
                    continue;

                var adjacentPieces = GetAdjacentPieces(board, source.InstanceId, piecesById);
                if (adjacentPieces.Count == 0)
                    continue;

                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability.Trigger != PieceAbilityTrigger.AdjacentAura)
                        continue;

                    for (int neighborIndex = 0; neighborIndex < adjacentPieces.Count; neighborIndex++)
                    {
                        var adjacentPiece = adjacentPieces[neighborIndex];
                        if (!ability.NeighborFilter.Matches(adjacentPiece.Definition))
                            continue;

                        ApplyEffect(source.InstanceId, adjacentPiece.InstanceId, ability, resultsById, links);
                    }
                }
            }

            return new SynergyEngine.FightStartSynergySnapshot(resultsById, links);
        }

        public static void ApplyToCombatants(
            SynergyEngine.FightStartSynergySnapshot snapshot,
            IList<CombatantState> combatants)
        {
            if (snapshot == null || combatants == null)
                return;

            foreach (var combatant in combatants)
            {
                if (!snapshot.TryGet(combatant.InstanceId, out var bonuses))
                    continue;

                combatant.DamageBonus += bonuses.DamageBonus;
                combatant.ArmorBuffSteps += bonuses.ArmorBuffSteps;
                combatant.MoveCharge += bonuses.MoveChargeBonus;
            }
        }

        private static List<PlacedPiece> GetAdjacentPieces(
            BoardState board,
            string sourceId,
            IReadOnlyDictionary<string, PlacedPiece> piecesById)
        {
            // ponytail: duplicated adjacency helper from SynergyEngine; ceiling is dual maintenance drift, upgrade path is a shared internal adjacency utility.
            var adjacentPieces = new List<PlacedPiece>();
            foreach (var adjacentId in board.GetAdjacentInstanceIds(sourceId))
            {
                if (!piecesById.TryGetValue(adjacentId, out var adjacentPiece))
                    continue;

                adjacentPieces.Add(adjacentPiece);
            }

            return adjacentPieces;
        }

        private static void ApplyEffect(
            string sourceInstanceId,
            string targetInstanceId,
            PieceAbilityDefinition ability,
            Dictionary<string, SynergyEngine.SynergyResult> resultsById,
            List<SynergyEngine.SynergyLink> links)
        {
            if (!resultsById.TryGetValue(targetInstanceId, out var result))
                result = default;

            int amount = ResolveAmount(ability);
            if (amount == 0)
                return;

            links.Add(new SynergyEngine.SynergyLink
            {
                SourceInstanceId = sourceInstanceId,
                TargetInstanceId = targetInstanceId,
                SourceTagId = ability.Id,
                Stat = ability.Stat
            });

            switch (ability.Stat)
            {
                case SynergyStat.Damage:
                    result = new SynergyEngine.SynergyResult
                    {
                        DamageBonus = result.DamageBonus + amount,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus
                    };
                    break;
                case SynergyStat.ArmorType:
                    result = new SynergyEngine.SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps + amount,
                        MoveChargeBonus = result.MoveChargeBonus
                    };
                    break;
                case SynergyStat.MoveChargePercent:
                    result = new SynergyEngine.SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus + amount
                    };
                    break;
                case SynergyStat.AttackRange:
                case SynergyStat.MovementSpeed:
                default:
                    return;
            }

            resultsById[targetInstanceId] = result;
        }

        private static int ResolveAmount(PieceAbilityDefinition ability)
        {
            return ability.ModType switch
            {
                SynergyModType.Flat => ability.Magnitude,
                SynergyModType.TierStep => ability.Magnitude,
                SynergyModType.Percent => ability.Magnitude,
                _ => 0
            };
        }
    }
}
