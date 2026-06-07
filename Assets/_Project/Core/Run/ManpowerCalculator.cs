using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class ManpowerCalculator
    {
        public static int ComputeUpkeep(BoardState board, ContentRegistry content)
        {
            if (board == null)
                return 0;

            return board.Pieces
                .Where(p => IsCombatant(p.Definition))
                .Sum(p => p.Definition.ManpowerCost);
        }

        public static bool CanStartBattle(BoardState board, int manpower, ContentRegistry content) =>
            manpower >= ComputeUpkeep(board, content);

        public static int RefundSurvivors(
            BoardState board,
            IReadOnlyList<string> survivingInstanceIds,
            ContentRegistry content)
        {
            if (board == null || survivingInstanceIds == null || survivingInstanceIds.Count == 0)
                return 0;

            var survivors = new HashSet<string>(survivingInstanceIds);
            return board.Pieces
                .Where(p => survivors.Contains(p.InstanceId) && IsCombatant(p.Definition))
                .Sum(p => p.Definition.ManpowerCost);
        }

        private static bool IsCombatant(PieceDefinition definition) =>
            PieceTagQueries.HasTag(definition, GameTagIds.Combatant);
    }
}
