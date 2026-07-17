using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        // Runtime-built pause/HUD UI for the 3D arena (the orders window IS the pause UI).
        private CombatTacticOrdersWindow _ordersWindow3D;
        private CombatArmyHealthHud _armyHud3D;
        private CombatPlaybackSpeedControl _speedControl;
        private BattlefieldLayout _battlefieldLayout;

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
            _ordersWindow3D?.Hide();
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

            WireArenaSceneComponents();
            InitializeArenaFromRunState();
            freezeController?.Freeze();

            if (loadingDurationSeconds > 0f)
                yield return new WaitForSeconds(loadingDurationSeconds);
            else
                yield return null;

            HideLoadingOverlay();

            // Ceremony order: reveal the field, play the banner, THEN ask for orders.
            CombatFightBanner.Show(this, RunManager.Instance?.State?.FightIndex ?? 1);
            yield return new WaitForSeconds(CombatFightBanner.TotalSeconds + 0.1f);

            var combat = RunManager.Instance?.State?.Combat;
            if (IsOpeningTacticsPause(combat))
            {
                freezeController?.Freeze();
                var context = RunManager.Instance.Orchestrator?.GetCombatPauseContext();
                if (context != null)
                    ShowPauseUi(context);
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

            // Safety net: whenever we're back in the shop/build, the arena must be gone.
            // The normal unload runs after death presentations, but paths that reach
            // aftermath via the state-change event (defeat, resume) skip it, leaving the
            // arena rendered behind the shop. Route through the session, NOT a local
            // StartCoroutine — this object (CombatPanel) is often already inactive here,
            // which threw and silently skipped the unload (2026-07-12 playtest).
            if (state?.Phase == RunPhase.Build && CombatArenaSession.IsSceneLoaded)
                CombatArenaSession.RequestUnload();

            // The 3D army HUD is a top-level overlay canvas (not under CombatPanel like the
            // 2D bars), so the panel's phase-driven SetActive never reaches it — hide it
            // explicitly whenever the shop is the surface. Re-shown by EnsureHealthBarPresenter
            // when the next fight initializes.
            if (state?.Phase == RunPhase.Build && _armyHud3D != null)
                _armyHud3D.gameObject.SetActive(false);

            // Same top-level-overlay reasoning as the army HUD (its OnDisable also
            // guarantees timeScale returns to 1x whenever the shop is the surface).
            if (state?.Phase == RunPhase.Build && _speedControl != null)
                _speedControl.gameObject.SetActive(false);
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
            _ordersWindow3D?.Hide();
            battleReportPresenter?.ShowFromRunState();
        }

        private void OnPausedForCommands(PauseTriggerContext trigger)
        {
            if (RunManager.Instance == null)
                return;

            HideLoadingOverlay();

            var context = RunManager.Instance.Orchestrator.GetCombatPauseContext();
            if (context != null)
                ShowPauseUi(context);
        }

        /// <summary>The interactive orders window is THE combat pause UI
        /// (feeds SubmitCombatCommands + ContinueCombat).</summary>
        private void ShowPauseUi(CombatPauseContext context)
        {
            EnsureOrdersWindow3D();

            var bootstrap = CombatArenaBootstrap.Instance;
            var config = bootstrap != null ? bootstrap.Config : null;
            var mapper = _battlefieldLayout != null && config != null
                ? new CombatGridMapper(_battlefieldLayout, config.cellWidth, config.cellDepth)
                : null;

            var enemyCells = context.EnemyTargetCells;
            var battlefieldLayout = _battlefieldLayout;
            _ordersWindow3D.Show(
                new CombatTacticOrdersWindow.PauseContext
                {
                    CheckpointIndex = context.CheckpointIndex,
                    Authority = context.Authority,
                    ActiveTactic = context.ActiveTactic,
                    HasCommandPiece = context.HasCommandPiece,
                    StartingTactics = context.StartingTactics,
                    AvailableAbilities = context.AvailableAbilities,
                    Trigger = context.Trigger,
                    Mapper = mapper,
                    ArenaCamera = bootstrap != null ? bootstrap.ArenaCamera : null,
                    IsValidAbilityTarget = cell =>
                        enemyCells != null && enemyCells.Contains(cell),
                    PendingSelectedTactic = context.PendingSelectedTactic,
                    PendingSelectedAbilities = context.PendingSelectedAbilities,
                    TransportOrders = context.TransportOrders,
                    // §2.5 Armored Ark: "target a spot on the field" — any in-bounds cell,
                    // not a live-enemy gate like ability targeting.
                    IsValidTransportTargetCell = cell =>
                        battlefieldLayout != null
                        && cell.X >= 0 && cell.X < battlefieldLayout.TotalWidth
                        && cell.Y >= 0 && cell.Y < battlefieldLayout.Height
                },
                commands =>
                {
                    if (RunManager.Instance == null)
                        return;

                    RunManager.Instance.SubmitCombatCommands(commands);
                    combatDirector?.ContinueCombat();
                });
        }

        private void EnsureOrdersWindow3D()
        {
            if (_ordersWindow3D != null)
                return;

            _ordersWindow3D = GetComponent<CombatTacticOrdersWindow>();
            if (_ordersWindow3D == null)
                _ordersWindow3D = gameObject.AddComponent<CombatTacticOrdersWindow>();

            _ordersWindow3D.DraftChanged += SaveOrdersDraft;
        }

        private void SaveOrdersDraft()
        {
            var draft = _ordersWindow3D?.Draft;
            if (draft == null || RunManager.Instance?.Orchestrator == null)
                return;

            RunManager.Instance.Orchestrator.SavePauseDraft(
                draft.SelectedTactic,
                draft.Queued.Select(q => q.Command.Ability).ToList());
        }

        /// <summary>Cross-scene wiring the additive arena scene can't serialize:
        /// director/presenter live here in the Run scene.</summary>
        private void WireArenaSceneComponents()
        {
            var bootstrap = CombatArenaBootstrap.Instance;
            if (bootstrap == null)
                return;

            bootstrap.GetComponent<CombatArenaPunchInCamera>()
                ?.Configure(combatDirector, arenaPresenter);
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
            // The demo's opposing top-bar HUD is the army health UI. The HUD lives on its
            // own child (NO CombatDirector there — its OnEnable self-subscription must not
            // fire, this presenter already forwards events).

            // The Run scene may still serialize the legacy 2D bars object; it never
            // receives events (this presenter feeds the 3D HUD instead) — hide it or
            // it sits full forever.
            var legacyBars = transform.Find("ArmyHealthBars");
            if (legacyBars != null)
                legacyBars.gameObject.SetActive(false);

            if (_armyHud3D == null)
            {
                // Top-level on purpose: the HUD builds its own ScreenSpaceOverlay canvas,
                // and nesting it under the Run UI canvas collapses its rect to zero.
                // Also keeps it off any GameObject carrying a CombatDirector (see
                // CombatArmyHealthHud.EnsurePresenter's double-subscription warning).
                var hudRoot = new GameObject("Combat3DArmyHud");
                _armyHud3D = hudRoot.AddComponent<CombatArmyHealthHud>();
            }

            _armyHud3D.gameObject.SetActive(true); // hidden on return to shop
            healthBarPresenter = _armyHud3D.EnsurePresenter();

            if (_speedControl == null)
            {
                // Top-level for the same rect-inheritance reason as the army HUD.
                var speedRoot = new GameObject("Combat3DSpeedControl");
                _speedControl = speedRoot.AddComponent<CombatPlaybackSpeedControl>();
            }

            _speedControl.Configure(combatDirector);
            _speedControl.gameObject.SetActive(true); // hidden on return to shop
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

            var arenaAudio = GetComponent<CombatArenaAudioPresenter>();
            if (arenaAudio == null)
                arenaAudio = gameObject.AddComponent<CombatArenaAudioPresenter>();

            EnsureHealthBarPresenter();

            arenaPresenter?.Configure(combatDirector, arenaAudio);
            freezeController?.Configure(combatDirector, arenaPresenter);
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
            _battlefieldLayout = battlefield.Layout; // pause UI's grid mapper reads this
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
