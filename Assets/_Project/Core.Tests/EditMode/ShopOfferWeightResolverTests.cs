using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopOfferWeightResolverTests
    {
        private static ShopSlotProfile DefaultProfile => new()
        {
            SlotIndex = 0,
            BaseWeights = ShopOfferWeights.Default
        };

        [Test]
        public void Resolve_DefaultProfile_ReturnsTenEightyTen()
        {
            var weights = ShopOfferWeightResolver.Resolve(DefaultProfile, computedSalvagePercent: 10, hasEnemyFaction: true);

            Assert.AreEqual(10, weights.NeutralPercent);
            Assert.AreEqual(80, weights.FactionPercent);
            Assert.AreEqual(10, weights.SalvagePercent);
        }

        [Test]
        public void Resolve_SalvageTwentyFive_FactionAbsorbsRemainder()
        {
            var weights = ShopOfferWeightResolver.Resolve(DefaultProfile, computedSalvagePercent: 25, hasEnemyFaction: true);

            Assert.AreEqual(10, weights.NeutralPercent);
            Assert.AreEqual(25, weights.SalvagePercent);
            Assert.AreEqual(65, weights.FactionPercent);
        }

        [Test]
        public void Resolve_NoEnemyFaction_SalvageZero()
        {
            var weights = ShopOfferWeightResolver.Resolve(DefaultProfile, computedSalvagePercent: 25, hasEnemyFaction: false);

            Assert.AreEqual(0, weights.SalvagePercent);
            Assert.AreEqual(90, weights.FactionPercent);
        }

        [Test]
        public void WithSalvageLeg_PatchIncreasesSalvage()
        {
            var weights = ShopOfferWeights.Default
                .WithSalvageLeg(20, hasEnemyFaction: true)
                .ApplyPatch(new ShopOfferWeightPatch { SalvageDelta = 5 });

            Assert.AreEqual(25, weights.SalvagePercent);
            Assert.AreEqual(65, weights.FactionPercent);
        }
    }
}
