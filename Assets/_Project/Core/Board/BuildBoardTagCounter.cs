using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public static class BuildBoardTagCounter
    {
        public static int Count(BuildBoardSet boards, string tagId)
        {
            if (boards == null || string.IsNullOrEmpty(tagId))
                return 0;

            int total = 0;
            foreach (var piece in boards.AllPieces)
            {
                if (PieceTagQueries.HasAnyTag(piece.Definition, tagId))
                    total++;
            }

            return total;
        }
    }
}
