using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Run
{
    public sealed class ReservesSnapshot
    {
        public int Width { get; set; } = ReservesState.Width;
        public int Height { get; set; } = ReservesState.Height;
        public List<PlacedPieceRecord> Pieces { get; set; } = new();
    }

    public static class ReservesSnapshotMapper
    {
        public static ReservesSnapshot FromReserves(ReservesState reserves) =>
            new()
            {
                Width = ReservesState.Width,
                Height = ReservesState.Height,
                Pieces = reserves.Pieces.Select(p => new PlacedPieceRecord
                {
                    InstanceId = p.InstanceId,
                    PieceId = p.Definition.Id,
                    AnchorX = p.Anchor.X,
                    AnchorY = p.Anchor.Y,
                    RotationDegrees = (int)p.Rotation,
                    IsMercenary = p.IsMercenary
                }).ToList()
            };

        public static ReservesState ToReserves(ReservesSnapshot snapshot, ContentRegistry registry)
        {
            if (snapshot.Width != ReservesState.Width || snapshot.Height != ReservesState.Height)
            {
                // Older saves were authored on wider reserves (9x2, then 8x2). Migrate by
                // keeping anchors that still fit and repacking the rest instead of failing.
                if (snapshot.Width > ReservesState.Width && snapshot.Height == ReservesState.Height)
                {
                    return ToReservesFromLegacyWide(snapshot, registry);
                }

                throw new System.InvalidOperationException(
                    $"Reserves snapshot must be {ReservesState.Width}x{ReservesState.Height}, got {snapshot.Width}x{snapshot.Height}.");
            }

            var reserves = new ReservesState();
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var result = reserves.TryPlace(
                    definition,
                    new GridCoord(record.AnchorX, record.AnchorY),
                    record.InstanceId,
                    rotation,
                    record.IsMercenary);
                if (!result.Success)
                    throw new System.InvalidOperationException(
                        $"Failed to restore '{record.PieceId}' at ({record.AnchorX},{record.AnchorY}): {result.Reason}");
            }

            return reserves;
        }

        private static ReservesState ToReservesFromLegacyWide(
            ReservesSnapshot snapshot,
            ContentRegistry registry)
        {
            var reserves = new ReservesState();
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var anchor = new GridCoord(record.AnchorX, record.AnchorY);

                // Keep the authored spot when it still fits on the narrower board.
                if (reserves.TryPlace(definition, anchor, record.InstanceId, rotation, record.IsMercenary).Success)
                    continue;

                // Otherwise repack: first free anchor that accepts the piece. Only a
                // fully packed board drops a piece (16 -> 12 cells can overflow).
                TryRepack(reserves, definition, record.InstanceId, rotation, record.IsMercenary);
            }

            return reserves;
        }

        private static bool TryRepack(
            ReservesState reserves,
            PieceDefinition definition,
            string instanceId,
            PieceRotation rotation,
            bool isMercenary = false)
        {
            for (int y = 0; y < ReservesState.Height; y++)
            {
                for (int x = 0; x < ReservesState.Width; x++)
                {
                    if (reserves.TryPlace(definition, new GridCoord(x, y), instanceId, rotation, isMercenary).Success)
                        return true;
                }
            }

            return false;
        }

        private static PieceRotation RotationFromDegrees(int degrees) =>
            degrees switch
            {
                90 => PieceRotation.R90,
                180 => PieceRotation.R180,
                270 => PieceRotation.R270,
                _ => PieceRotation.R0
            };
    }
}
