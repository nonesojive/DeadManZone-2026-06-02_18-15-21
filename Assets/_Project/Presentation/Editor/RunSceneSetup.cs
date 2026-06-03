using DeadManZone.Game;
using DeadManZone.Presentation.Bench;
using DeadManZone.Presentation.Board;
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
            var fightStatus = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 20, FontStyles.Bold,
                new Vector2(0.28f, 0.5f), new Vector2(360f, 56f));
            var currencies = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 18, FontStyles.Normal,
                new Vector2(0.55f, 0.5f), new Vector2(420f, 56f));
            UiThemeSceneStyling.StyleLabel(fightStatus, theme);
            UiThemeSceneStyling.StyleLabel(currencies, theme, secondary: true);
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("statusText").objectReferenceValue = fightStatus;
            hudSerialized.FindProperty("currenciesText").objectReferenceValue = currencies;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            hud.ApplyTheme(theme);

            var tooltip = MenuSceneSetup.CreateLabelPublic(
                topBar.transform, "", 16, FontStyles.Italic,
                new Vector2(0.78f, 0.5f), new Vector2(400f, 56f));
            UiThemeSceneStyling.StyleLabel(tooltip, theme, secondary: true);

            var saveBtn = MenuSceneSetup.CreateSmallButtonPublic(
                topBar.transform, "Save & Exit", new Vector2(0.06f, 0.5f), new Vector2(140f, 40f));
            var menuBtn = MenuSceneSetup.CreateSmallButtonPublic(
                topBar.transform, "Main Menu", new Vector2(0.14f, 0.5f), new Vector2(140f, 40f));
            UiThemeSceneStyling.StyleButton(saveBtn, theme);
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
            var benchView = CreateBenchSection(bottomBar.transform, theme);
            var sellZone = CreateSellZone(bottomBar.transform, theme);

            var endOverlay = CreateRunEndOverlay(buildPanel.transform, theme, menuBtn);

            var combatPanel = CreateRegion(controllerRoot.transform, "CombatPanel", Vector2.zero, Vector2.one);
            combatPanel.SetActive(false);
            var combatOverlay = combatPanel.AddComponent<Image>();
            combatOverlay.color = theme.combatOverlayColor;
            combatOverlay.raycastTarget = false;

            var combatDirector = combatPanel.AddComponent<CombatDirector>();
            var combatBoard = combatPanel.AddComponent<CombatBoardPresenter>();
            var phasePanel = CreatePhaseCommandPanel(combatPanel.transform, theme, out var bannerText, out var bannerGroup);
            var flowPresenter = combatPanel.AddComponent<CombatFlowPresenter>();

            WireFlowPresenter(flowPresenter, combatDirector, phasePanel);
            WireCombatBoardPresenter(combatBoard, combatDirector, boardView, bannerText, bannerGroup);
            WireController(controller, buildPanel, buildCanvasGroup, combatPanel, boardView, shopView,
                benchView, combatDirector, phasePanel, hud, endOverlay, beginFight, saveBtn, menuBtn);

            var panelSerialized = new SerializedObject(phasePanel);
            panelSerialized.FindProperty("combatDirector").objectReferenceValue = combatDirector;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            var directorSerialized = new SerializedObject(combatDirector);
            directorSerialized.FindProperty("autoAdvanceAfterCommands").boolValue = false;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BoardView CreateBoardSection(Transform parent, UiThemeSO theme)
        {
            CreateZoneLabel(parent, "REAR", new Vector2(0.02f, 0.88f), theme.textSecondary);
            CreateZoneLabel(parent, "SUPPORT", new Vector2(0.02f, 0.55f), theme.textSecondary);
            CreateZoneLabel(parent, "FRONT", new Vector2(0.02f, 0.22f), theme.textSecondary);

            var gridRoot = new GameObject("TileGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(parent, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.08f, 0f);
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.cellSize = new Vector2(52, 52);
            grid.spacing = new Vector2(3, 3);
            grid.padding = new RectOffset(8, 8, 8, 8);

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

        private static void CreateZoneLabel(Transform parent, string text, Vector2 anchor, Color color)
        {
            var label = MenuSceneSetup.CreateLabelPublic(parent, text, 14, FontStyles.Bold, anchor, new Vector2(72f, 24f));
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
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
            var currencies = MenuSceneSetup.CreateLabelPublic(
                parent, "", 18, FontStyles.Normal,
                new Vector2(0.5f, 0.96f), new Vector2(700f, 28f));
            UiThemeSceneStyling.StyleLabel(currencies, theme, secondary: true);

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
            serialized.FindProperty("currenciesText").objectReferenceValue = currencies;
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

        private static BenchView CreateBenchSection(Transform bottomBar, UiThemeSO theme)
        {
            var benchRegion = CreateRegion(bottomBar.transform, "BenchRegion",
                new Vector2(0f, 0f), new Vector2(0.56f, 1f));

            var layout = benchRegion.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var slots = new BenchSlotView[RunOrchestrator.BenchLimit];
            for (int i = 0; i < RunOrchestrator.BenchLimit; i++)
                slots[i] = CreateBenchSlot(benchRegion.transform, i, theme);

            var benchView = benchRegion.AddComponent<BenchView>();
            var serialized = new SerializedObject(benchView);
            serialized.FindProperty("slots").arraySize = RunOrchestrator.BenchLimit;
            for (int i = 0; i < RunOrchestrator.BenchLimit; i++)
                serialized.FindProperty("slots").GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return benchView;
        }

        private static BenchSlotView CreateBenchSlot(Transform parent, int index, UiThemeSO theme)
        {
            var slot = new GameObject($"BenchSlot{index + 1}", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            var rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 72f);

            var layout = slot.AddComponent<LayoutElement>();
            layout.minWidth = 180f;
            layout.minHeight = 72f;

            var bg = slot.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(bg, theme);

            var chipRoot = new GameObject("ChipRoot", typeof(RectTransform));
            chipRoot.transform.SetParent(slot.transform, false);
            Stretch(chipRoot.GetComponent<RectTransform>());

            var label = MenuSceneSetup.CreateLabelPublic(
                slot.transform, $"Bench {index + 1}", 16, FontStyles.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(160f, 40f));
            UiThemeSceneStyling.StyleLabel(label, theme, secondary: true);

            var slotView = slot.AddComponent<BenchSlotView>();
            var serialized = new SerializedObject(slotView);
            serialized.FindProperty("slotIndex").intValue = index;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("background").objectReferenceValue = bg;
            serialized.FindProperty("chipRoot").objectReferenceValue = chipRoot.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return slotView;
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

        private static RunEndOverlayView CreateRunEndOverlay(Transform parent, UiThemeSO theme, Button menuButton)
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

            var overlay = root.AddComponent<RunEndOverlayView>();
            var serialized = new SerializedObject(overlay);
            serialized.FindProperty("root").objectReferenceValue = root;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = menuButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            overlay.ApplyTheme(theme);
            return overlay;
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

        private static void WireFlowPresenter(
            CombatFlowPresenter presenter,
            CombatDirector director,
            PhaseCommandPanel panel)
        {
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("combatDirector").objectReferenceValue = director;
            serialized.FindProperty("phaseCommandPanel").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireController(
            RunSceneController controller,
            GameObject buildPanel,
            CanvasGroup buildCanvasGroup,
            GameObject combatPanel,
            BoardView boardView,
            ShopView shopView,
            BenchView benchView,
            CombatDirector combatDirector,
            PhaseCommandPanel phasePanel,
            RunHudView hud,
            RunEndOverlayView endOverlay,
            Button beginFight,
            Button save,
            Button menu)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("buildPanel").objectReferenceValue = buildPanel;
            serialized.FindProperty("buildPanelCanvasGroup").objectReferenceValue = buildCanvasGroup;
            serialized.FindProperty("combatPanel").objectReferenceValue = combatPanel;
            serialized.FindProperty("boardView").objectReferenceValue = boardView;
            serialized.FindProperty("shopView").objectReferenceValue = shopView;
            serialized.FindProperty("benchView").objectReferenceValue = benchView;
            serialized.FindProperty("combatDirector").objectReferenceValue = combatDirector;
            serialized.FindProperty("phaseCommandPanel").objectReferenceValue = phasePanel;
            serialized.FindProperty("runHudView").objectReferenceValue = hud;
            serialized.FindProperty("runEndOverlay").objectReferenceValue = endOverlay;
            serialized.FindProperty("beginFightButton").objectReferenceValue = beginFight;
            serialized.FindProperty("saveAndExitButton").objectReferenceValue = save;
            serialized.FindProperty("backToMenuButton").objectReferenceValue = menu;
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
