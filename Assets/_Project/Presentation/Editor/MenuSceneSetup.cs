using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.MainMenu;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    public static class MenuSceneSetup
    {
        internal const string ScenesFolder = "Assets/_Project/Scenes";
        internal const string MainMenuPath = ScenesFolder + "/MainMenu.unity";
        private const string RunPath = ScenesFolder + "/Run.unity";

        [MenuItem("DeadManZone/Set Play Mode Start Scene to MainMenu")]
        public static void SetPlayModeStartScene()
        {
            var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            if (mainMenu == null)
            {
                Debug.LogWarning("MainMenu scene not found. Run DeadManZone/Setup Main Menu & Run Scenes first.");
                return;
            }

            EditorSceneManager.OpenScene(MainMenuPath);
            Debug.Log("Opened MainMenu as the active scene. Press Play to start from the main menu.");
        }

        [MenuItem("DeadManZone/Setup Main Menu & Run Scenes")]
        public static void SetupScenes()
        {
            UiThemeEditor.EnsureThemeAsset();
            EnsureFolder(ScenesFolder);
            CreateMainMenuScene();
            CreateRunScene();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("DeadManZone: MainMenu and Run scenes created. Open MainMenu and press Play.");
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EnsureEventSystem();

            var theme = UiThemeEditor.EnsureThemeAsset();
            var canvas = CreateCanvas("Canvas");
            var canvasBg = canvas.GetComponent<Image>();
            if (canvasBg == null)
                canvasBg = canvas.AddComponent<Image>();
            canvasBg.color = theme.backgroundColor;
            canvasBg.raycastTarget = false;

            var menuRoot = CreateStretchChild(canvas.transform, "MainMenu");
            var controller = menuRoot.AddComponent<MainMenuController>();

            var mainPanel = CreateStretchChild(menuRoot.transform, "MainPanel");
            UiThemeSceneStyling.AddPanelBackground(mainPanel.transform, theme);

            var title = CreateLabel(mainPanel.transform, "Dead Man Zone", 52, FontStyles.Bold);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(700, 80));
            title.color = theme.accentColor;

            var subtitle = CreateLabel(mainPanel.transform, "", 22, FontStyles.Normal);
            SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(640, 48));
            UiThemeSceneStyling.StyleLabel(subtitle, theme, secondary: true);

            var continueBtn = CreateButton(mainPanel.transform, "Continue", new Vector2(0.5f, 0.48f));
            var newRunBtn = CreateButton(mainPanel.transform, "New Run", new Vector2(0.5f, 0.36f));
            UiThemeSceneStyling.StyleButton(continueBtn, theme, accent: true);
            UiThemeSceneStyling.StyleButton(newRunBtn, theme);

            var factionPanel = CreateStretchChild(menuRoot.transform, "FactionPanel");
            factionPanel.SetActive(false);
            UiThemeSceneStyling.AddPanelBackground(factionPanel.transform, theme);
            var factionTitle = CreateLabel(factionPanel.transform, "Choose faction", 36, FontStyles.Bold,
                new Vector2(0.5f, 0.72f), new Vector2(500, 60));
            factionTitle.color = theme.textPrimary;
            var ironBtn = CreateButton(factionPanel.transform, "Iron Vanguard", new Vector2(0.5f, 0.45f));
            var backBtn = CreateButton(factionPanel.transform, "Back", new Vector2(0.5f, 0.28f));
            UiThemeSceneStyling.StyleButton(ironBtn, theme, accent: true);
            UiThemeSceneStyling.StyleButton(backBtn, theme);

            WireController(controller, mainPanel, continueBtn, newRunBtn, subtitle,
                factionPanel, ironBtn, backBtn);

            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void CreateRunScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EnsureEventSystem();

            var canvas = CreateCanvas("Canvas");
            RunSceneSetup.BuildRunScene(canvas);

            EditorSceneManager.SaveScene(scene, RunPath);
        }

        internal static RunManager CreateRunManager()
        {
            var go = new GameObject("RunManager");
            var manager = go.AddComponent<RunManager>();
            var database = ContentDatabase.Load();
            if (database == null)
                Debug.LogWarning("ContentDatabase not found. Run DeadManZone → Generate Vertical Slice Content first.");

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("contentDatabase").objectReferenceValue = database;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static void WireController(
            MainMenuController controller,
            GameObject mainPanel,
            Button continueButton,
            Button newRunButton,
            TMP_Text subtitle,
            GameObject factionPanel,
            Button ironButton,
            Button backButton)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("newRunButton").objectReferenceValue = newRunButton;
            serialized.FindProperty("subtitleText").objectReferenceValue = subtitle;
            serialized.FindProperty("factionPanel").objectReferenceValue = factionPanel;
            serialized.FindProperty("ironVanguardButton").objectReferenceValue = ironButton;
            serialized.FindProperty("factionBackButton").objectReferenceValue = backButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(RunPath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        internal static GameObject CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        internal static TMP_Text CreateLabelPublic(
            Transform parent,
            string text,
            float fontSize,
            FontStyles style,
            Vector2 anchor,
            Vector2 size) =>
            CreateLabel(parent, text, fontSize, style, anchor, size);

        private static TMP_Text CreateLabel(
            Transform parent,
            string text,
            float fontSize,
            FontStyles style,
            Vector2? anchor = null,
            Vector2? size = null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiThemeProvider.Current.textPrimary;

            if (anchor.HasValue && size.HasValue)
                SetAnchored(label.rectTransform, anchor.Value, size.Value);

            return label;
        }

        internal static Button CreateButtonPublic(Transform parent, string label, Vector2 anchor) =>
            CreateButton(parent, label, anchor, new Vector2(320, 56));

        internal static Button CreateSmallButtonPublic(
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 size) =>
            CreateButton(parent, label, anchor, size);

        private static Button CreateButton(Transform parent, string label, Vector2 anchor)
        {
            return CreateButton(parent, label, anchor, new Vector2(320, 56));
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            var theme = UiThemeProvider.Current;
            image.color = theme.buttonNormal;

            var button = go.AddComponent<Button>();
            UiThemeApplicator.ApplyButton(button, theme);

            SetAnchored(go.GetComponent<RectTransform>(), anchor, size);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                var leaf = System.IO.Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                    EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
