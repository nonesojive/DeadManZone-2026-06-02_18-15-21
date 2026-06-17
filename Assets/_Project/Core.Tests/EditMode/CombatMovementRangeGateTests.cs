using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatMovementRangeGateTests
    {
        [Test]
        public void ShouldAttemptMove_IsTrueWhenInRangeButNotAtEngagementGoal()
        {
            var mover = Combatant("mover", new GridCoord(5, 5), AttackRangeTier.Medium);
            var enemy = Combatant("enemy", new GridCoord(7, 5), AttackRangeTier.Medium);
            var goal = new GridCoord(7, 5);

            Assert.IsTrue(CombatMovementRules.ShouldAttemptMove(mover, new[] { enemy }, goal));
        }

        [Test]
        public void ShouldAttemptMove_IsFalseWhenAlreadyAtEngagementGoal()
        {
            var goal = new GridCoord(7, 5);
            var mover = Combatant("mover", goal, AttackRangeTier.Medium);
            var enemy = Combatant("enemy", new GridCoord(8, 5), AttackRangeTier.Medium);

            Assert.IsFalse(CombatMovementRules.ShouldAttemptMove(mover, new[] { enemy }, goal));
        }

        [Test]
        public void ShouldAttemptMove_IsTrueWhenEnemyOutOfRangeAndNotAtGoal()
        {
            var mover = Combatant("mover", new GridCoord(4, 5), AttackRangeTier.Short);
            var enemy = Combatant("enemy", new GridCoord(20, 5), AttackRangeTier.Short);
            var goal = new GridCoord(19, 5);

            Assert.IsTrue(CombatMovementRules.ShouldAttemptMove(mover, new[] { enemy }, goal));
        }

        [Test]
        public void ShouldAttemptMove_IsFalseForStaticUnits()
        {
            var mover = Combatant("hq", new GridCoord(0, 4), AttackRangeTier.Short);
            mover = new CombatantState
            {
                InstanceId = mover.InstanceId,
                Side = mover.Side,
                Definition = TestPieces.With(mover.Definition, movementSpeed: MovementSpeedTier.None),
                CurrentHp = mover.CurrentHp,
                AnchorPosition = mover.AnchorPosition
            };

            Assert.IsFalse(CombatMovementRules.ShouldAttemptMove(
                mover,
                new List<CombatantState>(),
                new GridCoord(5, 4)));
        }

        private static CombatantState Combatant(string id, GridCoord position, AttackRangeTier range)
        {
            var definition = TestPieces.With(
                TestPieces.RifleSquad(),
                attackRange: range,
                movementSpeed: MovementSpeedTier.Medium);

            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Player,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                AnchorPosition = position
            };
        }
    }
}
