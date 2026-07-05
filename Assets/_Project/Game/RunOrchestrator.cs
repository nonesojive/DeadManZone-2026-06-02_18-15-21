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
        private BoardState _fightStartCombatBoard;

        public RunState State { get; private set; }
        public FactionSO Faction { get; private set; }

        public RunOrchestrator(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _registry = ContentRegistryProvider.Build(content);
            _shopGenerator = new ShopGenerator(_registry, content.BuildShopConfig());
        }

        public bool TryLoadSavedRun()
        {
            var loaded = SaveManager.Load();
            if (loaded == null)
                return false;

            if (loaded.SaveSchemaVersion < 8 || loaded.Reserves == null)
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
            State.CombatBoard = Faction.CreateEmptyCombatBoardSnapshot();
            State.HqBoard = Faction.CreateEmptyHqBoardSnapshot();
            State.RerollCountThisRound = 0;
            ApplyMuster();
            ResetAuthorityForBuildRound();
            ResetMetaForNewRun();
            RefreshShop();
            _activeCombat = null;
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
            var combatBoard = GetCombatBoard();
            int upkeep = ManpowerCalculator.ComputeUpkeep(combatBoard, _registry);
            if (ManpowerCalculator.CanStartBattle(combatBoard, State.Manpower, _registry))
            {
                failureReason = null;
                return true;
            }

            failureReason =
                $"Insufficient manpower: fielding requires {upkeep} but only {State.Manpower} available.";
            return false;
        }

        public BoardState GetUpcomingEnemyBoard()
        {
            var enemyTemplate = _content.GetEnemyTemplate(State.FightIndex);
            if (enemyTemplate == null)
                return null;
            return enemyTemplate.BuildBoard(Faction, _registry);
        }

        public void BeginCombat()
        {
            if (State.Phase != RunPhase.Build)
                throw new InvalidOperationException("Combat can only start from the build phase.");

            if (!CanStartBattle(out string failureReason))
                throw new InvalidOperationException(failureReason);

            var playerBoard = GetCombatBoard();
            _fightStartCombatBoard = playerBoard;
            RecordCriticalMassIfTriggered();

            var enemyTemplate = _content.GetEnemyTemplate(State.FightIndex);
            if (enemyTemplate == null)
                throw new InvalidOperationException($"No enemy template for fight {State.FightIndex}.");

            var enemyBoard = enemyTemplate.BuildBoard(Faction, _registry);
            ResetAuthorityForBuildRound();
            var buildBoards = GetBuildBoards();
            var criticalMassSnapshot = CriticalMassEngine.Evaluate(buildBoards);
            if (criticalMassSnapshot.AuthorityBonus > 0)
                State.Authority += criticalMassSnapshot.AuthorityBonus;

            int combatSeed = State.RunSeed + State.FightIndex * 1000;
            var defaultTactic = ResolveDefaultPlayerTactic(Faction);

            State.Phase = RunPhase.Combat;
            State.Combat = new CombatSaveState
            {
                CombatSeed = combatSeed,
                EnemyBoard = BoardSnapshotMapper.FromBoard(enemyBoard),
                Requisition = State.Authority,
                Authority = State.Authority,
                PlayerTactic = defaultTactic,
                SubmittedCommands = new List<PhaseCommand>(),
                EventLog = new List<CombatEventRecord>()
            };

            _activeCombat = TickCombatRun.Start(
                playerBoard,
                enemyBoard,
                combatSeed,
                State.Authority,
                buildBoards);
            _activeCombat.SetPlayerTactic(defaultTactic);
            State.Combat.AwaitingCommand = _activeCombat.AwaitingCommand;
            State.Combat.CheckpointsFired = _activeCombat.CheckpointsFired;
            State.Combat.GlobalTick = 0;
            State.Combat.LastSegmentIndex = 0;
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
                GetCombatBoard(),
                _activeCombat.Requisition,
                _activeCombat.CurrentPauseIndex);
        }

        public int GetPrimaryActionBudget()
        {
            int budget = 1;
            if (_commandProcessor.GetBonusActionSlots(GetBuildBoards().ToAggregateBoard()) > 0)
                budget += 1;
            return budget;
        }

        public CombatPauseContext GetCombatPauseContext()
        {
            if (_activeCombat == null || State?.Combat == null || !_activeCombat.AwaitingCommand)
                return null;

            var board = GetBuildBoards().ToAggregateBoard();
            var abilities = GetAvailableCommands()
                .Where(c => c.Type == CommandType.UseAbility)
                .ToList();

            return new CombatPauseContext
            {
                CheckpointIndex = _activeCombat.CurrentPauseIndex,
                Trigger = _activeCombat.LastPauseTrigger,
                Authority = _activeCombat.Requisition,
                ActiveTactic = _activeCombat.PlayerTactic,
                HasCommandPiece = board.Pieces.Any(p =>
                    p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance)),
                AvailableAbilities = abilities,
                PendingSelectedTactic = State.Combat.PendingSelectedTactic,
                PendingSelectedAbilities = State.Combat.PendingSelectedAbilities,
                StartingTactics = Faction?.startingTactics
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

            int pauseIndex = _activeCombat.CurrentPauseIndex;
            var pending = State.Combat.SubmittedCommands
                .Where(c => c.AfterCheckpoint == pauseIndex)
                .ToList();
            var result = _activeCombat.Continue(pending);
            SyncCombatFromRunner(result);

            if (result.Status == CombatAdvanceStatus.Completed)
                _pendingCombatCompletion = result;

            Persist();
            return result;
        }

        public bool TryEmergencyDraft()
        {
            int shortfall = ComputeManpowerShortfallForNextFight();
            if (!EmergencyDraft.TryUse(State, shortfall))
                return false;

            Persist();
            return true;
        }

        private int ComputeManpowerShortfallForNextFight()
        {
            var board = GetCombatBoard();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, _registry);
            return Math.Max(0, upkeep - State.Manpower);
        }

        public bool TryRerollShop()
        {
            if (!CanRerollShop())
                return false;

            int goldCost = BaseRerollCost + State.RerollCountThisRound;
            int authorityCost = ComputeRerollLockAuthorityCost();

            State.Supplies -= goldCost;
            State.Authority -= authorityCost;
            State.RerollCountThisRound++;
            RerollShopOffers();
            Persist();
            return true;
        }

        public bool TryRerollLane(ShopLane lane) => TryRerollShop();

        public bool TrySellPlacedPiece(string instanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!TryFindPlacedPiece(instanceId, out var board, out var removed)
                || !board.TryRemove(instanceId, out removed))
                return false;

            ApplySalvageRefund(removed.Definition);
            SaveBoardForPiece(removed.Definition, board);
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

            if (!TryFindPlacedPiece(instanceId, out var board, out _))
                return false;

            var result = board.TryRelocate(instanceId, newAnchor, rotation);
            if (!result.Success)
                return false;

            var piece = board.Pieces.First(p => p.InstanceId == instanceId);
            SaveBoardForPiece(piece.Definition, board);
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
            int suppliesIncome = ApplyPostCombatIncome();

            if (!playerWon)
            {
                int moraleLoss = MoraleCalculator.ComputeLoss(
                    State.FightIndex,
                    result.PlayerCombatantsLost,
                    result.PlayerCombatantsTotal);
                State.Morale -= moraleLoss;
                ProcessFightEndMeta();
                State.LastBattleReport = BattleReportBuilder.Build(
                    System.Array.Empty<CombatantState>(),
                    playerWon,
                    isDraw,
                    casualties,
                    suppliesIncome,
                    moraleDelta: -moraleLoss);
                // The sim's report carries the real damage tables; without this the
                // defeat card always showed empty dealt/taken columns.
                if (result.BattleReport != null)
                {
                    State.LastBattleReport = new BattleReport
                    {
                        PlayerWon = playerWon,
                        IsDraw = isDraw,
                        ManpowerCasualties = casualties,
                        SuppliesEarned = suppliesIncome,
                        MoraleDelta = -moraleLoss,
                        TopDamageDealt = result.BattleReport.TopDamageDealt,
                        TopDamageTaken = result.BattleReport.TopDamageTaken
                    };
                }
                ApplySalvageAftermath();
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
                RefreshShop();
                Persist();
                return;
            }

            ProcessFightEndMeta();
            State.LastBattleReport = BattleReportBuilder.Build(
                System.Array.Empty<CombatantState>(),
                playerWon,
                isDraw,
                casualties,
                suppliesIncome,
                moraleDelta: 0);
            if (result.BattleReport != null)
            {
                State.LastBattleReport = new BattleReport
                {
                    PlayerWon = playerWon,
                    IsDraw = isDraw,
                    ManpowerCasualties = casualties,
                    SuppliesEarned = suppliesIncome,
                    MoraleDelta = 0,
                    TopDamageDealt = result.BattleReport.TopDamageDealt,
                    TopDamageTaken = result.BattleReport.TopDamageTaken
                };
            }

            ApplySalvageAftermath();
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
            RefreshShop();
            Persist();
        }

        private void ApplySalvageAftermath()
        {
            var enemyTemplate = _content.GetEnemyTemplate(State.FightIndex);
            if (enemyTemplate == null)
                return;

            State.LastEnemyFactionId = enemyTemplate.enemyFactionId;
            SyncSalvageChancePercent();
        }

        private void ResetAuthorityForBuildRound()
        {
            State.Authority = AuthorityCalculator.ComputeRoundPool(GetBuildBoards());
        }

        private void SyncCombatFromRunner(CombatAdvanceResult step)
        {
            State.Combat.Requisition = _activeCombat.Requisition;
            State.Combat.Authority = _activeCombat.Authority;
            State.Combat.PlayerTactic = _activeCombat.PlayerTactic;
            State.Combat.CheckpointsFired = _activeCombat.CheckpointsFired;
            State.Combat.GlobalTick = _activeCombat.GlobalTick;
            State.Combat.LastSegmentIndex = step.SegmentIndex;
            State.Combat.AwaitingCommand = step.Status == CombatAdvanceStatus.AwaitingCommand;
            State.Combat.EventLog = _activeCombat.Log.Events
                .Select(e => new CombatEventRecord
                {
                    Segment = e.Segment,
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
            _pendingCombatCompletion = null;
            if (State.Phase != RunPhase.Combat || State.Combat == null)
                return;

            if (State.SaveSchemaVersion < 5 && State.Phase == RunPhase.Combat)
            {
                State.Combat.SubmittedCommands = new List<PhaseCommand>();
                State.Combat.EventLog = new List<CombatEventRecord>();
                State.Combat.CheckpointsFired = 0;
                State.Combat.GlobalTick = 0;
                State.Combat.LastSegmentIndex = 0;
                State.Combat.AwaitingCommand = false;
            }

            var playerBoard = GetCombatBoard();
            var enemyBoard = BoardSnapshotMapper.ToBoard(State.Combat.EnemyBoard, _registry);
            var buildBoards = GetBuildBoards();
            _activeCombat = TickCombatRun.Start(
                playerBoard,
                enemyBoard,
                State.Combat.CombatSeed,
                State.Combat.Authority > 0 ? State.Combat.Authority : State.Combat.Requisition,
                buildBoards);

            var playerTactic = State.Combat.PlayerTactic;
            if (!TacticUnlockRules.IsUnlocked(Faction, playerTactic))
                playerTactic = ResolveDefaultPlayerTactic(Faction);

            if (playerTactic != default)
            {
                _activeCombat.SetPlayerTactic(playerTactic);
                State.Combat.PlayerTactic = playerTactic;
            }

            _activeCombat.FastForwardFromSave(
                State.Combat.CheckpointsFired,
                State.Combat.AwaitingCommand,
                State.Combat.SubmittedCommands);

            _pendingCombatCompletion = _activeCombat.BuildCompletionResultIfOver();
        }

        private static TacticType ResolveDefaultPlayerTactic(FactionSO faction)
        {
            const TacticType preferred = TacticType.DisciplinedFire;
            if (TacticUnlockRules.IsUnlocked(faction, preferred))
                return preferred;

            if (faction?.startingTactics != null && faction.startingTactics.Length > 0)
                return faction.startingTactics[0];

            return preferred;
        }

        private void Persist() => SaveManager.Save(State);
    }
}
