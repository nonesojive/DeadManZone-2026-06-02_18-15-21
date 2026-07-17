namespace DeadManZone.Core.Tags
{
    public enum CriticalMassStat
    {
        MaxHp,
        Damage,
        Accuracy,
        AttackSpeed,
        MovementSpeed,
        AttackRange,
        Authority,
        Supplies,
        // 2026-07-15 faction-roster-v1 §1.9 W2: per-faction identity-stack CM payoffs.
        /// <summary>Oathborn: "+max Morale, army-wide".</summary>
        MaxMorale,
        /// <summary>Crimson: "+suppression duration". Adds ticks to the attacking side's
        /// SuppressionRules.Apply call (CombatantState.SuppressionDurationBonusTicks).</summary>
        SuppressionDuration,
        /// <summary>Ashen: "low-state trigger bonuses strengthen". Percent uplift applied to
        /// each piece's own LowStateDamageBonus (LowStateRules.GetDamageBonus).</summary>
        LowStateDamageBonus
    }
}
