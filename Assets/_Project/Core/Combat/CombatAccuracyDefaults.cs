using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatAccuracyDefaults
    {
        public static int GetBaseAccuracy(PieceDefinition piece)
        {
            if (piece == null)
                return 78;

            if (piece.AccuracyOverride.HasValue)
                return Clamp(piece.AccuracyOverride.Value);

            int fromType = piece.AttackType switch
            {
                AttackType.Melee => 92,
                AttackType.Piercing => 80,
                AttackType.Explosive => 72,
                AttackType.Shredding => 68,
                AttackType.Gas => 75,
                _ => 78
            };

            if (piece.CombatRole == GameTagIds.Sniper)
                return 88;
            if (piece.CombatRole == GameTagIds.Artillery)
                return System.Math.Max(fromType, 72);

            return fromType;
        }

        private static int Clamp(int value) => System.Math.Clamp(value, 0, 100);
    }
}
