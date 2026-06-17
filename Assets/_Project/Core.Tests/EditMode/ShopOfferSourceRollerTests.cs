using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopOfferSourceRollerTests
    {
        [Test]
        public void Roll_DefaultWeights_RespectsNeutralBand()
        {
            var weights = ShopOfferWeights.Default;
            int neutralHits = 0;

            for (int seed = 0; seed < 1000; seed++)
            {
                if (ShopOfferSourceRoller.Roll(weights, new Rng(seed)) == ShopOfferSource.Neutral)
                    neutralHits++;
            }

            Assert.That(neutralHits, Is.InRange(70, 130));
        }
    }
}
