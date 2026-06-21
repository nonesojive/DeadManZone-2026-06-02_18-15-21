using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public static class CombatAttackSpeed
    {
        public static int GetEffectiveCooldown(int baseCooldownTicks, AttackSpeedTier tier) => tier switch
        {
            AttackSpeedTier.Slow => (int)System.Math.Ceiling(baseCooldownTicks * 1.5f),
            AttackSpeedTier.Fast => System.Math.Max(1, (int)System.Math.Floor(baseCooldownTicks * 0.75f)),
            _ => baseCooldownTicks
        };

        public static AttackSpeedTier StepTier(AttackSpeedTier baseTier, int steps)
        {
            int value = (int)baseTier + steps;
            if (value <= (int)AttackSpeedTier.Slow)
                return AttackSpeedTier.Slow;
            if (value >= (int)AttackSpeedTier.Fast)
                return AttackSpeedTier.Fast;
            return (AttackSpeedTier)value;
        }

        public static int GetEffectiveCooldown(int baseCooldownTicks, AttackSpeedTier tier, int tierSteps) =>
            GetEffectiveCooldown(baseCooldownTicks, StepTier(tier, tierSteps));
    }
}
