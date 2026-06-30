using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class BoardSnapshotMapperHqTests
    {
        [Test]
        public void ToBoard_HqSnapshot_KeepsHqKindAndRestoresBuilding()
        {
            var building = TestPieces.CommandBunker();
            var registry = new ContentRegistry();
            registry.Register(building, ShopLane.Defensive);
            var snapshot = new BoardSnapshot
            {
                BoardKind = BoardKind.Hq.ToString(),
                Width = 3,
                Height = 6,
                Pieces =
                {
                    new PlacedPieceRecord
                    {
                        InstanceId = "bunker_1",
                        PieceId = building.Id,
                        AnchorX = 1,
                        AnchorY = 2
                    }
                }
            };

            var board = BoardSnapshotMapper.ToBoard(snapshot, registry);

            Assert.AreEqual(BoardKind.Hq, board.Layout.Kind);
            Assert.AreEqual(3, board.Layout.Width);
            Assert.AreEqual(6, board.Layout.Height);
            Assert.AreEqual(1, board.Pieces.Count);
        }
    }
}
