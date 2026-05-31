using System.Collections.Generic;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopModifiers
    {
        public int GoldDiscountPercent { get; init; }
        public int ExtraGeneralSlots { get; init; }
        public bool EnemyTagPreview { get; init; }
        public bool GuaranteeEngineerOffer { get; init; }
    }

    public sealed class ShopState
    {
        public IReadOnlyList<ShopOffer> Offers { get; init; } = System.Array.Empty<ShopOffer>();
        public ShopModifiers Modifiers { get; init; } = new ShopModifiers();
        public int Seed { get; init; }
        public string FrozenOfferId { get; init; }
    }
}
