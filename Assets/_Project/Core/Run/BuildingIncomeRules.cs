using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class BuildingIncomeRules
    {
        public static int SumSuppliesFlatBonus(BuildBoardSet boards)
        {
            int bonus = 0;
            foreach (var piece in boards?.AllPieces ?? Enumerable.Empty<PlacedPiece>())
            {
                if (piece.Definition.Id == "supply_depot")
                    bonus += 5;
            }

            return bonus;
        }

        public static int SumAuthorityFromBuildings(BuildBoardSet boards)
        {
            int bonus = 0;
            if (boards == null)
                return 0;

            foreach (var piece in boards.AllPieces)
            {
                if (piece.Definition.Id == "command_outpost")
                    bonus += 1;
                if (piece.Definition.Id == "officer_quarters")
                    bonus += BuildBoardTagCounter.Count(boards, GameTagIds.Command);
            }

            return bonus;
        }
    }
}
