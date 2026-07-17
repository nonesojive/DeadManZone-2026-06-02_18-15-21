using DeadManZone.Presentation.MainMenu;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    internal static class CinematicMenuUiBuilder
    {
        private const string SwooshSfxPath = "Assets/SlimUI/Modern Menu 1/Audio/Clicks/SFX_Click_Whoosh.mp3";

        internal sealed class BuildResult
        {
            public GameObject CanvasRoot;
            public MainMenuController Controller;
            public MainMenuCameraDirector Director;
            public GameObject MainPanel;
            public Button ContinueButton;
            public Button NewRunButton;
            public Button AchievementsButton;
            public Button LeaderboardButton;
            public Button OptionsButton;
            public Button ExitButton;
            public GameObject OptionsPanel;
            public Button OptionsBackButton;
            public GameObject FactionPanel;
            public FactionSelectView FactionSelectView;
            public AchievementsPanelView AchievementsPanel;
            public LeaderboardPanelView LeaderboardPanel;
        }

        internal static BuildResult Build(Camera menuCamera, UiThemeSO theme, ScriptableObject menuTheme)
        {
            var canvasGo = CreateCanvasRoot(menuCamera);
            UiThemeSceneStyling.AddDecorBackground(canvasGo.transform, theme, menuScene: true);
            var menuRoot = MenuSceneSetup.CreateStretchChild(canvasGo.transform, "MainMenu");
            var controller = menuRoot.AddComponent<MainMenuController>();

            var director = menuCamera.GetComponent<MainMenuCameraDirector>();
            if (director == null)
                director = menuCamera.gameObject.AddComponent<MainMenuCameraDirector>();

            var mainPanel = BuildMainPanel(menuRoot.transform, theme, menuTheme, out var continueBtn, out var newRunBtn,
                out var achievementsBtn, out var leaderboardBtn, out var optionsBtn, out var exitBtn, out var subtitle);

            var optionsPanel = BuildOptionsPanel(menuRoot.transform, theme, menuTheme, out var optionsBackBtn);
            var (factionPanel, factionSelectView) = FactionSelectPanelBuilder.Build(menuRoot.transform, theme, menuTheme);
            var achievementsPanel = BuildAchievementsPanel(menuRoot.transform, theme, menuTheme, out _);
            var leaderboardPanel = BuildLeaderboardPanel(menuRoot.transform, theme, menuTheme, out _);
            var loadingOverlay = BuildLoadingOverlay(canvasGo.transform, theme, out _);

            WireDirector(director, menuCamera.GetComponent<Animator>(), menuRoot, loadingOverlay);

            return new BuildResult
            {
                CanvasRoot = canvasGo,
                Controller = controller,
                Director = director,
                MainPanel = mainPanel,
                ContinueButton = continueBtn,
                NewRunButton = newRunBtn,
                AchievementsButton = achievementsBtn,
                LeaderboardButton = leaderboardBtn,
                OptionsButton = optionsBtn,
                ExitButton = exitBtn,
                OptionsPanel = optionsPanel,
                OptionsBackButton = optionsBackBtn,
                FactionPanel = factionPanel,
                FactionSelectView = factionSelectView,
                AchievementsPanel = achievementsPanel,
                LeaderboardPanel = leaderboardPanel
            };
        }

        private static GameObject CreateCanvasRoot(Camera menuCamera)
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = menuCamera;
            canvas.planeDistance = 100f;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo;
        }

        private static GameObject BuildMainPanel(
            Transform parent,
            UiThemeSO theme,
            ScriptableObject menuTheme,
            out Button continueBtn,
            out Button newRunBtn,
            out Button achievementsBtn,
            out Button leaderboardBtn,
            out Button optionsBtn,
            out Button exitBtn,
            out TMP_Text subtitle)
        {
            var mainPanel = MenuSceneSetup.CreateStretchChild(parent, "MainPanel");

            var title = MenuSceneSetup.CreateLabelPublic(mainPanel.transform, "Until The Trenches Fall", 52,
                FontStyles.Bold, new Vector2(0.5f, 0.78f), new Vector2(900, 80));
            title.color = theme.textPrimary;

            subtitle = MenuSceneSetup.CreateLabelPublic(mainPanel.transform, string.Empty, 22, FontStyles.Normal,
                new Vector2(0.5f, 0.68f), new Vector2(900, 50));
            subtitle.color = theme.textSecondary;

            var buttonPanel = MenuSceneSetup.CreateStretchChild(mainPanel.transform, "MainMenuButtonPanel");
            continueBtn = CreateMenuButton(buttonPanel.transform, "ContinueButton", "Continue",
                new Vector2(0.5f, 0.52f), menuTheme, theme, accent: true);
            newRunBtn = CreateMenuButton(buttonPanel.transform, "NewRunButton", "New Run",
                new Vector2(0.5f, 0.44f), menuTheme, theme, accent: true);
            achievementsBtn = CreateMenuButton(buttonPanel.transform, "AchievementsButton", "Achievements",
                new Vector2(0.5f, 0.36f), menuTheme, theme);
            leaderboardBtn = CreateMenuButton(buttonPanel.transform, "LeaderboardButton", "Leaderboard",
                new Vector2(0.5f, 0.28f), menuTheme, theme);
            optionsBtn = CreateMenuButton(buttonPanel.transform, "OptionsButton", "Options",
                new Vector2(0.5f, 0.20f), menuTheme, theme);
            exitBtn = CreateMenuButton(buttonPanel.transform, "ExitButton", "Exit",
                new Vector2(0.5f, 0.12f), menuTheme, theme);

            return mainPanel;
        }

        private static GameObject BuildOptionsPanel(
            Transform parent,
            UiThemeSO theme,
            ScriptableObject menuTheme,
            out Button backButton)
        {
            var panel = MenuSceneSetup.CreateStretchChild(parent, "OptionsPanel");
            panel.SetActive(false);
            UiThemeSceneStyling.AddModalFrame(panel.transform, theme);

            MenuSceneSetup.CreateLabelPublic(panel.transform, "Options", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.72f), new Vector2(500, 60)).color = theme.textPrimary;

            var optionsView = panel.AddComponent<MenuOptionsPanel>();
            var musicSlider = CreateSliderRow(panel.transform, "Music Volume", new Vector2(0.5f, 0.52f), theme);
            var sfxSlider = CreateSliderRow(panel.transform, "SFX Volume", new Vector2(0.5f, 0.42f), theme);
            backButton = CreateMenuButton(panel.transform, "OptionsBackButton", "Back",
                new Vector2(0.5f, 0.22f), menuTheme, theme);
            var fullscreenBtn = CreateMenuButton(panel.transform, "FullscreenButton", "Fullscreen",
                new Vector2(0.5f, 0.32f), menuTheme, theme);
            var fullscreenLabel = fullscreenBtn.GetComponentInChildren<TMP_Text>();

            WireOptionsPanel(optionsView, musicSlider, sfxSlider, fullscreenBtn, fullscreenLabel);
            return panel;
        }

        private static AchievementsPanelView BuildAchievementsPanel(
            Transform parent,
            UiThemeSO theme,
            ScriptableObject menuTheme,
            out Button backButton)
        {
            var panel = MenuSceneSetup.CreateStretchChild(parent, "AchievementsPanel");
            panel.SetActive(false);
            UiThemeSceneStyling.AddModalFrame(panel.transform, theme);

            MenuSceneSetup.CreateLabelPublic(panel.transform, "Achievements", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.82f), new Vector2(500, 60)).color = theme.textPrimary;

            var listText = MenuSceneSetup.CreateLabelPublic(panel.transform, string.Empty, 18, FontStyles.Normal,
                new Vector2(0.5f, 0.48f), new Vector2(900, 420));
            listText.alignment = TextAlignmentOptions.TopLeft;
            listText.color = theme.textSecondary;

            backButton = CreateMenuButton(panel.transform, "AchievementsBackButton", "Back",
                new Vector2(0.5f, 0.08f), menuTheme, theme);

            var view = panel.AddComponent<AchievementsPanelView>();
            WireMetaPanel(view, panel, listText, backButton, theme);
            return view;
        }

        private static LeaderboardPanelView BuildLeaderboardPanel(
            Transform parent,
            UiThemeSO theme,
            ScriptableObject menuTheme,
            out Button backButton)
        {
            var panel = MenuSceneSetup.CreateStretchChild(parent, "LeaderboardPanel");
            panel.SetActive(false);
            UiThemeSceneStyling.AddModalFrame(panel.transform, theme);

            MenuSceneSetup.CreateLabelPublic(panel.transform, "Leaderboard", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.82f), new Vector2(500, 60)).color = theme.textPrimary;

            var listText = MenuSceneSetup.CreateLabelPublic(panel.transform, string.Empty, 18, FontStyles.Normal,
                new Vector2(0.5f, 0.48f), new Vector2(900, 420));
            listText.alignment = TextAlignmentOptions.TopLeft;
            listText.color = theme.textSecondary;

            backButton = CreateMenuButton(panel.transform, "LeaderboardBackButton", "Back",
                new Vector2(0.5f, 0.08f), menuTheme, theme);

            var view = panel.AddComponent<LeaderboardPanelView>();
            WireMetaPanel(view, panel, listText, backButton, theme);
            return view;
        }

        private static GameObject BuildLoadingOverlay(
            Transform canvasParent,
            UiThemeSO theme,
            out Slider loadingBar)
        {
            var overlay = MenuSceneSetup.CreateStretchChild(canvasParent, "LoadingOverlay");
            overlay.SetActive(false);
            if (theme.combatBackgroundSprite != null)
            {
                var decor = MenuSceneSetup.CreateStretchChild(overlay.transform, "LoadingDecor");
                var decorImage = decor.AddComponent<Image>();
                UiThemeApplicator.ApplyBackgroundPlate(decorImage, theme.combatBackgroundSprite, 0.45f);
            }

            var bg = overlay.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.03f, 0.88f);

            MenuSceneSetup.CreateLabelPublic(overlay.transform, "Deploying to the front…", 32, FontStyles.Bold,
                new Vector2(0.5f, 0.58f), new Vector2(700, 60)).color = theme.textPrimary;

            loadingBar = CreateSliderRow(overlay.transform, string.Empty, new Vector2(0.5f, 0.48f), theme);
            loadingBar.minValue = 0f;
            loadingBar.maxValue = 1f;
            loadingBar.value = 0f;

            return overlay;
        }

        private static void WireDirector(
            MainMenuCameraDirector director,
            Animator animator,
            GameObject menuContentRoot,
            GameObject loadingOverlay)
        {
            var serialized = new SerializedObject(director);
            serialized.FindProperty("menuCameraAnimator").objectReferenceValue = animator;
            serialized.FindProperty("menuCanvasRoot").objectReferenceValue = menuContentRoot;
            serialized.FindProperty("loadingOverlay").objectReferenceValue = loadingOverlay;
            serialized.FindProperty("loadingBar").objectReferenceValue =
                loadingOverlay.GetComponentInChildren<Slider>(true);
            serialized.FindProperty("loadingText").objectReferenceValue =
                loadingOverlay.GetComponentInChildren<TMP_Text>(true);

            var swooshClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SwooshSfxPath);
            if (swooshClip != null)
            {
                var audioGo = new GameObject("TransitionAudio");
                audioGo.transform.SetParent(director.transform, false);
                var source = audioGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.clip = swooshClip;
                source.volume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
                serialized.FindProperty("transitionSound").objectReferenceValue = source;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireController(BuildResult result)
        {
            var serialized = new SerializedObject(result.Controller);
            serialized.FindProperty("mainPanel").objectReferenceValue = result.MainPanel;
            serialized.FindProperty("continueButton").objectReferenceValue = result.ContinueButton;
            serialized.FindProperty("newRunButton").objectReferenceValue = result.NewRunButton;
            serialized.FindProperty("achievementsButton").objectReferenceValue = result.AchievementsButton;
            serialized.FindProperty("leaderboardButton").objectReferenceValue = result.LeaderboardButton;
            serialized.FindProperty("optionsButton").objectReferenceValue = result.OptionsButton;
            serialized.FindProperty("exitButton").objectReferenceValue = result.ExitButton;
            serialized.FindProperty("optionsPanel").objectReferenceValue = result.OptionsPanel;
            serialized.FindProperty("optionsBackButton").objectReferenceValue = result.OptionsBackButton;
            serialized.FindProperty("factionPanel").objectReferenceValue = result.FactionPanel;
            serialized.FindProperty("factionSelectView").objectReferenceValue = result.FactionSelectView;
            serialized.FindProperty("achievementsPanel").objectReferenceValue = result.AchievementsPanel;
            serialized.FindProperty("leaderboardPanel").objectReferenceValue = result.LeaderboardPanel;
            serialized.FindProperty("cameraDirector").objectReferenceValue = result.Director;

            var subtitle = result.MainPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in subtitle)
            {
                if (text.text == string.Empty && text.fontSize <= 24f)
                {
                    serialized.FindProperty("subtitleText").objectReferenceValue = text;
                    break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static BuildResult BuildAndWire(Camera menuCamera, UiThemeSO theme, ScriptableObject menuTheme)
        {
            var result = Build(menuCamera, theme, menuTheme);
            WireController(result);
            return result;
        }

        private static Button CreateMenuButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchor,
            ScriptableObject menuTheme,
            UiThemeSO theme,
            bool accent = false)
        {
            var button = MenuSceneSetup.CreateSmallButtonPublic(parent, label, anchor, new Vector2(360, 72));
            button.gameObject.name = objectName;
            button.onClick.RemoveAllListeners();
            UiThemeSceneStyling.StyleButton(button, theme, accent);
            return button;
        }

        private static Slider CreateSliderRow(Transform parent, string label, Vector2 anchor, UiThemeSO theme)
        {
            if (!string.IsNullOrEmpty(label))
            {
                var labelText = MenuSceneSetup.CreateLabelPublic(parent, label, 22, FontStyles.Normal,
                    new Vector2(anchor.x, anchor.y + 0.04f), new Vector2(420, 32));
                labelText.color = theme.textSecondary;
            }

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            SetAnchored(sliderGo.GetComponent<RectTransform>(), anchor, new Vector2(420, 24));

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderGo.transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = theme.buttonNormal;
            UiThemeApplicator.ApplyCard(background.GetComponent<Image>(), theme);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(8f, 8f);
            fillAreaRect.offsetMax = new Vector2(-8f, -8f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = theme.accentColor;
            UiThemeApplicator.ApplyCard(fill.GetComponent<Image>(), theme);
            if (theme.accentButtonSprite != null)
            {
                var fillImage = fill.GetComponent<Image>();
                fillImage.sprite = theme.accentButtonSprite;
                fillImage.type = Image.Type.Sliced;
                fillImage.color = Color.white;
            }

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;
            UiThemeSceneStyling.StyleSlider(slider, theme);
            return slider;
        }

        private static void WireOptionsPanel(
            MenuOptionsPanel view,
            Slider musicSlider,
            Slider sfxSlider,
            Button fullscreenButton,
            TMP_Text fullscreenLabel)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            serialized.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            serialized.FindProperty("fullscreenButton").objectReferenceValue = fullscreenButton;
            serialized.FindProperty("fullscreenLabel").objectReferenceValue = fullscreenLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMetaPanel(
            AchievementsPanelView view,
            GameObject panel,
            TMP_Text listText,
            Button backButton,
            UiThemeSO theme)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = panel;
            serialized.FindProperty("listText").objectReferenceValue = listText;
            serialized.FindProperty("backButton").objectReferenceValue = backButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.ApplyTheme(theme);
        }

        private static void WireMetaPanel(
            LeaderboardPanelView view,
            GameObject panel,
            TMP_Text listText,
            Button backButton,
            UiThemeSO theme)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("root").objectReferenceValue = panel;
            serialized.FindProperty("listText").objectReferenceValue = listText;
            serialized.FindProperty("backButton").objectReferenceValue = backButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.ApplyTheme(theme);
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
