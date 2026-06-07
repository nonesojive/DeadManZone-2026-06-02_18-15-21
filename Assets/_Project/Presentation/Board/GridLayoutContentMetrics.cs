using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Pixel size of the occupied cell block inside a GridLayoutGroup.
    /// </summary>
    public static class GridLayoutContentMetrics
    {
        public static float ContentWidth(GridLayoutGroup grid, int columnCount)
        {
            if (grid == null || columnCount <= 0)
                return 0f;

            float cell = grid.cellSize.x;
            if (cell <= 0f)
                return 0f;

            return columnCount * cell
                + (columnCount - 1) * grid.spacing.x
                + grid.padding.horizontal;
        }

        public static float ContentHeight(GridLayoutGroup grid, int rowCount)
        {
            if (grid == null || rowCount <= 0)
                return 0f;

            float cell = grid.cellSize.y;
            if (cell <= 0f)
                return 0f;

            return rowCount * cell
                + (rowCount - 1) * grid.spacing.y
                + grid.padding.vertical;
        }
    }
}
