using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-17 Oathborn transport tentpole: BoardSnapshot must round-trip
    /// PlacedPiece.CarrierInstanceId (build-phase cargo tags), or a save/load mid-Build would
    /// silently drop every Ark's loaded cargo. Mirrors BoardStateTransportTests' fixtures.</summary>
    public sealed class BoardSnapshotMapperTransportTests
    {
        private static PieceDefinition Transport() => new()
        {
            Id = "armored_ark",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new Common.GridCoord(0, 0) }),
            MaxHp = 100,
            IsTransport = true,
            TransportCapacity = 2
        };

        private static PieceDefinition Cargo() => new()
        {
            Id = "truncheon_line",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new Common.GridCoord(0, 0) }),
            MaxHp = 40
        };

        private static ContentRegistry BuildRegistry()
        {
            var registry = new ContentRegistry();
            registry.Register(Transport(), ShopLane.Offensive);
            registry.Register(Cargo(), ShopLane.Offensive);
            return registry;
        }

        [Test]
        public void FromBoard_ToBoard_RoundTripsCarrierInstanceId()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Transport(), TestBoards.FrontLineAnchor(), "ark_1");
            board.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(board.TryLoadCargo("cargo_1", "ark_1").Success);

            var snapshot = BoardSnapshotMapper.FromBoard(board);
            var restored = BoardSnapshotMapper.ToBoard(snapshot, BuildRegistry());

            var cargoPiece = System.Linq.Enumerable.Single(restored.Pieces, p => p.InstanceId == "cargo_1");
            Assert.AreEqual("ark_1", cargoPiece.CarrierInstanceId);
        }

        [Test]
        public void FromBoard_UnloadedPiece_CarrierInstanceIdIsNull()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Cargo(), TestBoards.FrontLineAnchor(), "loose_1");

            var snapshot = BoardSnapshotMapper.FromBoard(board);

            Assert.IsNull(snapshot.Pieces[0].CarrierInstanceId);
        }
    }
}
