using System;
using System.Collections.Generic;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
            LoadCriticalMassRules();
        }

        private static void LoadCriticalMassRules()
        {
            var database = CriticalMassDatabaseSO.LoadDefault();
            if (database != null)
            {
                database.RegisterWithCatalog();
                return;
            }

            CriticalMassDefaultRules.RegisterWithCatalog();
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

        public void StartNewRun(string factionId = FactionIds.IronmarchUnion)
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

        public bool TryRerollShop()
        {
            EnsureOrchestrator();
            bool rerolled = _orchestrator.TryRerollShop();
            if (rerolled)
                NotifyStateChanged();
            return rerolled;
        }

        public bool TryRerollLane(ShopLane lane) => TryRerollShop();

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

        public bool TryAcquireOfferToReserves(
            string offerId,
            GridCoord anchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryAcquireOfferToReserves(offerId, anchor, rotation);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryAcquireOfferToBoard(
            string offerId,
            GridCoord anchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryAcquireOfferToBoard(offerId, anchor, rotation: rotation);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryPlaceFromReserves(
            string instanceId,
            GridCoord boardAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryPlaceFromReserves(instanceId, boardAnchor, rotation);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TrySellFromReserves(string instanceId)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TrySellFromReserves(instanceId);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryMovePlacedPiece(
            string instanceId,
            GridCoord newAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryMovePlacedPiece(instanceId, newAnchor, rotation);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool TryMoveBoardToReserves(
            string boardInstanceId,
            GridCoord reservesAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryMoveBoardToReserves(boardInstanceId, reservesAnchor, rotation);
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool CanStartBattle(out string failureReason)
        {
            if (_orchestrator == null)
            {
                failureReason = "No active run.";
                return false;
            }

            return _orchestrator.CanStartBattle(out failureReason);
        }

        public bool TryEmergencyDraft()
        {
            EnsureOrchestrator();
            bool ok = _orchestrator.TryEmergencyDraft();
            if (ok)
                NotifyStateChanged();
            return ok;
        }

        public bool CanChooseFightOption(int index, out string reason)
        {
            if (_orchestrator == null)
            {
                reason = "No active run.";
                return false;
            }

            return _orchestrator.CanChooseOption(index, out reason);
        }

        public void ChooseFightOption(int index)
        {
            EnsureOrchestrator();
            _orchestrator.ChooseFightOption(index);
            NotifyStateChanged();
        }

        public BoardState GetOptionEnemyBoard(int index)
        {
            EnsureOrchestrator();
            return _orchestrator.GetOptionEnemyBoard(index);
        }

        public void BeginCombat()
        {
            EnsureOrchestrator();
            _orchestrator.BeginCombat();
            NotifyStateChanged();
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
                        record.Segment,
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
                SegmentIndex = combat?.LastSegmentIndex ?? 0,
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

        public void FinalizePendingCombat()
        {
            EnsureOrchestrator();
            _orchestrator.FinalizePendingCombat();
            NotifyStateChanged();
        }

        public void DismissAftermath()
        {
            EnsureOrchestrator();
            _orchestrator.DismissAftermath();
            NotifyStateChanged();
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
