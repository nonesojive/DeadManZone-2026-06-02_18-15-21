using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ShapePathfinderTests
    {
        private static readonly IReadOnlyList<GridCoord> SingleCellOffsets =
            CombatFootprint.ComputeOffsets(TestPieces.RifleSquad().Shape, rotation: 0);

        private static readonly IReadOnlyList<GridCoord> TwoCellOffsets =
            CombatFootprint.ComputeOffsets(TestPieces.FieldingHq().Shape, rotation: 0);

        [Test]
        public void FindStep_RoutesAroundBlockingAlly()
        {
            var layout = new BattlefieldLayout(
                playerHalfWidth: 7,
                neutralWidth: 0,
                enemyHalfWidth: 7,
                height: 10);
            var occupancy = new CombatOccupancyGrid();

            occupancy.Place("blocker", new GridCoord(3, 5), SingleCellOffsets);

            var current = new GridCoord(2, 5);
            var goal = new GridCoord(6, 5);

            var step = ShapePathfinder.FindStep(
                current,
                goal,
                SingleCellOffsets,
                moverInstanceId: "mover",
                occupancy,
                layout);

            Assert.IsNotNull(step);
            Assert.AreNotEqual(new GridCoord(3, 5), step.Value, "Should not step into blocking ally cell.");
            Assert.AreEqual(new GridCoord(2, 4), step.Value, "Deterministic detour prefers lower Y on equal heuristic.");
        }

        [Test]
        public void FindStep_ReturnsNullWhenNoValidPlacement()
        {
            var layout = new BattlefieldLayout(
                playerHalfWidth: 9,
                neutralWidth: 0,
                enemyHalfWidth: 9,
                height: 10);
            var occupancy = new CombatOccupancyGrid();
            var anchor = new GridCoord(5, 5);

            occupancy.Place("mover", anchor, TwoCellOffsets);
            occupancy.Place("block_w", new GridCoord(4, 5), SingleCellOffsets);
            occupancy.Place("block_e", new GridCoord(7, 5), SingleCellOffsets);
            occupancy.Place("block_nw", new GridCoord(5, 4), SingleCellOffsets);
            occupancy.Place("block_ne", new GridCoord(6, 4), SingleCellOffsets);
            occupancy.Place("block_sw", new GridCoord(5, 6), SingleCellOffsets);
            occupancy.Place("block_se", new GridCoord(6, 6), SingleCellOffsets);

            var step = ShapePathfinder.FindStep(
                anchor,
                goalAnchor: new GridCoord(8, 5),
                TwoCellOffsets,
                moverInstanceId: "mover",
                occupancy,
                layout);

            Assert.IsNull(step);
        }

        [Test]
        public void FindStep_SameInputs_ReturnsIdenticalStep()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var occupancy = new CombatOccupancyGrid();
            occupancy.Place("ally_a", new GridCoord(4, 5), SingleCellOffsets);
            occupancy.Place("ally_b", new GridCoord(4, 6), SingleCellOffsets);

            var current = new GridCoord(3, 5);
            var goal = new GridCoord(7, 5);

            var first = ShapePathfinder.FindStep(
                current,
                goal,
                SingleCellOffsets,
                "mover",
                occupancy,
                layout);
            var second = ShapePathfinder.FindStep(
                current,
                goal,
                SingleCellOffsets,
                "mover",
                occupancy,
                layout);

            Assert.AreEqual(first, second);
            Assert.IsNotNull(first);
        }
    }
}
