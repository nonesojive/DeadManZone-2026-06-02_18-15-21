using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Shop
{
    public interface IShopSlotUnlockRegistry
    {
        IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context);
    }

    public sealed class ShopSlotUnlockRegistry : IShopSlotUnlockRegistry
    {
        private readonly IReadOnlyList<IShopSlotUnlockProvider> _providers;

        public static ShopSlotUnlockRegistry Empty { get; } = new(Array.Empty<IShopSlotUnlockProvider>());

        public ShopSlotUnlockRegistry(IReadOnlyList<IShopSlotUnlockProvider> providers)
        {
            _providers = providers ?? Array.Empty<IShopSlotUnlockProvider>();
        }

        public IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context)
        {
            if (_providers.Count == 0)
                return Array.Empty<ShopSlotUnlock>();

            var unlocks = new List<ShopSlotUnlock>();
            for (int i = 0; i < _providers.Count; i++)
            {
                var batch = _providers[i].Evaluate(context);
                if (batch == null)
                    continue;

                for (int j = 0; j < batch.Count; j++)
                    unlocks.Add(batch[j]);
            }

            return unlocks;
        }
    }
}
