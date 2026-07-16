using DeadManZone.Core.Board;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Rates a single piece's combat worth — the atom of ARMY STRENGTH.
    ///
    /// The rating models the piece AS IT WILL FIGHT: after the fight-start engines have run, i.e.
    /// adjacency synergies (<see cref="PieceAbilityEngine"/>) AND army-wide Critical Mass
    /// (<see cref="CriticalMassEngine"/>). A rating built from raw <see cref="PieceDefinition"/>
    /// stats is a lie the moment the player builds a composition — which is the entire game.
    ///
    /// The apply ORDER below mirrors <c>TickCombatRun</c>'s constructor exactly: critical mass
    /// first, then synergy. That order is load-bearing for HP, because each stage applies its
    /// percentage to the RUNNING TOTAL, not to the base. If the sim's order changes, change it
    /// here in the same commit or the preview silently drifts away from the fight it predicts.
    /// </summary>
    public static class PieceCombatRating
    {
        /// <summary>Raw stat line, no fight-start engines. Used for the BaseTotal reference only.</summary>
        public static int ComputeBase(PieceDefinition piece) =>
            Compute(piece, synergy: null, criticalMass: default);

        public static int Compute(PieceDefinition piece, PieceAbilityEngine.SynergyResult? synergy) =>
            Compute(piece, synergy, criticalMass: default);

        public static int Compute(
            PieceDefinition piece,
            PieceAbilityEngine.SynergyResult? synergy,
            CriticalMassCombatModifiers criticalMass)
        {
            if (piece == null || !ManpowerCalculator.CountsTowardFielding(piece))
                return 0;

            var syn = synergy ?? default;

            // --- HP: critical mass, THEN synergy (TickCombatRun order). Each stage's percent
            //     applies to the running total, so this ordering is not cosmetic.
            int maxHp = piece.MaxHp + criticalMass.MaxHpFlat;
            if (criticalMass.MaxHpPercent != 0)
                maxHp += (int)System.Math.Round(maxHp * (criticalMass.MaxHpPercent / 100f));

            maxHp += syn.MaxHpFlat;
            if (syn.MaxHpPercent != 0)
                maxHp += (int)System.Math.Round(maxHp * (syn.MaxHpPercent / 100f));

            // Armor steps come from synergy ONLY — Critical Mass has no armor channel
            // (see CriticalMassCombatModifiers).
            var armor = CombatDamageResolver.StepArmor(piece.ArmorType, syn.ArmorBuffSteps);
            float durabilityDivisor = CombatStrengthConfig.GetArmorDurabilityDivisor(armor);
            float ehp = maxHp / System.Math.Max(0.01f, durabilityDivisor);

            // --- Damage: flat bonuses stack; the percent channel is Critical Mass's.
            float damage = piece.BaseDamage + criticalMass.DamageFlat + syn.DamageBonus;
            if (criticalMass.DamagePercent != 0)
                damage *= 1f + criticalMass.DamagePercent / 100f;

            // --- Rate of fire: both engines can step the attack-speed tier.
            int attackSpeedSteps = criticalMass.AttackSpeedSteps + syn.AttackSpeedSteps;
            int cooldown = System.Math.Max(1, CombatAttackSpeed.GetEffectiveCooldown(
                piece.CooldownTicks,
                piece.AttackSpeed,
                attackSpeedSteps));

            // --- Accuracy: CombatAccuracyResolver adds this as PERCENTAGE POINTS onto the base.
            int accuracy = System.Math.Clamp(
                CombatAccuracyDefaults.GetBaseAccuracy(piece) + criticalMass.AccuracyPercent,
                0,
                100);
            float accuracyFactor = accuracy / 100f * CombatStrengthConfig.EffectiveFireRate;

            float dps = damage / cooldown * accuracyFactor;

            // --- Reach: Critical Mass can step the range tier (e.g. grenadier).
            var range = CombatRange.StepTier(piece.AttackRange, criticalMass.AttackRangeSteps);
            float rangeMult = CombatStrengthConfig.GetRangeMultiplier(range);

            float abilityBonus = CombatStrengthConfig.GetAbilityFlatBonus(piece.GrantedAbility);

            float rating = (float)System.Math.Sqrt(ehp * System.Math.Max(0.01f, dps))
                * CombatStrengthConfig.Scale
                * rangeMult
                + abilityBonus;

            return System.Math.Max(1, (int)System.Math.Round(rating));
        }
    }
}
