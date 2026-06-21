using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Presentation.UI
{
    [CreateAssetMenu(
        fileName = "UnitCardIcons",
        menuName = "DeadManZone/UI/Unit Card Icons")]
    public sealed class UnitCardIconsSO : ScriptableObject
    {
        [Header("Armor — SPR_MilitaryCombat_Badge_Shield_05_*")]
        [SerializeField] private Sprite lightArmorShield;
        [SerializeField] private Sprite mediumArmorShield;
        [SerializeField] private Sprite heavyArmorShield;

        // ponytail: attack/role sprite arrays land when Synty combat-role + attack-type art is imported.
        [Header("Attack Type Icons (future)")]
        [SerializeField] private Sprite[] attackTypeIcons = new Sprite[8];

        [Header("Combat Role Icons (future)")]
        [SerializeField] private Sprite assaultRoleIcon;
        [SerializeField] private Sprite artilleryRoleIcon;
        [SerializeField] private Sprite supportRoleIcon;
        [SerializeField] private Sprite sniperRoleIcon;
        [SerializeField] private Sprite mechanicRoleIcon;

        public Sprite GetArmorIcon(ArmorType armorType) => armorType switch
        {
            ArmorType.Light => lightArmorShield,
            ArmorType.Medium => mediumArmorShield,
            ArmorType.Heavy => heavyArmorShield,
            _ => null
        };

        public Sprite GetAttackTypeIcon(AttackType attackType)
        {
            if (attackType == AttackType.None || attackTypeIcons == null)
                return null;

            int index = (int)attackType - 1;
            return index >= 0 && index < attackTypeIcons.Length ? attackTypeIcons[index] : null;
        }

        public Sprite GetCombatRoleIcon(string combatRoleTagId)
        {
            if (string.IsNullOrWhiteSpace(combatRoleTagId))
                return null;

            // ponytail: expand when role icon set is finalized.
            return combatRoleTagId switch
            {
                GameTagIds.Assault => assaultRoleIcon,
                GameTagIds.Artillery => artilleryRoleIcon,
                GameTagIds.Support => supportRoleIcon,
                GameTagIds.Sniper => sniperRoleIcon,
                GameTagIds.Mechanic => mechanicRoleIcon,
                _ => null
            };
        }

#if UNITY_INCLUDE_TESTS
        public void AssignArmorIconsForTests(Sprite light, Sprite medium, Sprite heavy)
        {
            lightArmorShield = light;
            mediumArmorShield = medium;
            heavyArmorShield = heavy;
        }
#endif
    }
}
