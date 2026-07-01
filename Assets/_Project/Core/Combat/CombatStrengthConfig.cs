using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    /// <summary>Tunable weights for army strength heuristics. ponytail: not sim-calibrated until balance pass.</summary>
    public static class CombatStrengthConfig
    {
        /// <summary>Matches TutorialBalanceFixtures.EstimatedEffectiveFireRate.</summary>
        public const float EffectiveFireRate = 0.75f;

        public const float Scale = 12f;

        public const float FavorableRatioThreshold = 1.15f;
        public const float DangerousRatioThreshold = 0.85f;

        public static float GetArmorDurabilityDivisor(ArmorType armor) =>
            CombatDamageResolver.BaselineArmorMultiplier(armor);

        public static float GetRangeMultiplier(AttackRangeTier tier) => tier switch
        {
            AttackRangeTier.Long => 1.12f,
            AttackRangeTier.Medium => 1.06f,
            AttackRangeTier.Short => 1.0f,
            AttackRangeTier.Melee => 0.95f,
            _ => 1f
        };

        public static float GetAbilityFlatBonus(GrantedAbility ability) => ability switch
        {
            GrantedAbility.GrenadeLob => 25f,
            GrantedAbility.ShieldAllies => 20f,
            GrantedAbility.CannonBlast => 35f,
            _ => 0f
        };
    }
}
