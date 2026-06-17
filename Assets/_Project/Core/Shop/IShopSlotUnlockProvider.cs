using System.Collections.Generic;

namespace DeadManZone.Core.Shop
{
    public interface IShopSlotUnlockProvider
    {
        IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context);
    }
}
