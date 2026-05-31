using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public sealed class CombatResolver
    {
        private const int DeploymentTicks = 6;
        private const int GrindTicks = 12;
        private const int FinalPushTicks = 6;

        private readonly CommandProcessor _commandProcessor = new CommandProcessor();

        public CombatResult Resolve(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            IReadOnlyList<PhaseCommand> commands,
            int requisition = 0)
        {
            var rng = new Rng(seed);
            var log = new CombatEventLog();
            var stances = new StanceState();
            var playerCombatants = SpawnCombatants(playerBoard, CombatSide.Player);
            var enemyCombatants = SpawnCombatants(enemyBoard, CombatSide.Enemy);

            ApplyAdjacencyBonuses(playerBoard, playerCombatants);
            ApplyAdjacencyBonuses(enemyBoard, enemyCombatants);

            var commandList = commands ?? System.Array.Empty<PhaseCommand>();
            int reqPool = requisition;

            RunPhase(CombatPhase.Deployment, DeploymentTicks, 0.2f, playerCombatants, enemyCombatants, stances, rng, log);
            ApplyPhaseCommands(CombatPhase.Deployment, commandList, playerBoard, ref reqPool, stances, playerCombatants, enemyCombatants, log);

            if (!IsSideDefeated(playerCombatants) && !IsSideDefeated(enemyCombatants))
            {
                RunPhase(CombatPhase.Grind, GrindTicks, 1.0f, playerCombatants, enemyCombatants, stances, rng, log);
                ApplyPhaseCommands(CombatPhase.Grind, commandList, playerBoard, ref reqPool, stances, playerCombatants, enemyCombatants, log);
            }

            if (!IsSideDefeated(playerCombatants) && !IsSideDefeated(enemyCombatants))
                RunPhase(CombatPhase.FinalPush, FinalPushTicks, 1.3f, playerCombatants, enemyCombatants, stances, rng, log);

            return new CombatResult
            {
                EventLog = log,
                PlayerWon = IsSideDefeated(enemyCombatants) && !IsSideDefeated(playerCombatants)
            };
        }

        private void ApplyPhaseCommands(
            CombatPhase completedPhase,
            IReadOnlyList<PhaseCommand> commands,
            BoardState playerBoard,
            ref int requisition,
            StanceState stances,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log)
        {
            foreach (var command in commands.Where(c => c.AfterPhase == completedPhase))
                _commandProcessor.TryApply(command, playerBoard, ref requisition, stances, playerCombatants, enemyCombatants, log, completedPhase);
        }

        private void RunPhase(
            CombatPhase phase,
            int ticks,
            float damageScale,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            StanceState stances,
            Rng rng,
            CombatEventLog log)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                if (IsSideDefeated(playerCombatants) || IsSideDefeated(enemyCombatants))
                    break;

                foreach (var combatant in playerCombatants.Where(c => c.IsAlive))
                    Act(combatant, enemyCombatants, stances.PlayerStance, stances.PlayerDamageBuff, phase, tick, damageScale, log);

                foreach (var combatant in enemyCombatants.Where(c => c.IsAlive))
                    Act(combatant, playerCombatants, stances.EnemyStance, stances.EnemyDamageBuff, phase, tick, damageScale, log);
            }
        }

        private void Act(
            CombatantState actor,
            IList<CombatantState> enemies,
            StanceType stance,
            int damageBuff,
            CombatPhase phase,
            int tick,
            float damageScale,
            CombatEventLog log)
        {
            if (phase == CombatPhase.Deployment)
                log.Append(phase, tick, actor.InstanceId, "move", null, 0);

            if (!actor.CanAttack)
                return;

            if (actor.CooldownRemaining > 0)
            {
                actor.CooldownRemaining--;
                return;
            }

            var target = CombatTargeting.SelectTarget(actor, enemies.ToList(), stance);
            if (target == null)
                return;

            int damage = CalculateDamage(actor, damageBuff, damageScale, phase);
            target.CurrentHp -= damage;
            log.Append(phase, tick, actor.InstanceId, "damage", target.InstanceId, damage);

            actor.CooldownRemaining = System.Math.Max(1, actor.Definition.CooldownTicks);
        }

        private static int CalculateDamage(CombatantState actor, int damageBuff, float damageScale, CombatPhase phase)
        {
            float assaultMultiplier = phase == CombatPhase.FinalPush ? 1.1f : 1f;
            int raw = actor.Definition.BaseDamage + actor.DamageBonus + damageBuff;
            return System.Math.Max(1, (int)(raw * damageScale * assaultMultiplier));
        }

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

        private static List<CombatantState> SpawnCombatants(BoardState board, CombatSide side)
        {
            return board.Pieces
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
        }

        private static bool IsSideDefeated(IEnumerable<CombatantState> combatants) =>
            combatants.All(c => !c.IsAlive);
    }
}
