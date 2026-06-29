using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Shop;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Migrates Run scenes to unified shop grid and hides legacy lane rows when present.
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

            var runUiRoot = RunUiAuthoringLock.FindBuildPanel(shopArea);
            if (runUiRoot != null && RunUiAuthoringLock.ShouldPreserve(runUiRoot))
                return;

            var buildPanel = runUiRoot ?? shopArea.parent?.parent;

            if (buildPanel != null)
                ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel, UiThemeProvider.Current);

            HideLegacyLaneRows();
            ConfigureUnifiedOffersGrid();
        }

        private void HideLegacyLaneRows()
        {
            foreach (var rowName in new[] { "OffensiveRow", "DefensiveRow", "SpecialtyRow" })
            {
                var row = shopArea.Find(rowName);
                if (row != null)
                    row.gameObject.SetActive(false);
            }
        }

        private void ConfigureUnifiedOffersGrid()
        {
            var offersGrid = shopArea.Find("OffersGrid") as RectTransform;
            if (offersGrid == null)
            {
                var shopPanel = shopArea.Find("ShopPanel");
                if (shopPanel != null)
                    offersGrid = shopPanel.Find("OffersGrid") as RectTransform;
            }

            if (offersGrid == null)
                return;

            offersGrid.anchorMin = new Vector2(0.04f, 0.08f);
            offersGrid.anchorMax = new Vector2(0.96f, 0.92f);
            offersGrid.offsetMin = Vector2.zero;
            offersGrid.offsetMax = Vector2.zero;
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
