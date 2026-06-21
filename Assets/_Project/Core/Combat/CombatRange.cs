using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatRange
    {
        public static int GetRangeCells(AttackRangeTier tier) => tier switch
        {
            AttackRangeTier.Melee => 1,
            AttackRangeTier.Short => 3,
            AttackRangeTier.Medium => 5,
            AttackRangeTier.Long => 8,
            _ => 5
        };

        public static AttackRangeTier StepTier(AttackRangeTier baseTier, int steps)
        {
            int value = (int)baseTier + steps;
            if (value <= (int)AttackRangeTier.Melee)
                return AttackRangeTier.Melee;
            if (value >= (int)AttackRangeTier.Long)
                return AttackRangeTier.Long;
            return (AttackRangeTier)value;
        }

        public static int GetRangeCells(AttackRangeTier tier, int tierSteps) =>
            GetRangeCells(StepTier(tier, tierSteps));

        public static bool IsInRange(GridCoord from, GridCoord to, AttackRangeTier tier) =>
            Distance(from, to) <= GetRangeCells(tier);

        public static int Distance(GridCoord from, GridCoord to) =>
            System.Math.Max(System.Math.Abs(from.X - to.X), System.Math.Abs(from.Y - to.Y));

        public static int Manhattan(GridCoord from, GridCoord to) =>
            System.Math.Abs(from.X - to.X) + System.Math.Abs(from.Y - to.Y);
    }
}
