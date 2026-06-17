using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatPresentationEngagementTests
    {
        private static readonly BattlefieldLayout Layout = new(7, 2, 7, 10);

        [Test]
        public void ComputeGoal_MatchesRoleEngagement_ForAssaultClosingFront()
        {
            var assault = CreateCombatant("assault", GameTagIds.Assault, CombatSide.Player, new GridCoord(2, 5));
            var frontEnemy = CreateEnemy("enemy_front", new GridCoord(10, 5));
            var rearEnemy = CreateEnemy("enemy_rear", new GridCoord(12, 5));

            var expected = RoleEngagement.ComputeGoal(
                assault,
                new[] { assault },
                new[] { rearEnemy, frontEnemy },
                Layout);

            var cells = new[]
            {
                ToCell(assault),
                ToCell(frontEnemy),
                ToCell(rearEnemy)
            };
            var anchors = new Dictionary<string, GridCoord>
            {
                [assault.InstanceId] = assault.AnchorPosition,
                [frontEnemy.InstanceId] = frontEnemy.AnchorPosition,
                [rearEnemy.InstanceId] = rearEnemy.AnchorPosition
            };

            var actual = CombatPresentationEngagement.ComputeGoal(
                cells[0],
                anchors[assault.InstanceId],
                cells,
                anchors,
                Layout);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldChase_ReturnsFalseWhenAnchorEqualsGoal()
        {
            var assault = CreateCombatant("assault", GameTagIds.Assault, CombatSide.Player, new GridCoord(2, 5));
            var enemy = CreateEnemy("enemy_front", new GridCoord(10, 5));
            var goal = RoleEngagement.ComputeGoal(
                assault,
                new[] { assault },
                new[] { enemy },
                Layout);
            assault.AnchorPosition = goal;

            var cells = new[] { ToCell(assault), ToCell(enemy) };
            var anchors = new Dictionary<string, GridCoord>
            {
                [assault.InstanceId] = goal,
                [enemy.InstanceId] = enemy.AnchorPosition
            };

            Assert.IsFalse(CombatPresentationEngagement.ShouldChase(
                cells[0],
                anchors[assault.InstanceId],
                cells,
                anchors,
                Layout));
        }

        [Test]
        public void ComputeChaseAnchor_CapsLead_WhenEngagementGoalIsFarAway()
        {
            var assault = CreateCombatant("assault", GameTagIds.Assault, CombatSide.Player, new GridCoord(2, 5));
            var enemy = CreateEnemy("enemy_front", new GridCoord(10, 5));

            var cells = new[] { ToCell(assault), ToCell(enemy) };
            var anchors = new Dictionary<string, GridCoord>
            {
                [assault.InstanceId] = assault.AnchorPosition,
                [enemy.InstanceId] = enemy.AnchorPosition
            };

            var chase = CombatPresentationEngagement.ComputeChaseAnchor(
                cells[0],
                anchors[assault.InstanceId],
                cells,
                anchors,
                Layout,
                maxLeadCells: 2f);

            Assert.That(
                CombatRange.Manhattan(assault.AnchorPosition, chase),
                Is.LessThanOrEqualTo(2));
            Assert.That(chase, Is.Not.EqualTo(new GridCoord(10, 5)));
        }

        [Test]
        public void ShouldChase_ReturnsFalseWhenEnemyInAttackRange()
        {
            var assault = CreateCombatant("assault", GameTagIds.Assault, CombatSide.Player, new GridCoord(8, 5));
            var enemy = CreateEnemy("enemy_front", new GridCoord(10, 5));

            var cells = new[] { ToCell(assault), ToCell(enemy) };
            var anchors = new Dictionary<string, GridCoord>
            {
                [assault.InstanceId] = assault.AnchorPosition,
                [enemy.InstanceId] = enemy.AnchorPosition
            };

            Assert.IsFalse(CombatPresentationEngagement.ShouldChase(
                cells[0],
                anchors[assault.InstanceId],
                cells,
                anchors,
                Layout));
        }

        private static BattlefieldCell ToCell(CombatantState combatant) =>
            new()
            {
                InstanceId = combatant.InstanceId,
                Definition = combatant.Definition,
                Side = combatant.Side,
                Position = combatant.AnchorPosition
            };

        private static CombatantState CreateCombatant(
            string id,
            string combatRole,
            CombatSide side,
            GridCoord position,
            string primary = null)
        {
            var definition = TestPieces.CreateUnit(id, combatRole: combatRole, primary: primary);
            return new CombatantState
            {
                InstanceId = id,
                Side = side,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                AnchorPosition = position,
                SpawnAnchorY = position.Y
            };
        }

        private static CombatantState CreateEnemy(string id, GridCoord position) =>
            CreateCombatant(id, GameTagIds.Assault, CombatSide.Enemy, position);
    }
}
