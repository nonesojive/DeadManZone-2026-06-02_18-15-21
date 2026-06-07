using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Applies tag-owned adjacency synergy buffs from a fight-start snapshot.</summary>
    public static class SynergyEngine
    {
        public readonly struct SynergyResult
        {
            public int DamageBonus { get; init; }
            public int ArmorBuffSteps { get; init; }
            public int MoveChargeBonus { get; init; }
        }

        public sealed class FightStartSynergySnapshot
        {
            private readonly IReadOnlyDictionary<string, SynergyResult> _resultsByInstanceId;

            public static FightStartSynergySnapshot Empty { get; } =
                new(new Dictionary<string, SynergyResult>());

            internal FightStartSynergySnapshot(IReadOnlyDictionary<string, SynergyResult> resultsByInstanceId)
            {
                _resultsByInstanceId = resultsByInstanceId;
            }

            public bool TryGet(string instanceId, out SynergyResult result)
            {
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    result = default;
                    return false;
                }

                return _resultsByInstanceId.TryGetValue(instanceId, out result);
            }
        }

        public static FightStartSynergySnapshot EvaluateFightStart(BoardState board)
        {
            if (board == null)
                return FightStartSynergySnapshot.Empty;

            var piecesById = new Dictionary<string, PlacedPiece>(System.StringComparer.Ordinal);
            var resultsById = new Dictionary<string, SynergyResult>(System.StringComparer.Ordinal);
            foreach (var piece in board.Pieces)
            {
                piecesById[piece.InstanceId] = piece;
                resultsById[piece.InstanceId] = default;
            }

            foreach (var source in piecesById.Values)
            {
                var sourceSynergyTags = source.Definition.SynergyTags;
                if (sourceSynergyTags == null || sourceSynergyTags.Count == 0)
                    continue;

                var adjacentPieces = GetAdjacentPieces(board, source.InstanceId, piecesById);
                if (adjacentPieces.Count == 0)
                    continue;

                var seenSourceTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < sourceSynergyTags.Count; i++)
                {
                    string sourceTagId = sourceSynergyTags[i];
                    if (string.IsNullOrWhiteSpace(sourceTagId) || !seenSourceTags.Add(sourceTagId))
                        continue;

                    var rules = SynergyRuleCatalog.GetRulesForSourceTag(sourceTagId);
                    for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                    {
                        var rule = rules[ruleIndex];
                        ApplyRule(source, adjacentPieces, rule, resultsById);
                    }
                }
            }

            return new FightStartSynergySnapshot(resultsById);
        }

        public static void ApplyToCombatants(
            FightStartSynergySnapshot snapshot,
            IList<CombatantState> combatants)
        {
            if (snapshot == null || combatants == null)
                return;

            foreach (var combatant in combatants)
            {
                if (!snapshot.TryGet(combatant.InstanceId, out var synergy))
                    continue;

                combatant.DamageBonus += synergy.DamageBonus;
                combatant.ArmorBuffSteps += synergy.ArmorBuffSteps;
                combatant.MoveCharge += synergy.MoveChargeBonus;
            }
        }

        private static List<PlacedPiece> GetAdjacentPieces(
            BoardState board,
            string sourceId,
            IReadOnlyDictionary<string, PlacedPiece> piecesById)
        {
            var adjacentPieces = new List<PlacedPiece>();
            foreach (var adjacentId in board.GetAdjacentInstanceIds(sourceId))
            {
                if (!piecesById.TryGetValue(adjacentId, out var adjacentPiece))
                    continue;

                adjacentPieces.Add(adjacentPiece);
            }

            return adjacentPieces;
        }

        private static void ApplyRule(
            PlacedPiece sourcePiece,
            IReadOnlyList<PlacedPiece> adjacentPieces,
            SynergyEffectDefinition rule,
            Dictionary<string, SynergyResult> resultsById)
        {
            if (rule.Direction == SynergyDirection.Outbound)
            {
                for (int i = 0; i < adjacentPieces.Count; i++)
                {
                    var adjacentPiece = adjacentPieces[i];
                    if (!rule.NeighborFilter.Matches(adjacentPiece.Definition))
                        continue;

                    ApplyEffect(adjacentPiece.InstanceId, rule, resultsById);
                }

                return;
            }

            if (rule.Direction == SynergyDirection.Inbound)
            {
                for (int i = 0; i < adjacentPieces.Count; i++)
                {
                    if (!rule.NeighborFilter.Matches(adjacentPieces[i].Definition))
                        continue;

                    ApplyEffect(sourcePiece.InstanceId, rule, resultsById);
                }
            }
        }

        private static void ApplyEffect(
            string targetInstanceId,
            SynergyEffectDefinition rule,
            Dictionary<string, SynergyResult> resultsById)
        {
            if (!resultsById.TryGetValue(targetInstanceId, out var result))
                result = default;

            int amount = ResolveAmount(rule);
            if (amount == 0)
                return;

            switch (rule.Stat)
            {
                case SynergyStat.Damage:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus + amount,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus
                    };
                    break;
                case SynergyStat.ArmorType:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps + amount,
                        MoveChargeBonus = result.MoveChargeBonus
                    };
                    break;
                case SynergyStat.MoveChargePercent:
                    result = new SynergyResult
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

        private static int ResolveAmount(SynergyEffectDefinition rule)
        {
            return rule.ModType switch
            {
                SynergyModType.Flat => rule.Magnitude,
                SynergyModType.TierStep => rule.Magnitude,
                SynergyModType.Percent => rule.Magnitude,
                _ => 0
            };
        }
    }
}
