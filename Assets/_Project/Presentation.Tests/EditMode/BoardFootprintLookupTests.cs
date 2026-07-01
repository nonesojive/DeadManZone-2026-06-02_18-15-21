using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Board;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BoardFootprintLookupTests
    {
        [Test]
        public void TryGetPieceAt_ReturnsPieceForNonAnchorFootprintCell()
        {
            var board = new BoardState(TestBoards.CombatLayout);

            var definition = new PieceDefinition
            {
                Id = "wide_unit",
                DisplayName = "Wide Unit",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[]
                {
                    new GridCoord(0, 0),
                    new GridCoord(1, 0),
                    new GridCoord(0, 1)
                })
            };

            var placeResult = board.TryPlace(definition, new GridCoord(0, 1), "piece_a");
            Assert.IsTrue(placeResult.Success, placeResult.Reason);

            bool found = BoardFootprintLookup.TryGetPieceAt(board, new GridCoord(1, 1), out var piece);

            Assert.IsTrue(found);
            Assert.NotNull(piece);
            Assert.AreEqual("piece_a", piece.InstanceId);
            Assert.AreEqual(new GridCoord(0, 1), piece.Anchor);
        }
    }
}
