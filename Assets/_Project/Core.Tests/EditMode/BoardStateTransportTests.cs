using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §2.5 transport tentpole: BoardState.TryLoadCargo,
    /// the Build-phase load step. Purely a data tag (PlacedPiece.CarrierInstanceId) — doesn't
    /// move or re-validate either piece's own board cell.</summary>
    public sealed class BoardStateTransportTests
    {
        private static PieceDefinition Transport(int capacity = 1) => new()
        {
            Id = "armored_ark",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            IsTransport = true,
            TransportCapacity = capacity
        };

        private static PieceDefinition Cargo() => new()
        {
            Id = "truncheon_line",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 40
        };

        [Test]
        public void TryLoadCargo_ValidTransportAndCargo_Succeeds()
        {
            var board = new BoardState(TestBoards.Layout);
            var arkPlacement = board.TryPlace(Transport(), TestBoards.FrontLineAnchor(), "ark_1");
            var cargoPlacement = board.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(arkPlacement.Success, arkPlacement.Reason);
            Assert.IsTrue(cargoPlacement.Success, cargoPlacement.Reason);

            var result = board.TryLoadCargo("cargo_1", "ark_1");

            Assert.IsTrue(result.Success, result.Reason);
            var cargoPiece = System.Linq.Enumerable.Single(board.Pieces, p => p.InstanceId == "cargo_1");
            Assert.AreEqual("ark_1", cargoPiece.CarrierInstanceId);
        }

        [Test]
        public void TryLoadCargo_TransportAtCapacity_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Transport(capacity: 1), TestBoards.FrontLineAnchor(), "ark_1");
            board.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            board.TryPlace(Cargo(), TestBoards.SupportLineAnchor(2, 0), "cargo_2");
            Assert.IsTrue(board.TryLoadCargo("cargo_1", "ark_1").Success);

            var result = board.TryLoadCargo("cargo_2", "ark_1");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Transport is full", result.Reason);
        }

        [Test]
        public void TryLoadCargo_SourceNotATransport_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Cargo(), TestBoards.FrontLineAnchor(), "not_a_transport");
            board.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");

            var result = board.TryLoadCargo("cargo_1", "not_a_transport");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void TryLoadCargo_TransportCannotBeCargo_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Transport(), TestBoards.FrontLineAnchor(), "ark_1");
            board.TryPlace(Transport(), TestBoards.SupportLineAnchor(1, 0), "ark_2");

            var result = board.TryLoadCargo("ark_2", "ark_1");

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void TryLoadCargo_UnknownPieceIds_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            Assert.IsFalse(board.TryLoadCargo("missing_cargo", "missing_ark").Success);
        }
    }
}
