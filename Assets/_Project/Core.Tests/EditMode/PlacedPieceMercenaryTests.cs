using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 §1.4 W1b: PlacedPiece.IsMercenary is
    /// acquisition-based and PERMANENT — it must survive a relocation on the same board,
    /// a board snapshot save/load round trip, and a reserves snapshot round trip. Pure
    /// Core-level tests (no ContentDatabase/RunOrchestrator dependency).</summary>
    public sealed class PlacedPieceMercenaryTests
    {
        private static PieceDefinition Fighter() => new()
        {
            Id = "dust_fighter",
            DisplayName = "Dust Fighter",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 10,
            FactionId = FactionIds.DustScourge
        };

        [Test]
        public void BoardState_TryPlace_SetsTheFlag()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Fighter(), TestBoards.FrontLineAnchor(), "merc_1", isMercenary: true);

            Assert.IsTrue(board.Pieces.Single().IsMercenary);
        }

        [Test]
        public void BoardState_TryRelocate_PreservesTheFlag()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(Fighter(), TestBoards.FrontLineAnchor(), "merc_1", isMercenary: true);

            var result = board.TryRelocate("merc_1", TestBoards.FrontLineAnchor(2), PieceRotation.R0);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(board.Pieces.Single(p => p.InstanceId == "merc_1").IsMercenary,
                "moving the piece must not clear its permanent mercenary flag");
        }

        [Test]
        public void ReservesState_TryRelocate_PreservesTheFlag()
        {
            var reserves = new ReservesState();
            reserves.TryPlace(Fighter(), new GridCoord(0, 0), "merc_1", isMercenary: true);

            var result = reserves.TryRelocate("merc_1", new GridCoord(1, 0));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(reserves.Pieces.Single().IsMercenary);
        }

        [Test]
        public void BoardSnapshot_RoundTrip_PreservesTheFlag()
        {
            var registry = new ContentRegistry();
            var piece = Fighter();
            registry.Register(piece, ShopLane.Offensive);

            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(piece, TestBoards.FrontLineAnchor(), "merc_1", isMercenary: true);
            board.TryPlace(piece, TestBoards.FrontLineAnchor(2), "regular_1", isMercenary: false);

            var snapshot = BoardSnapshotMapper.FromBoard(board);
            var restored = BoardSnapshotMapper.ToBoard(snapshot, registry);

            Assert.IsTrue(restored.Pieces.Single(p => p.InstanceId == "merc_1").IsMercenary);
            Assert.IsFalse(restored.Pieces.Single(p => p.InstanceId == "regular_1").IsMercenary);
        }

        [Test]
        public void ReservesSnapshot_RoundTrip_PreservesTheFlag()
        {
            var registry = new ContentRegistry();
            var piece = Fighter();
            registry.Register(piece, ShopLane.Offensive);

            var reserves = new ReservesState();
            reserves.TryPlace(piece, new GridCoord(0, 0), "merc_1", isMercenary: true);

            var snapshot = ReservesSnapshotMapper.FromReserves(reserves);
            var restored = ReservesSnapshotMapper.ToReserves(snapshot, registry);

            Assert.IsTrue(restored.Pieces.Single().IsMercenary);
        }

        [Test]
        public void OlderSaves_WithNoIsMercenaryKey_DefaultToFalse()
        {
            // Additive field: a PlacedPieceRecord deserialized from JSON that predates this
            // wave has no "IsMercenary" key and must default false (Newtonsoft's normal
            // missing-property behavior — asserted here as a Core-level self-check).
            var record = new PlacedPieceRecord { InstanceId = "x", PieceId = "y" };
            Assert.IsFalse(record.IsMercenary);
        }

        [Test]
        public void SalvageCalculator_MercenarySellsForZero_AcrossAllRefundTypes()
        {
            var piece = Fighter();
            var refund = SalvageCalculator.Compute(piece, FactionIds.DustScourge, isMercenary: true);

            Assert.AreEqual(0, refund.Supplies);
            Assert.AreEqual(0, refund.Authority);
            Assert.AreEqual(0, refund.Manpower);
        }
    }
}
