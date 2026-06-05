using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public static class CombatMovementSpeed
    {
        public const int NormalStepChargeCost = 100;
        public const int NeutralStepChargeCost = 200;

        public static int GetChargePerTick(MovementSpeedTier tier) => tier switch
        {
            MovementSpeedTier.Low => 3,
            MovementSpeedTier.High => 6,
            MovementSpeedTier.Medium => 5,
            _ => 0
        };

        /// <summary>Future tactics pass: Advance +10%, Stand Ground -10%, etc.</summary>
        public static int GetChargePerTick(MovementSpeedTier tier, TacticType tactic) =>
            GetChargePerTick(tier);
    }
}
