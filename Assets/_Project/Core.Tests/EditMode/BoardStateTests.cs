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
    }
}
