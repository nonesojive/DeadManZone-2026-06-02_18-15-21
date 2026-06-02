using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Board
{
    public static class PieceVisualLookup
    {
        public static PieceDefinitionSO GetSource(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId))
                return null;

            var database = ContentDatabase.Load();
            if (database == null)
                return null;

            foreach (var piece in database.Pieces)
            {
                if (piece != null && piece.id == pieceId)
                    return piece;
            }

            return null;
        }
    }
}
