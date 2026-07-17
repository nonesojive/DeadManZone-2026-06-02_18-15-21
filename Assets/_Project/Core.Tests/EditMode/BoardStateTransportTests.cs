using System.Linq;
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

        // ------------------------------------------------------------------
        // 2026-07-17 round-3 playtest fix: a piece is EITHER on the main board OR in a
        // transport's cargo hold, never both. The tests below are new for this round.
        // ------------------------------------------------------------------

        [Test]
        public void TryLoadCargo_VacatesMainBoardCell_AnotherPieceCanPlaceThere()
        {
            var board = BuildBoardWithArk(out var arkId);
            var cargoAnchor = TestBoards.SupportLineAnchor(1, 0);
            board.TryPlace(Cargo1Cell(), cargoAnchor, "cargo_1");

            Assert.IsFalse(board.CanPlace(Cargo1Cell("other"), cargoAnchor),
                "sanity: the cell is occupied before loading");

            var result = board.TryLoadCargo("cargo_1", arkId);
            Assert.IsTrue(result.Success, result.Reason);

            Assert.IsTrue(board.CanPlace(Cargo1Cell("other"), cargoAnchor),
                "loading cargo must vacate its old main-board cell — a piece is either on the board or in the hold, never both");

            var placeOther = board.TryPlace(Cargo1Cell("other"), cargoAnchor, "other_1");
            Assert.IsTrue(placeOther.Success, placeOther.Reason);
        }

        [Test]
        public void TryEmbarkCargo_FromNowhere_NeverOccupiesAMainBoardCell()
        {
            var board = BuildBoardWithArk(out var arkId);

            // Fill the whole 4-cell hold from four separate embarks — under the OLD
            // "place on the board, then tag it" composite this would have claimed four
            // real front-zone cells via a row-major scan (the very first being (7,0)).
            // With the round-3 fix there is no board round-trip at all, so every one of
            // those cells must still be free afterward.
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell("c1"), arkId, "cargo_1").Success);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell("c2"), arkId, "cargo_2").Success);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell("c3"), arkId, "cargo_3").Success);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell("c4"), arkId, "cargo_4").Success);

            var cargoPiece = System.Linq.Enumerable.Single(board.Pieces, p => p.InstanceId == "cargo_1");
            Assert.AreEqual(arkId, cargoPiece.CarrierInstanceId);
            Assert.IsTrue(cargoPiece.CargoAnchor.HasValue);

            foreach (var frontZoneCell in new[]
                     {
                         new GridCoord(7, 0), new GridCoord(8, 0),
                         new GridCoord(7, 1), new GridCoord(8, 1)
                     })
            {
                Assert.IsTrue(
                    board.CanPlace(Cargo1Cell("probe"), frontZoneCell),
                    $"embarking cargo must never consume a main-board cell (checked {frontZoneCell})");
            }
        }

        [Test]
        public void TryRelocate_CarriedPiece_UnloadsOntoBoardAndClearsCarrierTag()
        {
            var board = BuildBoardWithArk(out var arkId);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell(), arkId, "cargo_1").Success);

            var targetAnchor = TestBoards.SupportLineAnchor(2, 0);
            var result = board.TryRelocate("cargo_1", targetAnchor, PieceRotation.R0);

            Assert.IsTrue(result.Success, result.Reason);
            var piece = System.Linq.Enumerable.Single(board.Pieces, p => p.InstanceId == "cargo_1");
            Assert.IsNull(piece.CarrierInstanceId, "relocating a carried piece is how it un-embarks onto the board");
            Assert.IsFalse(board.CanPlace(Cargo1Cell("probe"), targetAnchor), "the piece now legitimately occupies its new cell");
        }

        [Test]
        public void TryRemoveTransportEvictingCargo_EvictsCargoToReserves()
        {
            var board = BuildBoardWithArk(out var arkId);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell(), arkId, "cargo_1").Success);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo2Cell(), arkId, "cargo_2").Success);
            var reserves = new ReservesState();

            var removed = board.TryRemoveTransportEvictingCargo(arkId, reserves, out var transportPiece);

            Assert.IsTrue(removed);
            Assert.AreEqual(arkId, transportPiece.InstanceId);
            Assert.IsFalse(board.Pieces.Any(p => p.InstanceId == arkId), "the transport itself is gone (sold)");
            Assert.IsFalse(board.Pieces.Any(p => p.InstanceId == "cargo_1" || p.InstanceId == "cargo_2"),
                "cargo must leave the board along with its transport");
            Assert.IsTrue(reserves.Pieces.Any(p => p.InstanceId == "cargo_1"), "cargo_1 evicted to reserves");
            Assert.IsTrue(reserves.Pieces.Any(p => p.InstanceId == "cargo_2"), "cargo_2 evicted to reserves");
            Assert.IsTrue(reserves.Pieces.All(p => p.CarrierInstanceId == null), "evicted cargo is no longer tagged as anyone's cargo");
        }

        [Test]
        public void TryRemoveTransportEvictingCargo_ReservesFull_RefusesSellAndChangesNothing()
        {
            var board = BuildBoardWithArk(out var arkId);
            Assert.IsTrue(board.TryEmbarkCargo(Cargo1Cell(), arkId, "cargo_1").Success);

            var reserves = new ReservesState();
            for (int y = 0; y < ReservesState.Height; y++)
            for (int x = 0; x < ReservesState.Width; x++)
                reserves.TryPlace(Cargo1Cell($"filler_{x}_{y}"), new GridCoord(x, y));

            var removed = board.TryRemoveTransportEvictingCargo(arkId, reserves, out _);

            Assert.IsFalse(removed, "a full reserves must refuse the sell rather than orphan cargo");
            Assert.IsTrue(board.Pieces.Any(p => p.InstanceId == arkId), "the transport must still be on the board");
            var cargoPiece = System.Linq.Enumerable.Single(board.Pieces, p => p.InstanceId == "cargo_1");
            Assert.AreEqual(arkId, cargoPiece.CarrierInstanceId, "cargo must still be tagged — nothing was mutated");
        }

        [Test]
        public void TryRemove_CarriedPiece_DoesNotVacateAnotherPiecesCell()
        {
            var board = BuildBoardWithArk(out var arkId);
            var cargoAnchor = TestBoards.SupportLineAnchor(1, 0);
            board.TryPlace(Cargo1Cell(), cargoAnchor, "cargo_1");
            Assert.IsTrue(board.TryLoadCargo("cargo_1", arkId).Success);

            // A different piece now legitimately occupies the vacated cell.
            var placeOther = board.TryPlace(Cargo1Cell("other"), cargoAnchor, "other_1");
            Assert.IsTrue(placeOther.Success, placeOther.Reason);

            Assert.IsTrue(board.TryRemove("cargo_1", out _), "sanity: the carried piece is removable (sell-from-hold path)");

            Assert.IsFalse(board.CanPlace(Cargo1Cell("probe"), cargoAnchor),
                "removing the carried piece must not vacate the OTHER piece's real cell");
        }
    }
}
