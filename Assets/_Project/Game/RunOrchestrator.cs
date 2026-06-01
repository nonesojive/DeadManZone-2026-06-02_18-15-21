using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;

namespace DeadManZone.Game
{
    /// <summary>Pure run flow logic used by RunManager and tests.</summary>
    public sealed class RunOrchestrator
    {
        public const int MaxFights = 5;
        public const int BenchLimit = 3;
        public const int BaseRerollCost = 1;

        private readonly ContentDatabase _content;
        private readonly ContentRegistry _registry;
        private readonly ShopGenerator _shopGenerator;
        private readonly CommandProcessor _commandProcessor = new();

        private PhasedCombatRun _activeCombat;

        public RunState State { get; private set; }
        public FactionSO Faction { get; private set; }

        public RunOrchestrator(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _registry = content.BuildRegistry();
            _shopGenerator = new ShopGenerator(_registry);
        }

        public bool TryLoadSavedRun()
        {
            var loaded = SaveManager.Load();
            if (loaded == null)
                return false;

            State = loaded;
            Faction = _content.GetFaction(State.FactionId);
            RestoreActiveCombatFromSave();
            return Faction != null;
        }

        public void StartNewRun(string factionId, int? runSeed = null)
        {
            Faction = _content.GetFaction(factionId);
            if (Faction == null)
                throw new InvalidOperationException($"Unknown faction '{factionId}'.");

            int seed = runSeed ?? Environment.TickCount;
            State = RunState.CreateNew(factionId, seed, Faction.startingGold, Faction.startingRequisition);
            State.PlayerBoard = Faction.CreateEmptyBoardSnapshot();
            State.RerollCountThisRound = 0;
            RefreshShop();
            _activeCombat = null;
            Persist();
        }

        public BoardState GetPlayerBoard() =>
            State.PlayerBoard == null
                ? new BoardState(Faction.CreateBoardLayout())
                : BoardSnapshotMapper.ToBoard(State.PlayerBoard, _registry);

        public void SavePlayerBoard(BoardState board)
        {
            State.PlayerBoard = BoardSnapshotMapper.FromBoard(board, Faction.rearRows, Faction.supportRows);
            Persist();
        }

        public void BeginCombat()
        {
            if (State.Phase != RunPhase.Build)
                throw new InvalidOperationException("Combat can only start from the build phase.");

            var playerBoard = GetPlayerBoard();
            var enemyTemplate = _content.GetEnemyTemplate(State.FightIndex);
            if (enemyTemplate == null)
                throw new InvalidOperationException($"No enemy template for fight {State.FightIndex}.");

            var enemyBoard = enemyTemplate.BuildBoard(Faction, _registry);
            int combatSeed = State.RunSeed + State.FightIndex * 1000;

            State.Phase = RunPhase.Combat;
            State.Combat = new CombatSaveState
            {
                CombatSeed = combatSeed,
                EnemyBoard = BoardSnapshotMapper.FromBoard(enemyBoard, Faction.rearRows, Faction.supportRows),
                Requisition = State.Requisition,
                SubmittedCommands = new List<PhaseCommand>(),
                EventLog = new List<CombatEventRecord>()
            };

            _activeCombat = PhasedCombatRun.Start(playerBoard, enemyBoard, combatSeed, State.Requisition);
            var firstStep = _activeCombat.Continue(Array.Empty<PhaseCommand>());
            SyncCombatFromRunner(firstStep);
            Persist();
        }

        public IReadOnlyList<AvailableCommand> GetAvailableCommands()
        {
            if (_activeCombat == null || !_activeCombat.AwaitingCommand)
                return Array.Empty<AvailableCommand>();

            return _commandProcessor.GetAvailableCommands(
                GetPlayerBoard(),
                _activeCombat.Requisition,
                _activeCombat.LastCompletedPhase);
        }

        public int GetPrimaryActionBudget()
        {
            int budget = 1;
            if (_commandProcessor.GetBonusActionSlots(GetPlayerBoard()) > 0)
                budget += 1;
            return budget;
        }

        public void SubmitCombatCommand(PhaseCommand command)
        {
            if (_activeCombat == null || !_activeCombat.AwaitingCommand)
                throw new InvalidOperationException("Not awaiting a combat command.");

            State.Combat.SubmittedCommands.Add(command);
            Persist();
        }

