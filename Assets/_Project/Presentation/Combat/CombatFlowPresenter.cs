using System.Collections;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat.Arena;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Wires RunManager combat state to CombatDirector and pause UI.</summary>
    public sealed class CombatFlowPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private TacticPausePanel tacticPausePanel;
        [SerializeField] private PhaseCommandPanel phaseCommandPanel;
        [SerializeField] private BattleReportPresenter battleReportPresenter;
        [SerializeField] private CombatLogPresenter combatLogPresenter;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private float loadingDurationSeconds = 1f;
        [SerializeField] private CombatArenaSceneLoader arenaLoader;
        [SerializeField] private CombatArenaPresenter arenaPresenter;
        [SerializeField] private CombatArenaFreezeController freezeController;

        private Coroutine _loadingRoutine;
        private ContentRegistry _contentRegistry;

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponent<CombatDirector>();

            EnsureArenaComponents();
            InitializeContentRegistry();
        }

        private void OnEnable()
        {
            EnsureArenaComponents();

            if (combatDirector != null)
            {
                combatDirector.PausedForCommands += OnPausedForCommands;
                combatDirector.CombatPresentationCompleted += OnCombatPresentationCompleted;
            }

            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;

            if (RunManager.Instance?.State?.Phase == RunPhase.Combat)
                BeginCombatPresentation();
            else if (RunManager.Instance?.State?.Phase == RunPhase.Aftermath)
                ShowBattleReport();
        }

        private void OnDisable()
        {
            if (combatDirector != null)
            {
                combatDirector.PausedForCommands -= OnPausedForCommands;
                combatDirector.CombatPresentationCompleted -= OnCombatPresentationCompleted;
            }

            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;

            if (_loadingRoutine != null)
            {
                StopCoroutine(_loadingRoutine);
                _loadingRoutine = null;
            }
        }

        public void BeginCombatPresentation()
        {
            tacticPausePanel?.Hide();
            battleReportPresenter?.Hide();
            combatLogPresenter?.Hide();
            if (phaseCommandPanel != null)
                phaseCommandPanel.Hide();

            ShowLoadingOverlay();
            _loadingRoutine = StartCoroutine(LoadingThenPresent());
        }

        private IEnumerator LoadingThenPresent()
        {
            freezeController?.Resume();

            if (arenaLoader != null)
                yield return arenaLoader.LoadAsync();

            InitializeArenaFromRunState();

            if (loadingDurationSeconds > 0f)
                yield return new WaitForSeconds(loadingDurationSeconds);
            else
                yield return null;

            HideLoadingOverlay();
            combatDirector?.PresentCombatAfterLoading();
            _loadingRoutine = null;
        }

        private void OnRunStateChanged(RunState state)
        {
            if (state?.Phase == RunPhase.Aftermath)
                ShowBattleReport();
        }

        private void OnCombatPresentationCompleted()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.FinalizePendingCombat();

            ShowBattleReport();

            if (arenaLoader != null)
                StartCoroutine(arenaLoader.UnloadAsync());
        }

        private void ShowBattleReport()
        {
            tacticPausePanel?.Hide();
            combatLogPresenter?.Hide();
            if (phaseCommandPanel != null)
                phaseCommandPanel.Hide();

            battleReportPresenter?.ShowFromRunState();
        }

        private void OnPausedForCommands(CombatPhase completedPhase)
        {
            if (RunManager.Instance == null)
                return;

            HideLoadingOverlay();

            var context = RunManager.Instance.Orchestrator.GetCombatPauseContext();
            if (tacticPausePanel != null)
            {
                tacticPausePanel.ShowPause(context);
                return;
            }

            if (phaseCommandPanel == null)
                return;

            var available = RunManager.Instance.Orchestrator.GetAvailableCommands();
            int budget = RunManager.Instance.Orchestrator.GetPrimaryActionBudget();
            phaseCommandPanel.ShowCommands(available, completedPhase, budget, budget);
        }

        private void ShowLoadingOverlay()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(true);

            if (loadingText != null)
                loadingText.text = "Entering combat…";
        }

        private void HideLoadingOverlay()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        }

        private void EnsureArenaComponents()
        {
            arenaLoader ??= GetComponent<CombatArenaSceneLoader>();
            arenaLoader ??= gameObject.AddComponent<CombatArenaSceneLoader>();

            arenaPresenter ??= GetComponent<CombatArenaPresenter>();
            arenaPresenter ??= gameObject.AddComponent<CombatArenaPresenter>();

            freezeController ??= GetComponent<CombatArenaFreezeController>();
            freezeController ??= gameObject.AddComponent<CombatArenaFreezeController>();

            var arenaVfx = GetComponent<CombatArenaVfx>();
            if (arenaVfx == null)
                arenaVfx = gameObject.AddComponent<CombatArenaVfx>();

            arenaPresenter?.Configure(combatDirector, arenaVfx);
            freezeController?.Configure(combatDirector, arenaPresenter);
            arenaVfx?.Configure(freezeController);
        }

        private void InitializeContentRegistry()
        {
            var database = ContentDatabase.Load();
            if (database != null)
                _contentRegistry = ContentRegistryProvider.Build(database);
        }

        private void InitializeArenaFromRunState()
        {
            if (arenaPresenter == null || RunManager.Instance == null || _contentRegistry == null)
                return;

            var state = RunManager.Instance.State;
            if (state?.Phase != RunPhase.Combat || state.Combat?.EnemyBoard == null)
                return;

            var playerBoard = RunManager.Instance.Orchestrator?.GetPlayerBoard();
            if (playerBoard == null)
                return;

            var enemyBoard = BoardSnapshotMapper.ToBoard(state.Combat.EnemyBoard, _contentRegistry);
            var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            arenaPresenter.InitializeArena(battlefield);
        }
    }
}
