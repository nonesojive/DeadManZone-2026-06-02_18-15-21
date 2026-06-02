using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class BoardStateTests
    {
        private static BoardLayout DefaultLayout() =>
            BoardLayout.CreateStandard(
                width: 8,
                height: 6,
                rearRows: 2,
                supportRows: 2,
                specialTiles: new[]
                {
                    new GridCoord(1, 2),
                    new GridCoord(4, 2)
                });

        [Test]
        public void CanPlace_ReturnsFalseForInvalidZone()
        {
            var board = new BoardState(DefaultLayout());
            Assert.IsFalse(board.CanPlace(TestPieces.RifleSquad(), new GridCoord(0, 0)));
        }

        [Test]
        public void CannotPlaceUnitInRearZone()
        {
            var board = new BoardState(DefaultLayout());
            var result = board.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 0));
            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("zone").IgnoreCase);
        }

        [Test]
        public void BuildingOnSpecialTile_FlagsSpecialBonus()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new GridCoord(1, 2)).Success);
            Assert.IsTrue(board.IsOnSpecialTile(board.Pieces.First().InstanceId));
        }

        [Test]
        public void RemovePiece_FreesCellsForPlacement()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            var anchor = new GridCoord(1, 2);
            Assert.IsTrue(board.TryPlace(bunker, anchor, "bunker_1").Success);

            Assert.IsTrue(board.TryRemove("bunker_1", out var removed));
            Assert.AreEqual("bunker_1", removed.InstanceId);
            Assert.IsEmpty(board.Pieces);

            var retry = board.TryPlace(bunker, anchor, "bunker_2");
            Assert.IsTrue(retry.Success, retry.Reason);
        }

        [Test]
        public void TryRelocate_MovesPieceToValidAnchor()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new GridCoord(1, 2), "bunker_1").Success);

            var result = board.TryRelocate("bunker_1", new GridCoord(0, 2));

            Assert.IsTrue(result.Success, result.Reason);
            Assert.AreEqual(1, board.Pieces.Count);
            Assert.AreEqual(new GridCoord(0, 2), board.Pieces.First().Anchor);
        }

        [Test]
        public void TryRelocate_RejectsInvalidZoneAndRestoresPosition()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            var anchor = new GridCoord(1, 2);
            Assert.IsTrue(board.TryPlace(bunker, anchor, "bunker_1").Success);

            var result = board.TryRelocate("bunker_1", new GridCoord(0, 4));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(anchor, board.Pieces.First().Anchor);
        }
    }
}
