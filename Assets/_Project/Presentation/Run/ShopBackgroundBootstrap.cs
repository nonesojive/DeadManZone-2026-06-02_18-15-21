using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Applies bunker backdrop across the full build/shop screen (board, reserves, shop, HUD).
    /// </summary>
    public static class ShopBackgroundBootstrap
    {
        private const string SceneBackdropName = "ShopSceneBackdrop";
        private const string ScrimName = "ShopSceneScrim";
        private const string LegacyShopBackgroundName = "ShopBackground";

        public static void ApplyToBuildPanel(Transform buildPanel, UiThemeSO theme = null)
        {
            if (buildPanel == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (theme.shopBackgroundSprite == null)
                return;

            RemoveLegacyShopAreaBackground(buildPanel);
            EnsureSceneBackdrop(buildPanel, theme);
            ClearPanelBackgrounds(buildPanel);
            SoftenLaneRows(FindShopArea(buildPanel), theme);
        }

        /// <summary>Backward-compatible entry; forwards to the full shop-scene backdrop.</summary>
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

        private static void EnsureSceneBackdrop(Transform buildPanel, UiThemeSO theme)
        {
            var bgTransform = buildPanel.Find(SceneBackdropName);
            Image bgImage;
            if (bgTransform == null)
            {
                var bgGo = new GameObject(SceneBackdropName, typeof(RectTransform));
                bgGo.transform.SetParent(buildPanel, false);
                bgGo.transform.SetAsFirstSibling();
                Stretch(bgGo.GetComponent<RectTransform>());
                bgImage = bgGo.AddComponent<Image>();
                bgImage.raycastTarget = false;

                var scrimGo = new GameObject(ScrimName, typeof(RectTransform));
                scrimGo.transform.SetParent(bgGo.transform, false);
                Stretch(scrimGo.GetComponent<RectTransform>());
                var scrimImage = scrimGo.AddComponent<Image>();
                scrimImage.raycastTarget = false;
            }
            else
            {
                bgImage = bgTransform.GetComponent<Image>();
                Stretch(bgTransform.GetComponent<RectTransform>());
            }

            if (bgImage == null)
                return;

            bgImage.sprite = theme.shopBackgroundSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.color = Color.white;

            var scrim = bgImage.transform.Find(ScrimName)?.GetComponent<Image>();
            if (scrim != null)
                scrim.color = theme.shopBackgroundScrimColor;
        }

        private static void ClearPanelBackgrounds(Transform buildPanel)
        {
            var panelImage = buildPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = Color.clear;

            foreach (var barName in new[] { "TopBar", "BottomBar" })
            {
                var bar = buildPanel.Find(barName);
                if (bar == null)
                    continue;

                var image = bar.GetComponent<Image>();
                if (image != null)
                    image.color = Color.clear;
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
            if (shopArea == null)
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
