namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8 tentpole (Crimson): "suppressed = attack-speed
    /// tier stepped down + movement charge slowed for N ticks, applied on hit by
    /// suppression-tagged attacks." Reuses the two existing dials (attack-speed tiers,
    /// movement charge accrual) rather than inventing a new one. Mirrors MovementSlowRules —
    /// a small, directly-testable pure-rules seam kept out of TickCombatRun's tick loop.
    ///
    /// Border rule (§1.8): this is the game's ONLY enemy-facing debuff family. Any future
    /// enemy-slow effect is a Suppression variant, never a new system.
    ///
    /// Stacking rule (PROVISIONAL): a new on-hit application REFRESHES the duration; it does
    /// not stack tiers/magnitude. Tune in playtest.</summary>
    public static class SuppressionRules
    {
        // PROVISIONAL — tune in playtest. ~4s at 10 ticks/sec.
        public const int SuppressionDurationTicks = 40;

        /// <summary>How many attack-speed tiers a suppressed unit steps down (Fast→Medium,
        /// Medium→Slow, Slow stays Slow — CombatAttackSpeed.StepTier floors at Slow).</summary>
        public const int SuppressionAttackSpeedStepDown = 1;

        // PROVISIONAL — tune in playtest. Deliberately the same dial as MovementSlowRules,
        // not a shared implementation: Suppression is on-hit + duration-tracked, Trench Works'
        // aura is permanent-while-adjacent (see MovementSlowRules' header note).
        public const int SuppressionMovementSlowPercent = 50;

        /// <summary>Applies (or refreshes) Suppression on <paramref name="target"/>.</summary>
        public static void Apply(CombatantState target)
        {
            if (target == null)
                return;

            target.SuppressionTicksRemaining = SuppressionDurationTicks;
        }

        /// <summary>Ticks the duration down by one; call once per combatant per sim tick.</summary>
        public static void TickDown(CombatantState combatant)
        {
            if (combatant == null || combatant.SuppressionTicksRemaining <= 0)
                return;

            combatant.SuppressionTicksRemaining--;
        }

        /// <summary>Attack-speed tier steps after folding in the suppression step-down, if any.
        /// Combine with any other steps (e.g. low-state) at the call site.</summary>
        public static int GetEffectiveAttackSpeedSteps(CombatantState combatant) =>
            combatant == null
                ? 0
                : combatant.AttackSpeedSteps - (combatant.IsSuppressed ? SuppressionAttackSpeedStepDown : 0);

        public static int ApplyMovementSuppression(int moveChargePerTick, bool isSuppressed) =>
            isSuppressed ? moveChargePerTick * (100 - SuppressionMovementSlowPercent) / 100 : moveChargePerTick;
    }
}
