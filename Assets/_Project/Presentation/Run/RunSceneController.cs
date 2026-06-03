using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Bench;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunSceneController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject buildPanel;
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private CanvasGroup buildPanelCanvasGroup;

        [Header("Views")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private ShopView shopView;
        [SerializeField] private BenchView benchView;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private PhaseCommandPanel phaseCommandPanel;
        [SerializeField] private RunHudView runHudView;
        [SerializeField] private RunEndOverlayView runEndOverlay;

        [Header("Hub")]
        [SerializeField] private Button beginFightButton;
        [SerializeField] private Button saveAndExitButton;
        [SerializeField] private Button backToMenuButton;

        private void Awake()
        {
            if (beginFightButton != null)
                beginFightButton.onClick.AddListener(OnBeginFight);
            if (saveAndExitButton != null)
                saveAndExitButton.onClick.AddListener(OnSaveAndExit);
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() => GameScenes.LoadMainMenu());
        }

        private void OnEnable()
        {
            EnsureRunManager();
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        private void OnRunStateChanged(RunState state) => RefreshAll();

        private void RefreshAll()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
            {
                runHudView?.Refresh(null);
                runEndOverlay?.Hide();
                if (buildPanel != null)
                    buildPanel.SetActive(true);
                if (combatPanel != null)
                    combatPanel.SetActive(false);
                SetBuildPanelAlpha(1f);
                return;
            }

            var state = RunManager.Instance.State;
            bool inBuild = state.Phase == RunPhase.Build;
            bool inCombat = state.Phase == RunPhase.Combat;
            bool runEnded = state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat;

            if (buildPanel != null)
                buildPanel.SetActive(true);

            if (combatPanel != null)
                combatPanel.SetActive(inCombat || runEnded);

            SetBuildPanelAlpha(inCombat && !runEnded ? 0.4f : 1f);

            if (inBuild && RunManager.Instance?.Orchestrator != null)
            {
                bool canStart = RunManager.Instance.Orchestrator.CanStartBattle(out string failureReason);
                if (beginFightButton != null)
                    beginFightButton.interactable = canStart;
                runHudView?.Refresh(state, failureReason);
            }
            else
            {
                runHudView?.Refresh(state);
            }

            if (runEnded)
            {
                if (phaseCommandPanel != null)
                    phaseCommandPanel.Hide();
                runEndOverlay?.Show(state.Phase);
                return;
            }

            runEndOverlay?.Hide();

            if (inBuild)
            {
                boardView?.RefreshFromRunManager();
                benchView?.Refresh();
                shopView?.RefreshFromRunManager();
                if (phaseCommandPanel != null)
                    phaseCommandPanel.Hide();
            }
        }

        private void SetBuildPanelAlpha(float alpha)
        {
            if (buildPanelCanvasGroup == null)
                return;

            buildPanelCanvasGroup.alpha = alpha;
            buildPanelCanvasGroup.interactable = alpha > 0.9f;
            buildPanelCanvasGroup.blocksRaycasts = alpha > 0.9f;
        }

        private void OnBeginFight()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.BeginCombat();
        }

        private void OnSaveAndExit()
        {
            RunManager.Instance?.SaveAndExit();
            GameScenes.LoadMainMenu();
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
