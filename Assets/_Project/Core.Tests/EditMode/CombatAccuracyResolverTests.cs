using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatAccuracyResolverTests
    {
        [Test]
        public void InnerRange_KeepsFullAccuracy()
        {
            int effective = CombatAccuracyResolver.GetEffectiveAccuracy(80, distance: 4, maxRange: 8);
            Assert.AreEqual(80, effective);
        }

        [Test]
        public void MaxRange_ReducesAccuracyTowardFloor()
        {
            int effective = CombatAccuracyResolver.GetEffectiveAccuracy(80, distance: 8, maxRange: 8);
            Assert.AreEqual(40, effective);
        }

        [Test]
        public void GrazeBand_WiderAtMaxRange()
        {
            int near = CombatAccuracyResolver.GetGrazeBand(distance: 1, maxRange: 8);
            int far = CombatAccuracyResolver.GetGrazeBand(distance: 8, maxRange: 8);
            Assert.AreEqual(2, near);
            Assert.AreEqual(24, far);
        }

        [Test]
        public void ResolveOutcome_HitGrazeMiss()
        {
            int full = 30;
            var hit = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 50, fullDamage: full);
            var graze = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 85, fullDamage: full);
            var miss = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 96, fullDamage: full);

            Assert.AreEqual(CombatAttackOutcomeKind.Hit, hit.Kind);
            Assert.AreEqual(30, hit.Damage);
            Assert.AreEqual(CombatAttackOutcomeKind.Graze, graze.Kind);
            Assert.AreEqual(10, graze.Damage);
            Assert.AreEqual(CombatAttackOutcomeKind.Miss, miss.Kind);
            Assert.AreEqual(0, miss.Damage);
        }
    }
}
