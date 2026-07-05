using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
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
        [SerializeField] private BattleReportPresenter battleReportPresenter;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private float loadingDurationSeconds = 1f;
        [SerializeField] private CombatArenaSceneLoader arenaLoader;
        [SerializeField] private CombatArenaPresenter arenaPresenter;
        [SerializeField] private CombatArenaFreezeController freezeController;
        [SerializeField] private ArmyHealthBarPresenter healthBarPresenter;

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
                combatDirector.EventReplayed += OnCombatEventReplayed;
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
                combatDirector.EventReplayed -= OnCombatEventReplayed;
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

            ShowLoadingOverlay();
            _loadingRoutine = StartCoroutine(LoadingThenPresent());
        }

        private IEnumerator LoadingThenPresent()
        {
            // Hold arena presentation until opening tactics continue or combat playback starts.
            freezeController?.Freeze();

            if (arenaLoader != null)
                yield return arenaLoader.LoadAsync();

            InitializeArenaFromRunState();
            freezeController?.Freeze();

            if (loadingDurationSeconds > 0f)
                yield return new WaitForSeconds(loadingDurationSeconds);
            else
                yield return null;

            HideLoadingOverlay();
            CombatFightBanner.Show(this, RunManager.Instance?.State?.FightIndex ?? 1);
            var combat = RunManager.Instance?.State?.Combat;
            if (IsOpeningTacticsPause(combat))
            {
                freezeController?.Freeze();
                var context = RunManager.Instance.Orchestrator?.GetCombatPauseContext();
                if (tacticPausePanel != null && context != null)
                    tacticPausePanel.ShowPause(context);
            }
            else
            {
                freezeController?.Resume();
                combatDirector?.PresentCombatAfterLoading();
            }

            _loadingRoutine = null;
        }

        private static bool IsOpeningTacticsPause(CombatSaveState combat) =>
            combat is { AwaitingCommand: true, GlobalTick: 0 }
            && (combat.EventLog == null || combat.EventLog.Count == 0);

        private void OnRunStateChanged(RunState state)
        {
            if (state?.Phase == RunPhase.Aftermath)
                ShowBattleReport();
        }

        private void OnCombatPresentationCompleted()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.FinalizePendingCombat();

            StartCoroutine(ShowReportAfterDeathPresentations());
        }

        private IEnumerator ShowReportAfterDeathPresentations()
        {
            // fight_end fires on the same tick as destroyed; let die strips finish
            // before the report covers them and the arena unloads.
            if (arenaPresenter != null)
                yield return arenaPresenter.WaitForPendingDeathPresentations();

            ShowBattleReport();

            if (arenaLoader != null)
                yield return arenaLoader.UnloadAsync();
        }

        private void ShowBattleReport()
        {
            tacticPausePanel?.Hide();
            battleReportPresenter?.ShowFromRunState();
        }

        private void OnPausedForCommands(PauseTriggerContext trigger)
        {
            if (RunManager.Instance == null)
                return;

            HideLoadingOverlay();

            var context = RunManager.Instance.Orchestrator.GetCombatPauseContext();
            if (tacticPausePanel != null)
            {
                tacticPausePanel.ShowPause(context);
            }
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

        private void EnsureHealthBarPresenter()
        {
            if (healthBarPresenter == null || !healthBarPresenter.IsWired)
            {
                var barsRoot = transform.Find("ArmyHealthBars");
                if (barsRoot != null)
                    healthBarPresenter = barsRoot.GetComponent<ArmyHealthBarPresenter>();
            }

            if (healthBarPresenter != null
                && healthBarPresenter.IsWired
                && !CombatHealthBarUiFactory.UsesSyntyBars(healthBarPresenter))
            {
                healthBarPresenter = CombatHealthBarUiFactory.CreateUnder(transform);
            }
            else if (healthBarPresenter == null || !healthBarPresenter.IsWired)
            {
                healthBarPresenter = CombatHealthBarUiFactory.CreateUnder(transform);
            }

            var orphan = GetComponent<ArmyHealthBarPresenter>();
            if (orphan != null && orphan != healthBarPresenter && !orphan.IsWired)
                Destroy(orphan);
        }

        private void EnsureArenaComponents()
        {
            arenaLoader ??= GetComponent<CombatArenaSceneLoader>();
            arenaLoader ??= gameObject.AddComponent<CombatArenaSceneLoader>();

            arenaPresenter ??= GetComponent<CombatArenaPresenter>();
            arenaPresenter ??= gameObject.AddComponent<CombatArenaPresenter>();

            freezeController ??= GetComponent<CombatArenaFreezeController>();
            freezeController ??= gameObject.AddComponent<CombatArenaFreezeController>();

#if UNITY_EDITOR
            if (GetComponent<CombatArenaCameraTuner>() == null)
                gameObject.AddComponent<CombatArenaCameraTuner>();
#endif

            var arenaVfx = GetComponent<CombatArena2DVfx>();
            if (arenaVfx == null)
                arenaVfx = gameObject.AddComponent<CombatArena2DVfx>();

            var arenaAudio = GetComponent<CombatArenaAudioPresenter>();
            if (arenaAudio == null)
                arenaAudio = gameObject.AddComponent<CombatArenaAudioPresenter>();

            EnsureHealthBarPresenter();

            arenaPresenter?.Configure(combatDirector, arenaAudio);
            freezeController?.Configure(combatDirector, arenaPresenter);
            arenaVfx?.Configure(freezeController);
        }

        private void OnCombatEventReplayed(CombatEvent combatEvent)
        {
            if (healthBarPresenter == null || !healthBarPresenter.IsWired)
                EnsureHealthBarPresenter();

            healthBarPresenter?.HandleReplayEvent(combatEvent);
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
            var excludeSegment = state.Combat.AwaitingCommand ? state.Combat.LastSegmentIndex : (int?)null;
            var savedEvents = new List<CombatEvent>(CombatEventMapper.FromRecords(state.Combat.EventLog));

            healthBarPresenter?.InitializeFromBattlefield(battlefield);
            foreach (var savedEvent in savedEvents)
            {
                if (excludeSegment.HasValue && savedEvent.Segment == excludeSegment.Value)
                    continue;

                healthBarPresenter?.ApplyEventStateOnly(savedEvent);
            }

            healthBarPresenter?.SnapBars();
            arenaPresenter.RestoreState(battlefield, savedEvents, excludeSegment);
        }
    }
}
