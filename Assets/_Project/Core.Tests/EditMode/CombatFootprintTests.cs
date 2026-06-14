using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatFootprintTests
    {
        [Test]
        public void ComputeOffsets_OneByOneShape_ReturnsSingleOriginOffset()
        {
            var shape = new PieceShape(new[] { new GridCoord(0, 0) });
            var offsets = CombatFootprint.ComputeOffsets(shape, rotation: 0);

            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(new GridCoord(0, 0), offsets[0]);
        }

        [Test]
        public void ComputeOccupiedCells_HorizontalTwoCellAtAnchor_ReturnsExpectedCells()
        {
            var shape = TestPieces.FieldingHq().Shape;
            var offsets = CombatFootprint.ComputeOffsets(shape, rotation: 0);
            var anchor = new GridCoord(3, 4);
            var occupied = CombatFootprint.ComputeOccupiedCells(anchor, offsets);

            CollectionAssert.AreEquivalent(
                shape.GetCells(anchor, PieceRotation.R0).ToList(),
                occupied.ToList());
            CollectionAssert.AreEquivalent(
                new[] { new GridCoord(3, 4), new GridCoord(4, 4) },
                occupied.ToList());
        }

        [Test]
        public void OccupancyGrid_PlaceTwoNonOverlappingPieces_Succeeds()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var grid = new CombatOccupancyGrid();
            var rifleOffsets = CombatFootprint.ComputeOffsets(TestPieces.RifleSquad().Shape, rotation: 0);
            var hqOffsets = CombatFootprint.ComputeOffsets(TestPieces.FieldingHq().Shape, rotation: 0);

            Assert.IsTrue(grid.CanPlace("rifle_1", new GridCoord(1, 1), rifleOffsets, layout));
            grid.Place("rifle_1", new GridCoord(1, 1), rifleOffsets);

            Assert.IsTrue(grid.CanPlace("hq_1", new GridCoord(3, 1), hqOffsets, layout));
            grid.Place("hq_1", new GridCoord(3, 1), hqOffsets);

            Assert.IsTrue(grid.IsOccupied(new GridCoord(1, 1)));
            Assert.IsTrue(grid.IsOccupied(new GridCoord(3, 1)));
            Assert.IsTrue(grid.IsOccupied(new GridCoord(4, 1)));
        }

        [Test]
        public void OccupancyGrid_OverlappingPlacement_IsRejected()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var grid = new CombatOccupancyGrid();
            var hqOffsets = CombatFootprint.ComputeOffsets(TestPieces.FieldingHq().Shape, rotation: 0);

            grid.Place("hq_1", new GridCoord(3, 4), hqOffsets);

            Assert.IsFalse(grid.CanPlace("hq_2", new GridCoord(4, 4), hqOffsets, layout));
        }

        [Test]
        public void OccupancyGrid_Remove_FreesOccupiedCells()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var grid = new CombatOccupancyGrid();
            var hqOffsets = CombatFootprint.ComputeOffsets(TestPieces.FieldingHq().Shape, rotation: 0);
            var anchor = new GridCoord(3, 4);

            grid.Place("hq_1", anchor, hqOffsets);
            Assert.IsTrue(grid.IsOccupied(new GridCoord(3, 4)));
            Assert.IsTrue(grid.IsOccupied(new GridCoord(4, 4)));

            grid.Remove("hq_1");

            Assert.IsFalse(grid.IsOccupied(new GridCoord(3, 4)));
            Assert.IsFalse(grid.IsOccupied(new GridCoord(4, 4)));
            Assert.IsTrue(grid.CanPlace("hq_2", anchor, hqOffsets, layout));
        }

        [Test]
        public void CombatantState_RecomputeOccupiedCells_UsesAnchorAndOffsets()
        {
            var offsets = CombatFootprint.ComputeOffsets(TestPieces.FieldingHq().Shape, rotation: 0);
            var combatant = new CombatantState
            {
                InstanceId = "hq_1",
                Definition = TestPieces.FieldingHq(),
                AnchorPosition = new GridCoord(3, 4),
                ShapeOffsets = offsets
            };

            combatant.RecomputeOccupiedCells();

            CollectionAssert.AreEquivalent(
                new[] { new GridCoord(3, 4), new GridCoord(4, 4) },
                combatant.OccupiedCells.ToList());
        }

        [Test]
        public void CombatantState_AnchorPosition_UpdatesOccupiedFootprint()
        {
            var combatant = new CombatantState
            {
                InstanceId = "rifle_1",
                Definition = TestPieces.RifleSquad(),
                AnchorPosition = new GridCoord(2, 5)
            };

            Assert.AreEqual(new GridCoord(2, 5), combatant.AnchorPosition);

            combatant.AnchorPosition = new GridCoord(4, 6);
            Assert.AreEqual(new GridCoord(4, 6), combatant.AnchorPosition);
        }
    }
}
