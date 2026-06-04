using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

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

        public BoardState PlayerBoard => _playerBoard;
        public int Authority { get; private set; }
        public int Requisition => Authority;
        public CombatPhase LastCompletedPhase { get; private set; }
        public CombatSegment ActiveSegment { get; private set; }
        public int SegmentTick { get; private set; }
        public CombatEventLog Log => _log;
        public bool IsFightOver { get; private set; }
        public bool PlayerWon { get; private set; }
        public bool IsDraw { get; private set; }

        public bool AwaitingCommand =>
            !IsFightOver &&
            (LastCompletedPhase == CombatPhase.Deployment || LastCompletedPhase == CombatPhase.Grind);

        public TacticType PlayerTactic => _tactics.PlayerTactic;

        public bool IsPlayerHqAlive =>
            _playerCombatants.Any(c => c.HasTag(GameTags.Hq) && c.IsAlive);

        public void SetPlayerTactic(TacticType tactic) => _tactics.PlayerTactic = tactic;

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
            ApplyAdjacencyBonuses(playerBoard, _playerCombatants);
            ApplyAdjacencyBonuses(enemyBoard, _enemyCombatants);
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

            if (LastCompletedPhase == default)
            {
                RunSegment(CombatSegment.Opening, CombatPhase.Deployment, SegmentTickBudget.Opening, 0.2f);
                LastCompletedPhase = CombatPhase.Deployment;
                return IsFightOver ? CompleteResult() : AwaitingResult(CombatPhase.Deployment);
            }

            if (LastCompletedPhase == CombatPhase.Deployment)
            {
                ApplyCommands(commands, CombatPhase.Deployment);
                if (TryEndFight())
                    return CompleteResult();

                RunSegment(CombatSegment.MainFight, CombatPhase.Grind, SegmentTickBudget.MainFight, 1.0f);
                LastCompletedPhase = CombatPhase.Grind;
                return IsFightOver ? CompleteResult() : AwaitingResult(CombatPhase.Grind);
            }

            if (LastCompletedPhase == CombatPhase.Grind)
            {
                ApplyCommands(commands, CombatPhase.Grind);
                if (TryEndFight())
                    return CompleteResult();

                RunSegment(CombatSegment.BriefPush, CombatPhase.FinalPush, CombatPacingConfig.BriefPushTicks, 1.0f);
                if (TryEndFight())
                    return CompleteResult();

                RunGasUntilEnd();
                LastCompletedPhase = CombatPhase.FinalPush;
                return CompleteFight();
            }

            return CompleteResult();
        }

        public void FastForwardToCheckpoint(CombatPhase completedPhase, IReadOnlyList<PhaseCommand> submittedCommands)
        {
            if (completedPhase == default)
                return;

            Continue(System.Array.Empty<PhaseCommand>());

            if (completedPhase == CombatPhase.Deployment)
                return;

            Continue(FilterCommands(submittedCommands, CombatPhase.Deployment));

            if (completedPhase == CombatPhase.Grind)
                return;

            Continue(FilterCommands(submittedCommands, CombatPhase.Grind));
        }

        private void RunGasUntilEnd()
        {
            ActiveSegment = CombatSegment.GasFinal;
            for (SegmentTick = 0; SegmentTick < CombatPacingConfig.MaxGasTicks; SegmentTick++)
            {
                if (TryEndFight())
                    break;

                TryMoveSide(_playerCombatants, _enemyCombatants, ActiveSegment, CombatPhase.FinalPush);
                TryMoveSide(_enemyCombatants, _playerCombatants, ActiveSegment, CombatPhase.FinalPush);
                ApplyGas(CombatPhase.FinalPush);
                ResolveAttacks(
                    _playerCombatants,
                    _enemyCombatants,
                    _tactics.PlayerTactic,
                    _tactics.PlayerDamageBuff,
                    CombatPhase.FinalPush,
                    1.2f);
                ResolveAttacks(
                    _enemyCombatants,
                    _playerCombatants,
                    _tactics.EnemyTactic,
                    _tactics.EnemyDamageBuff,
                    CombatPhase.FinalPush,
                    1.2f);
            }
        }

        private void RunSegment(CombatSegment segment, CombatPhase phase, int tickBudget, float damageScale)
        {
            ActiveSegment = segment;
            for (SegmentTick = 0; SegmentTick < tickBudget; SegmentTick++)
            {
                if (TryEndFight())
                    break;

                TryMoveSide(_playerCombatants, _enemyCombatants, segment, phase);
                TryMoveSide(_enemyCombatants, _playerCombatants, segment, phase);

                if (segment == CombatSegment.GasFinal)
                    ApplyGas(phase);

                ResolveAttacks(_playerCombatants, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, phase, damageScale);
                ResolveAttacks(_enemyCombatants, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, phase, damageScale);
            }
        }

        private void TryMoveSide(
            IList<CombatantState> movers,
            IList<CombatantState> targets,
            CombatSegment segment,
            CombatPhase phase)
        {
            var aliveTargets = targets.Where(t => t.IsAlive).OrderBy(t => t.InstanceId).ToList();
            if (aliveTargets.Count == 0)
                return;

            var goal = aliveTargets[0].Position;
            var blocked = new HashSet<GridCoord>(_occupied);

            foreach (var mover in movers.Where(m => m.IsAlive && m.HasTag(GameTags.Combatant)).OrderBy(m => m.InstanceId))
            {
                var next = CombatMovement.StepTowardTarget(mover.Position, goal, _layout, segment, blocked);
                if (next == null || next.Value.Equals(mover.Position))
                    continue;

                _occupied.Remove(mover.Position);
                mover.Position = next.Value;
                _occupied.Add(mover.Position);
                _log.Append(phase, SegmentTick, mover.InstanceId, "move", null, 0);
            }
        }

        private void ApplyGas(CombatPhase phase)
        {
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsAlive).OrderBy(c => c.InstanceId))
            {
                if (GasDamageSystem.IsMitigated(combatant.Definition))
                    continue;

                int damage = GasDamageSystem.GetDamage(combatant.Position, SegmentTick, _layout);
                combatant.CurrentHp -= damage;
                _log.Append(phase, SegmentTick, "gas", combatant.InstanceId, "gas_damage", damage);
            }
        }

        private void ResolveAttacks(
            IList<CombatantState> attackers,
            IList<CombatantState> defenders,
            TacticType tactic,
            int damageBuff,
            CombatPhase phase,
            float damageScale)
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
                    damageScale,
                    target.ArmorBuffSteps,
                    actor.DamageBonus + damageBuff);
                target.CurrentHp -= damage;
                actor.DamageDealtThisFight += damage;
                target.DamageTakenThisFight += damage;
                _log.Append(phase, SegmentTick, actor.InstanceId, "damage", target.InstanceId, damage);
                actor.CooldownRemaining = System.Math.Max(1, actor.Definition.CooldownTicks);
            }
        }

        private void ApplyCommands(IReadOnlyList<PhaseCommand> commands, CombatPhase completedPhase)
        {
            var phaseCommands = FilterCommands(commands, completedPhase);
            if (phaseCommands.Count == 0)
                return;

            int authority = Authority;
            _commandProcessor.TryApplyBatch(
                phaseCommands,
                _playerBoard,
                ref authority,
                _tactics,
                _playerCombatants,
                _enemyCombatants,
                _log,
                completedPhase);
            Authority = authority;

            foreach (var combatant in _playerCombatants)
                combatant.ArmorBuffSteps = 0;
        }

        private bool TryEndFight()
        {
            var (fightOver, playerWon, isDraw) = CombatWinChecker.Evaluate(_playerCombatants, _enemyCombatants);
            if (!fightOver)
                return false;

            IsFightOver = true;
            PlayerWon = playerWon;
            IsDraw = isDraw;
            return true;
        }

        private CombatAdvanceResult CompleteFight()
        {
            TryEndFight();
            return CompleteResult();
        }

        private CombatAdvanceResult AwaitingResult(CombatPhase phase) =>
            new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.AwaitingCommand,
                CompletedPhase = phase,
                EventLog = _log
            };

        private CombatAdvanceResult CompleteResult()
        {
            var (total, lost, hqDamaged) = ComputePlayerLossStats();
            var survivors = _playerCombatants
                .Where(c => c.IsAlive && c.HasTag(GameTags.Combatant))
                .Select(c => c.InstanceId)
                .ToList();

            return new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.Completed,
                CompletedPhase = LastCompletedPhase,
                PlayerWon = PlayerWon,
                IsDraw = IsDraw,
                EventLog = _log,
                PlayerCombatantsTotal = total,
                PlayerCombatantsLost = lost,
                PlayerHqDamaged = hqDamaged,
                SurvivingPlayerCombatantIds = survivors,
                BattleReport = BattleReportBuilder.Build(
                    _playerCombatants,
                    PlayerWon,
                    IsDraw,
                    manpowerRefunded: 0,
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
                if (combatant.HasTag(GameTags.Combatant))
                {
                    total++;
                    if (!combatant.IsAlive)
                        lost++;
                }

                if (combatant.HasTag(GameTags.Hq) && combatant.CurrentHp < combatant.Definition.MaxHp)
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

        private static List<CombatantState> SpawnCombatants(BoardState board, CombatSide side, int xOffset) =>
            board.Pieces
                .OrderBy(p => p.InstanceId)
                .Where(p => p.Definition.MaxHp > 0)
                .Select(p => new CombatantState
                {
                    InstanceId = p.InstanceId,
                    Side = side,
                    Definition = p.Definition,
                    CurrentHp = p.Definition.MaxHp,
                    CooldownRemaining = 0,
                    Position = new GridCoord(xOffset + p.Anchor.X, p.Anchor.Y)
                })
                .ToList();

        private static void ApplyAdjacencyBonuses(BoardState board, IList<CombatantState> combatants)
        {
            foreach (var combatant in combatants)
            {
                foreach (var adjacentId in board.GetAdjacentInstanceIds(combatant.InstanceId))
                {
                    var adjacent = board.Pieces.First(p => p.InstanceId == adjacentId);
                    if (adjacent.Definition.Tags.Contains("Supply"))
                        combatant.DamageBonus += 1;
                }
            }
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            CombatPhase phase) =>
            commands?.Where(c => c.AfterPhase == phase).ToList() ?? new List<PhaseCommand>();
    }
}
