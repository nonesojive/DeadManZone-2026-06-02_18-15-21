namespace DeadManZone.Core.Run
{
    public static class MoraleCalculator
    {
        public static int ComputeLoss(int fightIndex, int combatantsLost, int totalCombatants)
        {
            float severity = totalCombatants == 0 ? 1f : (float)combatantsLost / totalCombatants;
            int baseLoss = (int)(6 * severity);
            int scale = 1 + fightIndex / 3;
            return baseLoss * scale;
        }
    }
}
