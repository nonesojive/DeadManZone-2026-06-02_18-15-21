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
            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(FilterCommands(commands, run.CurrentPauseIndex));

            return new CombatResult
            {
                EventLog = run.Log,
                PlayerWon = run.PlayerWon
            };
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            int checkpointIndex) =>
            commands?.Where(c => c.AfterCheckpoint == checkpointIndex).ToList()
            ?? (IReadOnlyList<PhaseCommand>)System.Array.Empty<PhaseCommand>();
    }
}
