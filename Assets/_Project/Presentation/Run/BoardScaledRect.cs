using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Sizes a RectTransform to match N board grid cells (including spacing).
    /// </summary>
    public sealed class BoardScaledRect : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private int cellColumns = 1;
        [SerializeField] private int cellRows = 1;
        [SerializeField] private float sizeScale = 1f;

        private RectTransform _rect;
        private int _applyPass;

        public void Configure(BoardView view, int columns, int rows, float scale = 1f)
        {
            boardView = view;
            cellColumns = Mathf.Max(1, columns);
            cellRows = Mathf.Max(1, rows);
            sizeScale = Mathf.Max(0.5f, scale);
            _applyPass = 0;
            ApplySize();
        }

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();
        }

        private void OnEnable()
        {
            _applyPass = 0;
            ApplySize();
        }

        private void LateUpdate()
        {
            if (_applyPass >= 2)
                return;

            ApplySize();
            _applyPass++;
        }

        public void ApplySize()
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            if (_rect == null)
                return;

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            float cellW = boardView != null ? boardView.CellSize.x : 48f;
            float cellH = boardView != null ? boardView.CellSize.y : 48f;
            float gapX = boardView != null ? boardView.CellSpacing.x : 3f;
            float gapY = boardView != null ? boardView.CellSpacing.y : 3f;

            float width = cellColumns * cellW + (cellColumns - 1) * gapX;
            float height = cellRows * cellH + (cellRows - 1) * gapY;
            _rect.sizeDelta = new Vector2(width * sizeScale, height * sizeScale);
        }
    }
}
