using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Shop;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Migrates older Run scenes to current shop chrome (labels, reroll sizing, margins).
    /// </summary>
    public sealed class ShopUiBootstrap : MonoBehaviour
    {
        private const float ShopRightInset = BuildLayoutMetrics.ShopRightInset;

        [SerializeField] private Transform shopArea;
        [SerializeField] private BoardView boardView;
        [SerializeField] private TMP_Text sharedTooltip;

        public void Configure(Transform shop, BoardView board, TMP_Text tooltip = null)
        {
            shopArea = shop;
            boardView = board;
            sharedTooltip = tooltip;
            Apply();
        }

        private void OnEnable() => Apply();

        public void Apply()
        {
            if (shopArea == null)
                return;

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (sharedTooltip == null)
                sharedTooltip = FindSharedTooltip();

            EnsureSharedTooltipMarker();
            var buildPanel = shopArea.parent?.parent;
            if (buildPanel != null)
                ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel, UiThemeProvider.Current);
            ApplyLaneRows();
            ApplyRerollButtons();
        }

        private TMP_Text FindSharedTooltip()
        {
            var buildPanel = shopArea.parent?.parent;
            var topBar = buildPanel != null ? buildPanel.Find("TopBar") : null;
            if (topBar == null)
                return null;

            var marker = topBar.GetComponentInChildren<ShopSharedTooltip>(true);
            if (marker != null)
                return marker.GetComponent<TMP_Text>();

            var named = topBar.Find("ShopTooltip");
            return named != null ? named.GetComponent<TMP_Text>() : null;
        }

        private static void EnsureSharedTooltipMarker()
        {
            foreach (var marker in FindObjectsByType<ShopSharedTooltip>(FindObjectsSortMode.None))
                return;

            foreach (var label in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (label.transform.parent == null || label.transform.parent.name != "TopBar")
                    continue;

                if (label.fontStyle.HasFlag(FontStyles.Italic))
                {
                    label.gameObject.name = "ShopTooltip";
                    label.gameObject.AddComponent<ShopSharedTooltip>();
                    return;
                }
            }
        }

        private void ApplyLaneRows()
        {
            foreach (var rowName in new[] { "OffensiveRow", "DefensiveRow", "SpecialtyRow" })
            {
                var row = shopArea.Find(rowName) as RectTransform;
                if (row == null)
                    continue;

                row.anchorMax = new Vector2(ShopRightInset, row.anchorMax.y);

                HideLaneLabel(row);

                var offers = row.Find("Offers") as RectTransform;
                if (offers != null)
                {
                    offers.anchorMin = new Vector2(0.04f, 0.14f);
                    offers.anchorMax = new Vector2(0.90f, 0.86f);
                    offers.offsetMin = Vector2.zero;
                    offers.offsetMax = Vector2.zero;
                }
            }
        }

        private static void HideLaneLabel(RectTransform row)
        {
            foreach (var label in row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name is "Offensive" or "Defensive" or "Specialty")
                    label.gameObject.SetActive(false);
            }
        }

        private void ApplyRerollButtons()
        {
            foreach (var rowName in new[] { "OffensiveRow", "DefensiveRow", "SpecialtyRow" })
            {
                var row = shopArea.Find(rowName);
                if (row == null)
                    continue;

                var reroll = row.Find("RerollButton") ?? row.Find("Reroll");
                if (reroll == null)
                    continue;

                var rect = reroll.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.965f, 0.5f);
                    rect.anchorMax = new Vector2(0.965f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                }

                var scaled = reroll.GetComponent<BoardScaledRect>();
                if (scaled == null)
                    scaled = reroll.gameObject.AddComponent<BoardScaledRect>();
                scaled.Configure(boardView, 1, 1);

                var label = reroll.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = "\u21BB";
                    label.fontSize = 22;
                    label.alignment = TextAlignmentOptions.Center;
                }

                if (sharedTooltip != null)
                {
                    var tooltip = reroll.GetComponent<ShopRerollTooltip>();
                    if (tooltip == null)
                        tooltip = reroll.gameObject.AddComponent<ShopRerollTooltip>();
                    tooltip.Configure(sharedTooltip);
                }
                else
                {
                    var tooltip = reroll.GetComponent<ShopRerollTooltip>();
                    if (tooltip == null)
                        tooltip = reroll.gameObject.AddComponent<ShopRerollTooltip>();
                    tooltip.Configure(null);
                }
            }
        }

        public static void EnsureOnShopArea(Transform shop, BoardView board, TMP_Text tooltip = null)
        {
            if (shop == null)
                return;

            var bootstrap = shop.GetComponent<ShopUiBootstrap>();
            if (bootstrap == null)
                bootstrap = shop.gameObject.AddComponent<ShopUiBootstrap>();
            bootstrap.Configure(shop, board, tooltip);
        }
    }
}
