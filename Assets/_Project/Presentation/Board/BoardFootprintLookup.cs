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
                // 2026-07-17 round-3 playtest fix: a carried piece's Anchor is stale bookkeeping
                // (BoardState.TryLoadCargo vacates its real cell) — it's never "at" a main-board
                // cell anymore.
                if (candidate.CarrierInstanceId != null)
                    continue;

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
