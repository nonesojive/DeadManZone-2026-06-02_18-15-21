using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatAccuracyResolver
    {
        public static int GetEffectiveAccuracy(int baseAccuracy, int distance, int maxRange)
        {
            if (maxRange <= 0)
                return System.Math.Clamp(baseAccuracy, 0, 100);

            float innerEdge = CombatAccuracyConfig.InnerRangeFraction * maxRange;
            if (distance <= innerEdge)
                return System.Math.Clamp(baseAccuracy, 0, 100);

            int floor = System.Math.Max(
                CombatAccuracyConfig.AbsoluteAccuracyFloor,
                (int)System.Math.Round(baseAccuracy * CombatAccuracyConfig.AccuracyFloorFraction));

            float t = (distance - innerEdge) / System.Math.Max(1f, maxRange - innerEdge);
            int effective = baseAccuracy - (int)System.Math.Round((baseAccuracy - floor) * t);
            return System.Math.Clamp(effective, 0, 100);
        }

        public static int GetGrazeBand(int distance, int maxRange)
        {
            if (maxRange <= 1)
                return CombatAccuracyConfig.GrazeBandAtPointBlank;

            float t = (distance - 1f) / (maxRange - 1f);
            t = System.Math.Clamp(t, 0f, 1f);
            float maxBand = CombatAccuracyConfig.GrazeBandBaseline * CombatAccuracyConfig.GrazeBandMaxMultiplier;
            float band = CombatAccuracyConfig.GrazeBandAtPointBlank
                + t * (maxBand - CombatAccuracyConfig.GrazeBandAtPointBlank);
            return (int)System.Math.Round(band);
        }

        public static CombatAttackOutcome ResolveOutcome(
            int effectiveAccuracy,
            int grazeBand,
            int roll,
            int fullDamage)
        {
            if (roll <= effectiveAccuracy)
                return Outcome(CombatAttackOutcomeKind.Hit, fullDamage, roll, effectiveAccuracy, grazeBand);

            if (roll <= effectiveAccuracy + grazeBand)
            {
                int grazeDamage = System.Math.Max(
                    1,
                    (int)System.Math.Round(fullDamage * CombatAccuracyConfig.GrazeDamageFraction));
                return Outcome(CombatAttackOutcomeKind.Graze, grazeDamage, roll, effectiveAccuracy, grazeBand);
            }

            return Outcome(CombatAttackOutcomeKind.Miss, 0, roll, effectiveAccuracy, grazeBand);
        }

        public static CombatAttackOutcome Resolve(
            Rng rng,
            PieceDefinition attacker,
            PieceDefinition defender,
            int distance,
            int accuracyModifier,
            int flatDamageBonus,
            int defenderArmorBuffSteps)
        {
            int maxRange = CombatRange.GetRangeCells(attacker.AttackRange);
            int baseAccuracy = CombatAccuracyDefaults.GetBaseAccuracy(attacker) + accuracyModifier;
            int effective = GetEffectiveAccuracy(baseAccuracy, distance, maxRange);
            int grazeBand = GetGrazeBand(distance, maxRange);
            int roll = rng.NextInt(1, 101);
            int fullDamage = CombatDamageResolver.ComputeDamage(
                attacker,
                defender,
                1f,
                defenderArmorBuffSteps,
                flatDamageBonus);
            return ResolveOutcome(effective, grazeBand, roll, fullDamage);
        }

        private static CombatAttackOutcome Outcome(
            CombatAttackOutcomeKind kind,
            int damage,
            int roll,
            int effectiveAccuracy,
            int grazeBand) =>
            new()
            {
                Kind = kind,
                Damage = damage,
                Roll = roll,
                EffectiveAccuracy = effectiveAccuracy,
                GrazeBand = grazeBand
            };
    }
}
