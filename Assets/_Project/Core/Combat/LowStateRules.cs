namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §2.9/§4 Ashen low-state triggers: "per-unit live
    /// check, one universal threshold: below 50% HP or morale → piece-defined bonuses
    /// activate." Evaluated live in the tick sim (not a fight-start snapshot) — mirrors
    /// MovementSlowRules/CombatStealthRules as a small, directly-testable pure-rules seam.</summary>
    public static class LowStateRules
    {
        /// <summary>The one universal threshold (§2.9) — not per-piece, not per-faction.</summary>
        public const int LowStateThresholdPercent = 50;

        public static bool IsLowState(CombatantState combatant)
        {
            if (combatant == null || !combatant.IsAlive)
                return false;

            if (combatant.Definition.MaxHp > 0 &&
                combatant.CurrentHp * 100 <= combatant.Definition.MaxHp * LowStateThresholdPercent)
                return true;

            if (combatant.CanBreak && combatant.Definition.MaxMorale > 0 &&
                combatant.CurrentMorale * 100 <= combatant.Definition.MaxMorale * LowStateThresholdPercent)
                return true;

            return false;
        }

        public static int GetDamageBonus(CombatantState combatant) =>
            IsLowState(combatant) ? combatant.Definition.LowStateDamageBonus : 0;

        public static int GetAttackSpeedSteps(CombatantState combatant) =>
            IsLowState(combatant) ? combatant.Definition.LowStateAttackSpeedSteps : 0;
    }
}
