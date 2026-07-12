using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Combat;
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

            if (RunUiAuthoringLock.ShouldPreserve(buildPanel))
            {
                // M6 restyle-not-redesign: the authoring lock protects hand-authored
                // LAYOUT, not the old palette. At runtime the pure-recolor kit pass
                // still runs; in the editor the lock keeps full authority so Setup
                // menu passes never dirty an authored scene. Everything below the
                // gate (controllers, rebuilds, fitters) stays structural-only.
                if (Application.isPlaying)
                    ApplyGrimdarkSkin();
                return;
            }

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (mainRowLayout == null)
                mainRowLayout = buildPanel.Find("MainRow")?.GetComponent<BuildRowLayoutFitter>();

            EnsureBuildScreenHudController();
            CriticalMassDrawerBootstrap.Ensure(buildPanel);
            UnitCardPanelBootstrap.EnsureOnBuildPanel(buildPanel);
            BuildUiChromeBootstrap.RemoveFromBuildPanel(buildPanel);

            RunHudResourcePanelStyling.EnsureBackground(buildPanel, UiThemeProvider.Current);
            ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel, UiThemeProvider.Current);
            MoveLastLogToTopBar();
            HideLegacyLastLogButton();
            ApplyRunHudPanel();
            ApplyReservesLayout();
            ApplySellZoneSize();
            ApplyCenterColumnLayout();
            ApplyCombatButtonLabel();
            ApplyGrimdarkSkin();
            DualBoardBootstrap.EnsureHqBoardView(boardView, buildPanel.Find("MainRow/BoardArea") ?? buildPanel);
        }

        /// <summary>M6 skin-only pass: pure recolor onto the grimdark kit. Must stay safe
        /// on a hand-authored panel (it also runs under the authoring lock): null-tolerant
        /// lookups, Image color/sprite + TMP color only — no anchors, sizes, hierarchy,
        /// or component additions.</summary>
        private void ApplyGrimdarkSkin()
        {
            ApplyGrimdarkChrome();
            ApplyGrimdarkResourceStrip();
            StyleBarBackground(buildPanel.Find("TopBar"));
            StyleBarBackground(buildPanel.Find("BottomBar"));
        }

        /// <summary>Recolor only the bar's own background Image (if authored) to the
        /// kit's dark band — children are handled by the button/strip passes.</summary>
        private static void StyleBarBackground(Transform bar)
        {
            var image = bar != null ? bar.GetComponent<Image>() : null;
            if (image == null)
                return;

            image.sprite = null;
            image.color = CombatGrimdarkSkin.BandDark;
        }

        /// <summary>M6: restyle the scene-authored chrome buttons (COMBAT, REROLL, MENU,
        /// Last Log, emergency Draft) into the grimdark kit at runtime. Matched by label —
        /// the scene bakes theme styling; this pass overrides it without touching layout.</summary>
        private void ApplyGrimdarkChrome()
        {
            StyleBarButtons(buildPanel.Find("BottomBar"));
            StyleBarButtons(buildPanel.Find("TopBar"));
        }

        /// <summary>M6: pull the top resource strip (authored TopResourcePanel or an old
        /// builder RunHudPanel kept by the lock) onto the kit: white sprite boxes become
        /// flat smoky CardBody, value labels bone, captions/deltas body text. The kit has
        /// no positive-delta green, so income deltas land on dim bone (BodyText). The Dread
        /// strip needs nothing — RunHudView builds and colors it with kit colors already.</summary>
        private void ApplyGrimdarkResourceStrip()
        {
            StyleResourceStrip(FindTopStrip("TopResourcePanel"));
            StyleResourceStrip(FindTopStrip(RunHudPanelBuilder.PanelName));

            // RunHudView.ApplyTheme is kit-colors-only since M6; let it refine whatever
            // labels it has wired (serialized authored refs win over the name heuristic).
            var topBar = buildPanel.Find("TopBar");
            var hud = topBar != null ? topBar.GetComponent<RunHudView>() : null;
            if (hud != null)
                hud.ApplyTheme(UiThemeProvider.Current);
        }

        private Transform FindTopStrip(string name)
        {
            var strip = buildPanel.Find(name);
            if (strip == null)
                strip = buildPanel.Find("TopBar/" + name);
            return strip;
        }

        /// <summary>Names RunHudView/RunHudPanelBuilder use for primary values — these
        /// read in bone; everything else in the strip reads as secondary body text.</summary>
        private static readonly string[] StripValueNames =
        {
            "FightTitle", "FightLabel", "FightNumber", "FightIndex",
            "SuppliesNumber", "ManpowerNumber", "AuthorityNumber",
            "SalvageNumber", "StrengthNumber",
        };

        internal static void StyleResourceStrip(Transform strip) // internal for EditMode test
        {
            if (strip == null)
                return;

            foreach (var image in strip.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponentInParent<Button>(true) != null)
                    continue;

                if (image.name == "Rule" || image.name == "Divider")
                    continue; // keep the builder panel's bone hairlines

                // Resource ICONS survive the flatten: killing every sprite stripped the
                // strip's information scent (bare numbers, no glyphs — 2026-07-12 smoke).
                // Small square sprites are icons — keep the sprite, tint to bone; wide
                // sprites are the white boxes this pass exists to flatten.
                var rect = image.rectTransform.rect;
                bool looksLikeIcon = image.sprite != null
                    && rect.width <= 72f
                    && Mathf.Abs(rect.width - rect.height) <= rect.width * 0.5f;
                if (looksLikeIcon)
                {
                    image.color = CombatGrimdarkSkin.Bone;
                    continue;
                }

                CombatGrimdarkSkin.StyleFrame(image); // recolor only: sprite -> null, CardBody
            }

            foreach (var label in strip.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != null)
                    continue;

                label.color = System.Array.IndexOf(StripValueNames, label.name) >= 0
                    ? CombatGrimdarkSkin.Bone
                    : CombatGrimdarkSkin.BodyText;
            }
        }

        private static void StyleBarButtons(Transform bar)
        {
            if (bar == null)
                return;

            foreach (var button in bar.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;

                string text = label.text ?? string.Empty;
                bool isCombat = text.Contains("COMBAT") || text.Contains("Begin Fight");
                bool known = isCombat || text.Contains("REROLL") || text.Contains("MENU")
                    || text.Contains("Last Log") || text.Contains("Draft");
                if (!known)
                    continue;

                CombatGrimdarkSkin.StyleButton(button);
                if (isCombat)
                    label.color = CombatGrimdarkSkin.VictoryGold; // primary CTA keeps the brass accent
            }
        }

        private void ApplyCenterColumnLayout()
        {
            var infoRegion = buildPanel.transform.Find("BottomBar/InfoMessageRegion") as RectTransform;
            if (infoRegion != null)
                CenterColumnLayoutFitter.EnsureOnBuildPanel(buildPanel, infoRegion, mainRowLayout);
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
                CriticalMassDrawerBootstrap.Ensure(buildPanel));
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

        private void HideLegacyLastLogButton()
        {
            var topBar = buildPanel.Find("TopBar");
            if (topBar == null)
                return;

            foreach (var button in topBar.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null && label.text.Contains("Last Log"))
                    button.gameObject.SetActive(false);
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
                RunHudResourcePanelStyling.EnsureLayers(panel, UiThemeProvider.Current);

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

            SellZoneVisualBootstrap.Apply(sell, UiThemeProvider.Current);

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
