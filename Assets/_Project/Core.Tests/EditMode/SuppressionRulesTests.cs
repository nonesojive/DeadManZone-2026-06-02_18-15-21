using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8 Suppression tentpole (Crimson). Mirrors
    /// MovementSlowRulesTests.cs — the precedent for this kind of pure-rules seam.</summary>
    public sealed class SuppressionRulesTests
    {
        private static CombatantState MakeCombatant(int attackSpeedSteps = 0) => new()
        {
            InstanceId = "target",
            Side = CombatSide.Enemy,
            Definition = TestPieces.RifleSquad(),
            AnchorPosition = new GridCoord(5, 5),
            CurrentHp = 100,
            AttackSpeedSteps = attackSpeedSteps
        };

        [Test]
        public void Apply_SetsFullDuration()
        {
            var target = MakeCombatant();
            SuppressionRules.Apply(target);

            Assert.AreEqual(SuppressionRules.SuppressionDurationTicks, target.SuppressionTicksRemaining);
            Assert.IsTrue(target.IsSuppressed);
        }

        [Test]
        public void Apply_ReAppliedMidDuration_RefreshesRatherThanStacks()
        {
            var target = MakeCombatant();
            SuppressionRules.Apply(target);
            SuppressionRules.TickDown(target);
            SuppressionRules.TickDown(target);
            Assert.AreEqual(SuppressionRules.SuppressionDurationTicks - 2, target.SuppressionTicksRemaining);

            SuppressionRules.Apply(target);

            Assert.AreEqual(SuppressionRules.SuppressionDurationTicks, target.SuppressionTicksRemaining,
                "a second on-hit application refreshes the duration, it does not add to it (PROVISIONAL stacking rule)");
        }

        [Test]
        public void TickDown_ReachesZero_NoLongerSuppressed()
        {
            var target = MakeCombatant();
            target.SuppressionTicksRemaining = 1;

            SuppressionRules.TickDown(target);

            Assert.AreEqual(0, target.SuppressionTicksRemaining);
            Assert.IsFalse(target.IsSuppressed);
        }

        [Test]
        public void TickDown_AlreadyZero_StaysAtZero()
        {
            var target = MakeCombatant();

            SuppressionRules.TickDown(target);

            Assert.AreEqual(0, target.SuppressionTicksRemaining);
        }

        [Test]
        public void GetEffectiveAttackSpeedSteps_WhenSuppressed_StepsDownByOne()
        {
            var target = MakeCombatant(attackSpeedSteps: 1);
            SuppressionRules.Apply(target);

            Assert.AreEqual(0, SuppressionRules.GetEffectiveAttackSpeedSteps(target));
        }

        [Test]
        public void GetEffectiveAttackSpeedSteps_WhenNotSuppressed_Unchanged()
        {
            var target = MakeCombatant(attackSpeedSteps: 1);

            Assert.AreEqual(1, SuppressionRules.GetEffectiveAttackSpeedSteps(target));
        }

        [Test]
        public void ApplyMovementSuppression_WhenSuppressed_HalvesCharge()
        {
            Assert.AreEqual(50, SuppressionRules.ApplyMovementSuppression(100, isSuppressed: true));
        }

        [Test]
        public void ApplyMovementSuppression_WhenNotSuppressed_Unchanged()
        {
            Assert.AreEqual(100, SuppressionRules.ApplyMovementSuppression(100, isSuppressed: false));
        }
    }
}
