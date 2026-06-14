using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Migrates older Run scenes for HUD layout, sell zone placement, and reserves strip.
    /// </summary>
    public sealed class RunBuildUiBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform buildPanel;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        public void Configure(Transform panel, BoardView board, BuildRowLayoutFitter rowLayout = null)
        {
            buildPanel = panel;
            boardView = board;
            mainRowLayout = rowLayout;
            Apply();
        }

        private void OnEnable() => Apply();

        public void Apply()
        {
            if (buildPanel == null)
                return;

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (mainRowLayout == null)
                mainRowLayout = buildPanel.Find("MainRow")?.GetComponent<BuildRowLayoutFitter>();

            ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel, UiThemeProvider.Current);
            MoveLastLogToTopBar();
            ApplyRunHudPanel();
            ApplyReservesLayout();
            ApplySellZoneSize();
            ApplyCenterColumnLayout();
            ApplyCombatButtonLabel();
            EnsureBuildScreenHudController();
            BuildUiChromeBootstrap.Apply(buildPanel);
        }

        private void ApplyCenterColumnLayout()
        {
            var messages = buildPanel.GetComponentInChildren<BuildMessagesView>(true);
            var buffStrip = buildPanel.transform.Find("BottomBar/BuffStripRegion") as RectTransform;
            if (buffStrip == null)
                buffStrip = buildPanel.GetComponentInChildren<BuffIconStripView>(true)?.GetComponent<RectTransform>();

            if (messages != null || buffStrip != null)
                CenterColumnLayoutFitter.EnsureOnBuildPanel(
                    buildPanel,
                    messages != null ? messages.GetComponent<RectTransform>() : null,
                    buffStrip,
                    mainRowLayout);
        }

        private void ApplyCombatButtonLabel()
        {
            var bottomBar = buildPanel.Find("BottomBar");
            if (bottomBar == null)
                return;

            foreach (var button in bottomBar.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label == null || !label.text.Contains("Begin Fight"))
                    continue;

                label.text = "COMBAT";
                break;
            }
        }

        private void EnsureBuildScreenHudController()
        {
            var controller = buildPanel.GetComponent<BuildScreenHudController>();
            if (controller == null)
                controller = buildPanel.gameObject.AddComponent<BuildScreenHudController>();
            controller.Configure(
                buildPanel,
                boardView,
                buildPanel.GetComponentInChildren<UnitCardPanelView>(true),
                buildPanel.GetComponentInChildren<BuildMessagesView>(true),
                buildPanel.GetComponentInChildren<BuffIconStripView>(true));
        }

        private const float LastLogAnchorX = 0.895f;
        private const float LastLogWidth = 124f;

        private void MoveLastLogToTopBar()
        {
            var topBar = buildPanel.Find("TopBar");
            if (topBar == null)
                return;

            Button lastLog = null;
            foreach (var button in topBar.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null && label.text.Contains("Last Log"))
                {
                    lastLog = button;
                    break;
                }
            }

            if (lastLog == null)
            {
                var bottomBar = buildPanel.Find("BottomBar");
                if (bottomBar == null)
                    return;

                foreach (var button in bottomBar.GetComponentsInChildren<Button>(true))
                {
                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null && label.text.Contains("Last Log"))
                    {
                        lastLog = button;
                        break;
                    }
                }
            }

            if (lastLog == null)
                return;

            var rect = lastLog.GetComponent<RectTransform>();
            rect.SetParent(topBar, false);
            rect.anchorMin = new Vector2(LastLogAnchorX, 0.5f);
            rect.anchorMax = new Vector2(LastLogAnchorX, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(LastLogWidth, 40f);

            var text = lastLog.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void ApplyRunHudPanel()
        {
            if (buildPanel == null)
                return;

            var topBar = buildPanel.Find("TopBar");
            var hud = topBar != null ? topBar.GetComponent<RunHudView>() : null;
            if (hud == null && topBar != null)
                hud = topBar.gameObject.AddComponent<RunHudView>();

            Transform panel = buildPanel.Find(RunHudPanelBuilder.PanelName);
            if (panel == null && topBar != null)
                panel = topBar.Find(RunHudPanelBuilder.PanelName);

            if (RunHudPanelBuilder.NeedsRebuild(buildPanel))
            {
                if (topBar != null)
                    RemoveLegacyStatusText(topBar);

                var built = RunHudPanelBuilder.Create(buildPanel, UiThemeProvider.Current);
                if (hud != null)
                {
                    RunHudPanelBuilder.WireRunHudView(hud, built);
                    hud.ApplyTheme(UiThemeProvider.Current);
                }

                panel = built.Root;
            }
            else if (panel != null)
            {
                var frame = panel.GetComponent<Image>();
                RunHudPanelBuilder.ApplyFrameStyle(frame, UiThemeProvider.Current);
            }

            if (panel is RectTransform panelRect)
                RunHudLayoutFitter.EnsureOnBuildPanel(buildPanel, panelRect, mainRowLayout);
        }

        private static void RemoveLegacyStatusText(Transform topBar)
        {
            foreach (var text in topBar.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.parent != topBar)
                    continue;

                if (text.fontStyle.HasFlag(FontStyles.Italic))
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(text.gameObject);
                else
#endif
                    Object.Destroy(text.gameObject);
            }
        }

        private void ApplyReservesLayout()
        {
            var bottomBar = buildPanel.Find("BottomBar") as RectTransform;
            if (bottomBar == null)
                return;

            var reservesRegion = bottomBar.Find("ReservesRegion") as RectTransform;
            if (reservesRegion == null)
                return;

            ReservesLabelStripFactory.Ensure(reservesRegion, UiThemeProvider.Current);
            ReservesLayoutFitter.EnsureOnBuildPanel(
                buildPanel,
                bottomBar,
                reservesRegion,
                mainRowLayout,
                boardView);
        }

        private void ApplySellZoneSize()
        {
            var bottomBar = buildPanel.Find("BottomBar");
            if (bottomBar == null)
                return;

            var sell = bottomBar.Find("SellZone");
            if (sell == null)
                return;

            var rect = sell.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(BuildLayoutMetrics.SellAnchorX, BuildLayoutMetrics.BottomBarCenterY);
                rect.anchorMax = new Vector2(BuildLayoutMetrics.SellAnchorX, BuildLayoutMetrics.BottomBarCenterY);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, BuildLayoutMetrics.BottomBarVerticalOffsetPixels);
            }

            var scaled = sell.GetComponent<BoardScaledRect>();
            if (scaled == null)
                scaled = sell.gameObject.AddComponent<BoardScaledRect>();
            scaled.Configure(boardView, 3, 3, 0.92f);

            var label = sell.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = 14;
                label.enableWordWrapping = true;
                label.alignment = TextAlignmentOptions.Center;
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(4f, 4f);
                labelRect.offsetMax = new Vector2(-4f, -4f);
            }

            var beginFight = bottomBar.GetComponentsInChildren<Button>(true);
            foreach (var button in beginFight)
            {
                var buttonLabel = button.GetComponentInChildren<TMP_Text>();
                if (buttonLabel == null || !buttonLabel.text.Contains("Begin Fight"))
                    continue;

                var fightRect = button.GetComponent<RectTransform>();
                fightRect.anchorMin = new Vector2(BuildLayoutMetrics.BeginFightAnchorX, BuildLayoutMetrics.BottomBarCenterY);
                fightRect.anchorMax = new Vector2(BuildLayoutMetrics.BeginFightAnchorX, BuildLayoutMetrics.BottomBarCenterY);
                fightRect.pivot = new Vector2(0.5f, 0.5f);
                fightRect.anchoredPosition = new Vector2(0f, BuildLayoutMetrics.BottomBarVerticalOffsetPixels);
                break;
            }
        }

        public static void EnsureOnBuildPanel(Transform panel, BoardView board, BuildRowLayoutFitter rowLayout = null)
        {
            if (panel == null)
                return;

            var bootstrap = panel.GetComponent<RunBuildUiBootstrap>();
            if (bootstrap == null)
                bootstrap = panel.gameObject.AddComponent<RunBuildUiBootstrap>();
            bootstrap.Configure(panel, board, rowLayout);
        }
    }
}
