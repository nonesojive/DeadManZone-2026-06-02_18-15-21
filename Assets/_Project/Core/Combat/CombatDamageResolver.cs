using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatDamageResolver
    {
        public static int ComputeDamage(
            PieceDefinition attacker,
            PieceDefinition defender,
            float damageScale,
            int armorBuffSteps,
            int flatBonus = 0,
            int damagePercentBonus = 0)
        {
            float baseDamage = (attacker.BaseDamage + flatBonus) * damageScale;
            var armor = StepArmor(defender.ArmorType, armorBuffSteps);
            float afterArmor = baseDamage * BaselineArmorMultiplier(armor);
            float typeMultiplier = AttackTypeMultiplier(attacker.AttackType, armor, defender);
            float scaled = afterArmor * typeMultiplier;
            if (damagePercentBonus != 0)
                scaled *= 1f + damagePercentBonus / 100f;

            return System.Math.Max(1, (int)System.Math.Round(scaled));
        }

        public static ArmorType StepArmor(ArmorType baseArmor, int steps)
        {
            int value = (int)baseArmor + steps;
            if (value <= (int)ArmorType.None)
                return ArmorType.None;
            if (value >= (int)ArmorType.Heavy)
                return ArmorType.Heavy;
            return (ArmorType)value;
        }

        public static float BaselineArmorMultiplier(ArmorType armor) => armor switch
        {
            ArmorType.Medium => 0.85f,
            ArmorType.Heavy => 0.70f,
            _ => 1.0f
        };

        public static float AttackTypeMultiplier(
            AttackType attackType,
            ArmorType armor,
            PieceDefinition defender)
        {
            var profile = AttackTypeProfileCatalog.Get(attackType);
            if (profile == null)
                return 1f;

            bool isStructure = PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Building)
                || PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Structure);

            if (profile.StrongVsStructures && isStructure)
                return profile.StrongMultiplier;

            if (!string.IsNullOrWhiteSpace(profile.StrongPrimaryTagId)
                && PieceTagQueries.HasPrimaryTag(defender, profile.StrongPrimaryTagId))
                return profile.StrongMultiplier;

            if (!string.IsNullOrWhiteSpace(profile.WeakPrimaryTagId)
                && (PieceTagQueries.HasPrimaryTag(defender, profile.WeakPrimaryTagId)
                    || (profile.WeakPrimaryTagId == GameTagIds.Building
                        && PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Structure))))
                return profile.WeakMultiplier;

            return profile.GetMultiplierForArmor(armor);
        }
    }
}
