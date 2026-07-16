using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Applies piece-owned fight-start buffs from a combat-board snapshot.</summary>
    public static class PieceAbilityEngine
    {
        public readonly struct SynergyResult
        {
            public int DamageBonus { get; init; }
            public int ArmorBuffSteps { get; init; }
            public int MoveChargeBonus { get; init; }
            public int MaxHpFlat { get; init; }
            public int MaxHpPercent { get; init; }
            public int AttackSpeedSteps { get; init; }
            public int MovementSpeedBonus { get; init; }
            public int MoraleResistancePercent { get; init; }
        }

        public readonly struct SynergyLink
        {
            public string SourceInstanceId { get; init; }
            public string TargetInstanceId { get; init; }
            public string SourceTagId { get; init; }
            public SynergyStat Stat { get; init; }
        }

        public sealed class FightStartSynergySnapshot
        {
            private readonly IReadOnlyDictionary<string, SynergyResult> _resultsByInstanceId;
            private readonly IReadOnlyList<SynergyLink> _links;

            public IReadOnlyList<SynergyLink> Links => _links;

            public static FightStartSynergySnapshot Empty { get; } =
                new(new Dictionary<string, SynergyResult>(), new List<SynergyLink>());

            internal FightStartSynergySnapshot(
                IReadOnlyDictionary<string, SynergyResult> resultsByInstanceId,
                IReadOnlyList<SynergyLink> links)
            {
                _resultsByInstanceId = resultsByInstanceId;
                _links = links;
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

        public static FightStartSynergySnapshot EvaluateFightStart(BoardState combatBoard) =>
            EvaluateFightStart(combatBoard, buildBoards: null);

        public static FightStartSynergySnapshot EvaluateFightStart(
            BoardState combatBoard,
            BuildBoardSet buildBoards)
        {
            if (combatBoard == null)
                return FightStartSynergySnapshot.Empty;

            var piecesById = new Dictionary<string, PlacedPiece>(System.StringComparer.Ordinal);
            var resultsById = new Dictionary<string, SynergyResult>(System.StringComparer.Ordinal);
            var links = new List<SynergyLink>();

            foreach (var piece in combatBoard.Pieces)
            {
                piecesById[piece.InstanceId] = piece;
                resultsById[piece.InstanceId] = default;
            }

            ApplyAdjacentAuras(combatBoard, piecesById, resultsById, links);
            ApplyBoardPerTagCount(combatBoard, buildBoards, piecesById, resultsById, links);
            ApplyFightStartGlobals(combatBoard, buildBoards, piecesById, resultsById, links);

            return new FightStartSynergySnapshot(resultsById, links);
        }

        public static void ApplyToCombatants(
            FightStartSynergySnapshot snapshot,
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
                combatant.AttackSpeedSteps += bonuses.AttackSpeedSteps;
                combatant.MovementSpeedBonus += bonuses.MovementSpeedBonus;
                combatant.MoraleDamageResistancePercent += bonuses.MoraleResistancePercent;

                if (bonuses.MaxHpFlat == 0 && bonuses.MaxHpPercent == 0)
                    continue;

                int maxHp = combatant.CurrentHp + bonuses.MaxHpFlat;
                if (bonuses.MaxHpPercent != 0)
                    maxHp += (int)System.Math.Round(maxHp * (bonuses.MaxHpPercent / 100f));

                combatant.CurrentHp = maxHp;
            }
        }

        private static void ApplyAdjacentAuras(
            BoardState board,
            IReadOnlyDictionary<string, PlacedPiece> piecesById,
            Dictionary<string, SynergyResult> resultsById,
            List<SynergyLink> links)
        {
            foreach (var source in piecesById.Values)
            {
                var abilities = source.Definition.Abilities;
                if (abilities == null || abilities.Count == 0)
                    continue;

                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability.Trigger != PieceAbilityTrigger.AdjacentAura)
                        continue;

                    int radius = ability.Radius <= 1 ? 1 : ability.Radius;
                    var reachablePieces = GetPiecesWithinHops(board, source.InstanceId, piecesById, radius);
                    for (int neighborIndex = 0; neighborIndex < reachablePieces.Count; neighborIndex++)
                    {
                        var reachablePiece = reachablePieces[neighborIndex];
                        if (!ability.NeighborFilter.Matches(reachablePiece.Definition))
                            continue;

                        string targetId = ability.ApplyToSelf ? source.InstanceId : reachablePiece.InstanceId;
                        ApplyEffect(source.InstanceId, targetId, ability, resultsById, links);
                    }
                }
            }
        }

        private static void ApplyBoardPerTagCount(
            BoardState combatBoard,
            BuildBoardSet buildBoards,
            IReadOnlyDictionary<string, PlacedPiece> piecesById,
            Dictionary<string, SynergyResult> resultsById,
            List<SynergyLink> links)
        {
            if (buildBoards == null)
                return;

            foreach (var source in piecesById.Values)
            {
                var abilities = source.Definition.Abilities;
                if (abilities == null || abilities.Count == 0)
                    continue;

                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability.Trigger != PieceAbilityTrigger.BoardPerTagCount)
                        continue;

                    int count = BuildBoardTagCounter.Count(buildBoards, ability.CountTagId);
                    if (count <= 0)
                        continue;

                    int amount = ResolveAmount(ability) * count;
                    if (amount == 0)
                        continue;

                    foreach (var target in piecesById.Values)
                    {
                        if (!ability.NeighborFilter.Matches(target.Definition))
                            continue;

                        ApplyEffect(source.InstanceId, target.InstanceId, ability, amount, resultsById, links);
                    }
                }
            }
        }

        private static void ApplyFightStartGlobals(
            BoardState combatBoard,
            BuildBoardSet buildBoards,
            IReadOnlyDictionary<string, PlacedPiece> piecesById,
            Dictionary<string, SynergyResult> resultsById,
            List<SynergyLink> links)
        {
            IEnumerable<PlacedPiece> sources = buildBoards != null
                ? buildBoards.AllPieces
                : combatBoard.Pieces;

            foreach (var source in sources)
            {
                var abilities = source.Definition.Abilities;
                if (abilities == null || abilities.Count == 0)
                    continue;

                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability.Trigger != PieceAbilityTrigger.FightStart)
                        continue;

                    foreach (var target in piecesById.Values)
                    {
                        if (!ability.NeighborFilter.Matches(target.Definition))
                            continue;

                        ApplyEffect(source.InstanceId, target.InstanceId, ability, resultsById, links);
                    }
                }
            }
        }

        /// <summary>BFS over board-touching adjacency out to <paramref name="hops"/> steps.
        /// hops=1 is exactly <see cref="GetAdjacentPieces"/>; "within 2 cells" (Breakthrough
        /// Tank) reuses this same board-topology adjacency at hops=2 rather than introducing
        /// raw grid-distance geometry (2026-07-15 faction-roster-v1 §2.2).</summary>
        private static List<PlacedPiece> GetPiecesWithinHops(
            BoardState board,
            string sourceId,
            IReadOnlyDictionary<string, PlacedPiece> piecesById,
            int hops)
        {
            var visited = new HashSet<string>(System.StringComparer.Ordinal) { sourceId };
            var frontier = new List<string> { sourceId };
            var result = new List<PlacedPiece>();

            for (int hop = 0; hop < hops; hop++)
            {
                var nextFrontier = new List<string>();
                foreach (var id in frontier)
                {
                    foreach (var adjacentId in board.GetAdjacentInstanceIds(id))
                    {
                        if (!visited.Add(adjacentId))
                            continue;

                        nextFrontier.Add(adjacentId);
                        if (piecesById.TryGetValue(adjacentId, out var piece))
                            result.Add(piece);
                    }
                }

                frontier = nextFrontier;
                if (frontier.Count == 0)
                    break;
            }

            return result;
        }

        private static void ApplyEffect(
            string sourceInstanceId,
            string targetInstanceId,
            PieceAbilityDefinition ability,
            Dictionary<string, SynergyResult> resultsById,
            List<SynergyLink> links) =>
            ApplyEffect(sourceInstanceId, targetInstanceId, ability, ResolveAmount(ability), resultsById, links);

        private static void ApplyEffect(
            string sourceInstanceId,
            string targetInstanceId,
            PieceAbilityDefinition ability,
            int amount,
            Dictionary<string, SynergyResult> resultsById,
            List<SynergyLink> links)
        {
            if (!resultsById.TryGetValue(targetInstanceId, out var result))
                result = default;

            if (amount == 0)
                return;

            links.Add(new SynergyLink
            {
                SourceInstanceId = sourceInstanceId,
                TargetInstanceId = targetInstanceId,
                SourceTagId = ability.Id,
                Stat = ability.Stat
            });

            switch (ability.Stat)
            {
                case SynergyStat.Damage:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus + amount,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps,
                        MovementSpeedBonus = result.MovementSpeedBonus,
                        MoraleResistancePercent = result.MoraleResistancePercent
                    };
                    break;
                case SynergyStat.ArmorType:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps + amount,
                        MoveChargeBonus = result.MoveChargeBonus,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps,
                        MovementSpeedBonus = result.MovementSpeedBonus,
                        MoraleResistancePercent = result.MoraleResistancePercent
                    };
                    break;
                case SynergyStat.MoveChargePercent:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus + amount,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps,
                        MovementSpeedBonus = result.MovementSpeedBonus,
                        MoraleResistancePercent = result.MoraleResistancePercent
                    };
                    break;
                case SynergyStat.MaxHp:
                    if (ability.ModType == SynergyModType.Percent)
                    {
                        result = new SynergyResult
                        {
                            DamageBonus = result.DamageBonus,
                            ArmorBuffSteps = result.ArmorBuffSteps,
                            MoveChargeBonus = result.MoveChargeBonus,
                            MaxHpFlat = result.MaxHpFlat,
                            MaxHpPercent = result.MaxHpPercent + amount,
                            AttackSpeedSteps = result.AttackSpeedSteps,
                            MovementSpeedBonus = result.MovementSpeedBonus,
                            MoraleResistancePercent = result.MoraleResistancePercent
                        };
                    }
                    else
                    {
                        result = new SynergyResult
                        {
                            DamageBonus = result.DamageBonus,
                            ArmorBuffSteps = result.ArmorBuffSteps,
                            MoveChargeBonus = result.MoveChargeBonus,
                            MaxHpFlat = result.MaxHpFlat + amount,
                            MaxHpPercent = result.MaxHpPercent,
                            AttackSpeedSteps = result.AttackSpeedSteps,
                            MovementSpeedBonus = result.MovementSpeedBonus,
                            MoraleResistancePercent = result.MoraleResistancePercent
                        };
                    }

                    break;
                case SynergyStat.AttackSpeedSteps:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps + amount,
                        MovementSpeedBonus = result.MovementSpeedBonus,
                        MoraleResistancePercent = result.MoraleResistancePercent
                    };
                    break;
                case SynergyStat.MovementSpeed:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps,
                        MovementSpeedBonus = result.MovementSpeedBonus + amount,
                        MoraleResistancePercent = result.MoraleResistancePercent
                    };
                    break;
                case SynergyStat.MoraleResistancePercent:
                    result = new SynergyResult
                    {
                        DamageBonus = result.DamageBonus,
                        ArmorBuffSteps = result.ArmorBuffSteps,
                        MoveChargeBonus = result.MoveChargeBonus,
                        MaxHpFlat = result.MaxHpFlat,
                        MaxHpPercent = result.MaxHpPercent,
                        AttackSpeedSteps = result.AttackSpeedSteps,
                        MovementSpeedBonus = result.MovementSpeedBonus,
                        MoraleResistancePercent = result.MoraleResistancePercent + amount
                    };
                    break;
                case SynergyStat.AttackRange:
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
