using System;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Game
{
    public sealed class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] private ContentDatabase contentDatabase;

        private RunOrchestrator _orchestrator;

        public RunOrchestrator Orchestrator => _orchestrator;
        public RunState State => _orchestrator?.State;
        public bool HasActiveRun => _orchestrator?.State != null;

        public event Action<RunState> RunStateChanged;
        public event Action<CombatAdvanceResult> CombatAdvanced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureBootstrap();
            InitializeOrchestrator();
        }

        private void OnEnable()
        {
            RunSaveBootstrap.GetActiveRunState = () => _orchestrator?.State;
        }

        private void OnDisable()
        {
            RunSaveBootstrap.GetActiveRunState = null;
        }

        public void InitializeOrchestrator()
        {
            var database = contentDatabase != null ? contentDatabase : ContentDatabase.Load();
            if (database == null)
            {
                Debug.LogError("RunManager: ContentDatabase not assigned and not found in Resources.");
                return;
            }

            _orchestrator = new RunOrchestrator(database);
        }

        public bool TryContinueRun()
        {
            if (_orchestrator == null)
                return false;

            bool loaded = _orchestrator.TryLoadSavedRun();
            if (loaded)
                NotifyStateChanged();

            return loaded;
        }

        public void StartNewRun(string factionId = "iron_vanguard")
        {
            EnsureOrchestrator();
            _orchestrator.StartNewRun(factionId);
            NotifyStateChanged();
        }

        public void SaveAndExit()
        {
            _orchestrator?.SaveAndExit();
        }

        public void BeginCombat()
        {
            EnsureOrchestrator();
            _orchestrator.BeginCombat();
            NotifyStateChanged();
        }

        public void SubmitCombatCommand(PhaseCommand command)
        {
            _orchestrator.SubmitCombatCommand(command);
            NotifyStateChanged();
        }

        public CombatAdvanceResult AdvanceCombat()
        {
            var result = _orchestrator.AdvanceCombat();
            NotifyStateChanged();
            CombatAdvanced?.Invoke(result);
            return result;
        }

        private static void EnsureBootstrap()
        {
            if (RunSaveBootstrap.Instance != null)
                return;

            var bootstrapObject = new GameObject(nameof(RunSaveBootstrap));
            bootstrapObject.AddComponent<RunSaveBootstrap>();
        }

        private void EnsureOrchestrator()
        {
            if (_orchestrator != null)
                return;

            InitializeOrchestrator();
            if (_orchestrator == null)
                throw new InvalidOperationException("RunManager failed to initialize.");
        }

        private void NotifyStateChanged() => RunStateChanged?.Invoke(_orchestrator.State);
    }
}
