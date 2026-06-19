using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Presentation.Board
{
    public static class BoardFootprintLookup
    {
        public static bool TryGetPieceAt(BoardState board, GridCoord cell, out PlacedPiece piece)
        {
            piece = null;
            if (board == null)
                return false;

            foreach (var candidate in board.Pieces)
            {
                foreach (var occupiedCell in candidate.Definition.Shape.GetCells(candidate.Anchor, candidate.Rotation))
                {
                    if (occupiedCell.X != cell.X || occupiedCell.Y != cell.Y)
                        continue;

                    piece = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
