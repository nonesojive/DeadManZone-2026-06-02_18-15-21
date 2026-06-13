using System;

namespace DeadManZone.Core.Shop
{
    public enum FightOutcome
    {
        Victory,
        Defeat,
        Draw
    }

    public static class SalvageChanceCalculator
    {
        public const int VictoryBonusPercent = 10;
        public const int DestroyedTypeBonusPercent = 2;
        public const int DestroyedTypeBonusCap = 10;
        public const int GlobalCapPercent = 50;

        public static int Compute(
            int baseSalvagePercent,
            int boardBoost,
            FightOutcome outcome,
            int destroyedUniqueTypes)
        {
            if (outcome == FightOutcome.Defeat)
                return baseSalvagePercent;

            int destroyedBonus = Math.Min(
                destroyedUniqueTypes * DestroyedTypeBonusPercent,
                DestroyedTypeBonusCap);

            int total = baseSalvagePercent + boardBoost + VictoryBonusPercent + destroyedBonus;
            return Math.Min(total, GlobalCapPercent);
        }
    }
}
