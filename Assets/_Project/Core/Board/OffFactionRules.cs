using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.4: the off-faction ruleset (salvage / mercenary).
    /// `salvage` is a DERIVED tag — never stored, always recomputed from board state, so
    /// acquisition history is never tracked. `mercenary` is acquisition-based and PERMANENT
    /// (PlacedPiece.IsMercenary, set only by the Cartel merc slot at purchase) and it
    /// suppresses the salvage tag. Neutral pieces are neither. Salvage pieces carry no
    /// inherent downside — this is a labelling helper only, no stat modifiers live here.
    /// </summary>
    public static class OffFactionRules
    {
        public const string NeutralFactionId = "neutral";

        /// <summary>True for a piece bought through the mercenary shop slot — permanent,
        /// carried on the instance (survives moves/reserves via PlacedPiece.IsMercenary).</summary>
        public static bool IsMercenary(PlacedPiece piece) => piece != null && piece.IsMercenary;

        /// <summary>Derived, not tracked: true when the piece belongs to a faction other
        /// than the player's, isn't neutral, and hasn't been flagged mercenary (mercenary
        /// suppresses salvage — rule 2). CM counting and future consumers (Dust Scourge's
        /// salvage-count CM inversion, the Dust Scourge meta-unlock trigger) call this.</summary>
        public static bool IsSalvage(PlacedPiece piece, string playerFactionId)
        {
            if (piece?.Definition == null)
                return false;

            if (IsMercenary(piece))
                return false;

            string factionId = piece.Definition.FactionId;
            if (string.IsNullOrEmpty(factionId) || factionId == NeutralFactionId)
                return false;

            return factionId != playerFactionId;
        }

        /// <summary>"Fighter" for the mercenary slot's candidate pool (§1.9 Cartel):
        /// anything that isn't a building/structure — Category.Building is always
        /// excluded, and a `building`/`structure` Primary tag excludes a Category.Unit
        /// piece too (mirrors BoardPlacementRules' primary-tag gotcha, e.g.
        /// machine_gun_nest is Category.Unit but primary "structure").</summary>
        public static bool IsFighter(PieceDefinition piece)
        {
            if (piece == null)
                return false;

            if (piece.Category == PieceCategory.Building)
                return false;

            if (!string.IsNullOrWhiteSpace(piece.Primary))
            {
                string primary = piece.Primary.Trim().ToLowerInvariant();
                if (primary == GameTagIds.Building || primary == GameTagIds.Structure)
                    return false;
            }

            return true;
        }
    }
}
