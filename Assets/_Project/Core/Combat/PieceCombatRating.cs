using DeadManZone.Core.Board;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Combat
{
    public static class PieceCombatRating
    {
        public static int ComputeBase(PieceDefinition piece) =>
            Compute(piece, synergy: null);

        public static int Compute(PieceDefinition piece, PieceAbilityEngine.SynergyResult? synergy)
        {
            if (piece == null || !ManpowerCalculator.CountsTowardFielding(piece))
                return 0;

            int damageBonus = synergy?.DamageBonus ?? 0;
            int armorBuffSteps = synergy?.ArmorBuffSteps ?? 0;

            var armor = CombatDamageResolver.StepArmor(piece.ArmorType, armorBuffSteps);
            float durabilityDivisor = CombatStrengthConfig.GetArmorDurabilityDivisor(armor);
            float ehp = piece.MaxHp / System.Math.Max(0.01f, durabilityDivisor);

            int cooldown = CombatAttackSpeed.GetEffectiveCooldown(piece.CooldownTicks, piece.AttackSpeed);
            cooldown = System.Math.Max(1, cooldown);

            float accuracyFactor = CombatAccuracyDefaults.GetBaseAccuracy(piece) / 100f
                * CombatStrengthConfig.EffectiveFireRate;
            float dps = (piece.BaseDamage + damageBonus) / (float)cooldown * accuracyFactor;

            float rangeMult = CombatStrengthConfig.GetRangeMultiplier(piece.AttackRange);
            float abilityBonus = CombatStrengthConfig.GetAbilityFlatBonus(piece.GrantedAbility);

            float rating = (float)System.Math.Sqrt(ehp * System.Math.Max(0.01f, dps))
                * CombatStrengthConfig.Scale
                * rangeMult
                + abilityBonus;

            return System.Math.Max(1, (int)System.Math.Round(rating));
        }
    }
}
