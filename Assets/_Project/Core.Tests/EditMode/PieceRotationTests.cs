using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class PieceRotationTests
    {
        [Test]
        public void GetCells_Rotated90_OffsetsSwapped()
        {
            var shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) });
            var cells = shape.GetCells(new GridCoord(3, 4), PieceRotation.R90).ToList();
            Assert.Contains(new GridCoord(3, 4), cells);
            Assert.Contains(new GridCoord(3, 5), cells);
        }
    }
}
