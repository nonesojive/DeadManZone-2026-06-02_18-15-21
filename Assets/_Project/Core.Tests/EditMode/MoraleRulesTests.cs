using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1: MoraleRules.ApplyResistance is the seam Iron
    /// Guard's own stat and Breakthrough Tank's aura both consume.</summary>
    public sealed class MoraleRulesTests
    {
        [Test]
        public void ApplyResistance_NoResistance_ReturnsRawDamage()
        {
            Assert.AreEqual(10, MoraleRules.ApplyResistance(10, 0));
        }

        [Test]
        public void ApplyResistance_PartialResistance_ReducesProportionally()
        {
            // Iron Guard PROVISIONAL: 40% resistance.
            Assert.AreEqual(6, MoraleRules.ApplyResistance(10, 40));
        }

        [Test]
        public void ApplyResistance_StackedResistance_ReducesFurther()
        {
            // Iron Guard (40%) standing under Breakthrough Tank's aura (+25%) = 65%.
            Assert.AreEqual(3, MoraleRules.ApplyResistance(10, 65));
        }

        [Test]
        public void ApplyResistance_ClampsAbove100_NeverNegative()
        {
            Assert.AreEqual(0, MoraleRules.ApplyResistance(10, 150));
        }

        [Test]
        public void ApplyResistance_ClampsBelowZero_TreatedAsNoResistance()
        {
            Assert.AreEqual(10, MoraleRules.ApplyResistance(10, -20));
        }

        [Test]
        public void ApplyResistance_ZeroOrNegativeRawDamage_ReturnsZero()
        {
            Assert.AreEqual(0, MoraleRules.ApplyResistance(0, 50));
            Assert.AreEqual(0, MoraleRules.ApplyResistance(-5, 50));
        }
    }
}
