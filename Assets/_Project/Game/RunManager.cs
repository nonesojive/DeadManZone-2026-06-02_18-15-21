using System;
using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
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

        public bool TrySellPlacedPiece(string instanceId)
        {
            EnsureOrchestrator();
            bool sold = _orchestrator.TrySellPlacedPiece(instanceId);
            if (sold)
                NotifyStateChanged();
            return sold;
        }

        public bool TryPurchaseOffer(string offerId)
        {
            EnsureOrchestrator();
            bool purchased = _orchestrator.TryPurchaseOffer(offerId);
            if (purchased)
                NotifyStateChanged();
            return purchased;
        }

        public bool TryRerollLane(ShopLane lane)
        {
            EnsureOrchestrator();
            bool rerolled = _orchestrator.TryRerollLane(lane);
            if (rerolled)
                NotifyStateChanged();
            return rerolled;
        }

        public void SetFrozenOffer(string offerId)
        {
            EnsureOrchestrator();
            _orchestrator.SetFrozenOffer(offerId);
            NotifyStateChanged();
        }

        public void SetLockedOffer(ShopOffer offer, bool locked)
        {
            EnsureOrchestrator();
            _orchestrator.SetLockedOffer(offer, locked);
            NotifyStateChanged();
        }

        public bool TryAcquireOfferToBench(string offerId)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryAcquireOfferToBench(offerId);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryAcquireOfferToBoard(string offerId, Core.Common.GridCoord anchor)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryAcquireOfferToBoard(offerId, anchor);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryPlaceFromBench(int benchIndex, Core.Common.GridCoord anchor)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryPlaceFromBench(benchIndex, anchor);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TrySellFromBench(int benchIndex)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TrySellFromBench(benchIndex);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryMovePlacedPiece(string instanceId, Core.Common.GridCoord newAnchor)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryMovePlacedPiece(instanceId, newAnchor);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryMoveBoardToBench(string instanceId, int benchIndex)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryMoveBoardToBench(instanceId, benchIndex);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public void BeginCombat()
        {
            EnsureOrchestrator();
            _orchestrator.BeginCombat();
            NotifyStateChanged();

            if (_orchestrator.State.Phase == RunPhase.Combat)
                CombatAdvanced?.Invoke(BuildCombatAdvanceSnapshot());
        }

        private CombatAdvanceResult BuildCombatAdvanceSnapshot()
        {
            var combat = _orchestrator.State.Combat;
            var log = new CombatEventLog();
            if (combat?.EventLog != null)
            {
                foreach (var record in combat.EventLog)
                {
                    log.Append(
                        record.Phase,
                        record.Tick,
                        record.ActorId,
                        record.ActionType,
                        record.TargetId,
                        record.Value);
                }
            }

            return new CombatAdvanceResult
            {
                Status = combat is { AwaitingCommand: true }
                    ? CombatAdvanceStatus.AwaitingCommand
                    : CombatAdvanceStatus.Completed,
                CompletedPhase = combat?.CompletedPhase ?? default,
                EventLog = log
            };
        }

        public void SubmitCombatCommand(PhaseCommand command)
        {
            EnsureOrchestrator();
            _orchestrator.SubmitCombatCommand(command);
            NotifyStateChanged();
        }

        public void SubmitCombatCommands(IReadOnlyList<PhaseCommand> commands)
        {
            EnsureOrchestrator();
            _orchestrator.SubmitCombatCommands(commands);
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
