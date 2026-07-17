using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-17 round-2 playtest fix: BoardState.TryLoadCargo, the Build-phase load
    /// step. The cargo hold is a real 2x2 mini board (BoardState.CargoGridWidth/Height) — a
    /// piece is only tagged as cargo (PlacedPiece.CarrierInstanceId) if its own footprint
    /// actually fits alongside whatever else is already loaded. Doesn't move or re-validate
    /// either piece's own MAIN board cell — only the cargo hold's own coordinate space.</summary>
    public sealed class BoardStateTransportTests
    {
        private static PieceDefinition Transport() => new()
        {
            Id = "armored_ark",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            IsTransport = true,
            TransportCapacity = 4
        };

        private static PieceDefinition Cargo1Cell(string id = "single_cell") => new()
        {
            Id = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 40
        };

        /// <summary>2-cell horizontal domino, like Truncheon Line.</summary>
        private static PieceDefinition Cargo2Cell(string id = "truncheon_line") => new()
        {
            Id = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            MaxHp = 40
        };

        /// <summary>3-cell L, like Pilgrim Spears.</summary>
        private static PieceDefinition Cargo3CellL(string id = "pilgrim_spears") => new()
        {
            Id = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(0, 1) }),
            MaxHp = 40
        };

        private static BoardState BuildBoardWithArk(out string arkId)
        {
            var board = new BoardState(TestBoards.Layout);
            arkId = "ark_1";
            var arkPlacement = board.TryPlace(Transport(), TestBoards.FrontLineAnchor(), arkId);
            Assert.IsTrue(arkPlacement.Success, arkPlacement.Reason);
            return board;
        }

        [Test]
        public void TryLoadCargo_ValidTransportAndCargo_Succeeds()
        {
            var board = BuildBoardWithArk(out var arkId);
            var cargoPlacement = board.TryPlace(Cargo1Cell(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(cargoPlacement.Success, cargoPlacement.Reason);

            var result = board.TryLoadCargo("cargo_1", arkId);

            Assert.IsTrue(result.Success, result.Reason);
            var cargoPiece = System.Linq.Enumerable.Single(board.Pieces, p => p.InstanceId == "cargo_1");
            Assert.AreEqual(arkId, cargoPiece.CarrierInstanceId);
            Assert.IsTrue(cargoPiece.CargoAnchor.HasValue);
        }

        [Test]
        public void TryLoadCargo_TwoTwoCellPieces_TileTheHold()
        {
            var board = BuildBoardWithArk(out var arkId);
            board.TryPlace(Cargo2Cell("cargo_a"), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            board.TryPlace(Cargo2Cell("cargo_b"), TestBoards.SupportLineAnchor(1, 1), "cargo_2");

            Assert.IsTrue(board.TryLoadCargo("cargo_1", arkId).Success);
            var second = board.TryLoadCargo("cargo_2", arkId);

            Assert.IsTrue(second.Success, second.Reason);
        }

        [Test]
        public void TryLoadCargo_TwoCellThenThreeCell_FiveCellsIntoFourCellHold_Rejected()
        {
            var board = BuildBoardWithArk(out var arkId);
            board.TryPlace(Cargo2Cell(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            board.TryPlace(Cargo3CellL(), TestBoards.SupportLineAnchor(1, 1), "cargo_2");
            Assert.IsTrue(board.TryLoadCargo("cargo_1", arkId).Success);

            var result = board.TryLoadCargo("cargo_2", arkId);

            Assert.IsFalse(result.Success, "Truncheon Line (2 cells) + Pilgrim Spears (3 cells) must not both fit a 4-cell hold");
            Assert.AreEqual("Cargo does not fit in transport hold", result.Reason);
        }

        [Test]
        public void TryLoadCargo_SingleThreeCellLShape_Fits()
        {
            var board = BuildBoardWithArk(out var arkId);
            board.TryPlace(Cargo3CellL(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");

            var result = board.TryLoadCargo("cargo_1", arkId);

            Assert.IsTrue(result.Success, result.Reason);
        }

        [Test]
        public void TryLoadCargo_HoldFull_RejectsAnyFurtherCargo()
        {
            var board = BuildBoardWithArk(out var arkId);
            board.TryPlace(Cargo2Cell("cargo_a"), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            board.TryPlace(Cargo2Cell("cargo_b"), TestBoards.SupportLineAnchor(1, 1), "cargo_2");
            board.TryPlace(Cargo1Cell(), TestBoards.SupportLineAnchor(1, 2), "cargo_3");
            Assert.IsTrue(board.TryLoadCargo("cargo_1", arkId).Success);
            Assert.IsTrue(board.TryLoadCargo("cargo_2", arkId).Success);

            var result = board.TryLoadCargo("cargo_3", arkId);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Cargo does not fit in transport hold", result.Reason);
        }

        [Test]
        public void TryLoadCargo_SourceNotATransport_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Cargo1Cell(), TestBoards.FrontLineAnchor(), "not_a_transport");
            board.TryPlace(Cargo1Cell(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");

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
