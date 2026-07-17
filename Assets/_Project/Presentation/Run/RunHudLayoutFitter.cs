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
            // Force: EnsureHudParent just moved hudPanel onto buildPanel (full-screen), which
            // still carries its pre-reparent stretch anchors (0,0)-(1,1) until the anchor fix
            // below runs. The authoring lock exists to stop LATER re-migration from fighting a
            // hand-tweaked scene, not to block finishing the reparent this same call started —
            // skipping here left the panel full-screen-stretched (RunSceneSetup calls this
            // right after RunUiAuthoringLock.EnsureOn locks the fresh scene, so the very first
            // ApplyLayout was always skipped and the HUD scattered over the whole screen).
            ApplyLayoutInternal(force: true);
        }

        private void OnEnable() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout() => ApplyLayoutInternal(force: false);

        private void ApplyLayoutInternal(bool force)
        {
            if (!force && buildPanel != null && RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanel))
                return;

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
