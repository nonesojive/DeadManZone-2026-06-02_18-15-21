using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ShapeTransformsTests
    {
        [TestCase(PieceRotation.R0)]
        [TestCase(PieceRotation.R90)]
        [TestCase(PieceRotation.R180)]
        [TestCase(PieceRotation.R270)]
        public void InverseRotateOffset_UndoRotateOffset(PieceRotation rotation)
        {
            var local = new GridCoord(2, -1);
            var rotated = ShapeTransforms.RotateOffset(local, rotation);
            var restored = ShapeTransforms.InverseRotateOffset(rotated, rotation);
            Assert.AreEqual(local, restored);
        }
    }
}
