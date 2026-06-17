using DeadManZone.Presentation.Board;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Shrinks the board column and expands the shop so no dead space sits between them.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BuildRowLayoutFitter : MonoBehaviour
    {
        private const float MinSplit = 0.20f;
        private const float MaxSplit = 0.78f;
        private const int MaxBuildBoardColumns = 12;

        [SerializeField] private RectTransform boardArea;
        [SerializeField] private RectTransform centerArea;
        [SerializeField] private RectTransform shopArea;
        [SerializeField] private BoardView boardView;
        [SerializeField] private float shopGapPixels = 4f;
        [SerializeField] private float boardOuterPaddingPixels = 6f;

        private GridLayoutCellFitter _cellFitter;
        private int _applyPass;

        public Vector2 BoardAnchorMax { get; private set; } = new(0.5f, 1f);

        public BoardView BoardView => boardView;

        /// <summary>Normalized horizontal grid bounds on the build panel (0–1).</summary>
        public float BoardGridMinX { get; private set; }

        public float BoardGridMaxX { get; private set; } = 0.5f;

        public float CenterColumnMinX { get; private set; }

        public float CenterColumnMaxX { get; private set; }

        public float ShopColumnMinX { get; private set; }

        public void Configure(RectTransform board, RectTransform center, RectTransform shop, BoardView view)
        {
            boardArea = board;
            centerArea = center;
            shopArea = shop;
            boardView = view;
            _cellFitter = null;
            _applyPass = 0;
            ApplyLayout();
        }

        public void Configure(RectTransform board, RectTransform shop, BoardView view)
        {
            Configure(board, null, shop, view);
        }

        private void OnEnable()
        {
            _applyPass = 0;
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (_applyPass >= 2)
                return;

            ApplyLayout();
            _applyPass++;
        }

        private void OnRectTransformDimensionsChange()
        {
            _applyPass = 0;
            ApplyLayout();
        }

        public void InvalidateAndApply()
        {
            _applyPass = 0;
            ApplyLayout();
        }

        public void CacheAuthoringMetricsFromScene()
        {
            if (boardArea != null)
                BoardAnchorMax = boardArea.anchorMax;

            if (centerArea != null)
            {
                CenterColumnMinX = centerArea.anchorMin.x;
                CenterColumnMaxX = centerArea.anchorMax.x;
            }
            else if (shopArea != null)
            {
                CenterColumnMinX = boardArea != null ? boardArea.anchorMax.x : 0f;
                CenterColumnMaxX = shopArea.anchorMin.x;
            }

            if (shopArea != null)
                ShopColumnMinX = shopArea.anchorMin.x;

            if (boardArea != null)
            {
                BoardGridMinX = boardArea.anchorMin.x;
                BoardGridMaxX = boardArea.anchorMax.x;
            }
        }

        public void ApplyLayout()
        {
            if (RunUiAuthoringLock.ShouldSkipVisualMigration(GetBuildPanel()))
            {
                CacheAuthoringMetricsFromScene();
                SyncBoardGameplayLayout();
                return;
            }

            if (boardArea == null || shopArea == null || boardView == null)
                return;

            var grid = boardView.GridLayout;
            if (grid == null)
                return;

            var mainRow = GetComponent<RectTransform>();
            if (mainRow == null || mainRow.rect.width <= 1f)
                return;

            Canvas.ForceUpdateCanvases();

            if (_cellFitter == null)
                _cellFitter = grid.GetComponent<GridLayoutCellFitter>();

            int columns = grid.constraintCount;
            if (columns <= 0 && _cellFitter != null)
                columns = _cellFitter.ColumnCount;
            if (columns <= 0)
                columns = 9;

            var gridRect = grid.GetComponent<RectTransform>();
            float contentWidth = ResolveContentWidthPx(gridRect, grid, columns);
            if (contentWidth <= 1f)
                return;
            float gridSpan = gridRect.anchorMax.x - gridRect.anchorMin.x;
            if (gridSpan <= 0.01f)
                gridSpan = 0.96f;

            float boardWidthPx = contentWidth / gridSpan + boardOuterPaddingPixels;
            float split = (boardWidthPx + shopGapPixels) / mainRow.rect.width;
            float centerFrac = BuildLayoutMetrics.CenterColumnFraction;
            float shopMinFrac = BuildLayoutMetrics.ShopColumnMinFraction;
            split = Mathf.Clamp(split, MinSplit, MaxSplit);
            split = Mathf.Min(split, 1f - centerFrac - shopMinFrac);

            float centerEnd = split + centerFrac;

            boardArea.anchorMin = new Vector2(0f, boardArea.anchorMin.y);
            boardArea.anchorMax = new Vector2(split, boardArea.anchorMax.y);
            boardArea.offsetMin = Vector2.zero;
            boardArea.offsetMax = Vector2.zero;

            if (centerArea != null)
            {
                centerArea.anchorMin = new Vector2(split, centerArea.anchorMin.y);
                centerArea.anchorMax = new Vector2(centerEnd, centerArea.anchorMax.y);
                centerArea.offsetMin = Vector2.zero;
                centerArea.offsetMax = Vector2.zero;
            }

            shopArea.anchorMin = new Vector2(centerEnd, shopArea.anchorMin.y);
            shopArea.anchorMax = new Vector2(1f, shopArea.anchorMax.y);
            shopArea.offsetMin = Vector2.zero;
            shopArea.offsetMax = Vector2.zero;

            BoardAnchorMax = boardArea.anchorMax;
            CenterColumnMinX = split;
            CenterColumnMaxX = centerEnd;
            ShopColumnMinX = centerEnd;

            float gridMin = gridRect.anchorMin.x;
            float gridMax = gridRect.anchorMax.x;
            BoardGridMinX = split * gridMin;
            BoardGridMaxX = split * gridMax;

            var buildPanel = transform.parent as RectTransform;
            if (buildPanel != null &&
                BoardGridBoundsResolver.TryGetHorizontalBoundsInParent(buildPanel, boardView, out float resolvedMin, out float resolvedMax))
            {
                BoardGridMinX = resolvedMin;
                BoardGridMaxX = resolvedMax;
            }

            boardView?.GetComponent<BoardZoneStripLayout>()?.ApplyLayout();

            if (columns <= MaxBuildBoardColumns)
                boardView?.SyncLayoutFromBoard();

            transform.parent?.GetComponent<CenterColumnLayoutFitter>()?.ApplyLayout();
            transform.parent?.GetComponent<RunHudLayoutFitter>()?.ApplyLayout();
            transform.parent?.GetComponent<ReservesLayoutFitter>()?.ApplyLayout();
        }

        private void SyncBoardGameplayLayout()
        {
            if (boardView == null)
                return;

            var grid = boardView.GridLayout;
            if (grid == null)
                return;

            if (_cellFitter == null)
                _cellFitter = grid.GetComponent<GridLayoutCellFitter>();

            int columns = grid.constraintCount;
            if (columns <= 0 && _cellFitter != null)
                columns = _cellFitter.ColumnCount;

            boardView.GetComponent<BoardZoneStripLayout>()?.ApplyLayout();

            if (columns > 0 && columns <= MaxBuildBoardColumns)
                boardView.SyncLayoutFromBoard();
        }

        private Transform GetBuildPanel() => transform.parent;

        private static float ResolveContentWidthPx(RectTransform gridRect, GridLayoutGroup grid, int columns)
        {
            if (gridRect == null || grid == null || columns <= 0)
                return 0f;

            float left = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, 0, left: true);
            float right = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, columns - 1, left: false);
            return Mathf.Max(0f, right - left);
        }
    }
}
