using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class CombatResolver
    {
        public CombatResult Resolve(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            IReadOnlyList<PhaseCommand> commands,
            int requisition = 0)
        {
            var run = TickCombatRun.Start(playerBoard, enemyBoard, seed, requisition);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var deploymentCommands = FilterCommands(commands, CombatPhase.Deployment);
            run.Continue(deploymentCommands);

            var grindCommands = FilterCommands(commands, CombatPhase.Grind);
            run.Continue(grindCommands);

            return new CombatResult
            {
                EventLog = run.Log,
                PlayerWon = run.PlayerWon
            };
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            CombatPhase phase) =>
            commands?.Where(c => c.AfterPhase == phase).ToList()
            ?? (IReadOnlyList<PhaseCommand>)System.Array.Empty<PhaseCommand>();
    }
}
