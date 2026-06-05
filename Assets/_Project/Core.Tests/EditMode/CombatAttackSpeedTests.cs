using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatAttackSpeedTests
    {
        [Test]
        public void Slow_IncreasesCooldown()
        {
            Assert.AreEqual(5, CombatAttackSpeed.GetEffectiveCooldown(3, AttackSpeedTier.Slow));
        }

        [Test]
        public void Fast_DecreasesCooldown_MinOne()
        {
            Assert.AreEqual(2, CombatAttackSpeed.GetEffectiveCooldown(3, AttackSpeedTier.Fast));
            Assert.AreEqual(1, CombatAttackSpeed.GetEffectiveCooldown(1, AttackSpeedTier.Fast));
        }

        [Test]
        public void Medium_KeepsBaseCooldown()
        {
            Assert.AreEqual(4, CombatAttackSpeed.GetEffectiveCooldown(4, AttackSpeedTier.Medium));
        }
    }
}
