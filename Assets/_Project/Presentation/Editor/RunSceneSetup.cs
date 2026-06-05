using DeadManZone.Core.Board;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Combat;
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

            var controllerRoot = CreateRegion(canvas.transform, "RunScene", Vector2.zero, Vector2.one);
            var controller = controllerRoot.AddComponent<RunSceneController>();
            var buildPanel = CreateRegion(controllerRoot.transform, "BuildPanel", Vector2.zero, Vector2.one);
            var buildCanvasGroup = buildPanel.AddComponent<CanvasGroup>();
            UiThemeSceneStyling.AddPanelBackground(buildPanel.transform, theme);

            var topBar = CreateRegion(buildPanel.transform, "TopBar", new Vector2(0f, 0.92f), Vector2.one);
            UiThemeSceneStyling.AddPanelBackground(topBar.transform, theme);

            var hud = topBar.AddComponent<RunHudView>();
            var statusBlock = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 16, FontStyles.Bold,
                new Vector2(0.02f, 0.5f), new Vector2(400f, 72f));
            statusBlock.alignment = TextAlignmentOptions.TopLeft;
            statusBlock.enableWordWrapping = true;
            var statusRect = statusBlock.rectTransform;
            statusRect.anchorMin = new Vector2(0.012f, 0f);
            statusRect.anchorMax = new Vector2(0.58f, 1f);
            statusRect.pivot = new Vector2(0f, 0.5f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            UiThemeSceneStyling.StyleLabel(statusBlock, theme);
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("statusText").objectReferenceValue = statusBlock;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            hud.ApplyTheme(theme);

            var tooltip = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 16, FontStyles.Italic,
                new Vector2(0.62f, 0.5f), new Vector2(360f, 56f));
            UiThemeSceneStyling.StyleLabel(tooltip, theme, secondary: true);

            var menuBtn = MenuSceneSetup.CreateSmallButtonPublic(
                topBar.transform, "MENU", new Vector2(0.94f, 0.5f), new Vector2(100f, 40f));
            UiThemeSceneStyling.StyleButton(menuBtn, theme);

            var mainRow = CreateRegion(buildPanel.transform, "MainRow", new Vector2(0f, 0.16f), new Vector2(1f, 0.92f));
            var bottomBar = CreateRegion(buildPanel.transform, "BottomBar", Vector2.zero, new Vector2(1f, 0.16f));
            UiThemeSceneStyling.AddPanelBackground(bottomBar.transform, theme);

            var beginFight = MenuSceneSetup.CreateSmallButtonPublic(
                bottomBar.transform, "Begin Fight", new Vector2(0.92f, 0.5f), new Vector2(160f, 48f));
            UiThemeSceneStyling.StyleButton(beginFight, theme, accent: true);

            var boardArea = CreateRegion(mainRow.transform, "BoardArea", new Vector2(0f, 0f), new Vector2(0.56f, 1f));
            var shopArea = CreateRegion(mainRow.transform, "ShopArea", new Vector2(0.57f, 0f), Vector2.one);

            var boardView = CreateBoardSection(boardArea.transform, theme);
            var shopView = CreateShopSection(shopArea.transform, tooltip, theme);
            var reservesView = CreateReservesSection(bottomBar.transform, theme);
            var lastLogReview = CreateLastBattleLogReview(buildPanel.transform, theme);
            CreateLastBattleLogButton(bottomBar.transform, theme, lastLogReview);
            var sellZone = CreateSellZone(bottomBar.transform, theme);
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

            WireFlowPresenter(flowPresenter, combatDirector, tacticPanel, battleReport, loadingOverlay, loadingText);
            WireCombatBoardPresenter(combatBoard, combatDirector, boardView, bannerText, bannerGroup);
            WireController(controller, buildPanel, buildCanvasGroup, combatPanel, boardArea, shopArea, bottomBar,
                boardView, shopView, reservesView, combatDirector, tacticPanel, hud, endOverlay, pauseMenu,
                beginFight, menuBtn);

            var panelSerialized = new SerializedObject(tacticPanel);
            panelSerialized.FindProperty("combatDirector").objectReferenceValue = combatDirector;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            var directorSerialized = new SerializedObject(combatDirector);
            directorSerialized.FindProperty("autoAdvanceAfterCommands").boolValue = false;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BoardView CreateBoardSection(Transform parent, UiThemeSO theme)
        {
            const int boardWidth = 9;
            const int rearCols = 4;
            const int supportCols = 3;
            float gridLeft = 0.02f;
            float gridRight = 0.98f;
            float gridWidth = gridRight - gridLeft;

            CreateZoneHeader(parent, "REAR", gridLeft, gridLeft + gridWidth * rearCols / boardWidth, 0.90f, 0.98f, theme);
            CreateZoneHeader(
                parent,
                "SUPPORT",
                gridLeft + gridWidth * rearCols / boardWidth,
                gridLeft + gridWidth * (rearCols + supportCols) / boardWidth,
                0.90f,
                0.98f,
                theme);
            CreateZoneHeader(
                parent,
                "FRONT",
                gridLeft + gridWidth * (rearCols + supportCols) / boardWidth,
                gridRight,
                0.90f,
                0.98f,
                theme);

            var gridRoot = new GameObject("TileGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(parent, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(gridLeft, 0.05f);
            gridRect.anchorMax = new Vector2(gridRight, 0.89f);
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

            CreateZoneColorStrip(parent, theme, gridLeft, gridRight, rearCols, supportCols, boardWidth);

            var tilePrefab = CreateTilePrefab(theme);
            tilePrefab.transform.SetParent(parent, false);
            tilePrefab.SetActive(false);

            var boardView = parent.gameObject.AddComponent<BoardView>();
            var serialized = new SerializedObject(boardView);
            serialized.FindProperty("tileRoot").objectReferenceValue = gridRoot.transform;
            serialized.FindProperty("gridLayout").objectReferenceValue = grid;
            serialized.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return boardView;
        }

        private static void CreateZoneHeader(
            Transform parent,
            string text,
            float anchorMinX,
            float anchorMaxX,
            float anchorMinY,
            float anchorMaxY,
            UiThemeSO theme)
        {
            var header = CreateRegion(
                parent,
                text + "Header",
                new Vector2(anchorMinX, anchorMinY),
                new Vector2(anchorMaxX, anchorMaxY));
            var label = MenuSceneSetup.CreateLabelPublic(
                header.transform, text, 13, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(120f, 22f));
            label.color = theme.textSecondary;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateZoneColorStrip(
            Transform parent,
            UiThemeSO theme,
            float gridLeft,
            float gridRight,
            int rearCols,
            int supportCols,
            int boardWidth)
        {
            float gridWidth = gridRight - gridLeft;
            float yMin = 0f;
            float yMax = 0.04f;
            AddZoneStripSegment(parent, theme.rearZoneColor, gridLeft, gridLeft + gridWidth * rearCols / boardWidth, yMin, yMax);
            AddZoneStripSegment(
                parent,
                theme.supportZoneColor,
                gridLeft + gridWidth * rearCols / boardWidth,
                gridLeft + gridWidth * (rearCols + supportCols) / boardWidth,
                yMin,
                yMax);
            AddZoneStripSegment(
                parent,
                theme.frontZoneColor,
                gridLeft + gridWidth * (rearCols + supportCols) / boardWidth,
                gridRight,
                yMin,
                yMax);
        }

        private static void AddZoneStripSegment(
            Transform parent,
            Color color,
            float anchorMinX,
            float anchorMaxX,
            float anchorMinY,
            float anchorMaxY)
        {
            var segment = CreateRegion(parent, "ZoneStrip", new Vector2(anchorMinX, anchorMinY), new Vector2(anchorMaxX, anchorMaxY));
            var image = segment.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static GameObject CreateTilePrefab(UiThemeSO theme)
        {
            var tile = new GameObject("TilePrefab", typeof(RectTransform));
            var image = tile.AddComponent<Image>();
            image.color = theme.rearZoneColor;

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

        private static ShopView CreateShopSection(Transform parent, TMP_Text sharedTooltip, UiThemeSO theme)
        {
            var (generalRoot, rerollGeneral) = CreateLaneColumn(parent, "Offensive", 0.17f, theme);
            var (engineerRoot, rerollEngineers) = CreateLaneColumn(parent, "Defensive", 0.5f, theme);
            var (reqRoot, rerollReq) = CreateLaneColumn(parent, "Specialty", 0.83f, theme);

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
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return shopView;
        }

        private static (Transform offersRoot, Button reroll) CreateLaneColumn(
            Transform parent,
            string title,
            float centerX,
            UiThemeSO theme)
        {
            var column = CreateRegion(parent, title + "Column", new Vector2(centerX - 0.14f, 0.05f), new Vector2(centerX + 0.14f, 0.95f));
            var laneBg = column.AddComponent<Image>();
            laneBg.color = title switch
            {
                "Offensive" => theme.generalLaneTint,
                "Defensive" => theme.engineersLaneTint,
                _ => theme.requisitionLaneTint
            };
            laneBg.raycastTarget = false;

            var header = MenuSceneSetup.CreateLabelPublic(
                column.transform, title, 20, FontStyles.Bold,
                new Vector2(0.5f, 0.94f), new Vector2(180f, 28f));
            UiThemeSceneStyling.StyleLabel(header, theme);

            var offers = CreateRegion(column.transform, "Offers", new Vector2(0f, 0.12f), new Vector2(1f, 0.82f));
            var layout = offers.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var reroll = MenuSceneSetup.CreateSmallButtonPublic(
                column.transform, "Reroll", new Vector2(0.5f, 0.05f), new Vector2(140f, 34f));
            UiThemeSceneStyling.StyleButton(reroll, theme);
            return (offers.transform, reroll);
        }

        private static GameObject CreateOfferCardPrefab(UiThemeSO theme)
        {
            var card = new GameObject("OfferCard", typeof(RectTransform));
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 110f);

            var image = card.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(image, theme);
            image.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(card.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.55f);
            iconRect.anchorMax = new Vector2(0f, 0.55f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(32f, 32f);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.enabled = false;
            iconImage.raycastTarget = false;

            var offerView = card.AddComponent<ShopOfferView>();
            card.AddComponent<ShopOfferDragSource>();

            var pieceText = MenuSceneSetup.CreateLabelPublic(
                card.transform, "piece", 15, FontStyles.Bold,
                new Vector2(0.55f, 0.72f), new Vector2(120f, 24f));
            var priceText = MenuSceneSetup.CreateLabelPublic(
                card.transform, "0G", 14, FontStyles.Normal,
                new Vector2(0.55f, 0.5f), new Vector2(120f, 20f));
            UiThemeSceneStyling.StyleLabel(pieceText, theme);
            UiThemeSceneStyling.StyleLabel(priceText, theme, secondary: true);

            MenuSceneSetup.CreateLabelPublic(
                card.transform, "drag", 11, FontStyles.Italic,
                new Vector2(0.5f, 0.88f), new Vector2(180f, 16f));

            var lockBtn = MenuSceneSetup.CreateSmallButtonPublic(
                card.transform, "Lock", new Vector2(0.5f, 0.16f), new Vector2(100f, 28f));
            UiThemeSceneStyling.StyleButton(lockBtn, theme);

            var lockIndicator = new GameObject("Locked", typeof(RectTransform));
            lockIndicator.transform.SetParent(card.transform, false);
            Stretch(lockIndicator.GetComponent<RectTransform>());
            var lockImage = lockIndicator.AddComponent<Image>();
            lockImage.color = new Color(theme.accentColor.r, theme.accentColor.g, theme.accentColor.b, 0.25f);
            lockImage.enabled = false;
            lockImage.raycastTarget = false;

            var serialized = new SerializedObject(offerView);
            serialized.FindProperty("cardBackground").objectReferenceValue = image;
            serialized.FindProperty("iconImage").objectReferenceValue = iconImage;
            serialized.FindProperty("pieceIdText").objectReferenceValue = pieceText;
            serialized.FindProperty("priceText").objectReferenceValue = priceText;
            serialized.FindProperty("lockButton").objectReferenceValue = lockBtn;
            serialized.FindProperty("lockedIndicator").objectReferenceValue = lockImage;
            serialized.FindProperty("dragSource").objectReferenceValue = card.GetComponent<ShopOfferDragSource>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static ReservesView CreateReservesSection(Transform bottomBar, UiThemeSO theme)
        {
            var reservesRegion = CreateRegion(bottomBar.transform, "ReservesRegion",
                new Vector2(0f, 0f), new Vector2(0.48f, 1f));

            var title = MenuSceneSetup.CreateLabelPublic(
                reservesRegion.transform, "Reserves", 18, FontStyles.Bold,
                new Vector2(0.08f, 0.88f), new Vector2(140f, 28f));
            UiThemeSceneStyling.StyleLabel(title, theme);

            var gridRoot = new GameObject("ReservesGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(reservesRegion.transform, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.04f, 0.08f);
            gridRect.anchorMax = new Vector2(0.96f, 0.82f);
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
            image.color = theme.cardColor;
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
            UiThemeSceneStyling.AddPanelBackground(card.transform, theme);
            return card;
        }

        private static Button CreateLastBattleLogButton(
            Transform bottomBar,
            UiThemeSO theme,
            LastBattleLogReviewPresenter reviewPresenter)
        {
            var button = MenuSceneSetup.CreateSmallButtonPublic(
                bottomBar.transform, "Last Log", new Vector2(0.54f, 0.5f), new Vector2(120f, 44f));
            UiThemeSceneStyling.StyleButton(button, theme);

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
            UiThemeApplicator.ApplyPanel(sheetBg, theme);

            var title = MenuSceneSetup.CreateLabelPublic(
                sheet.transform, "Previous Battle Log (dev)", 22, FontStyles.Bold,
                new Vector2(0.5f, 0.94f), new Vector2(520f, 32f));
            UiThemeSceneStyling.StyleLabel(title, theme);

            var scrollRoot = CreateRegion(sheet.transform, "LogScroll", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.86f));
            var scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.25f);
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

        private static SellDropZone CreateSellZone(Transform bottomBar, UiThemeSO theme)
        {
            var sell = CreateRegion(bottomBar.transform, "SellZone", new Vector2(0.58f, 0.1f), new Vector2(0.78f, 0.9f));
            var image = sell.AddComponent<Image>();
            image.color = theme.sellZoneColor;

            var label = MenuSceneSetup.CreateLabelPublic(
                sell.transform, "Sell\n(drop here)", 20, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(260f, 60f));
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
            UiThemeSceneStyling.AddPanelBackground(card.transform, theme);

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
            UiThemeApplicator.ApplyPanel(sheetBg, theme);

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

        private static void WireFlowPresenter(
            CombatFlowPresenter presenter,
            CombatDirector director,
            TacticPausePanel panel,
            BattleReportPresenter battleReport,
            GameObject loadingOverlay,
            TMP_Text loadingText)
        {
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("combatDirector").objectReferenceValue = director;
            serialized.FindProperty("tacticPausePanel").objectReferenceValue = panel;
            serialized.FindProperty("battleReportPresenter").objectReferenceValue = battleReport;
            serialized.FindProperty("loadingOverlay").objectReferenceValue = loadingOverlay;
            serialized.FindProperty("loadingText").objectReferenceValue = loadingText;
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
            Button menu)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("buildPanel").objectReferenceValue = buildPanel;
            serialized.FindProperty("buildPanelCanvasGroup").objectReferenceValue = buildCanvasGroup;
            serialized.FindProperty("combatPanel").objectReferenceValue = combatPanel;
            serialized.FindProperty("boardArea").objectReferenceValue = boardArea.GetComponent<RectTransform>();
            serialized.FindProperty("shopArea").objectReferenceValue = shopArea;
            serialized.FindProperty("bottomBar").objectReferenceValue = bottomBar;
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
