using System.Linq;
using DeadManZone.Core.Board;

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

            // 2026-07-15 faction-roster-v1: officer_quarters was cut from the IronMarch
            // roster (§2.2) with no direct replacement, so its "+1 Authority per Command
            // piece" hook is gone too — command_outpost's flat +1/round is IronMarch's only
            // Authority building now.
            foreach (var piece in boards.AllPieces)
            {
                if (piece.Definition.Id == "command_outpost")
                    bonus += 1;
            }

            return bonus;
        }
    }
}
