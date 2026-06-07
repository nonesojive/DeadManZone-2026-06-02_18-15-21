using DeadManZone.Presentation.Board;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Converts live board grid edges into normalized anchors for sibling UI regions.
    /// </summary>
    public static class BoardGridBoundsResolver
    {
        public static bool TryGetHorizontalBoundsInParent(
            RectTransform targetParent,
            BoardView boardView,
            out float minX,
            out float maxX)
        {
            minX = 0f;
            maxX = 1f;

            if (targetParent == null || boardView == null)
                return false;

            var grid = boardView.GridLayout;
            if (grid == null)
                return false;

            var gridRect = grid.GetComponent<RectTransform>();
            if (gridRect == null)
                return false;

            int columns = grid.constraintCount;
            if (columns <= 0)
                return false;

            Canvas.ForceUpdateCanvases();
            return GridLayoutColumnMetrics.TryGetNormalizedHorizontalRange(
                gridRect,
                grid,
                targetParent,
                0,
                columns - 1,
                out minX,
                out maxX);
        }

        public static bool TryProjectBoardColumnRange(
            BoardView boardView,
            RectTransform targetRect,
            int colStart,
            int colEnd,
            out float minX,
            out float maxX)
        {
            minX = 0f;
            maxX = 1f;

            if (boardView == null || targetRect == null || colEnd < colStart)
                return false;

            var grid = boardView.GridLayout;
            if (grid == null)
                return false;

            var gridRect = grid.GetComponent<RectTransform>();
            if (gridRect == null)
                return false;

            Canvas.ForceUpdateCanvases();
            return GridLayoutColumnMetrics.TryGetNormalizedHorizontalRange(
                gridRect,
                grid,
                targetRect,
                colStart,
                colEnd,
                out minX,
                out maxX);
        }

        public static bool TryGetVerticalBoundsInParent(
            RectTransform targetParent,
            GridLayoutGroup grid,
            int rowCount,
            out float minY,
            out float maxY)
        {
            minY = 0f;
            maxY = 1f;

            if (targetParent == null || grid == null || rowCount <= 0)
                return false;

            var gridRect = grid.GetComponent<RectTransform>();
            if (gridRect == null)
                return false;

            Canvas.ForceUpdateCanvases();

            float parentHeight = targetParent.rect.height;
            if (parentHeight <= 1f)
                return false;

            float contentHeight = GridLayoutContentMetrics.ContentHeight(grid, rowCount);
            if (contentHeight <= 1f)
                return false;

            var worldCenter = gridRect.TransformPoint(gridRect.rect.center);
            var localCenter = targetParent.InverseTransformPoint(worldCenter);
            float centerY = (localCenter.y - targetParent.rect.yMin) / parentHeight;
            float halfSpan = (contentHeight * 0.5f) / parentHeight;

            minY = centerY - halfSpan;
            maxY = centerY + halfSpan;
            return maxY > minY;
        }
    }
}
