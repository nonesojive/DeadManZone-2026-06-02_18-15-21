using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Shop
{
    public static class SpecialtyLaneUnlock
    {
        public static bool IsUnlocked(BoardState board, string factionId, ContentRegistry registry = null)
        {
            if (board == null)
                return false;

            if (HasPieceOnBoard(board, "command_bunker"))
                return true;

            if (factionId == FactionIds.IronVanguard &&
                board.Pieces.Count(p => PieceTagQueries.HasTag(p.Definition, GameTagIds.Combatant)) >= 3)
                return true;

            if (registry != null &&
                board.Pieces.Any(p => p.Definition.ShopModifiers != ShopModifierFlags.None))
                return HasPieceOnBoard(board, "field_workshop");

            return false;
        }

        private static bool HasPieceOnBoard(BoardState board, string pieceId) =>
            board.Pieces.Any(p => p.Definition.Id == pieceId);
    }
}
