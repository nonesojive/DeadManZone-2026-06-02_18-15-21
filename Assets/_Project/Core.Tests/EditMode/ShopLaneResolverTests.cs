using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class ShopLaneResolverTests
    {
        [TestCase(GameTagIds.Assault, ShopLane.Offensive)]
        [TestCase(GameTagIds.Sniper, ShopLane.Offensive)]
        [TestCase(GameTagIds.Tank, ShopLane.Offensive)]
        [TestCase(GameTagIds.Support, ShopLane.Defensive)]
        [TestCase(GameTagIds.Utility, ShopLane.Defensive)]
        [TestCase(GameTagIds.Defender, ShopLane.Defensive)]
        [TestCase(GameTagIds.Artillery, ShopLane.Specialty)]
        public void Resolve_MapsCombatRoleToLane(string combatRole, ShopLane expectedLane)
        {
            Assert.AreEqual(expectedLane, ShopLaneResolver.ResolveLane(combatRole));
        }

        [Test]
        public void Resolve_EmptyRole_FallsBackToOffensive()
        {
            var result = ShopLaneResolver.Resolve(string.Empty);
            Assert.AreEqual(ShopLane.Offensive, result.Lane);
            Assert.AreEqual(ShopLaneResolveConfidence.UnknownRoleFallback, result.Confidence);
        }

        [Test]
        public void Resolve_Artillery_ReportsSpecialtyPendingRules()
        {
            var result = ShopLaneResolver.Resolve(GameTagIds.Artillery);
            Assert.AreEqual(ShopLane.Specialty, result.Lane);
            Assert.AreEqual(ShopLaneResolveConfidence.SpecialtyPendingRules, result.Confidence);
        }
    }
}
