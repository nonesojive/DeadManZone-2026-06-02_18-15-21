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

                // 2026-07-15 faction-roster-v1 W2: each faction's "+Supplies/round" common
                // building, id-hardcoded the same way supply_depot is above (no generic
                // tag-driven path exists yet for a flat per-round bonus — Supplier/SupplyLine
                // tags feed a different, Critical-Mass-based mechanism instead).
                if (piece.Definition.Id == "scavengers_cache" // Dust Scourge §2.3
                    || piece.Definition.Id == "freight_depot" // Cartel of Echoes §2.4
                    || piece.Definition.Id == "chrono_lab" // Paradox Engine §2.6
                    || piece.Definition.Id == "poison_garden" // Blightborn Pact §2.7
                    || piece.Definition.Id == "research_annex") // Crimson Assembly §2.8
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
