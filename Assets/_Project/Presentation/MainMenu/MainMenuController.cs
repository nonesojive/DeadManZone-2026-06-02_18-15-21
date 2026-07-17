using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat;
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
        [SerializeField] private FactionSelectView factionSelectView;

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
            factionSelectView?.SetConfirmHandler(StartFaction);
            factionSelectView?.SetBackHandler(ShowMainPanel);

            achievementsPanel?.SetBackHandler(ShowMainPanel);
            leaderboardPanel?.SetBackHandler(ShowMainPanel);

            ApplyGrimdarkSkin();
        }

        /// <summary>M6: runtime grimdark-kit pass over the scene-authored menu (same
        /// pattern as BattleReportPresenter.Awake). Colors/typography only — every
        /// anchor and flow stays authored. MenuOptionsPanel re-skins its own sliders
        /// in its Awake, which runs on first panel activation (after this pass).</summary>
        private void ApplyGrimdarkSkin()
        {
            // Sub-panels: flatten frames to the smoky card surface, leather the buttons.
            // factionPanel is NOT included here — FactionSelectView.ApplyGrimdarkSkin does its
            // own targeted pass; a blanket StyleCard/StylePanelText would null out the crest and
            // roster-icon sprites it manages (see that method's doc comment).
            CombatGrimdarkSkin.StyleCard(optionsPanel);
            CombatGrimdarkSkin.StylePanelText(optionsPanel);

            // Main panel: title band + bone lettering, leather buttons.
            if (mainPanel != null)
            {
                CombatGrimdarkSkin.StylePanelText(mainPanel);
                CombatGrimdarkSkin.AddBand(mainPanel.transform, 0.73f, 0.83f, "TitleBand");
            }
            CombatGrimdarkSkin.StyleBody(subtitleText);

            CombatGrimdarkSkin.StyleButton(continueButton);
            CombatGrimdarkSkin.StyleButton(newRunButton);
            CombatGrimdarkSkin.StyleButton(achievementsButton);
            CombatGrimdarkSkin.StyleButton(leaderboardButton);
            CombatGrimdarkSkin.StyleButton(optionsButton);
            CombatGrimdarkSkin.StyleButton(exitButton);

            // Primary CTAs keep the brass accent on their labels.
            AccentLabel(continueButton);
            AccentLabel(newRunButton);
        }

        private static void AccentLabel(Button button)
        {
            var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
                label.color = CombatGrimdarkSkin.VictoryGold;
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

            // factionPanel.SetActive(true) above triggers FactionSelectView.OnEnable, which
            // Show()s itself — see that class's OnEnable doc comment. No explicit call needed.
        }

        /// <summary>Wired as FactionSelectView's confirm handler (MARCH button). The view
        /// already gates on FactionSelectView's own unlock seam before invoking this — see
        /// that class's IsFactionUnlocked doc comment.</summary>
        private void StartFaction(string factionId)
        {
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
