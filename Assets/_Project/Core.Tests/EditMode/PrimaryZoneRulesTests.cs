using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PrimaryZoneRulesTests
    {
        [Test]
        public void BuildingPrimary_CannotPlaceInFrontZone()
        {
            var board = new BoardState(TestBoards.Layout);
            var piece = TestPieces.CreateUnit("building_primary_piece", primary: GameTagIds.Building);

            var result = board.TryPlace(piece, TestBoards.FrontLineAnchor());

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("zone").IgnoreCase);
        }

        [Test]
        public void InfantryPrimary_CanPlaceInFrontZone()
        {
            var board = new BoardState(TestBoards.Layout);
            var piece = TestPieces.CreateUnit("infantry_primary_piece", primary: GameTagIds.Infantry);

            var result = board.TryPlace(piece, TestBoards.FrontLineAnchor());

            Assert.IsTrue(result.Success, result.Reason);
        }

        [Test]
        public void EmptyPrimary_DoesNotBlockPlacement()
        {
            var board = new BoardState(TestBoards.Layout);
            var piece = TestPieces.CreateUnit("empty_primary_piece", primary: null);

            var result = board.TryPlace(piece, TestBoards.FrontLineAnchor(y: 4));

            Assert.IsTrue(result.Success, result.Reason);
        }
    }
}
