using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Run
{
    public static class AuthorityCalculator
    {
        private const int HqBaseAuthority = 2;
        /// <summary>Authority pool at build-round start: 2 from HQ plus 1 per command-capable building.</summary>
        public static int ComputeRoundPool(BoardState board)
        {
            int pool = HqBaseAuthority;
            if (board?.Pieces == null)
                return pool;

            pool += board.Pieces.Count(p =>
                p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
            return pool;
        }

        public static int ComputeRoundPool(BuildBoardSet boards) =>
            ComputeRoundPool(boards?.ToAggregateBoard());
    }
}
