using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Column edge positions inside a GridLayoutGroup rect (matches live tile boundaries).
    /// </summary>
    public static class GridLayoutColumnMetrics
    {
        public static float GetColumnEdgeLocal(
            RectTransform gridRect,
            GridLayoutGroup grid,
            int column,
            bool left)
        {
            float stride = grid.cellSize.x + grid.spacing.x;
            float origin = -gridRect.rect.width * gridRect.pivot.x + grid.padding.left;
            float edge = origin + column * stride;
            if (!left)
                edge += grid.cellSize.x;

            return edge;
        }

        public static bool TryGetNormalizedHorizontalRange(
            RectTransform gridRect,
            GridLayoutGroup grid,
            RectTransform targetRect,
            int colStart,
            int colEnd,
            out float minX,
            out float maxX)
        {
            minX = 0f;
            maxX = 1f;

            if (gridRect == null || grid == null || targetRect == null || colEnd < colStart)
                return false;

            float leftLocal = GetColumnEdgeLocal(gridRect, grid, colStart, left: true);
            float rightLocal = GetColumnEdgeLocal(gridRect, grid, colEnd, left: false);

            var worldLeft = gridRect.TransformPoint(new Vector3(leftLocal, gridRect.rect.center.y, 0f));
            var worldRight = gridRect.TransformPoint(new Vector3(rightLocal, gridRect.rect.center.y, 0f));
            var localLeft = targetRect.InverseTransformPoint(worldLeft);
            var localRight = targetRect.InverseTransformPoint(worldRight);

            float width = targetRect.rect.width;
            if (width <= 1f)
                return false;

            minX = (localLeft.x - targetRect.rect.xMin) / width;
            maxX = (localRight.x - targetRect.rect.xMin) / width;
            return maxX > minX;
        }
    }
}
