using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Run
{
    public static class AuthorityCalculator
    {
        private const int HqBaseAuthority = 2;
        private const string CommandTag = "Command";

        /// <summary>Authority pool at build-round start: 2 from HQ plus 1 per Command building.</summary>
        public static int ComputeRoundPool(BoardState board)
        {
            int pool = HqBaseAuthority;
            if (board?.Pieces == null)
                return pool;

            pool += board.Pieces.Count(p => p.Definition?.Tags?.Contains(CommandTag) == true);
            return pool;
        }
    }
}
