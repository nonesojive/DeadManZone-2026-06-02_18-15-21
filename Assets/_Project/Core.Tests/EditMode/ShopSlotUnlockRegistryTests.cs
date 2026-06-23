using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopSlotUnlockRegistryTests
    {
        private sealed class TestUnlockProvider : IShopSlotUnlockProvider
        {
            private readonly IReadOnlyList<ShopSlotUnlock> _unlocks;

            public TestUnlockProvider(IReadOnlyList<ShopSlotUnlock> unlocks) => _unlocks = unlocks;

            public IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context) => _unlocks;
        }

        [Test]
        public void UnlockProvider_AddsBonusSlot()
        {
            var config = ShopConfig.CreateDefault();
            var bonusProfile = config.GetBonusProfile(8);
            var registry = new ShopSlotUnlockRegistry(new IShopSlotUnlockProvider[]
            {
                new TestUnlockProvider(new[]
                {
                    new ShopSlotUnlock { SlotIndex = 8, Profile = bonusProfile }
                })
            });

            var context = new ShopUnlockContext
            {
                Board = new BoardState(TestBoards.Layout),
                FactionId = FactionIds.IronVanguard,
                Registry = TestContentRegistry.Create(),
                Modifiers = new ShopModifiers()
            };

            var layout = ShopSlotProfileResolver.ResolveActiveSlots(config, registry, context);

            Assert.AreEqual(9, layout.Count);
            Assert.IsTrue(layout.Any(p => p.SlotIndex == 8));
        }
    }
}
