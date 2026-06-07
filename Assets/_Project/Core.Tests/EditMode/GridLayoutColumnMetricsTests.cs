using DeadManZone.Presentation.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class GridLayoutColumnMetricsTests
    {
        [Test]
        public void GetColumnEdgeLocal_FirstAndLastColumnSpanContentWidth()
        {
            var gridRect = CreateGridRect(500f, 400f);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(40f, 40f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraintCount = 9;

            float left = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, 0, left: true);
            float right = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, 8, left: false);

            Assert.AreEqual(-244f, left, 0.001f);
            Assert.AreEqual(148f, right, 0.001f);
            Assert.AreEqual(
                GridLayoutContentMetrics.ContentWidth(grid, 9) - grid.padding.horizontal,
                right - left,
                0.001f);
        }

        [Test]
        public void TryGetNormalizedHorizontalRange_MapsColumnSpanToTargetRect()
        {
            var gridRect = CreateGridRect(500f, 400f);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(40f, 40f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraintCount = 9;

            var target = CreateGridRect(900f, 200f);
            target.SetParent(gridRect.parent, false);
            target.anchoredPosition = new Vector2(100f, 0f);

            bool ok = GridLayoutColumnMetrics.TryGetNormalizedHorizontalRange(
                gridRect,
                grid,
                target,
                1,
                8,
                out float minX,
                out float maxX);

            Assert.IsTrue(ok);
            Assert.Less(minX, maxX);
            Assert.Greater(minX, 0f);
            Assert.Less(maxX, 1f);
        }

        private static RectTransform CreateGridRect(float width, float height)
        {
            var go = new GameObject("Grid", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }
    }
}
