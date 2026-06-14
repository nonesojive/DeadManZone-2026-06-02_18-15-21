using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Aligns center-column UI (messages, unit card, buff strip) with the main-row center column.
    /// </summary>
    public sealed class CenterColumnLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform buildPanel;
        [SerializeField] private RectTransform messagesPanel;
        [SerializeField] private RectTransform buffStripRegion;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        public void Configure(
            RectTransform panel,
            RectTransform messages,
            RectTransform buffStrip,
            BuildRowLayoutFitter rowLayout)
        {
            buildPanel = panel;
            messagesPanel = messages;
            buffStripRegion = buffStrip;
            mainRowLayout = rowLayout;
            ApplyLayout();
        }

        private void OnEnable() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout()
        {
            if (mainRowLayout == null)
                return;

            float minX = mainRowLayout.CenterColumnMinX;
            float maxX = mainRowLayout.CenterColumnMaxX;
            if (maxX <= minX + 0.001f)
                return;

            if (messagesPanel != null)
            {
                messagesPanel.anchorMin = new Vector2(minX, BuildLayoutMetrics.TopBarAnchorMinY);
                messagesPanel.anchorMax = new Vector2(maxX, 1f);
                messagesPanel.offsetMin = new Vector2(4f, BuildLayoutMetrics.TopBarHudBottomInsetPixels);
                messagesPanel.offsetMax = new Vector2(-4f, -BuildLayoutMetrics.TopBarHudTopInsetPixels);
            }

            if (buffStripRegion != null)
            {
                buffStripRegion.anchorMin = new Vector2(minX, 0f);
                buffStripRegion.anchorMax = new Vector2(maxX, 1f);
                buffStripRegion.offsetMin = Vector2.zero;
                buffStripRegion.offsetMax = Vector2.zero;
            }
        }

        public static void EnsureOnBuildPanel(
            Transform buildPanelRoot,
            RectTransform messages,
            RectTransform buffStrip,
            BuildRowLayoutFitter rowLayout)
        {
            if (buildPanelRoot == null)
                return;

            var fitter = buildPanelRoot.GetComponent<CenterColumnLayoutFitter>();
            if (fitter == null)
                fitter = buildPanelRoot.gameObject.AddComponent<CenterColumnLayoutFitter>();

            fitter.Configure(
                buildPanelRoot as RectTransform,
                messages,
                buffStrip,
                rowLayout);
        }
    }
}
