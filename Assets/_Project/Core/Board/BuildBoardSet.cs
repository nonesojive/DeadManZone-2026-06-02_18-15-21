using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    /// <summary>Player build-phase combat and HQ boards plus helpers.</summary>
    public sealed class BuildBoardSet
    {
        public BoardState Combat { get; init; }
        public BoardState Hq { get; init; }

        public IEnumerable<PlacedPiece> AllPieces =>
            (Combat?.Pieces ?? System.Array.Empty<PlacedPiece>())
            .Concat(Hq?.Pieces ?? System.Array.Empty<PlacedPiece>());

        /// <summary>ponytail: 32x32 carrier grid; shop/unlock code only reads piece lists.</summary>
        public BoardState ToAggregateBoard()
        {
            var layout = BoardLayout.CreateUnzoned(32, 32, BoardKind.Combat);
            var merged = new BoardState(layout);
            CopyPieces(merged, Combat, xOffset: 0);
            CopyPieces(merged, Hq, xOffset: 16);
            return merged;
        }

        private static void CopyPieces(BoardState target, BoardState source, int xOffset)
        {
            if (source == null)
                return;

            foreach (var piece in source.Pieces)
            {
                target.TryPlace(
                    piece.Definition,
                    new GridCoord(piece.Anchor.X + xOffset, piece.Anchor.Y),
                    piece.InstanceId,
                    piece.Rotation);
            }
        }
    }
}