        public CombatAdvanceResult AdvanceCombat()
        {
            if (_activeCombat == null)
                throw new InvalidOperationException("No active combat.");

            var commands = State.Combat.SubmittedCommands;
            var result = _activeCombat.Continue(commands);
            SyncCombatFromRunner(result);

            if (result.Status == CombatAdvanceStatus.Completed)
                CompleteCombat(result.PlayerWon);

            Persist();
            return result;
        }

        public bool TryPurchaseOffer(string offerId)
        {
            var offer = State.Shop?.Offers?.FirstOrDefault(o => o.OfferId == offerId);
            if (offer == null)
                return false;

            if (State.Gold < offer.GoldPrice || State.Requisition < offer.RequisitionPrice)
                return false;

            if (State.BenchPieceIds.Count >= BenchLimit)
                return false;

            State.Gold -= offer.GoldPrice;
            State.Requisition -= offer.RequisitionPrice;
            State.BenchPieceIds.Add(offer.PieceId);
            Persist();
            return true;
        }

        public bool TryRerollLane(ShopLane lane)
        {
            int cost = BaseRerollCost + State.RerollCountThisRound;
            if (State.Gold < cost)
                return false;

            State.Gold -= cost;
            State.RerollCountThisRound++;
            RefreshShop();
            Persist();
            return true;
        }

        public void SetFrozenOffer(string offerId)
        {
            State.FrozenOfferId = offerId;
            Persist();
        }

        public void SaveAndExit() => Persist();

        public string GetNextEnemyPreviewTag()
        {
            var next = _content.GetEnemyTemplate(State.FightIndex);
            return next?.previewTag;
        }

        private void RefreshShop()
        {
            var board = GetPlayerBoard();
            int shopSeed = State.RunSeed + State.FightIndex * 100 + State.RerollCountThisRound;
            var shop = _shopGenerator.Generate(board, State.FactionId, State.FightIndex, shopSeed);

            if (!string.IsNullOrEmpty(State.FrozenOfferId) && State.Shop != null)
            {
                var frozen = State.Shop.Offers.FirstOrDefault(o => o.OfferId == State.FrozenOfferId);
                if (frozen != null && shop.Offers.All(o => o.OfferId != frozen.OfferId))
                    shop.Offers.Add(frozen);
            }

            State.Shop = shop;
        }

        private void CompleteCombat(bool playerWon)
        {
            _activeCombat = null;

            if (!playerWon)
            {
                State.Phase = RunPhase.Defeat;
                Persist();
                return;
            }

            var reward = FightRewardTable.GetReward(State.FightIndex);
            State.Gold += reward.Gold;
            State.Requisition += reward.Requisition;
            State.Combat = null;

            if (State.FightIndex >= MaxFights)
            {
                State.Phase = RunPhase.Victory;
                SaveManager.DeleteSave();
                return;
            }

            State.FightIndex++;
            State.Phase = RunPhase.Aftermath;
            State.RerollCountThisRound = 0;
            RefreshShop();
            State.Phase = RunPhase.Build;
            Persist();
        }

        private void SyncCombatFromRunner(CombatAdvanceResult step)
        {
            State.Combat.Requisition = _activeCombat.Requisition;
            State.Combat.CompletedPhase = _activeCombat.LastCompletedPhase;
            State.Combat.AwaitingCommand = step.Status == CombatAdvanceStatus.AwaitingCommand;
            State.Combat.EventLog = _activeCombat.Log.Events
                .Select(e => new CombatEventRecord
                {
                    Phase = e.Phase,
                    Tick = e.Tick,
                    ActorId = e.ActorId,
                    ActionType = e.ActionType,
                    TargetId = e.TargetId,
                    Value = e.Value
                })
                .ToList();
        }

        private void RestoreActiveCombatFromSave()
        {
            _activeCombat = null;
            if (State.Phase != RunPhase.Combat || State.Combat == null)
                return;

            var playerBoard = GetPlayerBoard();
            var enemyBoard = BoardSnapshotMapper.ToBoard(State.Combat.EnemyBoard, _registry);
            _activeCombat = PhasedCombatRun.Start(
                playerBoard,
                enemyBoard,
                State.Combat.CombatSeed,
                State.Combat.Requisition);

            _activeCombat.FastForwardToCheckpoint(
                State.Combat.CompletedPhase,
                State.Combat.SubmittedCommands);
        }

        private void Persist() => SaveManager.Save(State);
    }
}
