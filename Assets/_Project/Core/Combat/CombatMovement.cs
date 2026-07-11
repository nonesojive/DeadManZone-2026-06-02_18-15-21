using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatMovement
    {
        public static int NeutralMoveCost => 2;
        public static int NormalMoveCost => 1;

        public static int GetMoveCost(GridCoord from, GridCoord to, BattlefieldLayout layout)
        {
            if (layout.IsNeutralColumn(to.X) || layout.IsNeutralColumn(from.X))
                return NeutralMoveCost;

            return NormalMoveCost;
        }

        public static int GetStepChargeCost(
            GridCoord from,
            GridCoord to,
            BattlefieldLayout layout) =>
            GetMoveCost(from, to, layout) == NeutralMoveCost
                ? CombatMovementSpeed.NeutralStepChargeCost
                : CombatMovementSpeed.NormalStepChargeCost;
    }
}
