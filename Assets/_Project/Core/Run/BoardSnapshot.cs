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
    }

    public sealed class BoardSnapshot
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int RearRows { get; set; }
        public int SupportRows { get; set; }
        public List<GridCoordRecord> SpecialTiles { get; set; } = new();
        public List<PlacedPieceRecord> Pieces { get; set; } = new();
    }

    public sealed class GridCoordRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public static class BoardSnapshotMapper
    {
        public static BoardSnapshot FromBoard(BoardState board, int rearRows, int supportRows)
        {
            var snapshot = new BoardSnapshot
            {
                Width = board.Layout.Width,
                Height = board.Layout.Height,
                RearRows = rearRows,
                SupportRows = supportRows,
                SpecialTiles = board.Layout.SpecialTiles
                    .Select(t => new GridCoordRecord { X = t.X, Y = t.Y })
                    .ToList(),
                Pieces = board.Pieces.Select(p => new PlacedPieceRecord
                {
                    InstanceId = p.InstanceId,
                    PieceId = p.Definition.Id,
                    AnchorX = p.Anchor.X,
                    AnchorY = p.Anchor.Y
                }).ToList()
            };
            return snapshot;
        }

        public static BoardState ToBoard(BoardSnapshot snapshot, ContentRegistry registry)
        {
            var specialTiles = snapshot.SpecialTiles
                .Select(t => new GridCoord(t.X, t.Y))
                .ToArray();

            var layout = BoardLayout.CreateStandard(
                snapshot.Width,
                snapshot.Height,
                snapshot.RearRows,
                snapshot.SupportRows,
                specialTiles);

            var board = new BoardState(layout);
            foreach (var record in snapshot.Pieces)
            {
                var definition = registry.GetById(record.PieceId);
                board.TryPlace(definition, new GridCoord(record.AnchorX, record.AnchorY), record.InstanceId);
            }

            return board;
        }
    }
}
