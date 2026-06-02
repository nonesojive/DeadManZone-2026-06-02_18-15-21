using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class PieceShapeTests
    {
        [Test]
        public void GetCells_ReturnsAnchorPlusOffsets()
        {
            var shape = new Board.PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) });
            var cells = new System.Collections.Generic.List<GridCoord>(shape.GetCells(new GridCoord(2, 3)));
            CollectionAssert.AreEquivalent(
                new[] { new GridCoord(2, 3), new GridCoord(3, 3) },
                cells);
        }
    }
}
