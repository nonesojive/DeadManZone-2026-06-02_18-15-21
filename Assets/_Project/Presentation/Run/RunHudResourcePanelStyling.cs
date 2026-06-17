using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Opaque backing plate behind the run HUD frame so build UI does not show through.
    /// </summary>
    public static class RunHudResourcePanelStyling
    {
        public const string ResourceGridName = "ResourceGrid";
        public const string ResourcePanelBackgroundName = "ResourcePanelBackground";
        public const string HudPanelFrameName = "HudPanelFrame";

        public static void EnsureBackground(Transform buildPanelRoot, UiThemeSO theme = null, bool simulatePlayMode = false)
        {
            if (buildPanelRoot == null)
                return;

            if (RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanelRoot, simulatePlayMode || Application.isPlaying))
                return;

            theme ??= UiThemeProvider.Current;
            if (theme == null)
                return;

            var hudPanel = FindHudPanel(buildPanelRoot);
            if (hudPanel == null)
                return;

            EnsureLayers(hudPanel, theme, simulatePlayMode);
        }

        public static void EnsureLayers(Transform hudPanel, UiThemeSO theme, bool simulatePlayMode = false)
        {
            if (hudPanel == null || theme == null)
                return;

            if (RunUiAuthoringLock.ShouldSkipVisualMigration(
                    RunUiAuthoringLock.FindBuildPanel(hudPanel),
                    simulatePlayMode || Application.isPlaying))
                return;

            StripLegacyResourceGridBackground(hudPanel);
            EnsureBackingPlate(hudPanel, theme);
            PromoteRootImageToFrameChild(hudPanel, theme);
            OrderLayers(hudPanel);
        }

        public static void EnsureBackingPlate(Transform hudPanel, UiThemeSO theme)
        {
            if (hudPanel == null || theme == null)
                return;

            var background = hudPanel.Find(ResourcePanelBackgroundName);
            if (background == null)
            {
                var go = new GameObject(ResourcePanelBackgroundName, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(hudPanel, false);
                background = go.transform;
            }

            StretchFull(background.GetComponent<RectTransform>());

            var image = background.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = theme.resourcePanelBackgroundColor;
        }

        private static Transform FindHudPanel(Transform buildPanelRoot)
        {
            var hudPanel = buildPanelRoot.Find(RunHudPanelBuilder.PanelName);
            if (hudPanel == null)
                hudPanel = buildPanelRoot.Find("TopBar/" + RunHudPanelBuilder.PanelName);
            return hudPanel;
        }

        private static void StripLegacyResourceGridBackground(Transform hudPanel)
        {
            var resourceGrid = hudPanel.Find(ResourceGridName);
            if (resourceGrid == null)
                return;

            var legacyImage = resourceGrid.GetComponent<Image>();
            if (legacyImage == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(legacyImage);
            else
#endif
                Object.Destroy(legacyImage);
        }

        private static void PromoteRootImageToFrameChild(Transform hudPanel, UiThemeSO theme)
        {
            var rootImage = hudPanel.GetComponent<Image>();
            if (rootImage == null)
            {
                EnsureFrameChild(hudPanel, theme);
                return;
            }

            var frame = EnsureFrameChild(hudPanel, theme);
            CopyImageSettings(rootImage, frame.GetComponent<Image>());

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(rootImage);
            else
#endif
                Object.Destroy(rootImage);
        }

        private static Transform EnsureFrameChild(Transform hudPanel, UiThemeSO theme)
        {
            var frame = hudPanel.Find(HudPanelFrameName);
            if (frame == null)
            {
                var go = new GameObject(HudPanelFrameName, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(hudPanel, false);
                frame = go.transform;
            }

            StretchFull(frame.GetComponent<RectTransform>());

            var image = frame.GetComponent<Image>();
            image.raycastTarget = false;
            if (image.sprite == null)
                RunHudPanelBuilder.ApplyFrameStyle(image, theme);

            return frame;
        }

        private static void OrderLayers(Transform hudPanel)
        {
            var background = hudPanel.Find(ResourcePanelBackgroundName);
            var frame = hudPanel.Find(HudPanelFrameName);
            var chrome = hudPanel.Find(UiPanelChrome.ChromeRootName);

            if (background != null)
                background.SetAsFirstSibling();

            if (frame != null)
            {
                int index = background != null ? background.GetSiblingIndex() + 1 : 0;
                frame.SetSiblingIndex(index);
            }

            if (chrome != null)
                chrome.SetAsLastSibling();
        }

        private static void CopyImageSettings(Image source, Image destination)
        {
            if (source == null || destination == null)
                return;

            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.material = source.material;
            destination.type = source.type;
            destination.preserveAspect = source.preserveAspect;
            destination.fillCenter = source.fillCenter;
            destination.fillMethod = source.fillMethod;
            destination.fillAmount = source.fillAmount;
            destination.fillClockwise = source.fillClockwise;
            destination.fillOrigin = source.fillOrigin;
            destination.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            destination.raycastTarget = source.raycastTarget;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
