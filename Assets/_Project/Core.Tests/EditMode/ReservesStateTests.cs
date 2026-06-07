using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class ReservesStateTests
    {
        private static PieceDefinition SmallPiece() => TestPieces.RifleSquad();

        private static PieceDefinition TallPiece() => new()
        {
            Id = "tall_piece",
            DisplayName = "Tall",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(0, 1) }),
            MaxHp = 1
        };

        [Test]
        public void GridDimensions_Are8By2()
        {
            Assert.AreEqual(8, ReservesState.Width);
            Assert.AreEqual(2, ReservesState.Height);
        }

        [Test]
        public void TryPlace_TwoPieces_NoOverlap_WhenTheyFit()
        {
            var reserves = new ReservesState();
            var small = SmallPiece();

            Assert.IsTrue(reserves.TryPlace(small, new GridCoord(0, 0), "a").Success);
            Assert.IsTrue(reserves.TryPlace(small, new GridCoord(3, 0), "b").Success);
            Assert.AreEqual(2, reserves.Pieces.Count);
        }

        [Test]
        public void TryPlace_Overlap_Fails()
        {
            var reserves = new ReservesState();
            var small = SmallPiece();

            Assert.IsTrue(reserves.TryPlace(small, new GridCoord(0, 0), "a").Success);
            var result = reserves.TryPlace(small, new GridCoord(0, 0), "b");

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("occupied").IgnoreCase);
        }

        [Test]
        public void TryPlace_OutOfBounds_Fails()
        {
            var reserves = new ReservesState();
            var wide = new PieceDefinition
            {
                Id = "wide",
                DisplayName = "Wide",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(7, 0) }),
                MaxHp = 1
            };

            var result = reserves.TryPlace(wide, new GridCoord(1, 0));

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("bounds").IgnoreCase);
        }

        [Test]
        public void TryPlace_ExceedsHeight_Fails()
        {
            var reserves = new ReservesState();
            var tall = TallPiece();

            var result = reserves.TryPlace(tall, new GridCoord(0, 1));

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("bounds").IgnoreCase);
        }

        [Test]
        public void TryRelocate_MovesPieceAndPreservesRotation()
        {
            var reserves = new ReservesState();
            var unit = new PieceDefinition
            {
                Id = "wide_unit",
                DisplayName = "Wide Unit",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
                MaxHp = 10
            };

            Assert.IsTrue(reserves.TryPlace(unit, new GridCoord(0, 0), "unit_1", PieceRotation.R90).Success);

            var result = reserves.TryRelocate("unit_1", new GridCoord(5, 0));

            Assert.IsTrue(result.Success, result.Reason);
            var piece = reserves.Pieces.First();
            Assert.AreEqual(new GridCoord(5, 0), piece.Anchor);
            Assert.AreEqual(PieceRotation.R90, piece.Rotation);
        }
    }
}
