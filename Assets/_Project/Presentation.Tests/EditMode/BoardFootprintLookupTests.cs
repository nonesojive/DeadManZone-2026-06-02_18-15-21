using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BoardFootprintLookupTests
    {
        [Test]
        public void TryGetPieceAt_ReturnsPieceForNonAnchorFootprintCell()
        {
            var layout = BoardLayout.CreateHorizontalZones(
                width: 6,
                height: 4,
                rearCols: 2,
                supportCols: 1,
                specialTiles: System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);

            var definition = new PieceDefinition
            {
                Id = "command_center",
                DisplayName = "Command Center",
                Category = PieceCategory.Building,
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
