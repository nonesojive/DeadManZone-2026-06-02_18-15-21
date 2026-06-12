using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class GasDamageSystem
    {
        private const int NeutralGasBase = 3;
        private const int FrontGasBase = 1;

        public static int GetDamage(GridCoord position, int ticksSinceGasStart, BattlefieldLayout layout)
        {
            float ramp = 1f + ticksSinceGasStart / (float)CombatPacingConfig.GasRampReferenceTicks;
            int baseDamage = layout.IsNeutralColumn(position.X) ? NeutralGasBase : FrontGasBase;
            return System.Math.Max(1, (int)(baseDamage * ramp));
        }

        public static bool IsMitigated(PieceDefinition definition) =>
            definition?.Tags?.Contains("GasMask") == true;
    }
}
