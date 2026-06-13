using DeadManZone.Core.Meta;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Main panel")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Options")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button optionsBackButton;

        [Header("Faction select")]
        [SerializeField] private GameObject factionPanel;
        [SerializeField] private Button ironVanguardButton;
        [SerializeField] private Button dustScourgeButton;
        [SerializeField] private Button cartelButton;
        [SerializeField] private Button factionBackButton;
        [SerializeField] private TMP_Text factionDetailText;

        [Header("Meta panels")]
        [SerializeField] private AchievementsPanelView achievementsPanel;
        [SerializeField] private LeaderboardPanelView leaderboardPanel;

        [Header("Cinematic")]
        [SerializeField] private MainMenuCameraDirector cameraDirector;

        public GameObject ContinueButtonRoot => continueButton != null ? continueButton.gameObject : null;

        private void Awake()
        {
            SteamIntegration.Initialize();

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            if (newRunButton != null)
                newRunButton.onClick.AddListener(OnNewRunClicked);
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(ShowAchievementsPanel);
            if (leaderboardButton != null)
                leaderboardButton.onClick.AddListener(ShowLeaderboardPanel);
            if (optionsButton != null)
                optionsButton.onClick.AddListener(ShowOptionsPanel);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
            if (optionsBackButton != null)
                optionsBackButton.onClick.AddListener(ShowMainPanel);
            if (ironVanguardButton != null)
                ironVanguardButton.onClick.AddListener(() => StartFaction("iron_vanguard"));
            if (dustScourgeButton != null)
                dustScourgeButton.onClick.AddListener(() => StartFaction("dust_scourge"));
            if (cartelButton != null)
                cartelButton.onClick.AddListener(() => StartFaction("cartel_of_echoes"));
            if (factionBackButton != null)
                factionBackButton.onClick.AddListener(ShowMainPanel);

            achievementsPanel?.SetBackHandler(ShowMainPanel);
            leaderboardPanel?.SetBackHandler(ShowMainPanel);
        }

        private void OnEnable()
        {
            ShowMainPanel();
            Refresh();
        }

        public void Refresh()
        {
            bool hasSave = SaveManager.HasSave();
            if (continueButton != null)
                continueButton.gameObject.SetActive(hasSave);

            if (subtitleText != null)
            {
                subtitleText.text = hasSave
                    ? "Resume your campaign or begin a new run."
                    : "No saved run found. Start a new campaign.";
            }

            RefreshFactionButtons();
        }

        private void RefreshFactionButtons()
        {
            SetFactionButton(ironVanguardButton, "iron_vanguard", "IronMarch Union");
            SetFactionButton(dustScourgeButton, "dust_scourge", "Dust Scourge");
            SetFactionButton(cartelButton, "cartel_of_echoes", "Cartel of Echoes");
        }

        private static void SetFactionButton(Button button, string factionId, string displayName)
        {
            if (button == null)
                return;

            bool unlocked = MetaProgressionService.IsFactionUnlocked(factionId);
            button.interactable = unlocked;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = unlocked ? displayName : $"{displayName} (Locked)";
        }

        private void OnContinueClicked()
        {
            EnsureRunManager();
            if (!RunManager.Instance.TryContinueRun())
            {
                Debug.LogWarning("Continue requested but save could not be loaded.");
                Refresh();
                return;
            }

            if (cameraDirector != null)
                cameraDirector.LoadRunScene();
            else
                GameScenes.LoadRun();
        }

        private void OnNewRunClicked()
        {
            cameraDirector?.FocusSubPanel();
            if (factionPanel != null)
                factionPanel.SetActive(true);
            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            achievementsPanel?.Hide();
            leaderboardPanel?.Hide();

            if (factionDetailText != null)
            {
                factionDetailText.text =
                    "IronMarch Union — heavy industry and command.\n" +
                    "Dust Scourge — nomadic scavengers with gas warfare.\n" +
                    "Cartel of Echoes — stealth resonance and synergy.";
            }
        }

        private void StartFaction(string factionId)
        {
            if (!MetaProgressionService.IsFactionUnlocked(factionId))
                return;

            EnsureRunManager();
            RunManager.Instance.StartNewRun(factionId);
            if (cameraDirector != null)
                cameraDirector.LoadRunScene();
            else
                GameScenes.LoadRun();
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowAchievementsPanel()
        {
            cameraDirector?.FocusSubPanel();
            achievementsPanel?.Show();
            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (factionPanel != null)
                factionPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            leaderboardPanel?.Hide();
        }

        private void ShowLeaderboardPanel()
        {
            cameraDirector?.FocusSubPanel();
            leaderboardPanel?.Show();
            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (factionPanel != null)
                factionPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            achievementsPanel?.Hide();
        }

        private void ShowOptionsPanel()
        {
            cameraDirector?.FocusSubPanel();
            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (factionPanel != null)
                factionPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(true);
            achievementsPanel?.Hide();
            leaderboardPanel?.Hide();
        }

        public void ShowMainPanel()
        {
            cameraDirector?.FocusMain();
            if (factionPanel != null)
                factionPanel.SetActive(false);
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(true);
            achievementsPanel?.Hide();
            leaderboardPanel?.Hide();
            Refresh();
        }

        private static void EnsureRunManager()
        {
            if (RunManager.Instance != null)
                return;

            var managerObject = new GameObject(nameof(RunManager));
            managerObject.AddComponent<RunManager>();
        }
    }
}
