using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Aligns bottom-bar info message region with the main-row center column.</summary>
    public sealed class CenterColumnLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform buildPanel;
        [SerializeField] private RectTransform infoMessageRegion;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        public void Configure(
            RectTransform panel,
            RectTransform infoRegion,
            BuildRowLayoutFitter rowLayout)
        {
            buildPanel = panel;
            infoMessageRegion = infoRegion;
            mainRowLayout = rowLayout;
            ApplyLayout();
        }

        private void OnEnable() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout()
        {
            if (buildPanel != null && RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanel))
                return;

            if (mainRowLayout == null || infoMessageRegion == null)
                return;

            float minX = mainRowLayout.CenterColumnMinX;
            float maxX = mainRowLayout.CenterColumnMaxX;
            if (maxX <= minX + 0.001f)
                return;

            infoMessageRegion.anchorMin = new Vector2(minX, 0f);
            infoMessageRegion.anchorMax = new Vector2(maxX, 1f);
            infoMessageRegion.offsetMin = new Vector2(4f, 4f);
            infoMessageRegion.offsetMax = new Vector2(-4f, -4f);
        }

        public static void EnsureOnBuildPanel(
            Transform buildPanelRoot,
            RectTransform infoRegion,
            BuildRowLayoutFitter rowLayout)
        {
            if (buildPanelRoot == null)
                return;

            var fitter = buildPanelRoot.GetComponent<CenterColumnLayoutFitter>();
            if (fitter == null)
                fitter = buildPanelRoot.gameObject.AddComponent<CenterColumnLayoutFitter>();

            fitter.Configure(
                buildPanelRoot as RectTransform,
                infoRegion,
                rowLayout);
        }
    }
}
