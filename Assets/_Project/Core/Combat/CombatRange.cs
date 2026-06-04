using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatRange
    {
        public static int GetRangeCells(AttackRangeTier tier) => tier switch
        {
            AttackRangeTier.Short => 1,
            AttackRangeTier.Long => 6,
            _ => 3
        };

        public static bool IsInRange(GridCoord from, GridCoord to, AttackRangeTier tier) =>
            Manhattan(from, to) <= GetRangeCells(tier);

        public static int Manhattan(GridCoord from, GridCoord to) =>
            System.Math.Abs(from.X - to.X) + System.Math.Abs(from.Y - to.Y);
    }
}
