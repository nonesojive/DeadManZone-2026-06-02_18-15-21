using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Run
{
    public sealed class PlacedPieceRecord
    {
        public string InstanceId { get; set; }
        public string PieceId { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int RotationDegrees { get; set; }
    }

    public sealed class BoardSnapshot
    {
        public string BoardKind { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int RearCols { get; set; }
        public int SupportCols { get; set; }

        /// <summary>Legacy row-based layout; used when <see cref="RearCols"/> is zero.</summary>
        public int RearRows { get; set; }
        public int SupportRows { get; set; }

        public List<GridCoordRecord> SpecialTiles { get; set; } = new();
        public List<GridCoordRecord> BlockedCells { get; set; } = new();
        public List<PlacedPieceRecord> Pieces { get; set; } = new();
    }

    public sealed class GridCoordRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public static class BoardSnapshotMapper
    {
        public static BoardSnapshot FromBoard(BoardState board) =>
            new BoardSnapshot
            {
                BoardKind = board.Layout.Kind.ToString(),
                Width = board.Layout.Width,
                Height = board.Layout.Height,
                SpecialTiles = board.Layout.SpecialTiles
                    .Select(t => new GridCoordRecord { X = t.X, Y = t.Y })
                    .ToList(),
                BlockedCells = board.Layout.BlockedCells
                    .Select(t => new GridCoordRecord { X = t.X, Y = t.Y })
                    .ToList(),
                Pieces = board.Pieces.Select(p => new PlacedPieceRecord
                {
                    InstanceId = p.InstanceId,
                    PieceId = p.Definition.Id,
                    AnchorX = p.Anchor.X,
                    AnchorY = p.Anchor.Y,
                    RotationDegrees = (int)p.Rotation
                }).ToList()
            };

        [System.Obsolete("Use FromBoard(BoardState) for schema v8 boards.")]
        public static BoardSnapshot FromBoard(BoardState board, int rearCols, int supportCols)
        {
            var snapshot = FromBoard(board);
            snapshot.RearCols = rearCols;
            snapshot.SupportCols = supportCols;
            return snapshot;
        }

        public static BoardState ToBoard(BoardSnapshot snapshot, ContentRegistry registry)
        {
            var layout = CreateLayout(snapshot);
            var board = new BoardState(layout);
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var result = board.TryPlace(
                    definition,
                    new GridCoord(record.AnchorX, record.AnchorY),
                    record.InstanceId,
                    rotation);
                if (!result.Success)
                    throw new System.InvalidOperationException(
                        $"Failed to restore '{record.PieceId}' at ({record.AnchorX},{record.AnchorY}): {result.Reason}");
            }

            return board;
        }

        private static BoardLayout CreateLayout(BoardSnapshot snapshot)
        {
            var specialTiles = snapshot.SpecialTiles
                .Select(t => new GridCoord(t.X, t.Y))
                .ToArray();
            var blockedCells = snapshot.BlockedCells
                .Select(t => new GridCoord(t.X, t.Y))
                .ToArray();

            if (!string.IsNullOrWhiteSpace(snapshot.BoardKind)
                && System.Enum.TryParse<BoardKind>(snapshot.BoardKind, out var kind))
            {
                return kind switch
                {
                    BoardKind.Combat => BoardLayout.CreateCombatBoard(
                        System.Math.Max(snapshot.Width, snapshot.Height),
                        specialTiles),
                    BoardKind.Hq => BoardLayout.CreateHqBoard(
                        snapshot.Width,
                        snapshot.Height,
                        blockedCells,
                        specialTiles),
                    _ => BoardLayout.CreateUnzoned(
                        snapshot.Width,
                        snapshot.Height,
                        kind,
                        blockedCells,
                        specialTiles)
                };
            }

            if (snapshot.RearCols > 0 || snapshot.SupportCols > 0)
            {
                return BoardLayout.CreateHorizontalZones(
                    snapshot.Width,
                    snapshot.Height,
                    snapshot.RearCols > 0 ? snapshot.RearCols : 3,
                    snapshot.SupportCols > 0 ? snapshot.SupportCols : 3,
                    specialTiles);
            }

            return BoardLayout.CreateStandard(
                snapshot.Width,
                snapshot.Height,
                snapshot.RearRows,
                snapshot.SupportRows,
                specialTiles);
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
