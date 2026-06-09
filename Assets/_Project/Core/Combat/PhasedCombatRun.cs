using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public enum CombatAdvanceStatus
    {
        AwaitingCommand,
        Completed
    }

    public sealed class CombatAdvanceResult
    {
        public CombatAdvanceStatus Status { get; init; }
        public CombatPhase CompletedPhase { get; init; }
        public bool PlayerWon { get; init; }
        public bool IsDraw { get; init; }
        public BattleReport BattleReport { get; init; }
        public CombatEventLog EventLog { get; init; }
        public int PlayerCombatantsTotal { get; init; }
        public int PlayerCombatantsLost { get; init; }
        public bool PlayerHqDamaged { get; init; }
        public IReadOnlyList<string> SurvivingPlayerCombatantIds { get; init; } =
            System.Array.Empty<string>();
        public IReadOnlyList<CombatantState> PlayerCombatantsAtEnd { get; init; } =
            System.Array.Empty<CombatantState>();
    }

    public sealed class PhasedCombatRun
    {
        private const int DeploymentTicks = 6;
        private const int GrindTicks = 12;
        private const int FinalPushTicks = 6;

        private readonly CommandProcessor _commandProcessor = new();
        private readonly BoardState _playerBoard;
        private readonly Rng _rng;
        private readonly List<CombatantState> _playerCombatants;
        private readonly List<CombatantState> _enemyCombatants;
        private readonly TacticState _tactics = new();
        private readonly CombatEventLog _log = new();

        public BoardState PlayerBoard => _playerBoard;
        public int Requisition { get; private set; }
        public CombatPhase LastCompletedPhase { get; private set; }
        public CombatEventLog Log => _log;
        public bool IsFightOver { get; private set; }
        public bool PlayerWon { get; private set; }

        public bool AwaitingCommand =>
            !IsFightOver &&
            (LastCompletedPhase == CombatPhase.Deployment || LastCompletedPhase == CombatPhase.Grind);

        private PhasedCombatRun(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int requisition)
        {
            _playerBoard = playerBoard;
            _rng = new Rng(seed);
            Requisition = requisition;
            _playerCombatants = SpawnCombatants(playerBoard, CombatSide.Player);
            _enemyCombatants = SpawnCombatants(enemyBoard, CombatSide.Enemy);
        }

        public static PhasedCombatRun Start(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int requisition = 0) =>
            new PhasedCombatRun(playerBoard, enemyBoard, seed, requisition);

        public CombatAdvanceResult Continue(IReadOnlyList<PhaseCommand> commands)
        {
            if (IsFightOver)
                return CompleteResult();

            if (LastCompletedPhase == default)
            {
                RunPhase(CombatPhase.Deployment, DeploymentTicks, 0.2f);
                LastCompletedPhase = CombatPhase.Deployment;
                return AwaitingResult(CombatPhase.Deployment);
            }

            if (LastCompletedPhase == CombatPhase.Deployment)
            {
                ApplyCommands(commands, CombatPhase.Deployment);
                if (TryEndFight())
                    return CompleteResult();

                RunPhase(CombatPhase.Grind, GrindTicks, 1.0f);
                LastCompletedPhase = CombatPhase.Grind;
                return AwaitingResult(CombatPhase.Grind);
            }

            if (LastCompletedPhase == CombatPhase.Grind)
            {
                ApplyCommands(commands, CombatPhase.Grind);
                if (TryEndFight())
                    return CompleteResult();

                RunPhase(CombatPhase.FinalPush, FinalPushTicks, 1.3f);
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

            var deploymentCommands = FilterCommands(submittedCommands, CombatPhase.Deployment);
            Continue(deploymentCommands);

            if (completedPhase == CombatPhase.Grind)
                return;

            var grindCommands = FilterCommands(submittedCommands, CombatPhase.Grind);
            Continue(grindCommands);
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
            return new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.Completed,
                CompletedPhase = LastCompletedPhase,
                PlayerWon = PlayerWon,
                EventLog = _log,
                PlayerCombatantsTotal = total,
                PlayerCombatantsLost = lost,
                PlayerHqDamaged = hqDamaged
            };
        }

        private (int total, int lost, bool hqDamaged) ComputePlayerLossStats()
        {
            int total = 0;
            int lost = 0;
            bool hqDamaged = false;

            foreach (var combatant in _playerCombatants)
            {
                if (HasTag(combatant, GameTagIds.Combatant))
                {
                    total++;
                    if (!combatant.IsAlive)
                        lost++;
                }

                if (HasTag(combatant, GameTagIds.Hq) && combatant.CurrentHp < combatant.Definition.MaxHp)
                    hqDamaged = true;
            }

            return (total, lost, hqDamaged);
        }

        private static bool HasTag(CombatantState combatant, string tag) =>
            PieceTagQueries.HasTag(combatant.Definition, tag);

        private CombatAdvanceResult CompleteFight()
        {
            IsFightOver = true;
            PlayerWon = IsSideDefeated(_enemyCombatants) && !IsSideDefeated(_playerCombatants);
            return CompleteResult();
        }

        private bool TryEndFight()
        {
            if (!IsSideDefeated(_playerCombatants) && !IsSideDefeated(_enemyCombatants))
                return false;

            IsFightOver = true;
            PlayerWon = IsSideDefeated(_enemyCombatants) && !IsSideDefeated(_playerCombatants);
            return true;
        }

        private void ApplyCommands(IReadOnlyList<PhaseCommand> commands, CombatPhase completedPhase)
        {
            if (commands == null)
                return;

            int requisition = Requisition;
            foreach (var command in commands.Where(c => c.AfterPhase == completedPhase))
            {
                _commandProcessor.TryApply(
                    command,
                    _playerBoard,
                    ref requisition,
                    _tactics,
                    _playerCombatants,
                    _enemyCombatants,
                    _log,
                    completedPhase);
            }

            Requisition = requisition;
        }

        private void RunPhase(
            CombatPhase phase,
            int ticks,
            float damageScale)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                if (IsSideDefeated(_playerCombatants) || IsSideDefeated(_enemyCombatants))
                    break;

                foreach (var combatant in _playerCombatants.Where(c => c.IsAlive))
                    Act(combatant, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, phase, tick, damageScale);

                foreach (var combatant in _enemyCombatants.Where(c => c.IsAlive))
                    Act(combatant, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, phase, tick, damageScale);
            }
        }

        private void Act(
            CombatantState actor,
            IList<CombatantState> enemies,
            TacticType tactic,
            int damageBuff,
            CombatPhase phase,
            int tick,
            float damageScale)
        {
            if (phase == CombatPhase.Deployment)
                _log.Append(phase, tick, actor.InstanceId, "move", null, 0);

            if (!actor.CanAttack)
                return;

            if (actor.CooldownRemaining > 0)
            {
                actor.CooldownRemaining--;
                return;
            }

            var target = TacticTargeting.SelectTarget(actor, enemies.ToList(), tactic);
            if (target == null)
                return;

            int damage = CalculateDamage(actor, damageBuff, damageScale, phase);
            target.CurrentHp -= damage;
            _log.Append(phase, tick, actor.InstanceId, "damage", target.InstanceId, damage);
            actor.CooldownRemaining = CombatAttackSpeed.GetEffectiveCooldown(
                actor.Definition.CooldownTicks,
                actor.Definition.AttackSpeed);
        }

        private static int CalculateDamage(CombatantState actor, int damageBuff, float damageScale, CombatPhase phase)
        {
            float assaultMultiplier = phase == CombatPhase.FinalPush ? 1.1f : 1f;
            int raw = actor.Definition.BaseDamage + actor.DamageBonus + damageBuff;
            return System.Math.Max(1, (int)(raw * damageScale * assaultMultiplier));
        }


        private static List<CombatantState> SpawnCombatants(BoardState board, CombatSide side) =>
            board.Pieces
                .Where(p => p.Definition.MaxHp > 0)
                .Select(p => new CombatantState
                {
                    InstanceId = p.InstanceId,
                    Side = side,
                    Definition = p.Definition,
                    CurrentHp = p.Definition.MaxHp,
                    CooldownRemaining = 0
                })
                .ToList();

        private static bool IsSideDefeated(IEnumerable<CombatantState> combatants) =>
            combatants.All(c => !c.IsAlive);

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            CombatPhase phase) =>
            commands?.Where(c => c.AfterPhase == phase).ToList() ?? new List<PhaseCommand>();
    }
}
