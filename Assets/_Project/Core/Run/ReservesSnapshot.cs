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
            {
                if (snapshot.Width == 9 && ReservesState.Width == 8 && snapshot.Height == ReservesState.Height)
                {
                    return ToReservesFromLegacyNineWide(snapshot, registry);
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
                    rotation);
                if (!result.Success)
                    throw new System.InvalidOperationException(
                        $"Failed to restore '{record.PieceId}' at ({record.AnchorX},{record.AnchorY}): {result.Reason}");
            }

            return reserves;
        }

        private static ReservesState ToReservesFromLegacyNineWide(
            ReservesSnapshot snapshot,
            ContentRegistry registry)
        {
            var reserves = new ReservesState();
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var anchor = new GridCoord(record.AnchorX, record.AnchorY);
                if (!WouldFitLegacyPiece(definition, anchor, rotation))
                    continue;

                var result = reserves.TryPlace(definition, anchor, record.InstanceId, rotation);
                if (!result.Success)
                    continue;
            }

            return reserves;
        }

        private static bool WouldFitLegacyPiece(
            PieceDefinition definition,
            GridCoord anchor,
            PieceRotation rotation)
        {
            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= ReservesState.Width || cell.Y >= ReservesState.Height)
                    return false;
            }

            return true;
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
