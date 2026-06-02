using DeadManZone.Game;
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
        [SerializeField] private TMP_Text subtitleText;

        [Header("Faction select")]
        [SerializeField] private GameObject factionPanel;
        [SerializeField] private Button ironVanguardButton;
        [SerializeField] private Button factionBackButton;

        public GameObject ContinueButtonRoot => continueButton != null ? continueButton.gameObject : null;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (newRunButton != null)
                newRunButton.onClick.AddListener(OnNewRunClicked);

            if (ironVanguardButton != null)
                ironVanguardButton.onClick.AddListener(OnIronVanguardClicked);

            if (factionBackButton != null)
                factionBackButton.onClick.AddListener(ShowMainPanel);
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

            GameScenes.LoadRun();
        }

        private void OnNewRunClicked()
        {
            if (factionPanel != null)
                factionPanel.SetActive(true);
            if (mainPanel != null)
                mainPanel.SetActive(false);
        }

        private void OnIronVanguardClicked()
        {
            EnsureRunManager();
            RunManager.Instance.StartNewRun("iron_vanguard");
            GameScenes.LoadRun();
        }

        private void ShowMainPanel()
        {
            if (factionPanel != null)
                factionPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(true);
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
