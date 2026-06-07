using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class AuthorityCalculator
    {
        private const int HqBaseAuthority = 2;
        /// <summary>Authority pool at build-round start: 2 from HQ plus 1 per Command building.</summary>
        public static int ComputeRoundPool(BoardState board)
        {
            int pool = HqBaseAuthority;
            if (board?.Pieces == null)
                return pool;

            pool += board.Pieces.Count(p => PieceTagQueries.HasTag(p.Definition, GameTagIds.Command));
            return pool;
        }
    }
}
