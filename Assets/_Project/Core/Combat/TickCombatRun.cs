using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public sealed class TickCombatRun
    {
        private readonly CommandProcessor _commandProcessor = new();
        private readonly BoardState _playerBoard;
        private readonly BoardState _playerHqBoard;
        private readonly int _playerArtilleryCount;
        private readonly BattlefieldState _battlefield;
        private readonly BattlefieldLayout _layout;
        private readonly Rng _rng;
        private readonly List<CombatantState> _playerCombatants;
        private readonly List<CombatantState> _enemyCombatants;
        private readonly TacticState _tactics = new();
        private readonly CombatEventLog _log = new();
        private CombatOccupancyGrid _occupancyGrid = new();
        private bool _awaitingCommand;
        private bool _awaitingOpeningCommand;
        private bool _protectSupportArmorGranted;

        public BoardState PlayerBoard => _playerBoard;
        public int Authority { get; private set; }
        public int Requisition => Authority;
        public int CheckpointsFired { get; private set; }
        public int GlobalTick { get; private set; }
        public PauseTriggerContext LastPauseTrigger { get; private set; }
        public CombatEventLog Log => _log;
        public bool IsFightOver { get; private set; }
        public bool PlayerWon { get; private set; }
        public bool IsDraw { get; private set; }

        public bool AwaitingCommand => !IsFightOver && _awaitingCommand;

        /// <summary>Index of the pause currently awaiting commands, or -1.</summary>
        public int CurrentPauseIndex => AwaitingCommand
            ? _awaitingOpeningCommand ? 0 : 1
            : -1;

        public TacticType PlayerTactic => _tactics.PlayerTactic;

        public IReadOnlyList<CombatantState> PlayerCombatantsForTests => _playerCombatants;

        public IReadOnlyList<CombatantState> EnemyCombatantsForTests => _enemyCombatants;

        /// <summary>Cells the ability executor will honor as explicit targets right now —
        /// live enemy anchors, per <see cref="CombatAbilityExecutor.IsValidTargetCell"/>.
        /// Surfaced to the pause UI via <see cref="Run.CombatPauseContext.EnemyTargetCells"/>
        /// so the target picker validates against the same rule execution applies.</summary>
        public IReadOnlyList<GridCoord> GetLiveEnemyTargetCells() =>
            _enemyCombatants
                .Where(e => e.IsActive)
                .Select(e => e.AnchorPosition)
                .Distinct()
                .ToList();

        public IReadOnlyDictionary<GridCoord, string> OccupancySnapshotForTests => _occupancyGrid.Snapshot();

        private TickCombatRun(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority,
            BuildBoardSet playerBuildBoards,
            IReadOnlyList<ICombatRuleModifier> modifiers,
            bool suppressEnemyFightStartEngines)
        {
            _playerBoard = playerBoard;
            _playerHqBoard = playerBuildBoards?.Hq;
            // Grand Battery's Rolling Barrage scales with this (2026-07-15 faction-roster-v1
            // §2.2) — a fixed fight-start count, same reuse of BuildBoardTagCounter as the
            // Marksman-Doctrine Officer's BoardPerTagCount sniper-count aura.
            _playerArtilleryCount = BuildBoardTagCounter.Count(playerBuildBoards, GameTagIds.Artillery);
            _battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            _layout = _battlefield.Layout;
            _rng = new Rng(seed);
            Authority = authority;
            _playerCombatants = SpawnCombatants(playerBoard, CombatSide.Player, 0);
            _enemyCombatants = SpawnCombatants(enemyBoard, CombatSide.Enemy, _layout.EnemyOriginX);
            var playerSynergySnapshot = PieceAbilityEngine.EvaluateFightStart(playerBoard, playerBuildBoards);
            var playerCriticalMassSnapshot = CriticalMassEngine.Evaluate(playerBoard);
            CriticalMassEngine.ApplyToCombatants(playerCriticalMassSnapshot, _playerCombatants);
            PieceAbilityEngine.ApplyToCombatants(playerSynergySnapshot, _playerCombatants);

            // Easy Fight Options (M2) field a green enemy force: the ENEMY side's
            // fight-start engines (synergy auras, critical mass) are skipped entirely.
            // Player side and tactic buffs are untouched; Normal/Hard/bosses pass false.
            if (!suppressEnemyFightStartEngines)
            {
                var enemySynergySnapshot = PieceAbilityEngine.EvaluateFightStart(enemyBoard);
                var enemyCriticalMassSnapshot = CriticalMassEngine.Evaluate(enemyBoard);
                CriticalMassEngine.ApplyToCombatants(enemyCriticalMassSnapshot, _enemyCombatants);
                PieceAbilityEngine.ApplyToCombatants(enemySynergySnapshot, _enemyCombatants);
            }

            ApplyTacticDamageBuffs();

            // Rule modifiers (boss Twists now, Battle Conditions in M2) apply AFTER the
            // standard fight-start engines so a twist reads final fight-start state. The
            // restore path passes the same modifiers into Start, keeping replays identical.
            if (modifiers != null)
            {
                foreach (var modifier in modifiers)
                    modifier?.OnFightStart(_playerCombatants, _enemyCombatants);
            }

            _awaitingOpeningCommand = true;
            _awaitingCommand = true;
            LastPauseTrigger = new PauseTriggerContext
            {
                CheckpointIndex = 0,
                TriggeredBy = CombatSide.Player,
                Threshold = 1f
            };
        }

        public static TickCombatRun Start(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority = 0,
            BuildBoardSet playerBuildBoards = null,
            IReadOnlyList<ICombatRuleModifier> modifiers = null,
            bool suppressEnemyFightStartEngines = false) =>
            new TickCombatRun(
                playerBoard,
                enemyBoard,
                seed,
                authority,
                playerBuildBoards,
                modifiers,
                suppressEnemyFightStartEngines);

        public CombatAdvanceResult Continue(IReadOnlyList<PhaseCommand> commands)
        {
            if (IsFightOver)
                return CompleteResult();

            if (_awaitingCommand)
            {
                ApplyCommands(commands, CurrentPauseIndex);
                _awaitingCommand = false;
                if (_awaitingOpeningCommand)
                {
                    _awaitingOpeningCommand = false;
                    if (TryEndFight(CheckpointsFired))
                        return CompleteResult();
                }
                else if (TryEndFight(CheckpointsFired))
                {
                    return CompleteResult();
                }
            }

            int segment = CheckpointsFired;
            RunUntilPauseOrEnd(segment);
            return IsFightOver ? CompleteResult(segment) : AwaitingResult(segment);
        }

        public void FastForwardToCheckpoint(int checkpointsFired, IReadOnlyList<PhaseCommand> submittedCommands)
        {
            if (checkpointsFired <= 0 && !_awaitingOpeningCommand)
                return;

            if (_awaitingOpeningCommand)
                Continue(FilterCommands(submittedCommands, 0));

            while (!IsFightOver && _awaitingCommand && CheckpointsFired < checkpointsFired)
                Continue(FilterCommands(submittedCommands, CurrentPauseIndex));
        }

        /// <summary>Replays saved pause commands until the sim matches a persisted mid-fight snapshot.</summary>
        public void FastForwardFromSave(
            int checkpointsFired,
            bool savedAwaitingCommand,
            IReadOnlyList<PhaseCommand> submittedCommands)
        {
            FastForwardToCheckpoint(checkpointsFired, submittedCommands);

            while (!IsFightOver && !savedAwaitingCommand && AwaitingCommand)
            {
                var commands = FilterCommands(submittedCommands, CurrentPauseIndex);
                if (commands.Count == 0)
                    break;

                Continue(commands);
            }
        }

        public CombatAdvanceResult BuildCompletionResultIfOver() =>
            IsFightOver ? CompleteResult() : null;

        private void RunUntilPauseOrEnd(int segment)
        {
            while (!IsFightOver)
            {
                if (GlobalTick >= CombatPacingConfig.MaxFightTicks)
                {
                    EndAsDraw(segment);
                    return;
                }

                TryMoveSide(_playerCombatants, _enemyCombatants, segment);
                TryMoveSide(_enemyCombatants, _playerCombatants, segment);
                if (TryEndFight(segment))
                    return;

                if (GlobalTick >= CombatPacingConfig.GasStartTick)
                {
                    ApplyGas(segment);
                    if (TryEndFight(segment))
                        return;
                }

                ResolveAttacks(_playerCombatants, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, segment, CheckpointsFired);
                if (TryEndFight(segment))
                    return;

                ResolveAttacks(_enemyCombatants, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, segment, CheckpointsFired);
                if (TryEndFight(segment))
                    return;

                GlobalTick++;
                if (TryFireCheckpoint(segment))
                    return;
            }
        }

        private bool TryFireCheckpoint(int segment)
        {
            var thresholds = CombatPacingConfig.PauseThresholds;
            if (CheckpointsFired >= thresholds.Length)
                return false;

            var player = ArmyHealthTracker.Evaluate(_playerCombatants);
            var enemy = ArmyHealthTracker.Evaluate(_enemyCombatants);
            float lowest = System.Math.Min(player.Fraction, enemy.Fraction);

            int consumed = 0;
            float lastThreshold = 0f;
            while (CheckpointsFired + consumed < thresholds.Length &&
                   lowest <= thresholds[CheckpointsFired + consumed])
            {
                lastThreshold = thresholds[CheckpointsFired + consumed];
                consumed++;
            }

            if (consumed == 0)
                return false;

            var triggeredBy = player.Fraction <= enemy.Fraction ? CombatSide.Player : CombatSide.Enemy;
            _log.Append(segment, GlobalTick, "combat", "checkpoint", triggeredBy.ToString(), (int)(lastThreshold * 100));

            // Pause-granted armor (ShieldAllies) expires at every pause boundary,
            // whether or not the player submits commands here. Fight-start armor
            // (ArmorBuffSteps) is permanent and never touched. Enemy units have no
            // command path, so only the player side can carry pause-scoped armor.
            foreach (var combatant in _playerCombatants)
                combatant.PauseArmorBuffSteps = 0;

            CheckpointsFired += consumed;
            _awaitingCommand = true;
            LastPauseTrigger = new PauseTriggerContext
            {
                CheckpointIndex = 1,
                TriggeredBy = triggeredBy,
                Threshold = lastThreshold
            };
            return true;
        }

        private void EndAsDraw(int segment)
        {
            IsFightOver = true;
            IsDraw = true;
            PlayerWon = false;
            _log.Append(segment, GlobalTick, "combat", "fight_end", "draw", 0);
        }

        private void TryMoveSide(
            IReadOnlyList<CombatantState> movers,
            IReadOnlyList<CombatantState> targets,
            int segment)
        {
            var aliveTargets = targets.Where(t => t.IsActive).ToList();
            if (aliveTargets.Count == 0)
                return;

            foreach (var mover in movers.Where(m => m.IsActive && m.EffectiveMovementSpeed != 0).OrderBy(m => m.InstanceId))
            {
                if (mover.EffectiveMovementSpeed == 0)
                    continue;

                int moveChargePerTick = CombatMovementSpeed.GetChargePerTick(
                    mover.EffectiveMovementSpeed,
                    mover.Side == CombatSide.Player ? _tactics.PlayerTactic : _tactics.EnemyTactic);
                if (mover.MoveChargePercentBonus != 0)
                {
                    int percentMultiplier = System.Math.Max(0, 100 + mover.MoveChargePercentBonus);
                    moveChargePerTick = moveChargePerTick * percentMultiplier / 100;
                }

                moveChargePerTick = MovementSlowRules.ApplyMovementSlow(
                    moveChargePerTick,
                    MovementSlowRules.IsSlowed(mover, targets));

                mover.MoveCharge += moveChargePerTick;

                var goal = RoleEngagement.ComputeGoal(mover, movers, aliveTargets, _layout);
                if (!CombatMovementRules.ShouldAttemptMove(mover, aliveTargets, goal))
                    continue;

                if (IsGoalBlockedByFriendly(goal, mover.InstanceId, movers))
                    continue;

                var next = ShapePathfinder.FindStep(
                    mover.AnchorPosition,
                    goal,
                    mover.ShapeOffsets,
                    mover.InstanceId,
                    _occupancyGrid,
                    _layout,
                    spawnAnchorY: mover.SpawnAnchorY,
                    preferLaneHold: RoleEngagement.IsFrontlineMover(mover));
                if (next == null || next.Value.Equals(mover.AnchorPosition))
                    continue;

                int cost = CombatMovement.GetStepChargeCost(mover.AnchorPosition, next.Value, _layout);
                if (mover.MoveCharge < cost)
                    continue;

                mover.MoveCharge -= cost;
                _occupancyGrid.Move(mover.InstanceId, next.Value, mover.ShapeOffsets);
                mover.AnchorPosition = next.Value;
                mover.RecomputeOccupiedCells();
                _log.Append(
                    segment,
                    GlobalTick,
                    mover.InstanceId,
                    "move",
                    $"{next.Value.X},{next.Value.Y}",
                    0);
            }
        }

        private void ApplyGas(int segment)
        {
            // Routed units fled the field, so gas can't touch them.
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsActive).OrderBy(c => c.InstanceId))
            {
                if (GasDamageSystem.IsMitigated(combatant.Definition))
                    continue;

                int damage = GasDamageSystem.GetDamage(
                    combatant.AnchorPosition,
                    GlobalTick - CombatPacingConfig.GasStartTick,
                    _layout);
                combatant.CurrentHp -= damage;
                _log.Append(segment, GlobalTick, "gas", "gas_damage", combatant.InstanceId, damage);
                if (!combatant.IsAlive)
                    LogDestroyed(segment, combatant.InstanceId, "gas");
            }
        }

        private void ResolveAttacks(
            IList<CombatantState> attackers,
            IList<CombatantState> defenders,
            TacticType tactic,
            int damageBuff,
            int segment,
            int tacticsCheckpointIndex)
        {
            foreach (var actor in attackers.Where(a => a.IsActive && a.CanAttack).OrderBy(a => a.InstanceId))
            {
                if (actor.CooldownRemaining > 0)
                {
                    actor.CooldownRemaining--;
                    continue;
                }

                var target = TacticTargeting.SelectTarget(actor, defenders.ToList(), tactic, tacticsCheckpointIndex);
                if (target == null)
                    continue;

                int distance = CombatRange.Distance(actor.AnchorPosition, target.AnchorPosition);
                int accuracyMod = AccuracyModifierCollector.Collect(actor, target, tactic);
                var outcome = CombatAccuracyResolver.Resolve(
                    _rng,
                    actor.Definition,
                    target.Definition,
                    distance,
                    accuracyMod,
                    actor.DamageBonus + damageBuff,
                    target.TotalArmorSteps,
                    actor.DamagePercentBonus,
                    actor.AccuracyPercentBonus,
                    actor.Definition.AttackRange,
                    actor.AttackRangeSteps);

                string actionType = outcome.Kind switch
                {
                    CombatAttackOutcomeKind.Hit => "damage",
                    CombatAttackOutcomeKind.Graze => "graze",
                    _ => "miss"
                };

                _log.Append(segment, GlobalTick, actor.InstanceId, actionType, target.InstanceId, outcome.Damage);
                if (outcome.Damage > 0)
                {
                    target.CurrentHp -= outcome.Damage;
                    actor.DamageDealtThisFight += outcome.Damage;
                    target.DamageTakenThisFight += outcome.Damage;
                    if (!target.IsAlive)
                        LogDestroyed(segment, target.InstanceId, actor.InstanceId);
                    else if (actor.Definition.TerrorDamage > 0)
                        ApplyMoraleDamage(segment, target, actor.Definition.TerrorDamage, actor.InstanceId);
                }

                actor.CooldownRemaining = CombatAttackSpeed.GetEffectiveCooldown(
                    actor.Definition.CooldownTicks,
                    actor.Definition.AttackSpeed,
                    actor.AttackSpeedSteps);
            }
        }

        private void ApplyCommands(IReadOnlyList<PhaseCommand> commands, int checkpointIndex)
        {
            var pauseCommands = FilterCommands(commands, checkpointIndex);
            if (pauseCommands.Count == 0)
                return;

            int authority = Authority;
            _commandProcessor.TryApplyBatch(
                pauseCommands,
                _playerBoard,
                ref authority,
                _tactics,
                _playerCombatants,
                _enemyCombatants,
                _log,
                checkpointIndex,
                CheckpointsFired,
                GlobalTick,
                hqBoard: _playerHqBoard,
                artilleryCount: _playerArtilleryCount);
            Authority = authority;

            // Ability/strike kills inside the batch don't touch the occupancy grid
            // (only tick-loop kills route through LogDestroyed) — rebuild so dead
            // units stop blocking pathfinding for the rest of the fight.
            RebuildOccupied();
        }

        private bool TryEndFight(int segment)
        {
            var (fightOver, playerWon, isDraw) = CombatWinChecker.Evaluate(_playerCombatants, _enemyCombatants);
            if (!fightOver)
                return false;

            IsFightOver = true;
            PlayerWon = playerWon;
            IsDraw = isDraw;
            string outcome = isDraw ? "draw" : playerWon ? "victory" : "defeat";
            _log.Append(segment, GlobalTick, "combat", "fight_end", outcome, 0);
            return true;
        }

        private void LogDestroyed(int segment, string victimId, string sourceId)
        {
            _occupancyGrid.Remove(victimId);
            _log.Append(segment, GlobalTick, victimId, "destroyed", sourceId, 0);
            ApplyDeathShock(segment, victimId);
        }

        /// <summary>Deaths — never routs — shock the dead unit's living unbroken allies
        /// within <see cref="MoraleRules.DeathShockRadius"/> (ADR-0005). Shock-routs don't
        /// shock again; only deaths do.</summary>
        private void ApplyDeathShock(int segment, string deadInstanceId)
        {
            var dead = _playerCombatants.Concat(_enemyCombatants)
                .FirstOrDefault(c => c.InstanceId == deadInstanceId);
            if (dead == null)
                return;

            var allies = dead.Side == CombatSide.Player ? _playerCombatants : _enemyCombatants;
            foreach (var ally in allies
                         .Where(a => a.IsActive
                             && CombatRange.Distance(dead.AnchorPosition, a.AnchorPosition) <= MoraleRules.DeathShockRadius)
                         .OrderBy(a => a.InstanceId)
                         .ToList())
            {
                ApplyMoraleDamage(segment, ally, MoraleRules.DeathShockDamage, dead.InstanceId);
            }
        }

        private void ApplyMoraleDamage(int segment, CombatantState target, int amount, string sourceId)
        {
            if (!target.CanBreak || !target.IsActive || amount <= 0)
                return;

            amount = MoraleRules.ApplyResistance(amount, target.MoraleDamageResistancePercent);
            if (amount <= 0)
                return;

            target.CurrentMorale -= amount;
            _log.Append(segment, GlobalTick, sourceId, "morale_damage", target.InstanceId, amount);
            if (target.CurrentMorale > 0)
                return;

            // Break: the unit routs — flees the field, out of the fight, not dead. It stays
            // in the combatant lists (routed player units survive the fight, ADR-0005).
            target.IsBroken = true;
            _occupancyGrid.Remove(target.InstanceId);
            _log.Append(segment, GlobalTick, target.InstanceId, "rout", sourceId, 0);
        }

        private CombatAdvanceResult AwaitingResult(int segment) =>
            new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.AwaitingCommand,
                SegmentIndex = segment,
                PauseTrigger = LastPauseTrigger,
                EventLog = _log
            };

        private CombatAdvanceResult CompleteResult() => CompleteResult(CheckpointsFired);

        private CombatAdvanceResult CompleteResult(int segment)
        {
            var (total, lost) = ComputePlayerLossStats();
            var survivors = _playerCombatants
                .Where(c => c.IsAlive && c.Definition.MaxHp > 0)
                .Select(c => c.InstanceId)
                .ToList();
            int enemyKilled = _enemyCombatants.Count(c => c.Definition.MaxHp > 0 && !c.IsAlive);
            int enemyRouted = _enemyCombatants.Count(c => c.Definition.MaxHp > 0 && c.IsAlive && c.IsBroken);

            return new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.Completed,
                SegmentIndex = segment,
                PlayerWon = PlayerWon,
                IsDraw = IsDraw,
                EventLog = _log,
                PlayerCombatantsTotal = total,
                PlayerCombatantsLost = lost,
                EnemyKilled = enemyKilled,
                EnemyRouted = enemyRouted,
                SurvivingPlayerCombatantIds = survivors,
                PlayerCombatantsAtEnd = _playerCombatants,
                BattleReport = BattleReportBuilder.Build(
                    _playerCombatants,
                    PlayerWon,
                    IsDraw,
                    // field_hospital is Building-primary => always resolves to the HQ board
                    // (BoardPlacementRules.ResolveTargetBoard), not the combat board.
                    ManpowerCalculator.ComputeCasualties(_playerCombatants, _playerHqBoard),
                    suppliesEarned: 0,
                    enemyKilled: enemyKilled,
                    enemyRouted: enemyRouted)
            };
        }

        private (int total, int lost) ComputePlayerLossStats()
        {
            int total = 0;
            int lost = 0;

            foreach (var combatant in _playerCombatants)
            {
                if (combatant.Definition.MaxHp <= 0)
                    continue;

                total++;
                if (!combatant.IsAlive)
                    lost++;
            }

            return (total, lost);
        }

        private void RebuildOccupied()
        {
            _occupancyGrid = new CombatOccupancyGrid();
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsActive))
            {
                if (combatant.ShapeOffsets == null || combatant.ShapeOffsets.Count == 0)
                    continue;

                _occupancyGrid.Place(
                    combatant.InstanceId,
                    combatant.AnchorPosition,
                    combatant.ShapeOffsets);
            }
        }

        private static bool IsGoalBlockedByFriendly(
            GridCoord goal,
            string moverInstanceId,
            IReadOnlyList<CombatantState> allies)
        {
            foreach (var ally in allies)
            {
                if (!ally.IsActive || ally.InstanceId == moverInstanceId)
                    continue;

                if (ally.AnchorPosition.Equals(goal))
                    return true;
            }

            return false;
        }

        private List<CombatantState> SpawnCombatants(BoardState board, CombatSide side, int xOffset)
        {
            int halfWidth = board.Layout.Width;
            var combatants = new List<CombatantState>();

            foreach (var piece in board.Pieces
                         .OrderBy(p => p.InstanceId)
                         .Where(p => p.Definition.MaxHp > 0))
            {
                int rotationIndex = (int)piece.Rotation / 90;
                int localX = side == CombatSide.Enemy
                    ? BattlefieldLayout.MirrorEnemyAnchorX(
                        piece.Anchor.X,
                        piece.Definition.Shape,
                        halfWidth,
                        piece.Rotation)
                    : piece.Anchor.X;
                var anchor = new GridCoord(xOffset + localX, piece.Anchor.Y);
                var offsets = CombatFootprint.ComputeOffsets(piece.Definition.Shape, rotationIndex);
                var combatant = new CombatantState
                {
                    InstanceId = piece.InstanceId,
                    Side = side,
                    Definition = piece.Definition,
                    CurrentHp = piece.Definition.MaxHp,
                    CurrentMorale = piece.Definition.MaxMorale,
                    CooldownRemaining = 0,
                    AnchorPosition = anchor,
                    SpawnAnchorY = piece.Anchor.Y,
                    ShapeOffsets = offsets,
                    MoraleDamageResistancePercent = piece.Definition.MoraleDamageResistancePercent
                };
                combatant.RecomputeOccupiedCells();
                _occupancyGrid.Place(combatant.InstanceId, anchor, offsets);
                combatants.Add(combatant);
            }

            return combatants;
        }

        private void ApplyTacticDamageBuffs()
        {
            _tactics.PlayerDamageBuff = TacticEffects.GetDamageBuff(_tactics.PlayerTactic);
            _tactics.EnemyDamageBuff = TacticEffects.GetDamageBuff(_tactics.EnemyTactic);

            // Fight-start armor (ArmorBuffSteps) is permanent, so the ProtectSupport
            // grant must be idempotent — a second SetPlayerTactic(ProtectSupport)
            // (e.g. on the save-restore path) would otherwise double-grant forever.
            if (_tactics.PlayerTactic != TacticType.ProtectSupport || _protectSupportArmorGranted)
                return;

            _protectSupportArmorGranted = true;
            TacticEffects.ApplyProtectSupportBuffs(
                _tactics.PlayerTactic,
                _playerCombatants,
                _playerBoard.Layout);
        }

        public void SetPlayerTactic(TacticType tactic)
        {
            _tactics.PlayerTactic = tactic;
            ApplyTacticDamageBuffs();
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            int checkpointIndex) =>
            commands?.Where(c => c.AfterCheckpoint == checkpointIndex).ToList()
            ?? (IReadOnlyList<PhaseCommand>)new List<PhaseCommand>();
    }
}
