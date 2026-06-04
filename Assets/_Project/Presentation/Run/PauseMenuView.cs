using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
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
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button optionsBackButton;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Close);
            if (optionsButton != null)
                optionsButton.onClick.AddListener(ShowOptions);
            if (optionsBackButton != null)
                optionsBackButton.onClick.AddListener(ShowMain);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExit);

            Hide();
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

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            var card = mainPanel != null ? mainPanel.GetComponent<Image>() : null;
            if (card != null)
                UiThemeApplicator.ApplyCard(card, theme);

            UiThemeApplicator.ApplyAccentButton(resumeButton, theme);
            UiThemeApplicator.ApplyButton(optionsButton, theme);
            UiThemeApplicator.ApplyButton(mainMenuButton, theme);
            UiThemeApplicator.ApplyButton(exitButton, theme);
            UiThemeApplicator.ApplyButton(optionsBackButton, theme);
        }

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
