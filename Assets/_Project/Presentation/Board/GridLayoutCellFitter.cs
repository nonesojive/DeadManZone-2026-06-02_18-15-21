using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Sizes grid cells so columns and rows fill the parent rect (keeps zone chrome aligned with tiles).
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class GridLayoutCellFitter : MonoBehaviour
    {
        [SerializeField] private int columnCount = 9;
        [SerializeField] private int rowCount = 10;

        private GridLayoutGroup _grid;
        private RectTransform _rect;

        public void Configure(int columns, int rows)
        {
            columnCount = Mathf.Max(1, columns);
            rowCount = Mathf.Max(1, rows);
            Apply();
        }

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = GetComponent<RectTransform>();
        }

        private void OnRectTransformDimensionsChange() => Apply();

        private void OnEnable() => Apply();

        private void Apply()
        {
            if (_grid == null)
                _grid = GetComponent<GridLayoutGroup>();
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            if (_grid == null || _rect == null)
                return;

            var size = _rect.rect.size;
            if (size.x <= 1f || size.y <= 1f)
                return;

            float innerW = size.x - _grid.padding.horizontal - _grid.spacing.x * (columnCount - 1);
            float innerH = size.y - _grid.padding.vertical - _grid.spacing.y * (rowCount - 1);
            if (innerW <= 0f || innerH <= 0f)
                return;

            float cellW = innerW / columnCount;
            float cellH = innerH / rowCount;
            float cell = Mathf.Min(cellW, cellH);
            _grid.cellSize = new Vector2(cell, cell);
        }
    }
}
