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
            int flatBonus = 0)
        {
            float baseDamage = (attacker.BaseDamage + flatBonus) * damageScale;
            var armor = StepArmor(defender.ArmorType, armorBuffSteps);
            float afterArmor = baseDamage * BaselineArmorMultiplier(armor);
            float typeMultiplier = AttackTypeMultiplier(attacker.AttackType, armor, defender);
            return System.Math.Max(1, (int)(afterArmor * typeMultiplier));
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
            bool isBuilding = PieceTagQueries.HasTag(defender, GameTagIds.Building)
                || PieceTagQueries.HasTag(defender, GameTagIds.Structure);

            return attackType switch
            {
                AttackType.Ballistic when armor == ArmorType.Light => 1.25f,
                AttackType.Explosive when armor is ArmorType.Light or ArmorType.Medium || isBuilding => 1.30f,
                AttackType.Piercing when armor == ArmorType.Heavy => 1.35f,
                _ => 1.0f
            };
        }
    }
}
