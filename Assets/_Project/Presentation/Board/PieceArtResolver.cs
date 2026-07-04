using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Visual;
using UnityEngine;

namespace DeadManZone.Presentation.Board
{
    internal static class PieceArtResolver
    {
        public static Vector2Int ToLocalCell(GridCoord absolute, GridCoord anchor, PieceRotation rotation)
        {
            var offset = new GridCoord(absolute.X - anchor.X, absolute.Y - anchor.Y);
            var local = ShapeTransforms.InverseRotateOffset(offset, rotation);
            return new Vector2Int(local.X, local.Y);
        }

        public static bool ShouldUseFootprintIcon(
            PieceDefinitionSO source,
            GridCoord anchor,
            PieceRotation rotation,
            PieceDefinition definition)
        {
            // Fresh roster cards are the source of truth for board/reserve chips.
            // Legacy per-cell sprites can be stale and should not override authored icons.
            return source?.icon != null;
        }

        public static bool AllCellsHaveSprites(
            PieceDefinitionSO source,
            GridCoord anchor,
            PieceRotation rotation,
            PieceDefinition definition)
        {
            if (source == null || !source.HasCellArt() || definition?.Shape == null)
                return false;

            foreach (var local in definition.Shape.GetCells(new GridCoord(0, 0), PieceRotation.R0))
            {
                if (source.TryGetCellSprite(new Vector2Int(local.X, local.Y)) == null)
                    return false;
            }

            return true;
        }

        public static Color ResolveTint(PieceDefinition definition, PieceDefinitionSO source, UiThemeSO theme)
        {
            if (source != null && source.categoryTint.a > 0.01f)
                return source.categoryTint;

            return theme.GetCategoryTint(definition.Category);
        }

        public static Color ResolveFootprintBackground(PieceDefinitionSO source, UiThemeSO theme)
        {
            theme ??= UiThemeProvider.Current;
            var neutral = theme.neutralTokenBackgroundColor;

            if (source == null || string.IsNullOrWhiteSpace(source.factionId)
                || source.factionId == "neutral")
                return neutral;

            var faction = ContentDatabase.Load()?.GetFaction(source.factionId);
            if (faction != null && faction.tokenBackgroundColor.a > 0.01f)
                return faction.tokenBackgroundColor;

            return DefaultFactionTokenBackground(source.factionId, neutral);
        }

        private static Color DefaultFactionTokenBackground(string factionId, Color neutralFallback)
        {
            return factionId switch
            {
                FactionIds.IronmarchUnion => new Color(0.22f, 0.28f, 0.38f, 0.45f),
                FactionIds.DustScourge => new Color(0.42f, 0.34f, 0.24f, 0.45f),
                FactionIds.CartelOfEchoes => new Color(0.32f, 0.26f, 0.42f, 0.45f),
                "crimson_legion" => new Color(0.45f, 0.20f, 0.18f, 0.45f),
                "ash_wraiths" => new Color(0.28f, 0.28f, 0.30f, 0.45f),
                _ => neutralFallback
            };
        }
    }
}
