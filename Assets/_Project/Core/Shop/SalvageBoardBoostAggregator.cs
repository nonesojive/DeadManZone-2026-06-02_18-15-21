using DeadManZone.Core.Board;

namespace DeadManZone.Core.Shop
{
    public static class SalvageBoardBoostAggregator
    {
        public static int SumBoardBoost(BoardState board)
        {
            int sum = 0;
            foreach (var piece in board.Pieces)
            {
                sum += piece.Definition.SalvageChanceBonus;
                if (piece.Definition.ShopModifiers.HasFlag(ShopModifierFlags.SalvageChanceBoost5))
                    sum += 5;
            }

            return sum;
        }
    }
}
