using System.Collections.Generic;
using DeadManZone.Core.Board;

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
            float typeMultiplier = AttackTypeMultiplier(attacker.AttackType, armor, defender.Tags);
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
            IReadOnlyList<string> tags)
        {
            bool isBuilding = HasTag(tags, "building") || HasTag(tags, "structure");

            return attackType switch
            {
                AttackType.Ballistic when armor == ArmorType.Light => 1.25f,
                AttackType.Explosive when armor is ArmorType.Light or ArmorType.Medium || isBuilding => 1.30f,
                AttackType.Piercing when armor == ArmorType.Heavy => 1.35f,
                _ => 1.0f
            };
        }

        private static bool HasTag(IReadOnlyList<string> tags, string tag)
        {
            if (tags == null)
                return false;

            foreach (var entry in tags)
            {
                if (string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
