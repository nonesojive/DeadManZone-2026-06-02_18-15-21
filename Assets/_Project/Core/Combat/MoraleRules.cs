namespace DeadManZone.Core.Combat
{
    /// <summary>Per-unit morale tuning (ADR-0005): at 0 morale a unit Breaks and routs —
    /// flees the field, out of the fight, not dead.</summary>
    public static class MoraleRules
    {
        /// <summary>Chebyshev radius a death shocks living unbroken allies within. M5 initial, tune in playtest.</summary>
        public const int DeathShockRadius = 2;

        /// <summary>Morale damage each shocked ally takes when a unit dies. Softened
        /// 8 → 6 after the 2026-07-12 34-unit cascade smoke — packed blobs should
        /// bleed morale, not domino.</summary>
        public const int DeathShockDamage = 6;

        /// <summary>2026-07-15 faction-roster-v1: the seam for per-piece morale-damage
        /// resistance (Iron Guard's own stat, Breakthrough Tank's aura). Percent is clamped
        /// 0-100; damage is floored at 0, never negative.</summary>
        public static int ApplyResistance(int rawDamage, int resistancePercent)
        {
            if (rawDamage <= 0)
                return 0;

            int clampedPercent = System.Math.Clamp(resistancePercent, 0, 100);
            if (clampedPercent <= 0)
                return rawDamage;

            return System.Math.Max(0, rawDamage * (100 - clampedPercent) / 100);
        }
    }
}
