using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopSlotProfile
    {
        public int SlotIndex { get; init; }
        public ShopSlotKind Kind { get; init; }
        public ShopPoolBias PoolBias { get; init; }
        public ShopOfferWeights BaseWeights { get; init; } = ShopOfferWeights.Default;
        public float PreferredRoleWeight { get; init; } = 2f;
        public IReadOnlyList<string> PreferredCombatRoles { get; init; } = Array.Empty<string>();

        public ShopLane PoolLane => PoolBias.ToShopLane();
    }
}
