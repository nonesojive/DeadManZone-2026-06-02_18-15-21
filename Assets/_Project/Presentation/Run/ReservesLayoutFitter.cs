using DeadManZone.Core.Board;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Aligns reserves strip and grid with board column width and horizontal bounds.
    /// </summary>
    public sealed class ReservesLayoutFitter : MonoBehaviour
    {
        private const int MaxBuildBoardColumns = 12;

        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private RectTransform buildPanel;
        [SerializeField] private RectTransform reservesRegion;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;
        [SerializeField] private BoardView boardView;

        public void Configure(
            RectTransform bar,
            RectTransform panelRoot,
            RectTransform region,
            BuildRowLayoutFitter rowLayout,
            BoardView board)
        {
            bottomBar = bar;
            buildPanel = panelRoot;
            reservesRegion = region;
            mainRowLayout = rowLayout;
            boardView = board;
            ApplyLayout();
        }

        private void OnEnable() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout()
        {
            if (bottomBar == null || reservesRegion == null)
                return;

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (boardView?.GridLayout != null &&
                boardView.GridLayout.constraintCount > MaxBuildBoardColumns)
                return;

            if (buildPanel == null)
                buildPanel = transform as RectTransform;

            reservesRegion.gameObject.SetActive(true);

            float boardLeft = ResolveBoardMinX();
            float boardRight = ResolveBoardMaxX();
            if (boardRight <= boardLeft + 0.001f)
                return;

            reservesRegion.anchorMin = new Vector2(boardLeft, 0f);
            reservesRegion.anchorMax = new Vector2(boardRight, 1f);
            reservesRegion.offsetMin = Vector2.zero;
            reservesRegion.offsetMax = Vector2.zero;

            LayoutRebuilder.ForceRebuildLayoutImmediate(reservesRegion);
            Canvas.ForceUpdateCanvases();

            ReservesLabelStripFactory.Ensure(reservesRegion, UiThemeProvider.Current);

            var strip = reservesRegion.Find(ReservesLabelStripFactory.StripName) as RectTransform;
            var grid = reservesRegion.Find("ReservesGrid") as RectTransform;
            if (strip == null || grid == null)
                return;

            strip.gameObject.SetActive(true);
            grid.gameObject.SetActive(true);

            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
                gridLayout.constraintCount = ReservesState.Width;

            SyncReservesGridFromBoard(gridLayout, boardView);

            var cellFitter = grid.GetComponent<GridLayoutCellFitter>();
            if (cellFitter != null)
                cellFitter.enabled = false;

            float contentHeight = gridLayout != null
                ? GridLayoutContentMetrics.ContentHeight(gridLayout, ReservesState.Height)
                : 0f;

            float yMin = 0.08f;
            float yMax = 0.92f;
            float barHeight = bottomBar.rect.height;
            if (barHeight > 1f && contentHeight > 1f)
            {
                float halfSpan = Mathf.Min(0.45f, (contentHeight * 0.5f) / barHeight);
                yMin = 0.5f - halfSpan;
                yMax = 0.5f + halfSpan;
            }

            if (boardView != null &&
                BoardGridBoundsResolver.TryProjectBoardColumnRange(boardView, reservesRegion, 0, 0, out float stripMinX, out float stripMaxX))
            {
                int boardColumns = boardView.GridLayout.constraintCount;
                int gridStartCol = 1;
                int gridEndCol = Mathf.Max(gridStartCol, boardColumns - 1);

                if (BoardGridBoundsResolver.TryProjectBoardColumnRange(
                        boardView,
                        reservesRegion,
                        gridStartCol,
                        gridEndCol,
                        out float gridMinX,
                        out float gridMaxX))
                {
                    strip.anchorMin = new Vector2(stripMinX, yMin);
                    strip.anchorMax = new Vector2(stripMaxX, yMax);
                    grid.anchorMin = new Vector2(gridMinX, yMin);
                    grid.anchorMax = new Vector2(gridMaxX, yMax);
                }
                else
                {
                    ApplyFallbackStripLayout(strip, grid, yMin, yMax);
                }
            }
            else
            {
                ApplyFallbackStripLayout(strip, grid, yMin, yMax);
            }

            strip.offsetMin = Vector2.zero;
            strip.offsetMax = Vector2.zero;
            grid.offsetMin = Vector2.zero;
            grid.offsetMax = Vector2.zero;

            var reservesView = reservesRegion.GetComponent<ReservesView>();
            reservesView?.SyncLayoutFromBoard();
            reservesView?.Refresh();
        }

        private static void ApplyFallbackStripLayout(RectTransform strip, RectTransform grid, float yMin, float yMax)
        {
            float stripFraction = 1f / (ReservesState.Width + 1f);
            grid.anchorMin = new Vector2(stripFraction, yMin);
            grid.anchorMax = new Vector2(1f, yMax);
            strip.anchorMin = new Vector2(0f, yMin);
            strip.anchorMax = new Vector2(stripFraction, yMax);
        }

        private static void SyncReservesGridFromBoard(GridLayoutGroup reservesGrid, BoardView boardView)
        {
            var boardGrid = boardView?.GridLayout;
            if (boardGrid == null || reservesGrid == null)
                return;

            reservesGrid.cellSize = boardGrid.cellSize;
            reservesGrid.spacing = boardGrid.spacing;
            reservesGrid.padding = new RectOffset(0, 0, 0, 0);
        }

        private float ResolveBoardMinX()
        {
            if (buildPanel != null &&
                boardView != null &&
                BoardGridBoundsResolver.TryGetHorizontalBoundsInParent(buildPanel, boardView, out float min, out _))
                return min;

            if (mainRowLayout != null && mainRowLayout.BoardGridMaxX > mainRowLayout.BoardGridMinX)
                return mainRowLayout.BoardGridMinX;

            float split = mainRowLayout != null ? mainRowLayout.BoardAnchorMax.x : 0.5f;
            return split * BuildLayoutMetrics.BoardGridHorizontalMin;
        }

        private float ResolveBoardMaxX()
        {
            if (buildPanel != null &&
                boardView != null &&
                BoardGridBoundsResolver.TryGetHorizontalBoundsInParent(buildPanel, boardView, out _, out float max))
                return max;

            if (mainRowLayout != null && mainRowLayout.BoardGridMaxX > mainRowLayout.BoardGridMinX)
                return mainRowLayout.BoardGridMaxX;

            float split = mainRowLayout != null ? mainRowLayout.BoardAnchorMax.x : 0.5f;
            return split * BuildLayoutMetrics.BoardGridHorizontalMax;
        }

        public static void EnsureOnBuildPanel(
            Transform buildPanelTransform,
            RectTransform bottomBarRect,
            RectTransform reservesRegionRect,
            BuildRowLayoutFitter rowLayout,
            BoardView board)
        {
            if (buildPanelTransform == null || bottomBarRect == null || reservesRegionRect == null)
                return;

            var panelRoot = buildPanelTransform as RectTransform;
            var fitter = buildPanelTransform.GetComponent<ReservesLayoutFitter>();
            if (fitter == null)
                fitter = buildPanelTransform.gameObject.AddComponent<ReservesLayoutFitter>();
            fitter.Configure(bottomBarRect, panelRoot, reservesRegionRect, rowLayout, board);
        }
    }
}
