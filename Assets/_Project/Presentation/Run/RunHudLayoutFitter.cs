using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Aligns the run HUD panel with the board grid horizontal edges on the build panel.
    /// </summary>
    public sealed class RunHudLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform hudPanel;
        [SerializeField] private RectTransform buildPanel;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        public void Configure(RectTransform panel, RectTransform panelRoot, BuildRowLayoutFitter rowLayout)
        {
            hudPanel = panel;
            buildPanel = panelRoot;
            mainRowLayout = rowLayout;
            EnsureHudParent();
            ApplyLayout();
        }

        private void OnEnable() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout()
        {
            if (hudPanel == null)
                return;

            if (buildPanel == null)
                buildPanel = transform as RectTransform;

            EnsureHudParent();

            float minX = ResolveBoardMinX();
            float maxX = ResolveBoardMaxX();

            hudPanel.anchorMin = new Vector2(minX, BuildLayoutMetrics.HudPanelAnchorMinY);
            hudPanel.anchorMax = new Vector2(maxX, 1f);
            hudPanel.offsetMin = new Vector2(0f, BuildLayoutMetrics.TopBarHudBottomInsetPixels);
            hudPanel.offsetMax = new Vector2(0f, -BuildLayoutMetrics.TopBarHudTopInsetPixels);
        }

        private float ResolveBoardMinX()
        {
            if (buildPanel != null &&
                mainRowLayout?.BoardView != null &&
                BoardGridBoundsResolver.TryGetHorizontalBoundsInParent(
                    buildPanel,
                    mainRowLayout.BoardView,
                    out float min,
                    out _))
                return min;

            if (mainRowLayout != null && mainRowLayout.BoardGridMaxX > mainRowLayout.BoardGridMinX)
                return mainRowLayout.BoardGridMinX;

            float split = mainRowLayout != null ? mainRowLayout.BoardAnchorMax.x : 0.5f;
            return split * BuildLayoutMetrics.BoardGridHorizontalMin;
        }

        private float ResolveBoardMaxX()
        {
            if (buildPanel != null &&
                mainRowLayout?.BoardView != null &&
                BoardGridBoundsResolver.TryGetHorizontalBoundsInParent(
                    buildPanel,
                    mainRowLayout.BoardView,
                    out _,
                    out float max))
                return max;

            if (mainRowLayout != null && mainRowLayout.BoardGridMaxX > mainRowLayout.BoardGridMinX)
                return mainRowLayout.BoardGridMaxX;

            float split = mainRowLayout != null ? mainRowLayout.BoardAnchorMax.x : 0.5f;
            return split * BuildLayoutMetrics.BoardGridHorizontalMax;
        }

        private void EnsureHudParent()
        {
            if (hudPanel == null || buildPanel == null)
                return;

            if (hudPanel.parent == buildPanel)
                return;

            hudPanel.SetParent(buildPanel, false);
            hudPanel.SetAsLastSibling();
        }

        public static void EnsureOnBuildPanel(
            Transform buildPanelTransform,
            RectTransform hudPanelRect,
            BuildRowLayoutFitter rowLayout)
        {
            if (buildPanelTransform == null || hudPanelRect == null)
                return;

            var panelRoot = buildPanelTransform as RectTransform;
            var fitter = buildPanelTransform.GetComponent<RunHudLayoutFitter>();
            if (fitter == null)
                fitter = buildPanelTransform.gameObject.AddComponent<RunHudLayoutFitter>();
            fitter.Configure(hudPanelRect, panelRoot, rowLayout);
        }
    }
}
