using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Row edge positions inside a GridLayoutGroup rect (matches live tile boundaries).
    /// </summary>
    public static class GridLayoutRowMetrics
    {
        public static float GetRowEdgeLocal(
            RectTransform gridRect,
            GridLayoutGroup grid,
            int row,
            bool top)
        {
            float stride = grid.cellSize.y + grid.spacing.y;
            float topOrigin = gridRect.rect.height * (1f - gridRect.pivot.y) - grid.padding.top;
            float edge = topOrigin - row * stride;
            if (!top)
                edge -= grid.cellSize.y;

            return edge;
        }
    }
}
