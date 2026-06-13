using DeadManZone.Core.Board;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Shop;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    public static class RunSceneSetup
    {
        private const float BoardAreaAnchorMaxX = 0.50f;
        private const float ShopAreaAnchorMinX = 0.50f;

        public static void BuildRunScene(GameObject canvas)
        {
            var theme = UiThemeSceneStyling.LoadTheme();
            MenuSceneSetup.CreateRunManager();

            if (canvas.GetComponent<DragDropController>() == null)
                canvas.AddComponent<DragDropController>();

            var canvasBg = canvas.GetComponent<Image>();
            if (canvasBg == null)
                canvasBg = canvas.AddComponent<Image>();
            canvasBg.color = theme.backgroundColor;
            canvasBg.raycastTarget = false;
            UiThemeSceneStyling.AddDecorBackground(canvas.transform, theme, menuScene: false);

            var controllerRoot = CreateRegion(canvas.transform, "RunScene", Vector2.zero, Vector2.one);
            var controller = controllerRoot.AddComponent<RunSceneController>();
            var buildPanel = CreateRegion(controllerRoot.transform, "BuildPanel", Vector2.zero, Vector2.one);
            var buildCanvasGroup = buildPanel.AddComponent<CanvasGroup>();
            UiThemeSceneStyling.AddPanelBackground(buildPanel.transform, theme);
            RunBuildUiBootstrap.EnsureOnBuildPanel(buildPanel.transform, null);

            var topBar = CreateRegion(buildPanel.transform, "TopBar", new Vector2(0f, 0.92f), Vector2.one);
            UiThemeSceneStyling.AddSidebarBackground(topBar.transform, theme);

            var hud = topBar.AddComponent<RunHudView>();
            var builtHud = RunHudPanelBuilder.Create(buildPanel.transform, theme);
            RunHudPanelBuilder.WireRunHudView(hud, builtHud);
            hud.ApplyTheme(theme);

            var tooltipGo = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 16, FontStyles.Italic,
                new Vector2(0.62f, 0.5f), new Vector2(360f, 56f));
            tooltipGo.gameObject.name = "ShopTooltip";
            tooltipGo.gameObject.AddComponent<ShopSharedTooltip>();
            var tooltip = tooltipGo;
            UiThemeSceneStyling.StyleLabel(tooltip, theme, secondary: true);

            var menuBtn = MenuSceneSetup.CreateSmallButtonPublic(
                topBar.transform, "MENU", new Vector2(0.94f, 0.5f), new Vector2(100f, 40f));
            UiThemeSceneStyling.StyleButton(menuBtn, theme);

            var lastLogReview = CreateLastBattleLogReview(buildPanel.transform, theme);
            CreateLastBattleLogButton(topBar.transform, theme, lastLogReview);

            var mainRow = CreateRegion(buildPanel.transform, "MainRow", new Vector2(0f, 0.16f), new Vector2(1f, 0.92f));
            var bottomBar = CreateRegion(buildPanel.transform, "BottomBar", Vector2.zero, new Vector2(1f, 0.16f));
            UiThemeSceneStyling.AddSidebarBackground(bottomBar.transform, theme);

            var beginFight = MenuSceneSetup.CreateSmallButtonPublic(
                bottomBar.transform, "Begin Fight",
                new Vector2(BuildLayoutMetrics.BeginFightAnchorX, BuildLayoutMetrics.BottomBarCenterY),
                new Vector2(160f, 48f));
            var beginFightRect = beginFight.GetComponent<RectTransform>();
            beginFightRect.anchoredPosition = new Vector2(0f, BuildLayoutMetrics.BottomBarVerticalOffsetPixels);
            UiThemeSceneStyling.StyleButton(beginFight, theme, accent: true);

            var boardAreaGo = CreateRegion(mainRow.transform, "BoardArea", new Vector2(0f, 0f), new Vector2(BoardAreaAnchorMaxX, 1f));
            var shopAreaGo = CreateRegion(mainRow.transform, "ShopArea", new Vector2(ShopAreaAnchorMinX, 0f), Vector2.one);
            var boardAreaRect = boardAreaGo.GetComponent<RectTransform>();
            var shopAreaRect = shopAreaGo.GetComponent<RectTransform>();

            var boardView = CreateBoardSection(boardAreaGo.transform, theme);
            var shopView = CreateShopSection(shopAreaGo.transform, tooltip, theme, boardView);

            var rowLayout = mainRow.AddComponent<BuildRowLayoutFitter>();
            var rowLayoutSerialized = new SerializedObject(rowLayout);
            rowLayoutSerialized.FindProperty("boardArea").objectReferenceValue = boardAreaRect;
            rowLayoutSerialized.FindProperty("shopArea").objectReferenceValue = shopAreaRect;
            rowLayoutSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            rowLayoutSerialized.ApplyModifiedPropertiesWithoutUndo();
            var reservesView = CreateReservesSection(bottomBar.transform, theme);
            var sellZone = CreateSellZone(bottomBar.transform, theme, boardView);
            var pauseMenu = CreatePauseMenu(buildPanel.transform, theme);
            var endOverlay = CreateRunEndOverlay(buildPanel.transform, theme);

            var combatPanel = CreateRegion(controllerRoot.transform, "CombatPanel", Vector2.zero, Vector2.one);
            combatPanel.SetActive(false);
            var combatOverlay = combatPanel.AddComponent<Image>();
            var transparentOverlay = theme.combatOverlayColor;
            transparentOverlay.a = 0f;
            combatOverlay.color = transparentOverlay;
            combatOverlay.raycastTarget = false;

            var combatDirector = combatPanel.AddComponent<CombatDirector>();
            var combatBoard = combatPanel.AddComponent<CombatBoardPresenter>();
            var loadingOverlay = CreateCombatLoadingOverlay(combatPanel.transform, theme, out var loadingText);
            var tacticPanel = CreateTacticPausePanel(combatPanel.transform, theme, out var bannerText, out var bannerGroup);
            var battleReport = CreateBattleReportPanel(combatPanel.transform, theme);
            var flowPresenter = combatPanel.AddComponent<CombatFlowPresenter>();
            var healthBarPresenter = CreateArmyHealthBars(combatPanel.transform, theme);

            WireFlowPresenter(flowPresenter, combatDirector, tacticPanel, battleReport, loadingOverlay, loadingText, healthBarPresenter);
            WireCombatBoardPresenter(combatBoard, combatDirector, boardView, bannerText, bannerGroup);
            WireController(controller, buildPanel, buildCanvasGroup, combatPanel, boardAreaGo, shopAreaGo, bottomBar,
                boardView, shopView, reservesView, combatDirector, tacticPanel, hud, endOverlay, pauseMenu,
                beginFight, menuBtn, rowLayout);

            ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel.transform, theme);
            RunBuildUiBootstrap.EnsureOnBuildPanel(buildPanel.transform, boardView, rowLayout);
            BuildUiChromeBootstrap.Apply(buildPanel.transform);
            RunHudLayoutFitter.EnsureOnBuildPanel(buildPanel.transform, builtHud.Root, rowLayout);
            ReservesLayoutFitter.EnsureOnBuildPanel(
                buildPanel.transform,
                bottomBar.GetComponent<RectTransform>(),
                bottomBar.transform.Find("ReservesRegion") as RectTransform,
                rowLayout,
                boardView);

            var panelSerialized = new SerializedObject(tacticPanel);
            panelSerialized.FindProperty("combatDirector").objectReferenceValue = combatDirector;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            var directorSerialized = new SerializedObject(combatDirector);
            directorSerialized.FindProperty("autoAdvanceAfterCommands").boolValue = false;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateRunVisualProfileApplier();
        }

        private static void CreateRunVisualProfileApplier()
        {
            var go = new GameObject("VisualProfile");
            var applier = go.AddComponent<VisualProfileApplier>();
            VisualProfilePresetFactory.EnsureDefaultProfile();
            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath);
            var serialized = new SerializedObject(applier);
            serialized.FindProperty("profile").objectReferenceValue = runtimeProfile;
            serialized.FindProperty("sceneKind").enumValueIndex = (int)VisualProfileSceneKind.Run;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BoardView CreateBoardSection(Transform parent, UiThemeSO theme)
        {
            const int boardWidth = 9;
            const int rearCols = 4;
            const int supportCols = 3;
            float gridLeft = 0.02f;
            float gridRight = 0.98f;

            var backdropGo = new GameObject("BattlefieldBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropGo.transform.SetParent(parent, false);
            var backdrop = backdropGo.AddComponent<BoardBattlefieldBackdrop>();

            var gridRoot = new GameObject("TileGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(parent, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(gridLeft, BuildLayoutMetrics.BoardGridBottomY);
            gridRect.anchorMax = new Vector2(gridRight, BuildLayoutMetrics.BoardGridTopY);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = boardWidth;
            grid.cellSize = new Vector2(48, 48);
            grid.spacing = new Vector2(3, 3);
            grid.padding = new RectOffset(4, 4, 4, 4);

            const int boardHeight = 10;
            var gridFitter = gridRoot.AddComponent<GridLayoutCellFitter>();
            gridFitter.Configure(boardWidth, boardHeight);

            var overlayGo = new GameObject("GridOverlay", typeof(RectTransform), typeof(CanvasRenderer));
            overlayGo.transform.SetParent(parent, false);
            var gridOverlay = overlayGo.AddComponent<BoardGridOverlay>();
            backdrop.Configure(gridRect, null);
            gridOverlay.Configure(
                gridRect,
                grid,
                boardWidth,
                boardHeight,
                rearCols,
                supportCols,
                theme.boardGridLineColor,
                theme.boardZoneDividerColor);
            overlayGo.SetActive(false);

            var (rearStrip, rearLabel) = CreateZoneStripSegment(parent, "REAR", theme.rearZoneColor, theme);
            var (supportStrip, supportLabel) = CreateZoneStripSegment(parent, "SUPPORT", theme.supportZoneColor, theme);
            var (frontStrip, frontLabel) = CreateZoneStripSegment(parent, "FRONT", theme.frontZoneColor, theme);

            var boardRect = parent.GetComponent<RectTransform>();
            var zoneLayout = parent.gameObject.AddComponent<BoardZoneStripLayout>();
            zoneLayout.Configure(
                boardRect,
                gridRect,
                grid,
                rearStrip,
                supportStrip,
                frontStrip,
                rearLabel,
                supportLabel,
                frontLabel,
                rearCols,
                supportCols);

            var tilePrefab = CreateTilePrefab(theme);
            tilePrefab.transform.SetParent(parent, false);
            tilePrefab.SetActive(false);

            var boardView = parent.gameObject.AddComponent<BoardView>();
            var serialized = new SerializedObject(boardView);
            serialized.FindProperty("tileRoot").objectReferenceValue = gridRoot.transform;
            serialized.FindProperty("gridLayout").objectReferenceValue = grid;
            serialized.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("battlefieldBackdrop").objectReferenceValue = backdrop;
            serialized.FindProperty("gridOverlay").objectReferenceValue = gridOverlay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return boardView;
        }

        private static (RectTransform strip, TMP_Text label) CreateZoneStripSegment(
            Transform parent,
            string zoneName,
            Color color,
            UiThemeSO theme)
        {
            var segment = CreateRegion(parent, zoneName + "Strip", Vector2.zero, Vector2.zero);
            var image = segment.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var label = MenuSceneSetup.CreateLabelPublic(
                segment.transform, zoneName, 12, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(120f, 20f));
            label.alignment = TextAlignmentOptions.Center;
            label.color = theme.textPrimary;
            label.raycastTarget = false;

            return (segment.GetComponent<RectTransform>(), label);
        }

        private static GameObject CreateTilePrefab(UiThemeSO theme)
        {
            var tile = new GameObject("TilePrefab", typeof(RectTransform));
            var image = tile.AddComponent<Image>();
            UiThemeApplicator.ApplySlotEmpty(image, theme);
            image.color = theme.GetTileDisplayColor(theme.rearZoneColor);

            var special = new GameObject("SpecialOverlay", typeof(RectTransform));
            special.transform.SetParent(tile.transform, false);
            Stretch(special.GetComponent<RectTransform>());
            var specialImage = special.AddComponent<Image>();
            specialImage.color = theme.specialTileColor;
            specialImage.enabled = false;
            specialImage.raycastTarget = false;

            var invalid = new GameObject("InvalidOverlay", typeof(RectTransform));
            invalid.transform.SetParent(tile.transform, false);
            Stretch(invalid.GetComponent<RectTransform>());
            var invalidImage = invalid.AddComponent<Image>();
            invalidImage.color = theme.invalidPlacementColor;
            invalidImage.enabled = false;
            invalidImage.raycastTarget = false;

            var tileView = tile.AddComponent<BoardTileView>();
            var serialized = new SerializedObject(tileView);
            serialized.FindProperty("baseImage").objectReferenceValue = image;
            serialized.FindProperty("specialOverlay").objectReferenceValue = specialImage;
            serialized.FindProperty("placementOverlay").objectReferenceValue = invalidImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        private static ShopView CreateShopSection(Transform parent, TMP_Text sharedTooltip, UiThemeSO theme, BoardView boardView)
        {
            var (generalRoot, rerollGeneral, offensiveRow) = CreateLaneRow(parent, "Offensive", 2, theme, boardView, sharedTooltip);
            var (engineerRoot, rerollEngineers, defensiveRow) = CreateLaneRow(parent, "Defensive", 1, theme, boardView, sharedTooltip);
            var (reqRoot, rerollReq, specialtyRow) = CreateLaneRow(parent, "Specialty", 0, theme, boardView, sharedTooltip);

            var laneLayout = parent.gameObject.AddComponent<ShopLaneLayoutFitter>();
            laneLayout.Configure(offensiveRow, defensiveRow, specialtyRow);

            ShopUiBootstrap.EnsureOnShopArea(parent, boardView, sharedTooltip);

            var offerPrefab = CreateOfferCardPrefab(theme);
            offerPrefab.transform.SetParent(parent, false);
            offerPrefab.SetActive(false);

            var shopView = parent.gameObject.AddComponent<ShopView>();
            var serialized = new SerializedObject(shopView);
            serialized.FindProperty("generalLaneRoot").objectReferenceValue = generalRoot;
            serialized.FindProperty("engineersLaneRoot").objectReferenceValue = engineerRoot;
            serialized.FindProperty("requisitionLaneRoot").objectReferenceValue = reqRoot;
            serialized.FindProperty("offerCardPrefab").objectReferenceValue = offerPrefab;
            serialized.FindProperty("modifiersTooltipText").objectReferenceValue = sharedTooltip;
            serialized.FindProperty("rerollGeneralButton").objectReferenceValue = rerollGeneral;
            serialized.FindProperty("rerollEngineersButton").objectReferenceValue = rerollEngineers;
            serialized.FindProperty("rerollRequisitionButton").objectReferenceValue = rerollReq;
            serialized.FindProperty("boardView").objectReferenceValue = boardView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return shopView;
        }

        private static (Transform offersRoot, Button reroll, RectTransform rowRect) CreateLaneRow(
            Transform parent,
            string title,
            int laneIndexFromBottom,
            UiThemeSO theme,
            BoardView boardView,
            TMP_Text sharedTooltip)
        {
            var (minY, maxY) = BuildLayoutMetrics.GetShopLaneAnchors(laneIndexFromBottom);
            var row = CreateRegion(
                parent,
                title + "Row",
                new Vector2(0f, minY),
                new Vector2(BuildLayoutMetrics.ShopRightInset, maxY));
            var laneBg = row.AddComponent<Image>();
            UiThemeApplicator.ApplyInventoryPanel(laneBg, theme);
            var laneTint = title switch
            {
                "Offensive" => theme.generalLaneTint,
                "Defensive" => theme.engineersLaneTint,
                _ => theme.requisitionLaneTint
            };
            if (theme.shopBackgroundSprite != null)
            {
                laneBg.color = Color.clear;
                var tintOverlay = MenuSceneSetup.CreateStretchChild(row.transform, "LaneTint");
                tintOverlay.transform.SetAsFirstSibling();
                var tintImage = tintOverlay.AddComponent<Image>();
                laneTint.a = Mathf.Clamp(theme.shopLaneTintScaleWithBackground * 0.35f, 0.04f, 0.18f);
                tintImage.color = laneTint;
                tintImage.raycastTarget = false;
            }
            else if (theme.inventoryPanelSprite != null)
            {
                var tintOverlay = MenuSceneSetup.CreateStretchChild(row.transform, "LaneTint");
                tintOverlay.transform.SetAsFirstSibling();
                var tintImage = tintOverlay.AddComponent<Image>();
                tintImage.color = laneTint;
                tintImage.raycastTarget = false;
            }
            else
            {
                laneBg.color = laneTint;
            }
            laneBg.raycastTarget = false;

            var offers = CreateRegion(row.transform, "Offers", new Vector2(0.04f, 0.14f), new Vector2(0.90f, 0.86f));
            var layout = offers.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = ShopLayoutMetrics.LaneSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 4, 4);

            var reroll = CreateRerollButton(row.transform, theme, boardView, sharedTooltip);
            return (offers.transform, reroll, row.GetComponent<RectTransform>());
        }

        private static Button CreateRerollButton(
            Transform row,
            UiThemeSO theme,
            BoardView boardView,
            TMP_Text sharedTooltip)
        {
            var go = new GameObject("RerollButton", typeof(RectTransform));
            go.transform.SetParent(row, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.965f, 0.5f);
            rect.anchorMax = new Vector2(0.965f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(image, theme);
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            UiThemeSceneStyling.StyleButton(button, theme);

            var label = MenuSceneSetup.CreateLabelPublic(
                go.transform, "\u21BB", 22, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(48f, 48f));
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            UiThemeSceneStyling.StyleLabel(label, theme);

            var scaled = go.AddComponent<BoardScaledRect>();
            scaled.Configure(boardView, 1, 1);

            if (sharedTooltip != null)
            {
                var tooltip = go.AddComponent<ShopRerollTooltip>();
                tooltip.Configure(sharedTooltip);
            }

            return button;
        }

        private static GameObject CreateOfferCardPrefab(UiThemeSO theme)
        {
            const float defaultSquare = 150f;

            var card = new GameObject("OfferCard", typeof(RectTransform));
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                defaultSquare + ShopLayoutMetrics.CardPadding,
                defaultSquare + ShopLayoutMetrics.NameStripHeight + ShopLayoutMetrics.CardPadding);

            var image = card.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(image, theme);
            image.raycastTarget = false;

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 4, 4);

            var lockIndicator = new GameObject("LockedOverlay", typeof(RectTransform));
            lockIndicator.transform.SetParent(card.transform, false);
            Stretch(lockIndicator.GetComponent<RectTransform>());
            var lockImage = lockIndicator.AddComponent<Image>();
            lockImage.color = new Color(theme.accentColor.r, theme.accentColor.g, theme.accentColor.b, 0.35f);
            lockImage.enabled = false;
            lockImage.raycastTarget = false;
            var lockOverlayLayout = lockIndicator.AddComponent<LayoutElement>();
            lockOverlayLayout.ignoreLayout = true;
            lockIndicator.transform.SetAsFirstSibling();

            var squareRootGo = new GameObject("SquareRoot", typeof(RectTransform));
            squareRootGo.transform.SetParent(card.transform, false);
            var squareRoot = squareRootGo.GetComponent<RectTransform>();
            squareRoot.sizeDelta = new Vector2(defaultSquare, defaultSquare);
            var squareLayout = squareRootGo.AddComponent<LayoutElement>();
            squareLayout.minWidth = defaultSquare;
            squareLayout.minHeight = defaultSquare;
            squareLayout.preferredWidth = defaultSquare;
            squareLayout.preferredHeight = defaultSquare;

            var squareBg = squareRootGo.AddComponent<Image>();
            squareBg.color = new Color(0f, 0f, 0f, 0.12f);
            squareBg.raycastTarget = true;

            var previewRootGo = new GameObject("PreviewRoot", typeof(RectTransform));
            previewRootGo.transform.SetParent(squareRootGo.transform, false);
            var previewRoot = previewRootGo.GetComponent<RectTransform>();
            previewRoot.anchorMin = new Vector2(0.5f, 0.5f);
            previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            previewRoot.pivot = new Vector2(0.5f, 0.5f);
            previewRoot.anchoredPosition = Vector2.zero;
            previewRoot.sizeDelta = new Vector2(defaultSquare, defaultSquare);

            var blockRootGo = new GameObject("Blocks", typeof(RectTransform));
            blockRootGo.transform.SetParent(previewRootGo.transform, false);
            var blockRoot = blockRootGo.GetComponent<RectTransform>();
            blockRoot.anchorMin = new Vector2(0.5f, 0.5f);
            blockRoot.anchorMax = new Vector2(0.5f, 0.5f);
            blockRoot.pivot = new Vector2(0.5f, 0.5f);
            blockRoot.anchoredPosition = Vector2.zero;

            var offerView = card.AddComponent<ShopOfferView>();

            FixOfferPreviewComponent(previewRootGo, blockRoot, out var piecePreview);

            var priceBadgeGo = new GameObject("PriceBadge", typeof(RectTransform));
            priceBadgeGo.transform.SetParent(squareRootGo.transform, false);
            var priceBadgeRect = priceBadgeGo.GetComponent<RectTransform>();
            priceBadgeRect.anchorMin = new Vector2(0f, 1f);
            priceBadgeRect.anchorMax = new Vector2(0f, 1f);
            priceBadgeRect.pivot = new Vector2(0f, 1f);
            priceBadgeRect.anchoredPosition = new Vector2(6f, -6f);
            priceBadgeRect.sizeDelta = new Vector2(56f, 20f);
            var priceBadgeBg = priceBadgeGo.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(priceBadgeBg, theme);
            priceBadgeBg.raycastTarget = false;

            var priceText = MenuSceneSetup.CreateLabelPublic(
                priceBadgeGo.transform, "0G", 11, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(52f, 18f));
            priceText.alignment = TextAlignmentOptions.Center;
            UiThemeSceneStyling.StyleLabel(priceText, theme);

            var lockBtn = MenuSceneSetup.CreateSmallButtonPublic(
                squareRootGo.transform, "O", new Vector2(1f, 1f), new Vector2(24f, 24f));
            var lockBtnRect = lockBtn.GetComponent<RectTransform>();
            lockBtnRect.anchorMin = new Vector2(1f, 1f);
            lockBtnRect.anchorMax = new Vector2(1f, 1f);
            lockBtnRect.pivot = new Vector2(1f, 1f);
            lockBtnRect.anchoredPosition = new Vector2(-6f, -6f);
            UiThemeSceneStyling.StyleButton(lockBtn, theme);
            var lockIconImage = lockBtn.GetComponent<Image>();

            squareRootGo.AddComponent<ShopOfferDragSource>();

            var nameStripGo = new GameObject("NameStrip", typeof(RectTransform));
            nameStripGo.transform.SetParent(card.transform, false);
            var nameStrip = nameStripGo.GetComponent<RectTransform>();
            nameStrip.sizeDelta = new Vector2(defaultSquare, ShopLayoutMetrics.NameStripHeight);
            var nameStripLayout = nameStripGo.AddComponent<LayoutElement>();
            nameStripLayout.minHeight = ShopLayoutMetrics.NameStripHeight;
            nameStripLayout.preferredHeight = ShopLayoutMetrics.NameStripHeight;
            nameStripLayout.minWidth = defaultSquare;
            nameStripLayout.preferredWidth = defaultSquare;

            var nameStripBg = nameStripGo.AddComponent<Image>();
            nameStripBg.color = new Color(0f, 0f, 0f, 0.35f);
            nameStripBg.raycastTarget = false;

            var pieceText = MenuSceneSetup.CreateLabelPublic(
                nameStripGo.transform, "piece", 12, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(defaultSquare - 8f, ShopLayoutMetrics.NameStripHeight - 4f));
            pieceText.alignment = TextAlignmentOptions.Center;
            pieceText.enableWordWrapping = false;
            pieceText.overflowMode = TextOverflowModes.Ellipsis;
            UiThemeSceneStyling.StyleLabel(pieceText, theme);

            var serialized = new SerializedObject(offerView);
            serialized.FindProperty("cardBackground").objectReferenceValue = image;
            serialized.FindProperty("squareRoot").objectReferenceValue = squareRoot;
            serialized.FindProperty("previewRoot").objectReferenceValue = previewRoot;
            serialized.FindProperty("piecePreview").objectReferenceValue = piecePreview;
            serialized.FindProperty("nameStripRoot").objectReferenceValue = nameStrip;
            serialized.FindProperty("pieceIdText").objectReferenceValue = pieceText;
            serialized.FindProperty("priceBadgeBackground").objectReferenceValue = priceBadgeBg;
            serialized.FindProperty("priceBadgeText").objectReferenceValue = priceText;
            serialized.FindProperty("lockIconButton").objectReferenceValue = lockBtn;
            serialized.FindProperty("lockIconImage").objectReferenceValue = lockIconImage;
            serialized.FindProperty("lockedIndicator").objectReferenceValue = lockImage;
            serialized.FindProperty("dragSource").objectReferenceValue = squareRootGo.GetComponent<ShopOfferDragSource>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static void FixOfferPreviewComponent(
            GameObject previewRootGo,
            RectTransform blockRoot,
            out ShopPiecePreview piecePreview)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(previewRootGo);
            piecePreview = previewRootGo.GetComponent<ShopPiecePreview>();
            if (piecePreview == null)
                piecePreview = previewRootGo.AddComponent<ShopPiecePreview>();

            var previewSerialized = new SerializedObject(piecePreview);
            previewSerialized.FindProperty("blockRoot").objectReferenceValue = blockRoot;
            previewSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ReservesView CreateReservesSection(Transform bottomBar, UiThemeSO theme)
        {
            var reservesRegion = CreateRegion(bottomBar.transform, "ReservesRegion",
                Vector2.zero, Vector2.one);

            var gridRoot = new GameObject("ReservesGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(reservesRegion.transform, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.1f, 0.1f);
            gridRect.anchorMax = new Vector2(1f, 0.9f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ReservesState.Width;
            grid.cellSize = new Vector2(44f, 44f);
            grid.spacing = new Vector2(3f, 3f);
            grid.padding = new RectOffset(4, 4, 4, 4);

            var reservesFitter = gridRoot.AddComponent<GridLayoutCellFitter>();
            reservesFitter.Configure(ReservesState.Width, ReservesState.Height);

            ReservesLabelStripFactory.Ensure(reservesRegion.transform, theme);

            var tilePrefab = CreateReservesTilePrefab(theme);
            tilePrefab.transform.SetParent(reservesRegion.transform, false);
            tilePrefab.SetActive(false);

            var reservesView = reservesRegion.AddComponent<ReservesView>();
            var serialized = new SerializedObject(reservesView);
            serialized.FindProperty("tileRoot").objectReferenceValue = gridRoot.transform;
            serialized.FindProperty("gridLayout").objectReferenceValue = grid;
            serialized.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return reservesView;
        }

        private static GameObject CreateReservesTilePrefab(UiThemeSO theme)
        {
            var tile = new GameObject("ReservesTilePrefab", typeof(RectTransform));
            var image = tile.AddComponent<Image>();
            UiThemeApplicator.ApplyStorageSlotEmpty(image, theme);
            tile.AddComponent<ReservesTileView>();
            var serialized = new SerializedObject(tile.GetComponent<ReservesTileView>());
            serialized.FindProperty("baseImage").objectReferenceValue = image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        private static PauseMenuView CreatePauseMenu(Transform buildPanel, UiThemeSO theme)
        {
            var root = CreateRegion(buildPanel, "PauseMenu", Vector2.zero, Vector2.one);
            root.SetActive(false);
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            var overlayGroup = root.AddComponent<CanvasGroup>();

            var mainCard = CreatePauseCard(root.transform, "MainCard", theme, true);
            var optionsCard = CreatePauseCard(root.transform, "OptionsCard", theme, false);
            optionsCard.SetActive(false);

            MenuSceneSetup.CreateLabelPublic(
                mainCard.transform, "Paused", 32, FontStyles.Bold,
                new Vector2(0.5f, 0.78f), new Vector2(320f, 48f));

            var resume = MenuSceneSetup.CreateSmallButtonPublic(
                mainCard.transform, "Resume", new Vector2(0.5f, 0.58f), new Vector2(220f, 44f));
            var options = MenuSceneSetup.CreateSmallButtonPublic(
                mainCard.transform, "Options", new Vector2(0.5f, 0.44f), new Vector2(220f, 44f));
            var mainMenu = MenuSceneSetup.CreateSmallButtonPublic(
                mainCard.transform, "Main Menu", new Vector2(0.5f, 0.30f), new Vector2(220f, 44f));
            var exit = MenuSceneSetup.CreateSmallButtonPublic(
                mainCard.transform, "Exit", new Vector2(0.5f, 0.16f), new Vector2(220f, 44f));
            UiThemeSceneStyling.StyleButton(resume, theme, accent: true);
            UiThemeSceneStyling.StyleButton(options, theme);
            UiThemeSceneStyling.StyleButton(mainMenu, theme);
            UiThemeSceneStyling.StyleButton(exit, theme);

            var optionsBody = MenuSceneSetup.CreateLabelPublic(
                optionsCard.transform, "Options — coming soon", 22, FontStyles.Normal,
                new Vector2(0.5f, 0.55f), new Vector2(360f, 80f));
            UiThemeSceneStyling.StyleLabel(optionsBody, theme, secondary: true);
            var optionsBack = MenuSceneSetup.CreateSmallButtonPublic(
                optionsCard.transform, "Back", new Vector2(0.5f, 0.22f), new Vector2(160f, 40f));
            UiThemeSceneStyling.StyleButton(optionsBack, theme);

            var view = root.AddComponent<PauseMenuView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = root;
            serialized.FindProperty("overlayGroup").objectReferenceValue = overlayGroup;
            serialized.FindProperty("mainPanel").objectReferenceValue = mainCard;
            serialized.FindProperty("optionsPanel").objectReferenceValue = optionsCard;
            serialized.FindProperty("resumeButton").objectReferenceValue = resume;
            serialized.FindProperty("optionsButton").objectReferenceValue = options;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
            serialized.FindProperty("exitButton").objectReferenceValue = exit;
            serialized.FindProperty("optionsBackButton").objectReferenceValue = optionsBack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.ApplyTheme(theme);
            return view;
        }

        private static GameObject CreatePauseCard(Transform parent, string name, UiThemeSO theme, bool active)
        {
            var card = CreateRegion(parent, name, new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.68f));
            card.SetActive(active);
            var image = card.AddComponent<Image>();
            image.raycastTarget = true;
            UiThemeApplicator.ApplyModalFrame(image, theme);
            return card;
        }

        private static Button CreateLastBattleLogButton(
            Transform topBar,
            UiThemeSO theme,
            LastBattleLogReviewPresenter reviewPresenter)
        {
            var button = MenuSceneSetup.CreateSmallButtonPublic(
                topBar, "Last Log", new Vector2(0.895f, 0.5f), new Vector2(124f, 40f));
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.895f, 0.5f);
            rect.anchorMax = new Vector2(0.895f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            UiThemeSceneStyling.StyleButton(button, theme);

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }

            var bridge = button.gameObject.AddComponent<LastBattleLogReviewButton>();
            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("reviewPresenter").objectReferenceValue = reviewPresenter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static LastBattleLogReviewPresenter CreateLastBattleLogReview(Transform buildPanel, UiThemeSO theme)
        {
            var root = CreateRegion(buildPanel, "LastBattleLogReview", Vector2.zero, Vector2.one);
            root.SetActive(false);
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);

            var sheet = CreateRegion(root.transform, "LastBattleLogSheet", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f));
            var sheetBg = sheet.AddComponent<Image>();
            UiThemeApplicator.ApplyModalFrame(sheetBg, theme);

            var title = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "Previous Battle Log (dev)", 22, FontStyles.Bold,
                new Vector2(0.5f, 0.94f), new Vector2(520f, 32f));
            UiThemeSceneStyling.StyleLabel(title, theme);

            var scrollRoot = CreateRegion(sheet.transform, "LogScroll", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.86f));
            var scrollBg = scrollRoot.AddComponent<Image>();
            UiThemeApplicator.ApplyInventoryPanel(scrollBg, theme);
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = CreateRegion(scrollRoot.transform, "Viewport", Vector2.zero, Vector2.one);
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var content = CreateRegion(viewport.transform, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 200f);
            scroll.content = contentRect;

            var logText = MenuSceneSetup.CreateLabelPublic(
                content.transform, "", 14, FontStyles.Normal,
                new Vector2(0.5f, 1f), new Vector2(300f, 200f));
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.enableWordWrapping = true;
            var logRect = logText.rectTransform;
            logRect.anchorMin = new Vector2(0f, 1f);
            logRect.anchorMax = new Vector2(1f, 1f);
            logRect.pivot = new Vector2(0.5f, 1f);
            logRect.offsetMin = new Vector2(12f, -600f);
            logRect.offsetMax = new Vector2(-12f, 0f);
            UiThemeSceneStyling.StyleLabel(logText, theme, secondary: true);

            var closeBtn = MenuSceneSetup.CreateSmallButtonPublic(
                sheet.transform, "Close", new Vector2(0.5f, 0.04f), new Vector2(160f, 40f));
            UiThemeSceneStyling.StyleButton(closeBtn, theme, accent: true);

            var presenter = root.AddComponent<LastBattleLogReviewPresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("overlayRoot").objectReferenceValue = root;
            serialized.FindProperty("logText").objectReferenceValue = logText;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("closeButton").objectReferenceValue = closeBtn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return presenter;
        }

        private static SellDropZone CreateSellZone(Transform bottomBar, UiThemeSO theme, BoardView boardView)
        {
            var sell = CreateRegion(
                bottomBar,
                "SellZone",
                new Vector2(BuildLayoutMetrics.SellAnchorX, BuildLayoutMetrics.BottomBarCenterY),
                new Vector2(BuildLayoutMetrics.SellAnchorX, BuildLayoutMetrics.BottomBarCenterY));
            var rect = sell.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, BuildLayoutMetrics.BottomBarVerticalOffsetPixels);

            var image = sell.AddComponent<Image>();
            UiThemeApplicator.ApplySellZone(image, theme);

            var scaled = sell.AddComponent<BoardScaledRect>();
            scaled.Configure(boardView, 3, 3, 0.92f);

            var label = MenuSceneSetup.CreateLabelPublic(
                sell.transform, "Sell\n(drop here)", 14, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(120f, 120f));
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);
            UiThemeSceneStyling.StyleLabel(label, theme);

            return sell.AddComponent<SellDropZone>();
        }

        private static RunEndOverlayView CreateRunEndOverlay(Transform parent, UiThemeSO theme)
        {
            var root = CreateRegion(parent, "RunEndOverlay", Vector2.zero, Vector2.one);
            root.SetActive(false);
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);

            var card = CreateRegion(root.transform, "Card", new Vector2(0.32f, 0.28f), new Vector2(0.68f, 0.72f));
            var cardImage = card.AddComponent<Image>();
            UiThemeApplicator.ApplyModalFrame(cardImage, theme);

            var title = MenuSceneSetup.CreateLabelPublic(
                card.transform, "Victory", 42, FontStyles.Bold,
                new Vector2(0.5f, 0.72f), new Vector2(500f, 60f));
            var body = MenuSceneSetup.CreateLabelPublic(
                card.transform, "", 22, FontStyles.Normal,
                new Vector2(0.5f, 0.48f), new Vector2(520f, 80f));
            UiThemeSceneStyling.StyleLabel(title, theme);
            UiThemeSceneStyling.StyleLabel(body, theme, secondary: true);

            var endMenuBtn = MenuSceneSetup.CreateSmallButtonPublic(
                card.transform, "Main Menu", new Vector2(0.5f, 0.22f), new Vector2(200f, 44f));
            UiThemeSceneStyling.StyleButton(endMenuBtn, theme, accent: true);

            var overlay = root.AddComponent<RunEndOverlayView>();
            var serialized = new SerializedObject(overlay);
            serialized.FindProperty("root").objectReferenceValue = root;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = endMenuBtn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            overlay.ApplyTheme(theme);
            return overlay;
        }

        private static GameObject CreateCombatLoadingOverlay(
            Transform parent,
            UiThemeSO theme,
            out TMP_Text loadingText)
        {
            var overlay = CreateRegion(parent, "CombatLoadingOverlay", Vector2.zero, Vector2.one);
            if (theme.combatBackgroundSprite != null)
            {
                var decor = CreateRegion(overlay.transform, "CombatDecor", Vector2.zero, Vector2.one);
                var decorImage = decor.AddComponent<Image>();
                UiThemeApplicator.ApplyBackgroundPlate(decorImage, theme.combatBackgroundSprite, 0.35f);
            }

            var bg = overlay.AddComponent<Image>();
            bg.color = theme.combatOverlayColor;
            loadingText = MenuSceneSetup.CreateLabelPublic(
                overlay.transform, "Entering combat…", 32, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(520f, 48f));
            UiThemeSceneStyling.StyleLabel(loadingText, theme);
            overlay.SetActive(false);
            return overlay;
        }

        private static TacticPausePanel CreateTacticPausePanel(
            Transform parent,
            UiThemeSO theme,
            out TMP_Text bannerText,
            out CanvasGroup bannerGroup)
        {
            var sheet = CreateRegion(parent, "TacticPauseSheet", Vector2.zero, new Vector2(1f, 0.42f));
            var sheetBg = sheet.AddComponent<Image>();
            UiThemeApplicator.ApplySecurityTerminalFrame(sheetBg, theme);

            var bannerRoot = CreateRegion(parent, "PhaseBanner", new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.88f));
            bannerRoot.SetActive(false);
            var bannerBg = bannerRoot.AddComponent<Image>();
            UiThemeApplicator.ApplyBanner(bannerBg, theme);
            bannerText = MenuSceneSetup.CreateLabelPublic(
                bannerRoot.transform, "Deployment", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(500f, 50f));
            UiThemeSceneStyling.StyleLabel(bannerText, theme);
            bannerGroup = bannerRoot.AddComponent<CanvasGroup>();

            var panel = sheet.AddComponent<TacticPausePanel>();
            var title = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "Combat Pause", 22, FontStyles.Bold,
                new Vector2(0.5f, 0.88f), new Vector2(900f, 36f));
            var authority = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "Authority", 18, FontStyles.Normal,
                new Vector2(0.5f, 0.78f), new Vector2(900f, 28f));
            var reason = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "", 16, FontStyles.Italic,
                new Vector2(0.5f, 0.14f), new Vector2(900f, 24f));
            UiThemeSceneStyling.StyleLabel(title, theme);
            UiThemeSceneStyling.StyleLabel(authority, theme);
            UiThemeSceneStyling.StyleLabel(reason, theme, secondary: true);

            var tacticRow = CreateRegion(sheet.transform, "TacticRow", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f));
            var abilityRow = CreateRegion(sheet.transform, "AbilityRow", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.48f));
            var continueBtn = MenuSceneSetup.CreateSmallButtonPublic(
                sheet.transform, "Continue", new Vector2(0.5f, 0.05f), new Vector2(180f, 44f));
            UiThemeSceneStyling.StyleButton(continueBtn, theme, accent: true);

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("authorityText").objectReferenceValue = authority;
            serialized.FindProperty("reasonText").objectReferenceValue = reason;
            serialized.FindProperty("tacticRow").objectReferenceValue = tacticRow.transform;
            serialized.FindProperty("abilityRow").objectReferenceValue = abilityRow.transform;
            serialized.FindProperty("continueButton").objectReferenceValue = continueBtn;
            serialized.FindProperty("panelBackground").objectReferenceValue = sheetBg;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sheet.SetActive(false);
            return panel;
        }

        private static PhaseCommandPanel CreatePhaseCommandPanel(
            Transform parent,
            UiThemeSO theme,
            out TMP_Text bannerText,
            out CanvasGroup bannerGroup)
        {
            var sheet = CreateRegion(parent, "CommandSheet", Vector2.zero, new Vector2(1f, 0.38f));
            var sheetBg = sheet.AddComponent<Image>();
            UiThemeApplicator.ApplyPanel(sheetBg, theme);

            var bannerRoot = CreateRegion(parent, "PhaseBanner", new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.88f));
            bannerRoot.SetActive(false);
            var bannerBg = bannerRoot.AddComponent<Image>();
            bannerBg.color = theme.combatBannerColor;
            bannerText = MenuSceneSetup.CreateLabelPublic(
                bannerRoot.transform, "Deployment", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(500f, 50f));
            UiThemeSceneStyling.StyleLabel(bannerText, theme);
            bannerGroup = bannerRoot.AddComponent<CanvasGroup>();

            var panel = sheet.AddComponent<PhaseCommandPanel>();
            var text = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "", 20, FontStyles.Normal,
                new Vector2(0.5f, 0.62f), new Vector2(900f, 320f));
            text.alignment = TextAlignmentOptions.TopLeft;
            UiThemeSceneStyling.StyleLabel(text, theme);

            var submit = MenuSceneSetup.CreateSmallButtonPublic(
                sheet.transform, "Submit", new Vector2(0.42f, 0.12f), new Vector2(160f, 44f));
            var skip = MenuSceneSetup.CreateSmallButtonPublic(
                sheet.transform, "Skip", new Vector2(0.58f, 0.12f), new Vector2(160f, 44f));
            UiThemeSceneStyling.StyleButton(submit, theme, accent: true);
            UiThemeSceneStyling.StyleButton(skip, theme);

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("commandsText").objectReferenceValue = text;
            serialized.FindProperty("submitButton").objectReferenceValue = submit;
            serialized.FindProperty("skipButton").objectReferenceValue = skip;
            serialized.FindProperty("panelBackground").objectReferenceValue = sheetBg;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sheet.SetActive(false);
            return panel;
        }

        private static void WireCombatBoardPresenter(
            CombatBoardPresenter presenter,
            CombatDirector director,
            BoardView boardView,
            TMP_Text bannerText,
            CanvasGroup bannerGroup)
        {
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("boardView").objectReferenceValue = boardView;
            serialized.FindProperty("combatDirector").objectReferenceValue = director;
            serialized.FindProperty("phaseBannerText").objectReferenceValue = bannerText;
            serialized.FindProperty("phaseBannerGroup").objectReferenceValue = bannerGroup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BattleReportPresenter CreateBattleReportPanel(Transform parent, UiThemeSO theme)
        {
            var sheet = CreateRegion(parent, "BattleReportSheet", new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.82f));
            var sheetBg = sheet.AddComponent<Image>();
            UiThemeApplicator.ApplyModalFrame(sheetBg, theme);

            var presenter = sheet.AddComponent<BattleReportPresenter>();
            var outcome = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "Victory", 32, FontStyles.Bold,
                new Vector2(0.5f, 0.86f), new Vector2(700f, 44f));
            var summary = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "", 20, FontStyles.Normal,
                new Vector2(0.5f, 0.62f), new Vector2(700f, 120f));
            var dealt = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "", 18, FontStyles.Normal,
                new Vector2(0.3f, 0.32f), new Vector2(320f, 180f));
            dealt.alignment = TextAlignmentOptions.TopLeft;
            var taken = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "", 18, FontStyles.Normal,
                new Vector2(0.7f, 0.32f), new Vector2(320f, 180f));
            taken.alignment = TextAlignmentOptions.TopLeft;
            var continueBtn = MenuSceneSetup.CreateSmallButtonPublic(
                sheet.transform, "Continue", new Vector2(0.5f, 0.08f), new Vector2(180f, 44f));
            UiThemeSceneStyling.StyleLabel(outcome, theme);
            UiThemeSceneStyling.StyleLabel(summary, theme);
            UiThemeSceneStyling.StyleLabel(dealt, theme, secondary: true);
            UiThemeSceneStyling.StyleLabel(taken, theme, secondary: true);
            UiThemeSceneStyling.StyleButton(continueBtn, theme, accent: true);

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("panelRoot").objectReferenceValue = sheet;
            serialized.FindProperty("outcomeText").objectReferenceValue = outcome;
            serialized.FindProperty("summaryText").objectReferenceValue = summary;
            serialized.FindProperty("dealtText").objectReferenceValue = dealt;
            serialized.FindProperty("takenText").objectReferenceValue = taken;
            serialized.FindProperty("continueButton").objectReferenceValue = continueBtn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sheet.SetActive(false);
            return presenter;
        }

        private static ArmyHealthBarPresenter CreateArmyHealthBars(Transform parent, UiThemeSO theme)
        {
            var root = CreateRegion(parent, "ArmyHealthBars", new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f));
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var playerBar = CreateSingleArmyHealthBar(root.transform, "PlayerArmyBar", theme, new Color(0.2f, 0.75f, 0.35f));
            var enemyBar = CreateSingleArmyHealthBar(root.transform, "EnemyArmyBar", theme, new Color(0.85f, 0.25f, 0.2f));

            var presenter = root.AddComponent<ArmyHealthBarPresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("playerBar").objectReferenceValue = playerBar;
            serialized.FindProperty("enemyBar").objectReferenceValue = enemyBar;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static ArmyHealthBarView CreateSingleArmyHealthBar(
            Transform parent,
            string name,
            UiThemeSO theme,
            Color fillColor)
        {
            var barRoot = CreateRegion(parent, name, Vector2.zero, Vector2.one);
            var layoutElement = barRoot.AddComponent<LayoutElement>();
            layoutElement.minHeight = 18f;
            layoutElement.preferredHeight = 22f;

            var background = barRoot.AddComponent<Image>();
            UiThemeApplicator.ApplyPanel(background, theme);
            background.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);

            var fillRegion = CreateRegion(barRoot.transform, "FillRegion", new Vector2(0.02f, 0.2f), new Vector2(0.98f, 0.8f));
            var fillImage = fillRegion.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = fillColor;
            fillImage.fillAmount = 1f;
            if (fillImage.sprite == null)
                fillImage.sprite = DeadManZone.Presentation.UI.UiWhiteSprite.Get();

            CreateThresholdNotch(fillRegion.transform, 0.75f, theme);
            CreateThresholdNotch(fillRegion.transform, 0.30f, theme);

            var view = barRoot.AddComponent<ArmyHealthBarView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("fillImage").objectReferenceValue = fillImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void CreateThresholdNotch(Transform parent, float normalizedX, UiThemeSO theme)
        {
            const float notchWidth = 0.008f;
            var notch = CreateRegion(
                parent,
                normalizedX >= 0.5f ? "Notch75" : "Notch30",
                new Vector2(normalizedX - notchWidth * 0.5f, 0f),
                new Vector2(normalizedX + notchWidth * 0.5f, 1f));
            var image = notch.AddComponent<Image>();
            image.color = theme != null ? theme.textSecondary : new Color(1f, 1f, 1f, 0.65f);
            image.raycastTarget = false;
        }

        private static void WireFlowPresenter(
            CombatFlowPresenter presenter,
            CombatDirector director,
            TacticPausePanel panel,
            BattleReportPresenter battleReport,
            GameObject loadingOverlay,
            TMP_Text loadingText,
            ArmyHealthBarPresenter healthBarPresenter)
        {
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("combatDirector").objectReferenceValue = director;
            serialized.FindProperty("tacticPausePanel").objectReferenceValue = panel;
            serialized.FindProperty("battleReportPresenter").objectReferenceValue = battleReport;
            serialized.FindProperty("loadingOverlay").objectReferenceValue = loadingOverlay;
            serialized.FindProperty("loadingText").objectReferenceValue = loadingText;
            serialized.FindProperty("healthBarPresenter").objectReferenceValue = healthBarPresenter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireController(
            RunSceneController controller,
            GameObject buildPanel,
            CanvasGroup buildCanvasGroup,
            GameObject combatPanel,
            GameObject boardArea,
            GameObject shopArea,
            GameObject bottomBar,
            BoardView boardView,
            ShopView shopView,
            ReservesView reservesView,
            CombatDirector combatDirector,
            TacticPausePanel tacticPanel,
            RunHudView hud,
            RunEndOverlayView endOverlay,
            PauseMenuView pauseMenu,
            Button beginFight,
            Button menu,
            BuildRowLayoutFitter mainRowLayout)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("buildPanel").objectReferenceValue = buildPanel;
            serialized.FindProperty("buildPanelCanvasGroup").objectReferenceValue = buildCanvasGroup;
            serialized.FindProperty("combatPanel").objectReferenceValue = combatPanel;
            serialized.FindProperty("boardArea").objectReferenceValue = boardArea.GetComponent<RectTransform>();
            serialized.FindProperty("shopArea").objectReferenceValue = shopArea;
            serialized.FindProperty("bottomBar").objectReferenceValue = bottomBar;
            serialized.FindProperty("mainRowLayout").objectReferenceValue = mainRowLayout;
            serialized.FindProperty("boardView").objectReferenceValue = boardView;
            serialized.FindProperty("shopView").objectReferenceValue = shopView;
            serialized.FindProperty("reservesView").objectReferenceValue = reservesView;
            serialized.FindProperty("combatDirector").objectReferenceValue = combatDirector;
            serialized.FindProperty("tacticPausePanel").objectReferenceValue = tacticPanel;
            serialized.FindProperty("runHudView").objectReferenceValue = hud;
            serialized.FindProperty("runEndOverlay").objectReferenceValue = endOverlay;
            serialized.FindProperty("pauseMenuView").objectReferenceValue = pauseMenu;
            serialized.FindProperty("beginFightButton").objectReferenceValue = beginFight;
            serialized.FindProperty("menuButton").objectReferenceValue = menu;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateRegion(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
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
