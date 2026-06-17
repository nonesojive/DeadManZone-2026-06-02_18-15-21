using System;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Shop/Slot Profile")]
    public class ShopSlotProfileSO : ScriptableObject
    {
        public int slotIndex;
        public ShopSlotKind slotKind = ShopSlotKind.BaselineOffensive;
        public ShopPoolBias poolBias = ShopPoolBias.Offensive;

        [Header("Source weights (percent)")]
        [Range(0, 100)] public int neutralPercent = 10;
        [Range(0, 100)] public int factionPercent = 80;
        [Range(0, 100)] public int salvagePercent = 10;

        [Header("Piece pick bias")]
        public float preferredRoleWeight = 2f;
        public string[] preferredCombatRoles = Array.Empty<string>();

        public ShopSlotProfile ToCore() => new()
        {
            SlotIndex = slotIndex,
            Kind = slotKind,
            PoolBias = poolBias,
            BaseWeights = new ShopOfferWeights(neutralPercent, factionPercent, salvagePercent),
            PreferredRoleWeight = preferredRoleWeight,
            PreferredCombatRoles = preferredCombatRoles ?? Array.Empty<string>()
        };
    }
}
