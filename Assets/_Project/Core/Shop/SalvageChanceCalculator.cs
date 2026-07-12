using System;

namespace DeadManZone.Core.Shop
{
    public static class SalvageChanceCalculator
    {
        public const int GlobalCapPercent = 50;

        public static int Compute(int baseSalvagePercent, int boardBoost) =>
            Math.Min(baseSalvagePercent + boardBoost, GlobalCapPercent);

        /// <summary>Share of removed enemies that DIED, 0–100 (ADR-0005): routed units
        /// escaped with their gear, so an all-rout fight yields 0. No removals at all
        /// (legacy saves, rounds without a finished fight) yields the neutral 100.</summary>
        public static int KillSharePercent(int enemyKilled, int enemyRouted)
        {
            enemyKilled = Math.Max(0, enemyKilled);
            enemyRouted = Math.Max(0, enemyRouted);
            int removed = enemyKilled + enemyRouted;
            return removed == 0 ? 100 : 100 * enemyKilled / removed;
        }

        /// <summary>Effective salvage chance for the post-fight build round: the
        /// board-derived chance scaled by the last fight's kill share. Integer math.</summary>
        public static int ApplyKillShare(int chancePercent, int killSharePercent) =>
            chancePercent * killSharePercent / 100;
    }
}
