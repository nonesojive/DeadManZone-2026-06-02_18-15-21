using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Clears shop scene backdrop and panel chrome on the build screen.
    /// </summary>
    public static class ShopBackgroundBootstrap
    {
        private const string SceneBackdropName = "ShopSceneBackdrop";
        private const string LegacyShopBackgroundName = "ShopBackground";

        public static void ApplyToBuildPanel(Transform buildPanel, UiThemeSO theme = null, bool simulatePlayMode = false)
        {
            if (buildPanel == null)
                return;

            if (RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanel, simulatePlayMode || Application.isPlaying))
                return;

            theme ??= UiThemeProvider.Current;
            RemoveSceneBackdrop(buildPanel);
            RemoveLegacyShopAreaBackground(buildPanel);
            ClearShopPanelBackgrounds(buildPanel);
            SoftenLaneRows(FindShopArea(buildPanel), theme);
        }

        /// <summary>Backward-compatible entry; forwards to build panel cleanup.</summary>
        public static void Apply(Transform shopArea, UiThemeSO theme = null)
        {
            var buildPanel = shopArea != null ? shopArea.parent?.parent : null;
            if (buildPanel != null)
                ApplyToBuildPanel(buildPanel, theme);
        }

        public static void SetVisible(Transform buildPanel, bool visible)
        {
            if (buildPanel == null)
                return;

            var backdrop = buildPanel.Find(SceneBackdropName);
            if (backdrop != null)
                backdrop.gameObject.SetActive(visible);
        }

        public static void RemoveSceneBackdrop(Transform buildPanel)
        {
            if (buildPanel == null)
                return;

            var backdrop = buildPanel.Find(SceneBackdropName);
            if (backdrop != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(backdrop.gameObject);
                else
#endif
                    Object.Destroy(backdrop.gameObject);
            }
        }

        private static void ClearShopPanelBackgrounds(Transform buildPanel)
        {
            var shopPanel = buildPanel.Find("MainRow/ShopArea/ShopPanel");
            if (shopPanel != null)
            {
                var shopImage = shopPanel.GetComponent<Image>();
                if (shopImage != null)
                {
                    shopImage.sprite = null;
                    shopImage.color = Color.clear;
                }
            }
        }

        private static void RemoveLegacyShopAreaBackground(Transform buildPanel)
        {
            var shopArea = FindShopArea(buildPanel);
            if (shopArea == null)
                return;

            var legacy = shopArea.Find(LegacyShopBackgroundName);
            if (legacy != null)
                Object.Destroy(legacy.gameObject);
        }

        private static Transform FindShopArea(Transform buildPanel) =>
            buildPanel.Find("MainRow/ShopArea");

        private static void SoftenLaneRows(Transform shopArea, UiThemeSO theme)
        {
            if (shopArea == null || theme == null)
                return;

            float laneAlpha = Mathf.Clamp(theme.shopLaneTintScaleWithBackground * 0.35f, 0.04f, 0.18f);

            foreach (var rowName in new[] { "OffensiveRow", "DefensiveRow", "SpecialtyRow" })
            {
                var row = shopArea.Find(rowName);
                if (row == null)
                    continue;

                var rowImage = row.GetComponent<Image>();
                if (rowImage != null)
                    rowImage.color = Color.clear;

                var tintOverlay = row.Find("LaneTint")?.GetComponent<Image>();
                if (tintOverlay != null)
                {
                    var c = tintOverlay.color;
                    c.a = laneAlpha;
                    tintOverlay.color = c;
                }
            }
        }
    }
}
