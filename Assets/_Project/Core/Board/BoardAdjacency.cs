using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public static class BoardAdjacency
    {
        public readonly struct AdjacentPair
        {
            public string A { get; init; }
            public string B { get; init; }
        }

        public static IEnumerable<AdjacentPair> GetTouchingPairs(IEnumerable<PlacedPiece> pieces)
        {
            // 2026-07-17 round-3 playtest fix: a piece carried in a transport's cargo hold no
            // longer occupies a real main-board cell (BoardState.TryLoadCargo vacates it) — its
            // Anchor is stale bookkeeping only. Without this filter it would appear "physically
            // adjacent" to whatever now legitimately sits on that cell.
            var pieceList = pieces.Where(p => p.CarrierInstanceId == null).ToList();
            var cellOwners = new Dictionary<GridCoord, string>();

            foreach (var piece in pieceList)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation))
                    cellOwners[cell] = piece.InstanceId;
            }

            var seen = new HashSet<string>();
            foreach (var piece in pieceList)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation))
                {
                    foreach (var offset in OrthogonalOffsets)
                    {
                        var neighbor = new GridCoord(cell.X + offset.X, cell.Y + offset.Y);
                        if (!cellOwners.TryGetValue(neighbor, out var otherId))
                            continue;

                        if (otherId == piece.InstanceId)
                            continue;

                        var key = string.CompareOrdinal(piece.InstanceId, otherId) < 0
                            ? $"{piece.InstanceId}|{otherId}"
                            : $"{otherId}|{piece.InstanceId}";

                        if (!seen.Add(key))
                            continue;

                        yield return new AdjacentPair { A = piece.InstanceId, B = otherId };
                    }
                }
            }
        }

        private static readonly GridCoord[] OrthogonalOffsets =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1)
        };
    }
}
