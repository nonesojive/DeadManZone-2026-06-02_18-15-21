using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class RunHudResourcePanelStylingTests
    {
        [Test]
        public void EnsureLayers_SkipsWhenPlayModeAuthoringPreserved()
        {
            var buildPanel = new GameObject("BuildPanel");
            buildPanel.AddComponent<RunUiAuthoringLock>();

            var hudPanel = new GameObject(RunHudPanelBuilder.PanelName, typeof(RectTransform), typeof(Image));
            hudPanel.transform.SetParent(buildPanel.transform, false);
            var rootImage = hudPanel.GetComponent<Image>();
            rootImage.color = new Color(0.92f, 0.91f, 0.88f, 1f);

            var theme = ScriptableObject.CreateInstance<UiThemeSO>();
            theme.resourcePanelBackgroundColor = new Color(0.24f, 0.16f, 0.09f, 1f);

            try
            {
                RunHudResourcePanelStyling.EnsureBackground(buildPanel.transform, theme, simulatePlayMode: true);

                Assert.NotNull(hudPanel.GetComponent<Image>());
                Assert.IsNull(hudPanel.transform.Find(RunHudResourcePanelStyling.ResourcePanelBackgroundName));
            }
            finally
            {
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void EnsureLayers_PreservesRootFrameAndAddsFullPanelBacking()
        {
            var hudPanel = new GameObject(RunHudPanelBuilder.PanelName, typeof(RectTransform), typeof(Image));
            var rootImage = hudPanel.GetComponent<Image>();
            var expectedColor = new Color(0.92f, 0.91f, 0.88f, 1f);
            var expectedSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f));
            rootImage.color = expectedColor;
            rootImage.sprite = expectedSprite;

            var resourceGrid = new GameObject("ResourceGrid", typeof(RectTransform));
            resourceGrid.transform.SetParent(hudPanel.transform, false);

            var theme = ScriptableObject.CreateInstance<UiThemeSO>();
            theme.resourcePanelBackgroundColor = new Color(0.24f, 0.16f, 0.09f, 1f);

            try
            {
                RunHudResourcePanelStyling.EnsureLayers(hudPanel.transform, theme);

                var background = hudPanel.transform.Find(RunHudResourcePanelStyling.ResourcePanelBackgroundName);
                var frame = hudPanel.transform.Find(RunHudResourcePanelStyling.HudPanelFrameName);
                Assert.NotNull(background);
                Assert.NotNull(frame);
                Assert.Less(background.GetSiblingIndex(), frame.GetSiblingIndex());
                Assert.IsNull(hudPanel.GetComponent<Image>());
                Assert.IsNull(resourceGrid.GetComponent<Image>());

                var backgroundRect = background.GetComponent<RectTransform>();
                Assert.AreEqual(Vector2.zero, backgroundRect.anchorMin);
                Assert.AreEqual(Vector2.one, backgroundRect.anchorMax);
                Assert.AreEqual(theme.resourcePanelBackgroundColor, background.GetComponent<Image>().color);

                var frameImage = frame.GetComponent<Image>();
                Assert.AreEqual(expectedColor, frameImage.color);
                Assert.AreEqual(expectedSprite, frameImage.sprite);
            }
            finally
            {
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(hudPanel);
            }
        }
    }
}
