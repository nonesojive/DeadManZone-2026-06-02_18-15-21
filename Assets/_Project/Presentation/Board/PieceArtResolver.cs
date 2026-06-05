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
    }
}
