using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TickCombatRunFootprintTests
    {
        [Test]
        public void SpawnFight_MultiCellPiece_HasMultipleOccupiedCells()
        {
            var player = TestBoards.BuildingBoardWithCommandBunker();
            var enemy = TestBoards.StandardEnemy();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var blocker = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_blocker");

            Assert.Greater(blocker.OccupiedCells.Count, 1);
            CollectionAssert.AreEquivalent(
                CombatFootprint.ComputeOccupiedCells(blocker.AnchorPosition, blocker.ShapeOffsets).ToList(),
                blocker.OccupiedCells.ToList());
        }

        [Test]
        public void SpawnFight_MultiCellPiece_RegistersAllCellsOnOccupancyGrid()
        {
            var player = TestBoards.BuildingBoardWithCommandBunker();
            var enemy = TestBoards.StandardEnemy();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var blocker = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_blocker");
            var snapshot = run.OccupancySnapshotForTests;

            foreach (var cell in blocker.OccupiedCells)
            {
                Assert.IsTrue(snapshot.ContainsKey(cell), $"Expected occupancy at {cell.X},{cell.Y}");
                Assert.AreEqual(blocker.InstanceId, snapshot[cell]);
            }
        }

        [Test]
        public void SpawnFight_SingleCellPiece_HasOneOccupiedCell()
        {
            var player = TestBoards.StandardPlayer();
            var enemy = TestBoards.WeakEnemyOnly();
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var rifle = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_rifle_1");

            Assert.AreEqual(1, rifle.OccupiedCells.Count);
            Assert.AreEqual(rifle.AnchorPosition, rifle.OccupiedCells[0]);
        }

        [Test]
        public void SpawnFight_NonCombatantBuilding_BlocksFootprintCells()
        {
            var player = TestBoards.BuildingBoardWithCommandBunker();
            var enemy = TestBoards.WeakEnemyOnly();
            var run = TickCombatRun.Start(player, enemy, seed: 11);

            var bunker = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "bunker_test");
            var snapshot = run.OccupancySnapshotForTests;

            Assert.Greater(bunker.OccupiedCells.Count, 1, "Building footprint should occupy multiple cells.");
            foreach (var cell in bunker.OccupiedCells)
            {
                Assert.IsTrue(snapshot.ContainsKey(cell), $"Expected building occupancy at {cell.X},{cell.Y}");
                Assert.AreEqual(bunker.InstanceId, snapshot[cell]);
            }
        }

        [Test]
        public void DestroyedUnit_ReleasesOccupancyGrid()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "player_rifle");
            player.TryPlace(TestPieces.RifleSquad(), new GridCoord(7, 4), "player_rifle_2");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), TestBoards.FrontLineAnchor(), "enemy_weak");

            var run = TickCombatRun.Start(player, enemy, seed: 7);
            run.Continue(System.Array.Empty<PhaseCommand>());
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.IsFightOver);
            var snapshot = run.OccupancySnapshotForTests;
            Assert.IsFalse(snapshot.Values.Contains("enemy_weak"));
        }
    }
}
