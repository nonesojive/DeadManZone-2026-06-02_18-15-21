using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Draws cell grid lines aligned to a GridLayoutGroup.
    /// </summary>
    public sealed class BoardGridOverlay : MaskableGraphic
    {
        [SerializeField] private RectTransform gridRect;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private int columns = 9;
        [SerializeField] private int rows = 10;
        [SerializeField] private int rearColumns = 4;
        [SerializeField] private int supportColumns = 3;
        [SerializeField] private float lineWidth = 1.25f;
        [SerializeField] private float zoneDividerWidth = 2f;
        [SerializeField] private Color cellLineColor = new(1f, 1f, 1f, 0.14f);
        [SerializeField] private Color zoneDividerColor = new(1f, 1f, 1f, 0.28f);

        public void Configure(
            RectTransform gridRoot,
            GridLayoutGroup gridLayout,
            int columnCount,
            int rowCount,
            int rearCols,
            int supportCols,
            Color lineColor,
            Color dividerColor)
        {
            gridRect = gridRoot;
            grid = gridLayout;
            columns = Mathf.Max(1, columnCount);
            rows = Mathf.Max(1, rowCount);
            rearColumns = Mathf.Max(0, rearCols);
            supportColumns = Mathf.Max(0, supportCols);
            cellLineColor = lineColor;
            zoneDividerColor = dividerColor;
            color = Color.white;
            raycastTarget = false;
            SyncToGrid();
            SetVerticesDirty();
        }

        public void SyncToGrid()
        {
            if (gridRect == null)
                return;

            var overlayRect = rectTransform;
            overlayRect.anchorMin = gridRect.anchorMin;
            overlayRect.anchorMax = gridRect.anchorMax;
            overlayRect.pivot = gridRect.pivot;
            overlayRect.offsetMin = gridRect.offsetMin;
            overlayRect.offsetMax = gridRect.offsetMax;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (gridRect == null || grid == null || columns <= 0 || rows <= 0)
                return;

            float left = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, 0, left: true);
            float right = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, columns - 1, left: false);
            float top = GridLayoutRowMetrics.GetRowEdgeLocal(gridRect, grid, 0, top: true);
            float bottom = GridLayoutRowMetrics.GetRowEdgeLocal(gridRect, grid, rows - 1, top: false);

            for (int col = 0; col <= columns; col++)
            {
                float x = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, col, left: true);
                bool zoneDivider = col == rearColumns || col == rearColumns + supportColumns;
                float width = zoneDivider ? zoneDividerWidth : lineWidth;
                Color lineColor = zoneDivider ? zoneDividerColor : cellLineColor;
                AddVerticalLine(vertexHelper, x, bottom, top, width, lineColor);
            }

            for (int row = 0; row <= rows; row++)
            {
                float y = GridLayoutRowMetrics.GetRowEdgeLocal(gridRect, grid, row, top: true);
                AddHorizontalLine(vertexHelper, left, right, y, lineWidth, cellLineColor);
            }
        }

        private void AddVerticalLine(VertexHelper vertexHelper, float x, float yMin, float yMax, float width, Color lineColor)
        {
            float half = width * 0.5f;
            AddQuad(vertexHelper,
                new Vector2(x - half, yMin),
                new Vector2(x + half, yMax),
                lineColor);
        }

        private void AddHorizontalLine(VertexHelper vertexHelper, float xMin, float xMax, float y, float width, Color lineColor)
        {
            float half = width * 0.5f;
            AddQuad(vertexHelper,
                new Vector2(xMin, y - half),
                new Vector2(xMax, y + half),
                lineColor);
        }

        private void AddQuad(VertexHelper vertexHelper, Vector2 min, Vector2 max, Color lineColor)
        {
            var tint = (Color32)lineColor;
            int start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(min.x, min.y), tint, Vector4.zero);
            vertexHelper.AddVert(new Vector3(max.x, min.y), tint, Vector4.zero);
            vertexHelper.AddVert(new Vector3(max.x, max.y), tint, Vector4.zero);
            vertexHelper.AddVert(new Vector3(min.x, max.y), tint, Vector4.zero);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
