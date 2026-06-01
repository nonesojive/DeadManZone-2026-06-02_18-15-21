using System.Collections.Generic;
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
            var run = PhasedCombatRun.Start(playerBoard, enemyBoard, seed, requisition);
            run.Continue(System.Array.Empty<PhaseCommand>());
            run.Continue(commands);
            run.Continue(commands);

            return new CombatResult
            {
                EventLog = run.Log,
                PlayerWon = run.PlayerWon
            };
        }
    }
}
