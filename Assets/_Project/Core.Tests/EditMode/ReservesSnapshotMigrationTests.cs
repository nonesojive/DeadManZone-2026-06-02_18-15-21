using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Reserves shrank 8x2 -> 6x2 (shop visual rework). Wider legacy
    /// snapshots must migrate: keep fitting anchors, repack the rest, never throw.</summary>
    public sealed class ReservesSnapshotMigrationTests
    {
        private static ContentRegistry BuildRegistry(params PieceDefinition[] pieces)
        {
            var registry = new ContentRegistry();
            foreach (var piece in pieces)
                registry.Register(piece, ShopLane.Offensive);
            return registry;
        }

        private static ReservesSnapshot LegacyEightWide(params (string pieceId, string instanceId, int x, int y)[] records)
        {
            var snapshot = new ReservesSnapshot { Width = 8, Height = 2 };
            foreach (var (pieceId, instanceId, x, y) in records)
                snapshot.Pieces.Add(new PlacedPieceRecord
                {
                    InstanceId = instanceId,
                    PieceId = pieceId,
                    AnchorX = x,
                    AnchorY = y
                });
            return snapshot;
        }

        [Test]
        public void ToReserves_LegacyEightWide_KeepsFittingAnchorsAndRepacksOutOfBounds()
        {
            var unit = TestPieces.CreateUnit("legacy_unit");
            var registry = BuildRegistry(unit);
            var snapshot = LegacyEightWide(
                (unit.Id, "in_bounds", 2, 1),
                (unit.Id, "out_of_bounds", 7, 0));

            var reserves = ReservesSnapshotMapper.ToReserves(snapshot, registry);

            Assert.AreEqual(2, reserves.Pieces.Count, "both pieces must survive the migration");
            var kept = reserves.Pieces.Single(p => p.InstanceId == "in_bounds");
            Assert.AreEqual(new GridCoord(2, 1), kept.Anchor, "fitting anchor is preserved");
            var repacked = reserves.Pieces.Single(p => p.InstanceId == "out_of_bounds");
            Assert.Less(repacked.Anchor.X, ReservesState.Width, "repacked piece lands in bounds");
        }

        [Test]
        public void ToReserves_LegacyEightWide_FullBoard_DropsOnlyTheOverflow()
        {
            var unit = TestPieces.CreateUnit("legacy_unit");
            var registry = BuildRegistry(unit);
            // 13 single-cell pieces on an 8x2 board -> only 12 fit on 6x2.
            var records = Enumerable.Range(0, 13)
                .Select(i => (unit.Id, $"u{i}", i % 8, i / 8))
                .ToArray();

            var reserves = ReservesSnapshotMapper.ToReserves(
                LegacyEightWide(records), registry);

            Assert.AreEqual(ReservesState.Width * ReservesState.Height, reserves.Pieces.Count);
        }

        [Test]
        public void ToReserves_CurrentSize_RoundTrips()
        {
            var unit = TestPieces.CreateUnit("current_unit");
            var registry = BuildRegistry(unit);
            var reserves = new ReservesState();
            Assert.IsTrue(reserves.TryPlace(unit, new GridCoord(5, 1), "u1").Success);

            var restored = ReservesSnapshotMapper.ToReserves(
                ReservesSnapshotMapper.FromReserves(reserves), registry);

            Assert.AreEqual(new GridCoord(5, 1), restored.Pieces.Single().Anchor);
        }
    }
}
