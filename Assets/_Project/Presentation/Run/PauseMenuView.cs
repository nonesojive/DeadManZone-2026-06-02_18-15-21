using DeadManZone.Game;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadManZone.Presentation.Run
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button battleReportButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private LastBattleLogReviewPresenter battleReportReview;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            battleReportReview ??= FindAnyObjectByType<LastBattleLogReviewPresenter>();
            EnsureBattleReportButton();

            if (resumeButton != null)
                resumeButton.onClick.AddListener(Close);
            if (optionsButton != null)
                optionsButton.onClick.AddListener(ShowOptions);
            if (battleReportButton != null)
                battleReportButton.onClick.AddListener(OpenBattleReport);
            if (optionsBackButton != null)
                optionsBackButton.onClick.AddListener(ShowMain);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExit);

            ApplyGrimdarkSkin();
            Hide();
        }

        /// <summary>M6: runtime grimdark-kit pass over the scene-authored pause menu
        /// (same pattern as BattleReportPresenter.Awake). Colors/typography only —
        /// anchors and flow untouched.</summary>
        private void ApplyGrimdarkSkin()
        {
            StyleCard(mainPanel);
            StyleCard(optionsPanel);

            CombatGrimdarkSkin.StyleButton(resumeButton);
            CombatGrimdarkSkin.StyleButton(optionsButton);
            CombatGrimdarkSkin.StyleButton(battleReportButton);
            CombatGrimdarkSkin.StyleButton(mainMenuButton);
            CombatGrimdarkSkin.StyleButton(exitButton);
            CombatGrimdarkSkin.StyleButton(optionsBackButton);

            // Resume is the primary CTA — brass accent on the label.
            var resumeLabel = resumeButton != null
                ? resumeButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (resumeLabel != null)
                resumeLabel.color = CombatGrimdarkSkin.VictoryGold;
        }

        private static void StyleCard(GameObject panel)
        {
            if (panel == null)
                return;

            CombatGrimdarkSkin.StyleFrame(panel.GetComponent<Image>());
            CombatGrimdarkSkin.StylePanelText(panel);
        }

        public void Open()
        {
            RunManager.Instance?.SaveAndExit();
            ShowMain();
            if (root != null)
                root.SetActive(true);
            SetOverlayBlocking(true);
        }

        public void Close()
        {
            if (root != null)
                root.SetActive(false);
            SetOverlayBlocking(false);
        }

        public void Hide() => Close();

        /// <summary>Grimdark kit (M6); theme param kept so editor bake callers compile.
        /// Same visuals whether applied by editor setup or the Awake pass.</summary>
        public void ApplyTheme(UiThemeSO theme) => ApplyGrimdarkSkin();

        private void ShowMain()
        {
            if (mainPanel != null)
                mainPanel.SetActive(true);
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
        }

        private void ShowOptions()
        {
            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(true);
        }

        private void OpenBattleReport()
        {
            battleReportReview ??= FindAnyObjectByType<LastBattleLogReviewPresenter>();
            Close();
            battleReportReview?.Open();
        }

        private void EnsureBattleReportButton()
        {
            if (battleReportButton != null || mainPanel == null)
                return;

            var existing = mainPanel.transform.Find("BattleReportButton");
            if (existing != null)
            {
                battleReportButton = existing.GetComponent<Button>();
                return;
            }

            RepositionMenuButton(mainMenuButton, 0.24f);
            RepositionMenuButton(exitButton, 0.10f);
            battleReportButton = CreateMenuButton(mainPanel.transform, "Battle Report", 0.37f);
            UiThemeApplicator.ApplyButton(battleReportButton, UiThemeProvider.Current);
        }

        private static void RepositionMenuButton(Button button, float anchorY)
        {
            if (button == null)
                return;

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
        }

        private static Button CreateMenuButton(Transform parent, string label, float anchorY)
        {
            var theme = UiThemeProvider.Current;
            var go = new GameObject("BattleReportButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 44f);

            var image = go.GetComponent<Image>();
            UiThemeApplicator.ApplyCard(image, theme);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            UiThemeApplicator.ApplyLabel(text, secondary: false, theme);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private void OnMainMenu()
        {
            RunManager.Instance?.SaveAndExit();
            GameScenes.LoadMainMenu();
        }

        private void OnExit()
        {
            RunManager.Instance?.SaveAndExit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetOverlayBlocking(bool blocking)
        {
            if (overlayGroup == null)
                return;

            overlayGroup.alpha = blocking ? 1f : 0f;
            overlayGroup.interactable = blocking;
            overlayGroup.blocksRaycasts = blocking;
        }
    }
}
