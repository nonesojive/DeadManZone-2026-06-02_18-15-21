namespace DeadManZone.Core.Combat
{
    public static class AccuracyModifierCollector
    {
        // ponytail: v1 returns 0; tactics/abilities plug in here later
        public static int Collect(
            CombatantState attacker,
            CombatantState target,
            TacticType tactic) => 0;
    }
}
