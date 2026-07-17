using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
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
        /// <summary>Authored normal-fight template count. Since M1 this is NOT the run
        /// length — the Dread clock (ADR-0004) decides when the run ends; difficulty
        /// clamps to this last authored template.</summary>
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
            // CartelMercenarySlotProvider is always registered but is a no-op unless the
            // run's FactionId is Cartel (FactionPassives.HasMercenarySlot) — the same
            // "always present, gated by faction" shape as every other passive this wave.
            var unlockRegistry = new ShopSlotUnlockRegistry(new IShopSlotUnlockProvider[]
            {
                new CartelMercenarySlotProvider()
            });
            _shopGenerator = new ShopGenerator(_registry, content.BuildShopConfig(), unlockRegistry);
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
                Faction.startingAuthority);
            State.CombatBoard = Faction.CreateEmptyCombatBoardSnapshot();
            State.HqBoard = Faction.CreateEmptyHqBoardSnapshot();
            ApplyStartingLoadout();
            State.RerollCountThisRound = 0;
            // Muster runs AFTER the loadout on purpose: a starting supply depot feeds the
            // first muster — the faction's economic identity shows from round one.
            ApplyMuster();
            ResetAuthorityForBuildRound();
            ResetMetaForNewRun();
            RefreshShop();
            GenerateFightOptions();
            _activeCombat = null;
            Persist();
        }

        /// <summary>Pre-places the faction's starting pieces (FactionSO.startingPieces) on
        /// their category-resolved boards. Free — no supplies/authority; upkeep applies as
        /// usual. Authored anchors are preferences: if occupied/illegal, placement scans
        /// forward for the first legal cell; a piece with no legal cell logs and is skipped
        /// (never blocks the run).</summary>
        private void ApplyStartingLoadout()
        {
            var entries = Faction.startingPieces;
            if (entries == null || entries.Length == 0)
                return;

            var combat = GetCombatBoard();
            var hq = GetHqBoard();

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.pieceId) ||
                    !_registry.TryGetById(entry.pieceId, out var piece))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[StartingLoadout] Unknown starting piece '{entry?.pieceId}' on {Faction.factionId} — skipped.");
                    continue;
                }

                var board = BoardPlacementRules.ResolveTargetBoard(piece) == BoardKind.Hq ? hq : combat;
                string instanceId = $"start_{entry.pieceId}";
                var preferred = new GridCoord(entry.anchor.x, entry.anchor.y);

                if (board.TryPlace(piece, preferred, instanceId).Success)
                    continue;

                bool placed = false;
                for (int y = 0; y < board.Layout.Height && !placed; y++)
                    for (int x = 0; x < board.Layout.Width && !placed; x++)
                        placed = board.TryPlace(piece, new GridCoord(x, y), instanceId).Success;

                if (!placed)
                    UnityEngine.Debug.LogWarning(
                        $"[StartingLoadout] No legal cell for '{entry.pieceId}' on {Faction.factionId} — skipped.");
            }

            State.CombatBoard = BoardSnapshotMapper.FromBoard(combat);
            State.HqBoard = BoardSnapshotMapper.FromBoard(hq);
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

        /// <summary>Always true since M5 (ADR-0005): the Manpower fielding gate is gone —
        /// you can always march. Manpower is run health; the reckoning comes after the
        /// fight, not before it. Kept as a method because UI wires through it.</summary>
        public bool CanStartBattle(out string failureReason)
        {
            failureReason = null;
            return true;
        }

        // ---- Dread clock / boss framework (M1, ADR-0004) ----

        /// <summary>True when the next combat is a mandatory Boss Fight.</summary>
        public bool IsBossFightPending =>
            State != null && DreadRules.IsBossPending(State.Dread, State.BossesDefeated);

        /// <summary>Dread level that triggers the next boss (for the HUD meter).</summary>
        public int NextDreadThreshold => DreadRules.NextThreshold(State?.BossesDefeated ?? 0);

        /// <summary>Seeded hidden boss order — derived from the run seed, never persisted.</summary>
        public string[] GetBossOrder() => BossRoster.GetBossOrder(State.RunSeed);

        /// <summary>The boss the next combat will be, or null when no boss is pending.</summary>
        public BossDefinition GetPendingBoss() =>
            IsBossFightPending
                ? BossRoster.Get(GetBossOrder()[State.BossesDefeated])
                : null;

        /// <summary>
        /// Normal-fight enemy template keyed on the Dread difficulty clock, CLAMPED to the
        /// last authored template — variable-length runs exceed the authored 10 fights and
        /// the old modulo wrap in ContentDatabase.GetEnemyTemplate would reset difficulty
        /// to fight 1. Since M2 this is only the legacy fallback for pre-option v9 saves
        /// mid-round; live rounds fight the chosen Fight Option's template.
        /// </summary>
        private EnemyTemplateSO GetEnemyTemplateForDifficulty()
        {
            int maxFight = _content.EnemyTemplates
                .Where(e => e != null)
                .Select(e => e.fightNumber)
                .DefaultIfEmpty(0)
                .Max();
            if (maxFight <= 0)
                return null;

            int fightEquivalent = DreadRules.FightEquivalent(State.Dread);
            return _content.GetEnemyTemplate(Math.Min(fightEquivalent, maxFight));
        }

        public BoardState GetUpcomingEnemyBoard()
        {
            var pendingBoss = GetPendingBoss();
            if (pendingBoss != null)
                return BossRoster.BuildStageBoard(
                    pendingBoss, State.BossesDefeated, _registry, Faction.combatBoardSize);

            if (GetChosenOption() != null)
                return GetOptionEnemyBoard(State.ChosenFightOption);

            var enemyTemplate = GetEnemyTemplateForDifficulty();
            if (enemyTemplate == null)
                return null;
            return enemyTemplate.BuildBoard(Faction, _registry);
        }

        // ---- Fight Options (M2, ADR-0004) ----

        /// <summary>
        /// Round-start Fight Option generation — the ONLY place options roll. They
        /// persist across save/load and never regenerate mid-round (anti-scum); a
        /// re-fought loss keeps its FightIndex and Dread, so it re-rolls the SAME
        /// options. Boss-pending rounds get an EMPTY list — the Front Report shows
        /// the boss instead and BeginCombat runs the boss branch.
        /// </summary>
        private void GenerateFightOptions()
        {
            State.ChosenFightOption = -1;
            if (IsBossFightPending)
            {
                State.FightOptions = new List<FightOptionRecord>();
                return;
            }

            var sources = _content.EnemyTemplates
                .Where(t => t != null)
                .Select(t => new FightOptionArmySource
                {
                    FightNumber = t.fightNumber,
                    EnemyFactionId = t.enemyFactionId,
                    BuildBoard = () => t.BuildBoard(Faction, _registry)
                })
                .ToList();
            State.FightOptions = FightOptionGenerator.Generate(
                State.RunSeed, State.FightIndex, State.Dread, sources);
        }

        public bool CanChooseOption(int index, out string reason)
        {
            if (State?.FightOptions == null || index < 0 || index >= State.FightOptions.Count)
            {
                reason = "No such fight option.";
                return false;
            }

            if (State.FightOptions[index].Tier == FightOptionTier.Easy
                && State.Authority < DreadRules.EasyAuthorityCost)
            {
                reason = $"The easy front costs {DreadRules.EasyAuthorityCost} Authority; " +
                         $"only {State.Authority} available.";
                return false;
            }

            reason = null;
            return true;
        }

        public void ChooseFightOption(int index)
        {
            if (!CanChooseOption(index, out string reason))
                throw new InvalidOperationException(reason);

            State.ChosenFightOption = index;
            Persist();
        }

        /// <summary>Enemy board preview for one Front Report option — regenerated from
        /// its template key (armies are never persisted). Null on a bad index.</summary>
        public BoardState GetOptionEnemyBoard(int index)
        {
            if (State?.FightOptions == null || index < 0 || index >= State.FightOptions.Count)
                return null;

            var option = State.FightOptions[index];
            var template = _content.GetEnemyTemplate(option.TemplateFightNumber, option.EnemyFactionId);
            return template?.BuildBoard(Faction, _registry);
        }

        private FightOptionRecord GetChosenOption() =>
            State?.FightOptions != null
            && State.ChosenFightOption >= 0
            && State.ChosenFightOption < State.FightOptions.Count
                ? State.FightOptions[State.ChosenFightOption]
                : null;

        public void BeginCombat()
        {
            if (State.Phase != RunPhase.Build)
                throw new InvalidOperationException("Combat can only start from the build phase.");

            if (!CanStartBattle(out string failureReason))
                throw new InvalidOperationException(failureReason);

            var playerBoard = GetCombatBoard();
            _fightStartCombatBoard = playerBoard;
            RecordCriticalMassIfTriggered();

            // Dread threshold crossed → the next fight is the pending boss at the stage
            // matching how many bosses already fell (boss rounds ignore ChosenFightOption).
            // Normal fights consume the chosen Fight Option.
            BoardState enemyBoard;
            string bossId = null;
            string activeModifierId = null;
            FightOptionTier? activeTier = null;
            string arenaThemeId;
            var pendingBoss = GetPendingBoss();
            if (pendingBoss != null)
            {
                enemyBoard = BossRoster.BuildStageBoard(
                    pendingBoss, State.BossesDefeated, _registry, Faction.combatBoardSize);
                bossId = pendingBoss.BossId;
                activeModifierId = pendingBoss.TwistId;
                // Salvage targeting works on bosses too.
                State.LastEnemyFactionId = pendingBoss.EnemyFactionId;
                // A boss always fights on its pool's signature ground (M4).
                arenaThemeId = ArenaThemes.SignatureTheme(pendingBoss.EnemyFactionId);
            }
            else
            {
                var chosenOption = GetChosenOption();
                if (chosenOption == null && State.FightOptions is { Count: > 0 })
                    throw new InvalidOperationException(
                        "A Fight Option must be chosen before combat (ChooseFightOption).");

                // Legacy fallback: a v9 save from before M2 has no options for the
                // round already in progress — fight the Dread-difficulty template.
                var enemyTemplate = chosenOption != null
                    ? _content.GetEnemyTemplate(chosenOption.TemplateFightNumber, chosenOption.EnemyFactionId)
                    : GetEnemyTemplateForDifficulty();
                if (enemyTemplate == null)
                    throw new InvalidOperationException($"No enemy template for fight {State.FightIndex}.");

                enemyBoard = enemyTemplate.BuildBoard(Faction, _registry);
                // Normalize covers the legacy no-options fallback AND pre-M4 option
                // records (null ThemeId) — both land on the default theme.
                arenaThemeId = ArenaThemes.Normalize(chosenOption?.ThemeId);
                if (chosenOption != null)
                {
                    activeTier = chosenOption.Tier;
                    activeModifierId = chosenOption.ConditionId; // hard tier only, else null
                    // Salvage targeting keys on the chosen front's pool.
                    State.LastEnemyFactionId = chosenOption.EnemyFactionId;
                }
            }

            ResetAuthorityForBuildRound();
            var buildBoards = GetBuildBoards();
            var criticalMassSnapshot = CriticalMassEngine.Evaluate(buildBoards);
            if (criticalMassSnapshot.AuthorityBonus > 0)
                State.Authority += criticalMassSnapshot.AuthorityBonus;

            // The easy front spends command capital: debit the round pool BEFORE the
            // combat snapshot so Requisition is the reduced pool.
            if (activeTier == FightOptionTier.Easy)
            {
                if (State.Authority < DreadRules.EasyAuthorityCost)
                    throw new InvalidOperationException(
                        $"The easy front costs {DreadRules.EasyAuthorityCost} Authority; " +
                        $"only {State.Authority} available.");
                State.Authority -= DreadRules.EasyAuthorityCost;
            }

            int combatSeed = SeedStreams.Derive(State.RunSeed, "combat", State.FightIndex);
            var defaultTactic = ResolveDefaultPlayerTactic(Faction);

            State.Phase = RunPhase.Combat;
            State.Combat = new CombatSaveState
            {
                CombatSeed = combatSeed,
                EnemyBoard = BoardSnapshotMapper.FromBoard(enemyBoard),
                BossId = bossId,
                ActiveTwistId = activeModifierId,
                ActiveTier = activeTier,
                ArenaThemeId = arenaThemeId,
                Requisition = State.Authority,
                Authority = State.Authority,
                PlayerTactic = defaultTactic,
                StartingTactic = defaultTactic,
                SubmittedCommands = new List<PhaseCommand>(),
                EventLog = new List<CombatEventRecord>()
            };

            _activeCombat = TickCombatRun.Start(
                playerBoard,
                enemyBoard,
                combatSeed,
                State.Authority,
                buildBoards,
                ResolveActiveCombatModifiers(),
                suppressEnemyFightStartEngines: activeTier == FightOptionTier.Easy);
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
                _activeCombat.CurrentPauseIndex,
                GetBuildBoards()?.Hq);
        }

        public int GetPrimaryActionBudget() => 1;

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
                StartingTactics = Faction?.startingTactics,
                EnemyTargetCells = _activeCombat.GetLiveEnemyTargetCells(),
                TransportOrders = _activeCombat.PlayerCombatantsForTests
                    .Where(c => c.IsTransport && c.IsActive && c.EmbarkedCargoIds != null && c.EmbarkedCargoIds.Count > 0)
                    .Select(c => new TransportOrderOption
                    {
                        SourcePieceId = c.InstanceId,
                        SourceDisplayName = c.Definition?.DisplayName ?? c.InstanceId,
                        CargoCount = c.EmbarkedCargoIds.Count
                    })
                    .ToList()
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

            // Combat is deterministic and resumes from the BeginCombat save (seed + boards +
            // submitted commands + checkpoints via FastForwardFromSave) — it does NOT need the
            // per-segment event log on disk. Serializing that ever-growing log every ~2s was an
            // O(n^2) synchronous write that froze playback between segments. Persist only when
            // the fight ends; mid-fight quit/resume simply replays the fight from its start.
            if (result.Status == CombatAdvanceStatus.Completed)
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

            int goldCost = ComputeRerollGoldCost();
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

            ApplySalvageRefund(removed.Definition, removed.IsMercenary);
            SaveBoardForPiece(removed.Definition, board);
            return true;
        }

        private void ApplySalvageRefund(PieceDefinition piece, bool isMercenary = false)
        {
            var refund = SalvageCalculator.Compute(piece, State.FactionId, isMercenary);
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
            var pendingBoss = GetPendingBoss();
            if (pendingBoss != null)
                return pendingBoss.DisplayName;

            var chosen = GetChosenOption();
            if (chosen != null)
                return _content.GetEnemyTemplate(chosen.TemplateFightNumber, chosen.EnemyFactionId)?.previewTag;

            var next = GetEnemyTemplateForDifficulty();
            return next?.previewTag;
        }

        /// <summary>Rule modifiers for the active combat — a boss Twist or a hard option's
        /// Battle Condition, resolved by id through the shared catalog. Used by both
        /// BeginCombat and the save-restore path so replays stay identical.</summary>
        private IReadOnlyList<ICombatRuleModifier> ResolveActiveCombatModifiers() =>
            string.IsNullOrEmpty(State?.Combat?.ActiveTwistId)
                ? null
                : new[] { RuleModifierCatalog.Resolve(State.Combat.ActiveTwistId) };

        private void CompleteCombat(CombatAdvanceResult result)
        {
            State.LastCombatLogText = CombatLogFormatter.FormatAll(result.EventLog?.Events);
            _activeCombat = null;
            bool isBossFight = State.Combat?.BossId != null;
            // Captured before State.Combat is nulled; legacy saves (no tier) count as Normal.
            var activeTier = State.Combat?.ActiveTier ?? FightOptionTier.Normal;
            bool playerWon = result.PlayerWon;
            bool isDraw = result.IsDraw;
            // M3 salvage quality: a hard-front VICTORY upweights the following build
            // round's salvage offers toward rarer spoils (boss tiers are null → false).
            State.SalvageHardBoost = playerWon && activeTier == FightOptionTier.Hard && !isBossFight;
            var playerCombatants = result.PlayerCombatantsAtEnd ?? Array.Empty<CombatantState>();
            // field_hospital is Building-primary => always resolves to the HQ board.
            int casualties = ManpowerCalculator.ComputeCasualties(playerCombatants, GetBuildBoards()?.Hq);
            // Manpower is run health (ADR-0005): no clamp here — the deficit must
            // survive until AFTER the post-fight grants (muster, hard-victory package)
            // so they get their chance to save the run before the defeat check.
            State.Manpower -= casualties;
            // Routed enemies escaped with their gear: the coming build round's salvage
            // scales by the kill share (ADR-0005). Stamped for both outcomes — RefreshShop
            // re-syncs salvage every round and reads this back.
            State.LastFightSalvageKillPercent =
                SalvageChanceCalculator.KillSharePercent(result.EnemyKilled, result.EnemyRouted);
            // Blightborn's "Despair Dividend" (§1.9): +1 Supply per enemy unit that routed
            // this fight, win or lose — a no-op for every other faction.
            State.Supplies += FactionPassives.DespairDividendSupplies(State.FactionId, result.EnemyRouted);
            int suppliesIncome = ApplyPostCombatIncome();

            if (!playerWon)
            {
                ProcessFightEndMeta();
                State.LastBattleReport = BattleReportBuilder.Build(
                    System.Array.Empty<CombatantState>(),
                    playerWon,
                    isDraw,
                    casualties,
                    suppliesIncome);
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
                        PlayerRouted = result.BattleReport.PlayerRouted,
                        EnemyRouted = result.BattleReport.EnemyRouted,
                        EnemyKilled = result.BattleReport.EnemyKilled,
                        TopDamageDealt = result.BattleReport.TopDamageDealt,
                        TopDamageTaken = result.BattleReport.TopDamageTaken
                    };
                }
                ApplySalvageAftermath(isBossFight);
                State.Combat = null;

                // Losses grant no Dread, and a pending boss stays pending — the boss
                // simply awaits the next combat. Defeat is bleeding out: no Manpower
                // left even after the muster (ADR-0005).
                if (State.Manpower <= 0)
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
                // Same FightIndex + Dread → the SAME options re-roll (a re-fought
                // loss faces the same fronts); the choice itself resets.
                GenerateFightOptions();
                Persist();
                return;
            }

            ProcessFightEndMeta();
            State.LastBattleReport = BattleReportBuilder.Build(
                System.Array.Empty<CombatantState>(),
                playerWon,
                isDraw,
                casualties,
                suppliesIncome);
            if (result.BattleReport != null)
            {
                State.LastBattleReport = new BattleReport
                {
                    PlayerWon = playerWon,
                    IsDraw = isDraw,
                    ManpowerCasualties = casualties,
                    SuppliesEarned = suppliesIncome,
                    PlayerRouted = result.BattleReport.PlayerRouted,
                    EnemyRouted = result.BattleReport.EnemyRouted,
                    EnemyKilled = result.BattleReport.EnemyKilled,
                    TopDamageDealt = result.BattleReport.TopDamageDealt,
                    TopDamageTaken = result.BattleReport.TopDamageTaken
                };
            }

            ApplySalvageAftermath(isBossFight);
            State.Combat = null;

            // Dread clock resolution (ADR-0004): boss wins advance the boss track and
            // grant no Dread; the third boss win ends the run. Normal wins tick Dread.
            if (isBossFight)
            {
                State.BossesDefeated++;
                if (State.BossesDefeated >= DreadRules.BossCount)
                {
                    State.Phase = RunPhase.Victory;
                    ProcessRunEndMeta(victory: true);
                    SaveManager.DeleteSave();
                    return;
                }
            }
            else
            {
                // Hard-front victory pays its materiel package BEFORE the shop refresh.
                if (activeTier == FightOptionTier.Hard)
                {
                    State.Supplies += DreadRules.HardVictorySupplies;
                    State.Manpower += DreadRules.HardVictoryManpower;
                }

                State.Dread += DreadRules.DreadFor(activeTier);
            }

            // A win can still bleed the army out (ADR-0005): if the muster and the
            // hard-victory package couldn't pull Manpower above zero, the run ends.
            // The final-boss Victory above takes precedence — it already returned.
            if (State.Manpower <= 0)
            {
                State.Phase = RunPhase.Defeat;
                ProcessRunEndMeta(victory: false);
                Persist();
                return;
            }

            State.FightIndex++;
            State.Phase = RunPhase.Aftermath;
            State.RerollCountThisRound = 0;
            ResetAuthorityForBuildRound();
            RefreshShop();
            GenerateFightOptions();
            Persist();
        }

        private void ApplySalvageAftermath(bool isBossFight)
        {
            // Boss and Fight Option fights already stamped LastEnemyFactionId with
            // their pool at BeginCombat — don't overwrite it with the difficulty
            // template's faction. Only the legacy (pre-M2 save) path re-derives.
            if (isBossFight || GetChosenOption() != null)
            {
                SyncSalvageChancePercent();
                return;
            }

            var enemyTemplate = GetEnemyTemplateForDifficulty();
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
                buildBoards,
                ResolveActiveCombatModifiers(),
                // Null tier (boss fights, legacy saves) restores as Normal.
                suppressEnemyFightStartEngines: State.Combat.ActiveTier == FightOptionTier.Easy);

            // Re-apply the FIGHT-START tactic before fast-forwarding; the saved
            // PlayerTactic may be a mid-fight change, which must replay through
            // SubmittedCommands exactly as it did live — applying it here would
            // recompute fight-start buffs from the wrong tactic and diverge.
            var playerTactic = State.Combat.StartingTactic ?? State.Combat.PlayerTactic;
            if (!TacticUnlockRules.IsUnlocked(Faction, playerTactic))
                playerTactic = ResolveDefaultPlayerTactic(Faction);

            if (playerTactic != default)
                _activeCombat.SetPlayerTactic(playerTactic);

            _activeCombat.FastForwardFromSave(
                State.Combat.CheckpointsFired,
                State.Combat.AwaitingCommand,
                State.Combat.SubmittedCommands);
            State.Combat.PlayerTactic = _activeCombat.PlayerTactic;

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
