using System.Linq;
using DeadManZone.Core.Board;

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

        private static int CountSupplySynergyBonus(BoardState board) => 0;
    }
}
