using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ShopPoolFilterTests
    {
        [Test]
        public void DefaultOfferWeights_FavorFactionSource()
        {
            var rng = new Rng(123);
            int factionRolls = 0;
            for (int i = 0; i < 100; i++)
            {
                if (ShopOfferSourceRoller.Roll(ShopOfferWeights.Default, rng) == ShopOfferSource.Faction)
                    factionRolls++;
            }

            Assert.Greater(factionRolls, 70);
        }
    }
}
