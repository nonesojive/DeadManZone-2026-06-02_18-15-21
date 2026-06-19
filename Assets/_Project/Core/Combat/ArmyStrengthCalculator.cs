using DeadManZone.Core.Board;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Combat
{
    public static class ArmyStrengthCalculator
    {
        public static ArmyStrengthSnapshot Evaluate(BoardState board)
        {
            if (board == null || board.Pieces.Count == 0)
                return default;

            var synergySnapshot = SynergyEngine.EvaluateFightStart(board);
            int baseTotal = 0;
            int effectiveTotal = 0;

            foreach (var placed in board.Pieces)
            {
                if (!ManpowerCalculator.CountsTowardFielding(placed.Definition))
                    continue;

                baseTotal += PieceCombatRating.ComputeBase(placed.Definition);
                synergySnapshot.TryGet(placed.InstanceId, out var synergy);
                effectiveTotal += PieceCombatRating.Compute(placed.Definition, synergy);
            }

            return new ArmyStrengthSnapshot
            {
                BaseTotal = baseTotal,
                EffectiveTotal = effectiveTotal
            };
        }
    }
}
