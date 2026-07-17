using System.Collections.Generic;
using System.Linq;
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
            TransportCapacity = 4
        };

        private static PieceDefinition Cargo() => new()
        {
            Id = "truncheon_line",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new Common.GridCoord(0, 0) }),
            MaxHp = 40
        };

        /// <summary>2-cell domino, same footprint budget as the real Truncheon Line.</summary>
        private static PieceDefinition CargoDomino() => new()
        {
            Id = "truncheon_line_domino",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new Common.GridCoord(0, 0), new Common.GridCoord(1, 0) }),
            MaxHp = 40
        };

        private static PieceDefinition CargoSingle() => new()
        {
            Id = "single_cell_piece",
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

        private static ContentRegistry BuildRegistryWithDominoAndSingle()
        {
            var registry = new ContentRegistry();
            registry.Register(Transport(), ShopLane.Offensive);
            registry.Register(CargoDomino(), ShopLane.Offensive);
            registry.Register(CargoSingle(), ShopLane.Offensive);
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

        /// <summary>2026-07-17 round-2 playtest fix: the owner's actual repro save — 3 pieces
        /// (2+2+1 = 5 cells) tagged onto one Ark, made back when TryLoadCargo only counted
        /// PIECES (capacity 3) instead of footprint cells. Loading it today must never throw
        /// and never drop a piece — the piece(s) that don't fit the 4-cell hold lose their
        /// cargo tag and are gracefully evicted (to reserves, or left on the board).</summary>
        [Test]
        public void ToBoard_LegacyOverCapacitySave_EvictsExcessInsteadOfThrowing()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Transport(), TestBoards.FrontLineAnchor(), "ark_1");
            board.TryPlace(CargoDomino(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            board.TryPlace(CargoDomino(), TestBoards.SupportLineAnchor(1, 1), "cargo_2");
            board.TryPlace(CargoSingle(), TestBoards.SupportLineAnchor(1, 2), "cargo_3");

            // Simulate a save written before the fit-check existed: all three tagged onto
            // the Ark directly (bypassing TryLoadCargo, which would correctly refuse this).
            var snapshot = BoardSnapshotMapper.FromBoard(board);
            foreach (var record in snapshot.Pieces.Where(p => p.InstanceId.StartsWith("cargo_")))
                record.CarrierInstanceId = "ark_1";

            var reserves = new ReservesState();
            var warnings = new List<string>();

            BoardState restored = null;
            Assert.DoesNotThrow(() =>
                restored = BoardSnapshotMapper.ToBoard(snapshot, BuildRegistryWithDominoAndSingle(), reserves, warnings));

            var cargo1 = restored.Pieces.Single(p => p.InstanceId == "cargo_1");
            var cargo2 = restored.Pieces.Single(p => p.InstanceId == "cargo_2");
            Assert.AreEqual("ark_1", cargo1.CarrierInstanceId, "the first two dominoes fill the 4-cell hold exactly");
            Assert.AreEqual("ark_1", cargo2.CarrierInstanceId);

            bool cargo3StillOnBoard = restored.Pieces.Any(p => p.InstanceId == "cargo_3");
            bool cargo3InReserves = reserves.Pieces.Any(p => p.InstanceId == "cargo_3");
            Assert.IsTrue(cargo3StillOnBoard || cargo3InReserves, "the excess piece must never be dropped");
            if (cargo3StillOnBoard)
                Assert.IsNull(restored.Pieces.Single(p => p.InstanceId == "cargo_3").CarrierInstanceId);

            Assert.AreEqual(1, warnings.Count, "exactly one piece was evicted");
        }
    }
}
