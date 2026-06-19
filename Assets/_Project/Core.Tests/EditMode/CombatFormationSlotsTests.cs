using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatFormationSlotsTests
    {
        private static readonly BattlefieldLayout Layout = new(7, 2, 7, 10);

        [Test]
        public void FrontlineGoal_PreservesDistinctSpawnLanes()
        {
            var a = CreateMover("a_lane3", CombatSide.Player, spawnY: 3, y: 2);
            var b = CreateMover("b_lane7", CombatSide.Player, spawnY: 7, y: 2);
            var enemyFront = CreateEnemy("enemy_front", new GridCoord(10, 5));
            var enemyOther = CreateEnemy("enemy_other", new GridCoord(10, 3));

            var goalA = CombatFormationSlots.ResolveFrontlineGoal(
                a, new[] { a, b }, new[] { enemyOther, enemyFront }, Layout);
            var goalB = CombatFormationSlots.ResolveFrontlineGoal(
                b, new[] { a, b }, new[] { enemyOther, enemyFront }, Layout);

            Assert.AreEqual(new GridCoord(9, 3), goalA);
            Assert.AreEqual(new GridCoord(9, 7), goalB);
        }

        [Test]
        public void FrontlineGoal_SameSpawnY_SecondaryShiftsLane()
        {
            var first = CreateMover("a_first", CombatSide.Player, spawnY: 5, y: 2);
            var second = CreateMover("b_second", CombatSide.Player, spawnY: 5, y: 2);
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));

            var goalFirst = CombatFormationSlots.ResolveFrontlineGoal(
                first, new[] { first, second }, new[] { enemy }, Layout);
            var goalSecond = CombatFormationSlots.ResolveFrontlineGoal(
                second, new[] { first, second }, new[] { enemy }, Layout);

            Assert.AreEqual(new GridCoord(9, 5), goalFirst);
            Assert.AreNotEqual(goalFirst.Y, goalSecond.Y);
            Assert.AreEqual(9, goalSecond.X);
        }

        [Test]
        public void FrontlineGoal_BlockedContactCell_FallsBackToAdjacentY()
        {
            var lead = CreateMover("lead", CombatSide.Player, spawnY: 5, y: 9);
            var follower = CreateMover("follow", CombatSide.Player, spawnY: 5, y: 2);
            follower.AnchorPosition = new GridCoord(9, 5);
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));

            var goal = CombatFormationSlots.ResolveFrontlineGoal(
                lead, new[] { lead, follower }, new[] { enemy }, Layout);

            Assert.AreEqual(new GridCoord(9, 4), goal);
        }

        [Test]
        public void FrontlineGoal_DeterministicForSameInputs()
        {
            var a = CreateMover("a", CombatSide.Player, spawnY: 4, y: 2);
            var b = CreateMover("b", CombatSide.Player, spawnY: 6, y: 2);
            var enemy = CreateEnemy("enemy", new GridCoord(10, 5));
            var allies = new[] { a, b };
            var enemies = new[] { enemy };

            var g1 = CombatFormationSlots.ResolveFrontlineGoal(a, allies, enemies, Layout);
            var g2 = CombatFormationSlots.ResolveFrontlineGoal(a, allies, enemies, Layout);
            Assert.AreEqual(g1, g2);
        }

        [Test]
        public void RearSpreadY_DistinctSlotsAcrossFriendlyWidth()
        {
            int y0 = CombatFormationSlots.ResolveRearSpreadY(0, 2, friendlyMinY: 2, friendlyMaxY: 8);
            int y1 = CombatFormationSlots.ResolveRearSpreadY(1, 2, friendlyMinY: 2, friendlyMaxY: 8);
            Assert.AreEqual(2, y0);
            Assert.AreEqual(8, y1);
        }

        [Test]
        public void RearSpreadY_SingleUnit_Centers()
        {
            int y = CombatFormationSlots.ResolveRearSpreadY(0, 1, friendlyMinY: 2, friendlyMaxY: 8);
            Assert.AreEqual(5, y);
        }

        private static CombatantState CreateMover(string id, CombatSide side, int spawnY, int y)
        {
            var def = TestPieces.With(
                TestPieces.CreateUnit(id, primary: GameTagIds.Infantry, combatRole: GameTagIds.Assault),
                attackRange: AttackRangeTier.Short);
            return new CombatantState
            {
                InstanceId = id,
                Side = side,
                Definition = def,
                CurrentHp = def.MaxHp,
                AnchorPosition = new GridCoord(2, y),
                SpawnAnchorY = spawnY
            };
        }

        private static CombatantState CreateEnemy(string id, GridCoord pos)
        {
            var def = TestPieces.CreateUnit(id, combatRole: GameTagIds.Assault);
            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Enemy,
                Definition = def,
                CurrentHp = def.MaxHp,
                AnchorPosition = pos,
                SpawnAnchorY = pos.Y
            };
        }
    }
}
