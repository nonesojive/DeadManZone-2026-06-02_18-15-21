using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TickCombatRunFootprintTests
    {
        [Test]
        public void SpawnFight_MultiCellPiece_HasMultipleOccupiedCells()
        {
            var player = TestBoards.HqOnly();
            var enemy = TestBoards.StandardEnemy();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var hq = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_hq");

            Assert.Greater(hq.OccupiedCells.Count, 1);
            CollectionAssert.AreEquivalent(
                CombatFootprint.ComputeOccupiedCells(hq.AnchorPosition, hq.ShapeOffsets).ToList(),
                hq.OccupiedCells.ToList());
        }

        [Test]
        public void SpawnFight_MultiCellPiece_RegistersAllCellsOnOccupancyGrid()
        {
            var player = TestBoards.HqOnly();
            var enemy = TestBoards.StandardEnemy();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var hq = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_hq");
            var snapshot = run.OccupancySnapshotForTests;

            foreach (var cell in hq.OccupiedCells)
            {
                Assert.IsTrue(snapshot.ContainsKey(cell), $"Expected occupancy at {cell.X},{cell.Y}");
                Assert.AreEqual(hq.InstanceId, snapshot[cell]);
            }
        }

        [Test]
        public void SpawnFight_SingleCellPiece_HasOneOccupiedCell()
        {
            var player = TestBoards.StandardPlayer();
            var enemy = TestBoards.WeakEnemyOnly();
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var rifle = run.PlayerCombatantsForTests.Single(c => c.HasTag(GameTagIds.Combatant));

            Assert.AreEqual(1, rifle.OccupiedCells.Count);
            Assert.AreEqual(rifle.AnchorPosition, rifle.OccupiedCells[0]);
        }

        [Test]
        public void SpawnFight_NonCombatantBuilding_BlocksFootprintCells()
        {
            var player = TestBoards.HqOnly();
            var enemy = TestBoards.WeakEnemyOnly();
            var run = TickCombatRun.Start(player, enemy, seed: 11);

            var hq = run.PlayerCombatantsForTests.Single(c => c.HasTag(GameTagIds.Hq));
            var snapshot = run.OccupancySnapshotForTests;

            Assert.Greater(hq.OccupiedCells.Count, 1, "HQ footprint should occupy multiple cells.");
            foreach (var cell in hq.OccupiedCells)
            {
                Assert.IsTrue(snapshot.ContainsKey(cell), $"Expected HQ occupancy at {cell.X},{cell.Y}");
                Assert.AreEqual(hq.InstanceId, snapshot[cell]);
            }
        }
    }
}
