namespace DeadManZone.Core.Run
{
    public static class MoraleCalculator
    {
        public static int ComputeLoss(int fightIndex, int combatantsLost, int totalCombatants, bool hqDamage)
        {
            float severity = totalCombatants == 0 ? 1f : (float)combatantsLost / totalCombatants;
            int baseLoss = (int)(6 * severity) + (hqDamage ? 4 : 0);
            int scale = 1 + fightIndex / 3;
            return baseLoss * scale;
        }
    }
}
