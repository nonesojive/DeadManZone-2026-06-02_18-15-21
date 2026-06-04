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
                    RotationDegrees = (int)p.Rotation
                }).ToList()
            };

        public static ReservesState ToReserves(ReservesSnapshot snapshot, ContentRegistry registry)
        {
            if (snapshot.Width != ReservesState.Width || snapshot.Height != ReservesState.Height)
                throw new System.InvalidOperationException(
                    $"Reserves snapshot must be {ReservesState.Width}x{ReservesState.Height}, got {snapshot.Width}x{snapshot.Height}.");

            var reserves = new ReservesState();
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var result = reserves.TryPlace(
                    definition,
                    new GridCoord(record.AnchorX, record.AnchorY),
                    record.InstanceId,
                    rotation);
                if (!result.Success)
                    throw new System.InvalidOperationException(
                        $"Failed to restore '{record.PieceId}' at ({record.AnchorX},{record.AnchorY}): {result.Reason}");
            }

            return reserves;
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
