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
        /// <summary>Raw stat line, no fight-start engines. Used for the BaseTotal reference only.
        /// Kept as a dedicated 1-arg overload (not an optional parameter) — callers use it as a
        /// method group (e.g. OrderBy), which optional parameters would break.</summary>
        public static int ComputeBase(PieceDefinition piece) =>
            Compute(piece, synergy: null, criticalMass: default);

        /// <summary>Raw stat line at a StatScale (fight-option ratio scaling).</summary>
        public static int ComputeBase(PieceDefinition piece, float statScale) =>
            Compute(piece, synergy: null, criticalMass: default, statScale);

        public static int Compute(PieceDefinition piece, PieceAbilityEngine.SynergyResult? synergy) =>
            Compute(piece, synergy, criticalMass: default);

        /// <param name="statScale">PROVISIONAL 2026-07-19 owner spec (fight-option strength
        /// ratios): PlacedPiece.StatScale. Applied to MaxHp and BaseDamage with EXACTLY the
        /// spawn seam's rounding (TickCombatRun.SpawnCombatants: round-half-away, HP floored
        /// at 1, damage floored at 1 only for attackers) so the rating moves with the scale
        /// the same way the fight does. 1 reproduces the unscaled rating bit-for-bit.</param>
        public static int Compute(
            PieceDefinition piece,
            PieceAbilityEngine.SynergyResult? synergy,
            CriticalMassCombatModifiers criticalMass,
            float statScale = 1f)
        {
            if (piece == null || !ManpowerCalculator.CountsTowardFielding(piece))
                return 0;

            var syn = synergy ?? default;

            if (statScale <= 0f)
                statScale = 1f;
            int scaledMaxHp = statScale == 1f
                ? piece.MaxHp
                : System.Math.Max(1, (int)System.Math.Round(
                    piece.MaxHp * statScale, System.MidpointRounding.AwayFromZero));
            int scaledBaseDamage = piece.BaseDamage <= 0
                ? 0
                : System.Math.Max(1, (int)System.Math.Round(
                    piece.BaseDamage * statScale, System.MidpointRounding.AwayFromZero));

            // --- HP: critical mass, THEN synergy (TickCombatRun order). Each stage's percent
            //     applies to the running total, so this ordering is not cosmetic.
            int maxHp = scaledMaxHp + criticalMass.MaxHpFlat;
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
            float damage = scaledBaseDamage + criticalMass.DamageFlat + syn.DamageBonus;
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
