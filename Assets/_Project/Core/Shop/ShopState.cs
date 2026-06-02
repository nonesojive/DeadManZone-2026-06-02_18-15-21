using System.Collections.Generic;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopModifiers
    {
        public int GoldDiscountPercent { get; set; }
        public int ExtraGeneralSlots { get; set; }
        public bool EnemyTagPreview { get; set; }
        public bool GuaranteeEngineerOffer { get; set; }
    }

    public sealed class ShopState
    {
        public List<ShopOffer> Offers { get; set; } = new();
        public ShopModifiers Modifiers { get; set; } = new ShopModifiers();
        public int Seed { get; set; }
        public string FrozenOfferId { get; set; }
    }
}
