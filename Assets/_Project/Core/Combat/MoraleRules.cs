namespace DeadManZone.Core.Combat
{
    /// <summary>Per-unit morale tuning (ADR-0005): at 0 morale a unit Breaks and routs —
    /// flees the field, out of the fight, not dead.</summary>
    public static class MoraleRules
    {
        /// <summary>Chebyshev radius a death shocks living unbroken allies within. M5 initial, tune in playtest.</summary>
        public const int DeathShockRadius = 2;

        /// <summary>Morale damage each shocked ally takes when a unit dies. M5 initial, tune in playtest.</summary>
        public const int DeathShockDamage = 8;
    }
}
