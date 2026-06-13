using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class AttackTypeProfileCatalog
    {
        private static readonly AttackTypeProfile[] Profiles =
        {
            Profile(
                AttackType.Ballistic,
                "Ballistic",
                "Strong vs Medium armor, weak vs Heavy",
                strongArmor: ArmorType.Medium,
                weakArmor: ArmorType.Heavy,
                strongMultiplier: 1.25f),
            Profile(
                AttackType.Piercing,
                "Piercing",
                "Strong vs Heavy armor, weak vs Light",
                strongArmor: ArmorType.Heavy,
                weakArmor: ArmorType.Light,
                strongMultiplier: 1.35f),
            Profile(
                AttackType.Shredding,
                "Shredding",
                "Strong vs Light armor, weak vs Medium",
                strongArmor: ArmorType.Light,
                weakArmor: ArmorType.Medium,
                strongMultiplier: 1.25f),
            Profile(
                AttackType.Explosive,
                "Explosive",
                "Strong vs Heavy armor and structures",
                strongArmor: ArmorType.Heavy,
                strongVsStructures: true,
                strongMultiplier: 1.30f),
            Profile(AttackType.Fire, "Fire", "Strong vs Light armor, weak vs Heavy; applies burn",
                strongArmor: ArmorType.Light,
                weakArmor: ArmorType.Heavy,
                strongMultiplier: 1.20f),
            Profile(AttackType.Melee, "Melee", "Strong vs Light armor, weak vs Heavy",
                strongArmor: ArmorType.Light,
                weakArmor: ArmorType.Heavy,
                strongMultiplier: 1.25f,
                weakMultiplier: 0.80f),
            Profile(
                AttackType.Gas,
                "Gas",
                "Strong vs Infantry, weak vs buildings",
                strongPrimaryTagId: GameTagIds.Infantry,
                weakPrimaryTagId: GameTagIds.Building,
                strongMultiplier: 1.25f)
        };

        public static IReadOnlyList<AttackTypeProfile> All { get; } = Profiles;

        public static AttackTypeProfile Get(AttackType attackType)
        {
            for (int i = 0; i < Profiles.Length; i++)
            {
                if (Profiles[i].AttackType == attackType)
                    return Profiles[i];
            }

            return null;
        }

        public static float GetArmorMatrixMultiplier(AttackType attackType, ArmorType armor)
        {
            if (attackType == AttackType.None)
                return 1f;

            var profile = Get(attackType);
            return profile?.GetMultiplierForArmor(armor) ?? 1f;
        }

        private static AttackTypeProfile Profile(
            AttackType attackType,
            string displayName,
            string tooltip,
            ArmorType? strongArmor = null,
            ArmorType? weakArmor = null,
            string strongPrimaryTagId = null,
            string weakPrimaryTagId = null,
            bool strongVsStructures = false,
            float strongMultiplier = 1.25f,
            float weakMultiplier = 0.85f)
        {
            return new AttackTypeProfile
            {
                AttackType = attackType,
                TagId = AttackTypeTags.ToTagId(attackType),
                DisplayName = displayName,
                Tooltip = tooltip,
                StrongArmor = strongArmor,
                WeakArmor = weakArmor,
                StrongPrimaryTagId = strongPrimaryTagId,
                WeakPrimaryTagId = weakPrimaryTagId,
                StrongVsStructures = strongVsStructures,
                StrongMultiplier = strongMultiplier,
                WeakMultiplier = weakMultiplier
            };
        }
    }
}
