namespace DeadManZone.Core.Combat
{
    public static class CombatMovementSpeed
    {
        public const int NormalStepChargeCost = 100;
        public const int NeutralStepChargeCost = 200;

        public static int GetChargePerTick(int movementSpeed) =>
            movementSpeed <= 0 ? 0 : movementSpeed + 1;

        public static int GetChargePerTick(int movementSpeed, TacticType tactic)
        {
            int baseCharge = GetChargePerTick(movementSpeed);
            int multiplier = TacticEffects.GetMovementChargeMultiplier(tactic);
            return baseCharge * multiplier / 100;
        }
    }
}
