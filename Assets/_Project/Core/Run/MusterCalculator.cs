using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class MusterCalculator
    {
        public const int SupplySynergyThreshold = 2;
        public const int SupplySynergyMusterBonus = 2;

        public static int Compute(BoardState board, int baseMusterPerShop)
        {
            if (board == null)
                return baseMusterPerShop;

            int fromPieces = board.Pieces.Sum(p => p.Definition.MusterPerShop);
            int synergyBonus = CountSupplySynergyBonus(board);
            return baseMusterPerShop + fromPieces + synergyBonus;
        }

        private static int CountSupplySynergyBonus(BoardState board)
        {
            if (board?.Pieces == null)
                return 0;

            var piecesById = board.Pieces.ToDictionary(p => p.InstanceId);
            int pairs = 0;

            foreach (var pair in BoardAdjacency.GetTouchingPairs(board.Pieces))
            {
                if (!piecesById.TryGetValue(pair.A, out var pieceA) ||
                    !piecesById.TryGetValue(pair.B, out var pieceB))
                    continue;

                if (HasSupplySynergyTag(pieceA.Definition) && HasSupplySynergyTag(pieceB.Definition))
                    pairs++;
            }

            return pairs;
        }

        private static bool HasSupplySynergyTag(PieceDefinition definition) =>
            PieceTagQueries.HasSynergyTag(definition, GameTagIds.Supplier)
            || PieceTagQueries.HasSynergyTag(definition, GameTagIds.SupplyLine);
    }
}
