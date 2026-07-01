using System;

namespace DeadManZone.Core.Shop
{
    public static class SalvageChanceCalculator
    {
        public const int GlobalCapPercent = 50;

        public static int Compute(int baseSalvagePercent, int boardBoost) =>
            Math.Min(baseSalvagePercent + boardBoost, GlobalCapPercent);
    }
}
