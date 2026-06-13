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
        private readonly BattlefieldState _battlefield;
        private readonly BattlefieldLayout _layout;
        private readonly Rng _rng;
        private readonly List<CombatantState> _playerCombatants;
        private readonly List<CombatantState> _enemyCombatants;
        private readonly TacticState _tactics = new();
        private readonly CombatEventLog _log = new();
        private readonly HashSet<GridCoord> _occupied = new();
        private bool _awaitingCommand;

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
        public int CurrentPauseIndex => AwaitingCommand ? CheckpointsFired - 1 : -1;

        public TacticType PlayerTactic => _tactics.PlayerTactic;

        public IReadOnlyList<CombatantState> PlayerCombatantsForTests => _playerCombatants;

        public IReadOnlyList<CombatantState> EnemyCombatantsForTests => _enemyCombatants;

        public bool IsPlayerHqAlive =>
            _playerCombatants.Any(c => c.HasTag(GameTagIds.Hq) && c.IsAlive);

        private TickCombatRun(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority)
        {
            _playerBoard = playerBoard;
            _battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            _layout = _battlefield.Layout;
            _rng = new Rng(seed);
            Authority = authority;
            _playerCombatants = SpawnCombatants(playerBoard, CombatSide.Player, 0);
            _enemyCombatants = SpawnCombatants(enemyBoard, CombatSide.Enemy, _layout.EnemyOriginX);
            var playerSynergySnapshot = SynergyEngine.EvaluateFightStart(playerBoard);
            var enemySynergySnapshot = SynergyEngine.EvaluateFightStart(enemyBoard);
            var playerCriticalMassSnapshot = CriticalMassRules.EvaluateFightStart(playerBoard);
            var enemyCriticalMassSnapshot = CriticalMassRules.EvaluateFightStart(enemyBoard);
            SynergyEngine.ApplyToCombatants(playerSynergySnapshot, _playerCombatants);
            SynergyEngine.ApplyToCombatants(enemySynergySnapshot, _enemyCombatants);
            CriticalMassRules.ApplyToCombatants(playerBoard, _playerCombatants, playerCriticalMassSnapshot);
            CriticalMassRules.ApplyToCombatants(enemyBoard, _enemyCombatants, enemyCriticalMassSnapshot);
            ApplyTacticDamageBuffs();
            RebuildOccupied();
        }

        public static TickCombatRun Start(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority = 0) =>
            new TickCombatRun(playerBoard, enemyBoard, seed, authority);

        public CombatAdvanceResult Continue(IReadOnlyList<PhaseCommand> commands)
        {
            if (IsFightOver)
                return CompleteResult();

            if (_awaitingCommand)
            {
                ApplyCommands(commands, CheckpointsFired - 1);
                _awaitingCommand = false;
                if (TryEndFight(CheckpointsFired))
                    return CompleteResult();
            }

            int segment = CheckpointsFired;
            RunUntilPauseOrEnd(segment);
            return IsFightOver ? CompleteResult(segment) : AwaitingResult(segment);
        }

        public void FastForwardToCheckpoint(int checkpointsFired, IReadOnlyList<PhaseCommand> submittedCommands)
        {
            if (checkpointsFired <= 0)
                return;

            Continue(System.Array.Empty<PhaseCommand>());
            while (!IsFightOver && _awaitingCommand && CheckpointsFired < checkpointsFired)
                Continue(FilterCommands(submittedCommands, CheckpointsFired - 1));
        }

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

                ResolveAttacks(_playerCombatants, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, segment);
                if (TryEndFight(segment))
                    return;

                ResolveAttacks(_enemyCombatants, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, segment);
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

            CheckpointsFired += consumed;
            _awaitingCommand = true;
            LastPauseTrigger = new PauseTriggerContext
            {
                CheckpointIndex = CheckpointsFired - 1,
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
            IList<CombatantState> movers,
            IList<CombatantState> targets,
            int segment)
        {
            var aliveTargets = targets.Where(t => t.IsAlive).ToList();
            if (aliveTargets.Count == 0)
                return;

            var blocked = new HashSet<GridCoord>(_occupied);

            foreach (var mover in movers.Where(m => m.IsAlive && m.HasTag(GameTagIds.Combatant)).OrderBy(m => m.InstanceId))
            {
                if (mover.Definition.MovementSpeed == MovementSpeedTier.None)
                    continue;

                int moveChargePerTick = CombatMovementSpeed.GetChargePerTick(
                    mover.Definition.MovementSpeed,
                    mover.Side == CombatSide.Player ? _tactics.PlayerTactic : _tactics.EnemyTactic);
                if (mover.MoveChargePercentBonus != 0)
                {
                    int percentMultiplier = System.Math.Max(0, 100 + mover.MoveChargePercentBonus);
                    moveChargePerTick = moveChargePerTick * percentMultiplier / 100;
                }

                mover.MoveCharge += moveChargePerTick;

                if (!CombatMovementRules.ShouldAttemptMove(mover, aliveTargets))
                    continue;

                var goal = CombatMovementRules.SelectNearestEnemyPosition(mover.Position, aliveTargets);
                var next = CombatMovement.StepTowardTarget(mover.Position, goal, _layout, blocked);
                if (next == null || next.Value.Equals(mover.Position))
                    continue;

                int cost = CombatMovement.GetStepChargeCost(mover.Position, next.Value, _layout);
                if (mover.MoveCharge < cost)
                    continue;

                mover.MoveCharge -= cost;
                _occupied.Remove(mover.Position);
                mover.Position = next.Value;
                _occupied.Add(mover.Position);
                blocked.Add(mover.Position);
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
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsAlive).OrderBy(c => c.InstanceId))
            {
                if (GasDamageSystem.IsMitigated(combatant.Definition))
                    continue;

                int damage = GasDamageSystem.GetDamage(
                    combatant.Position,
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
            int segment)
        {
            foreach (var actor in attackers.Where(a => a.IsAlive && a.CanAttack).OrderBy(a => a.InstanceId))
            {
                if (actor.CooldownRemaining > 0)
                {
                    actor.CooldownRemaining--;
                    continue;
                }

                var target = TacticTargeting.SelectTarget(actor, defenders.ToList(), tactic);
                if (target == null)
                    continue;

                int damage = CombatDamageResolver.ComputeDamage(
                    actor.Definition,
                    target.Definition,
                    1f,
                    target.ArmorBuffSteps,
                    actor.DamageBonus + damageBuff);
                target.CurrentHp -= damage;
                actor.DamageDealtThisFight += damage;
                target.DamageTakenThisFight += damage;
                _log.Append(segment, GlobalTick, actor.InstanceId, "damage", target.InstanceId, damage);
                if (!target.IsAlive)
                    LogDestroyed(segment, target.InstanceId, actor.InstanceId);
                actor.CooldownRemaining = CombatAttackSpeed.GetEffectiveCooldown(
                    actor.Definition.CooldownTicks,
                    actor.Definition.AttackSpeed);
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
                GlobalTick);
            Authority = authority;

            foreach (var combatant in _playerCombatants)
                combatant.ArmorBuffSteps = 0;
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

        private void LogDestroyed(int segment, string victimId, string sourceId) =>
            _log.Append(segment, GlobalTick, victimId, "destroyed", sourceId, 0);

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
            var (total, lost, hqDamaged) = ComputePlayerLossStats();
            var survivors = _playerCombatants
                .Where(c => c.IsAlive && c.HasTag(GameTagIds.Combatant))
                .Select(c => c.InstanceId)
                .ToList();

            return new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.Completed,
                SegmentIndex = segment,
                PlayerWon = PlayerWon,
                IsDraw = IsDraw,
                EventLog = _log,
                PlayerCombatantsTotal = total,
                PlayerCombatantsLost = lost,
                PlayerHqDamaged = hqDamaged,
                SurvivingPlayerCombatantIds = survivors,
                PlayerCombatantsAtEnd = _playerCombatants,
                BattleReport = BattleReportBuilder.Build(
                    _playerCombatants,
                    PlayerWon,
                    IsDraw,
                    ManpowerCalculator.ComputeCasualties(_playerCombatants),
                    suppliesEarned: 0,
                    moraleDelta: 0)
            };
        }

        private (int total, int lost, bool hqDamaged) ComputePlayerLossStats()
        {
            int total = 0;
            int lost = 0;
            bool hqDamaged = false;

            foreach (var combatant in _playerCombatants)
            {
                if (combatant.HasTag(GameTagIds.Combatant))
                {
                    total++;
                    if (!combatant.IsAlive)
                        lost++;
                }

                if (combatant.HasTag(GameTagIds.Hq) && combatant.CurrentHp < combatant.Definition.MaxHp)
                    hqDamaged = true;
            }

            return (total, lost, hqDamaged);
        }

        private void RebuildOccupied()
        {
            _occupied.Clear();
            foreach (var c in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsAlive))
                _occupied.Add(c.Position);
        }

        private static List<CombatantState> SpawnCombatants(BoardState board, CombatSide side, int xOffset)
        {
            int halfWidth = board.Layout.Width;
            return board.Pieces
                .OrderBy(p => p.InstanceId)
                .Where(p => p.Definition.MaxHp > 0)
                .Select(p =>
                {
                    int localX = side == CombatSide.Enemy
                        ? BattlefieldLayout.MirrorEnemyAnchorX(
                            p.Anchor.X,
                            p.Definition.Shape,
                            halfWidth,
                            p.Rotation)
                        : p.Anchor.X;
                    return new CombatantState
                    {
                        InstanceId = p.InstanceId,
                        Side = side,
                        Definition = p.Definition,
                        CurrentHp = p.Definition.MaxHp,
                        CooldownRemaining = 0,
                        Position = new GridCoord(xOffset + localX, p.Anchor.Y)
                    };
                })
                .ToList();
        }

        private void ApplyTacticDamageBuffs()
        {
            _tactics.PlayerDamageBuff = TacticEffects.GetDamageBuff(_tactics.PlayerTactic);
            _tactics.EnemyDamageBuff = TacticEffects.GetDamageBuff(_tactics.EnemyTactic);
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
