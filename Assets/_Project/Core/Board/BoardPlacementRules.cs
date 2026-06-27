using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public static class BoardPlacementRules
    {
        public static BoardKind ResolveTargetBoard(PieceDefinition definition)
        {
            if (definition == null)
                return BoardKind.Combat;

            if (!string.IsNullOrWhiteSpace(definition.Primary))
            {
                string primary = definition.Primary.Trim().ToLowerInvariant();
                if (primary == GameTagIds.Building)
                    return BoardKind.Hq;
                if (primary is GameTagIds.Infantry or GameTagIds.Vehicle or GameTagIds.Structure)
                    return BoardKind.Combat;
            }

            return definition.Category switch
            {
                PieceCategory.Building => BoardKind.Hq,
                PieceCategory.Unit => BoardKind.Combat,
                PieceCategory.Hybrid => BoardKind.Combat,
                _ => BoardKind.Combat
            };
        }

        public static bool IsAllowedForBoard(PieceDefinition definition, BoardKind boardKind) =>
            ResolveTargetBoard(definition) == boardKind;

        public static string InvalidBoardReason(PieceDefinition definition, BoardKind boardKind)
        {
            var target = ResolveTargetBoard(definition);
            if (target == boardKind)
                return null;

            return target switch
            {
                BoardKind.Hq => "Buildings must be placed on the HQ board",
                BoardKind.Combat => "Units must be placed on the combat board",
                _ => "Invalid board for this piece"
            };
        }
    }
}
