using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game.Dev;

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
        private CombatAdvanceResult _pendingCombatCompletion;

        public RunState State { get; private set; }
        public FactionSO Faction { get; private set; }

        public RunOrchestrator(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _registry = ContentRegistryProvider.Build(content);
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
            PlaceStartingHq();
            ApplyMuster();
            ResetAuthorityForBuildRound();
            ResetMetaForNewRun();
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
                $"Insufficient manpower: fielding requires {upkeep} but only {State.Manpower} available.";
            return false;
        }

        public void BeginCombat()
        {
            if (State.Phase != RunPhase.Build)
                throw new InvalidOperationException("Combat can only start from the build phase.");

            if (!CanStartBattle(out string failureReason))
                throw new InvalidOperationException(failureReason);

            var playerBoard = GetPlayerBoard();
            RecordCriticalMassIfTriggered();

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
            State.Combat.AwaitingCommand = false;
            State.Combat.CompletedPhase = default;
            Persist();
        }

        public bool HasPendingCombatCompletion => _pendingCombatCompletion != null;

        public void FinalizePendingCombat()
        {
            if (_pendingCombatCompletion == null)
                return;

            CompleteCombat(_pendingCombatCompletion);
            _pendingCombatCompletion = null;
        }

        public void DismissAftermath()
        {
            if (State.Phase != RunPhase.Aftermath)
                return;

            State.Phase = RunPhase.Build;
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

        public CombatPauseContext GetCombatPauseContext()
        {
            if (_activeCombat == null || State?.Combat == null || !_activeCombat.AwaitingCommand)
                return null;

            var board = GetPlayerBoard();
            var abilities = GetAvailableCommands()
                .Where(c => c.Type == CommandType.UseAbility)
                .ToList();

            return new CombatPauseContext
            {
                CompletedPhase = _activeCombat.LastCompletedPhase,
                Authority = _activeCombat.Requisition,
                ActiveTactic = _activeCombat.PlayerTactic,
                HqAlive = _activeCombat.IsPlayerHqAlive,
                HasCommandPiece = board.Pieces.Any(p =>
                    PieceTagQueries.HasTag(p.Definition, GameTagIds.Command)),
                AvailableAbilities = abilities,
                PendingSelectedTactic = State.Combat.PendingSelectedTactic,
                PendingSelectedAbilities = State.Combat.PendingSelectedAbilities
            };
        }

        public void SavePauseDraft(TacticType selectedTactic, IReadOnlyList<GrantedAbility> abilities)
        {
            if (State?.Combat == null || _activeCombat == null || !_activeCombat.AwaitingCommand)
                return;

            State.Combat.PendingSelectedTactic = selectedTactic;
            State.Combat.PendingSelectedAbilities = abilities?.ToList() ?? new List<GrantedAbility>();
            Persist();
        }

        public void ClearPauseDraft()
        {
            if (State?.Combat == null)
                return;

            State.Combat.PendingSelectedTactic = null;
            State.Combat.PendingSelectedAbilities = new List<GrantedAbility>();
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

            ClearPauseDraft();
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
                _pendingCombatCompletion = result;

            Persist();
            return result;
        }

        public bool TryEmergencyDraft() => false;

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

            ApplySalvageRefund(removed.Definition);
            SavePlayerBoard(board);
            return true;
        }

        private void ApplySalvageRefund(PieceDefinition piece)
        {
            var refund = SalvageCalculator.Compute(piece, State.FactionId);
            State.Supplies += refund.Supplies;
            State.Authority += refund.Authority;
            State.Manpower += refund.Manpower;
            RecordSalvageMeta(refund.Supplies);
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
            State.LastCombatLogText = CombatLogFormatter.FormatAll(result.EventLog?.Events);
            _activeCombat = null;
            bool playerWon = result.PlayerWon;
            bool isDraw = result.IsDraw;
            var playerCombatants = result.PlayerCombatantsAtEnd ?? Array.Empty<CombatantState>();
            int casualties = ManpowerCalculator.ComputeCasualties(playerCombatants);
            State.Manpower = Math.Max(0, State.Manpower - casualties);

            if (!playerWon)
            {
                int moraleLoss = MoraleCalculator.ComputeLoss(
                    State.FightIndex,
                    result.PlayerCombatantsLost,
                    result.PlayerCombatantsTotal,
                    result.PlayerHqDamaged);
                State.Morale -= moraleLoss;
                ProcessFightEndMeta(playerWon: false, result.PlayerHqDamaged);
                State.LastBattleReport = BattleReportBuilder.Build(
                    System.Array.Empty<CombatantState>(),
                    playerWon,
                    isDraw,
                    casualties,
                    suppliesEarned: 0,
                    moraleDelta: -moraleLoss);
                State.Combat = null;

                if (State.Morale <= 0)
                {
                    State.Phase = RunPhase.Defeat;
                    ProcessRunEndMeta(victory: false);
                    Persist();
                    return;
                }

                State.Phase = RunPhase.Aftermath;
                State.RerollCountThisRound = 0;
                ResetAuthorityForBuildRound();
                ApplyMuster();
                RefreshShop();
                Persist();
                return;
            }

            var reward = FightRewardTable.GetReward(State.FightIndex, isDraw);
            State.Supplies += reward.Supplies;
            State.Authority += reward.BonusAuthority;
            ProcessFightEndMeta(playerWon: true, result.PlayerHqDamaged);
            State.LastBattleReport = BattleReportBuilder.Build(
                System.Array.Empty<CombatantState>(),
                playerWon,
                isDraw,
                casualties,
                reward.Supplies,
                moraleDelta: 0);
            if (result.BattleReport != null)
            {
                State.LastBattleReport = new BattleReport
                {
                    PlayerWon = playerWon,
                    IsDraw = isDraw,
                    ManpowerCasualties = casualties,
                    SuppliesEarned = reward.Supplies,
                    MoraleDelta = 0,
                    TopDamageDealt = result.BattleReport.TopDamageDealt,
                    TopDamageTaken = result.BattleReport.TopDamageTaken
                };
            }

            State.Combat = null;

            if (State.FightIndex >= MaxFights)
            {
                State.Phase = RunPhase.Victory;
                ProcessRunEndMeta(victory: true);
                SaveManager.DeleteSave();
                return;
            }

            State.FightIndex++;
            State.Phase = RunPhase.Aftermath;
            State.RerollCountThisRound = 0;
            ResetAuthorityForBuildRound();
            ApplyMuster();
            RefreshShop();
            Persist();
        }

        private void PlaceStartingHq()
        {
            var board = GetPlayerBoard();
            var hq = _registry.GetById(Faction.hqPieceId);
            if (hq == null)
                throw new InvalidOperationException($"HQ piece '{Faction.hqPieceId}' not found.");

            var anchor = new GridCoord(Faction.hqSpawnAnchor.x, Faction.hqSpawnAnchor.y);
            var rotation = (PieceRotation)Faction.hqSpawnRotation;
            var result = board.TryPlace(hq, anchor, instanceId: "hq_player", rotation);
            if (!result.Success)
                throw new InvalidOperationException($"Failed to spawn HQ: {result.Reason}");

            SavePlayerBoard(board);
        }

        private void ResetAuthorityForBuildRound()
        {
            State.Authority = AuthorityCalculator.ComputeRoundPool(GetPlayerBoard());
        }

        private void SyncCombatFromRunner(CombatAdvanceResult step)
        {
            State.Combat.Requisition = _activeCombat.Requisition;
            State.Combat.Authority = _activeCombat.Authority;
            State.Combat.PlayerTactic = _activeCombat.PlayerTactic;
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

            if (State.Combat.PlayerTactic != default)
                _activeCombat.SetPlayerTactic(State.Combat.PlayerTactic);
        }

        private void Persist() => SaveManager.Save(State);
    }
}
