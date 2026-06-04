using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;

namespace DeadManZone.Game
{
    /// <summary>Pure run flow logic used by RunManager and tests.</summary>
    public sealed partial class RunOrchestrator
    {
        public const int MaxFights = 10;
        public const int BaseRerollCost = 1;

        private readonly ContentDatabase _content;
        private readonly ContentRegistry _registry;
        private readonly ShopGenerator _shopGenerator;
        private readonly CommandProcessor _commandProcessor = new();

        private TickCombatRun _activeCombat;

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

            if (loaded.SaveSchemaVersion < 3 || loaded.Reserves == null)
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
            State = RunState.CreateNew(
                factionId,
                seed,
                Faction.startingSupplies,
                Faction.startingManpower,
                Faction.startingAuthority,
                Faction.startingMorale);
            State.PlayerBoard = Faction.CreateEmptyBoardSnapshot();
            State.RerollCountThisRound = 0;
            ResetAuthorityForBuildRound();
            RefreshShop();
            _activeCombat = null;
            Persist();
        }

        public BoardState GetPlayerBoard()
        {
            if (Faction == null)
                return new BoardState(BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>()));

            return State.PlayerBoard == null
                ? new BoardState(Faction.CreateBoardLayout())
                : BoardSnapshotMapper.ToBoard(State.PlayerBoard, _registry);
        }

        public void SavePlayerBoard(BoardState board)
        {
            State.PlayerBoard = BoardSnapshotMapper.FromBoard(board, Faction.rearCols, Faction.supportCols);
            Persist();
        }

        public ReservesState GetReserves()
        {
            if (State?.Reserves == null)
                return new ReservesState();

            return ReservesSnapshotMapper.ToReserves(State.Reserves, _registry);
        }

        public void SaveReserves(ReservesState reserves)
        {
            State.Reserves = ReservesSnapshotMapper.FromReserves(reserves);
            Persist();
        }

        public bool CanStartBattle(out string failureReason)
        {
            var playerBoard = GetPlayerBoard();
            int upkeep = ManpowerCalculator.ComputeUpkeep(playerBoard, _registry);
            if (ManpowerCalculator.CanStartBattle(playerBoard, State.Manpower, _registry))
            {
                failureReason = null;
                return true;
            }

            failureReason =
                $"Insufficient manpower: board upkeep is {upkeep} but only {State.Manpower} available.";
            return false;
        }

        public void BeginCombat()
        {
            if (State.Phase != RunPhase.Build)
                throw new InvalidOperationException("Combat can only start from the build phase.");

            if (!CanStartBattle(out string failureReason))
                throw new InvalidOperationException(failureReason);

            var playerBoard = GetPlayerBoard();
            int upkeep = ManpowerCalculator.ComputeUpkeep(playerBoard, _registry);
            State.Manpower -= upkeep;

            var enemyTemplate = _content.GetEnemyTemplate(State.FightIndex);
            if (enemyTemplate == null)
                throw new InvalidOperationException($"No enemy template for fight {State.FightIndex}.");

            var enemyBoard = enemyTemplate.BuildBoard(Faction, _registry);
            int combatSeed = State.RunSeed + State.FightIndex * 1000;

            State.Phase = RunPhase.Combat;
            State.Combat = new CombatSaveState
            {
                CombatSeed = combatSeed,
                EnemyBoard = BoardSnapshotMapper.FromBoard(enemyBoard, Faction.rearCols, Faction.supportCols),
                Requisition = State.Authority,
                Authority = State.Authority,
                SubmittedCommands = new List<PhaseCommand>(),
                EventLog = new List<CombatEventRecord>()
            };

            _activeCombat = TickCombatRun.Start(playerBoard, enemyBoard, combatSeed, State.Authority);
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

        public void SubmitCombatCommands(IReadOnlyList<PhaseCommand> commands)
        {
            if (_activeCombat == null || !_activeCombat.AwaitingCommand)
                throw new InvalidOperationException("Not awaiting a combat command.");

            if (commands == null || commands.Count == 0)
                return;

            foreach (var command in commands)
                State.Combat.SubmittedCommands.Add(command);

            Persist();
        }

        public CombatAdvanceResult AdvanceCombat()
        {
            if (_activeCombat == null)
                throw new InvalidOperationException("No active combat.");

            var pending = State.Combat.SubmittedCommands
                .Where(c => c.AfterPhase == State.Combat.CompletedPhase)
                .ToList();
            var result = _activeCombat.Continue(pending);
            SyncCombatFromRunner(result);

            if (result.Status == CombatAdvanceStatus.Completed)
                CompleteCombat(result);

            Persist();
            return result;
        }

        public bool TryEmergencyDraft()
        {
            var board = GetPlayerBoard();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, _registry);
            int shortfall = upkeep - State.Manpower;
            if (shortfall <= 0)
                return false;

            if (!EmergencyDraft.TryUse(State, shortfall))
                return false;

            Persist();
            return true;
        }

        public bool TryRerollLane(ShopLane lane)
        {
            int cost = BaseRerollCost + State.RerollCountThisRound;
            if (State.Supplies < cost)
                return false;

            State.Supplies -= cost;
            State.RerollCountThisRound++;
            RerollLaneOffers(lane);
            Persist();
            return true;
        }

        public bool TrySellPlacedPiece(string instanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var board = GetPlayerBoard();
            if (!board.TryRemove(instanceId, out var removed))
                return false;

            int refund = removed.Definition.GoldCost / 2;
            State.Supplies += refund;
            SavePlayerBoard(board);
            return true;
        }

        public bool TryMovePlacedPiece(
            string instanceId,
            GridCoord newAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var board = GetPlayerBoard();
            var result = board.TryRelocate(instanceId, newAnchor, rotation);
            if (!result.Success)
                return false;

            SavePlayerBoard(board);
            Persist();
            return true;
        }

        public void SaveAndExit() => Persist();

        public string GetNextEnemyPreviewTag()
        {
            var next = _content.GetEnemyTemplate(State.FightIndex);
            return next?.previewTag;
        }

        private void CompleteCombat(CombatAdvanceResult result)
        {
            _activeCombat = null;
            bool playerWon = result.PlayerWon;
            var playerBoard = GetPlayerBoard();
            State.Manpower += ManpowerCalculator.RefundSurvivors(
                playerBoard,
                result.SurvivingPlayerCombatantIds,
                _registry);

            if (!playerWon)
            {
                int moraleLoss = MoraleCalculator.ComputeLoss(
                    State.FightIndex,
                    result.PlayerCombatantsLost,
                    result.PlayerCombatantsTotal,
                    result.PlayerHqDamaged);
                State.Morale -= moraleLoss;
                State.Combat = null;

                if (State.Morale <= 0)
                {
                    State.Phase = RunPhase.Defeat;
                    Persist();
                    return;
                }

                State.Phase = RunPhase.Build;
                State.RerollCountThisRound = 0;
                ResetAuthorityForBuildRound();
                RefreshShop();
                Persist();
                return;
            }

            var reward = FightRewardTable.GetReward(State.FightIndex);
            State.Supplies += reward.Supplies;
            State.Manpower += reward.BonusManpower;
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
            ResetAuthorityForBuildRound();
            RefreshShop();
            State.Phase = RunPhase.Build;
            Persist();
        }

        private void ResetAuthorityForBuildRound()
        {
            State.Authority = AuthorityCalculator.ComputeRoundPool(GetPlayerBoard());
        }

        private void SyncCombatFromRunner(CombatAdvanceResult step)
        {
            State.Combat.Requisition = _activeCombat.Requisition;
            State.Combat.Authority = _activeCombat.Authority;
            State.Combat.ActiveSegment = (int)_activeCombat.ActiveSegment;
            State.Combat.SegmentTick = _activeCombat.SegmentTick;
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
            _activeCombat = TickCombatRun.Start(
                playerBoard,
                enemyBoard,
                State.Combat.CombatSeed,
                State.Combat.Authority > 0 ? State.Combat.Authority : State.Combat.Requisition);

            _activeCombat.FastForwardToCheckpoint(
                State.Combat.CompletedPhase,
                State.Combat.SubmittedCommands);
        }

        private void Persist() => SaveManager.Save(State);
    }
}
