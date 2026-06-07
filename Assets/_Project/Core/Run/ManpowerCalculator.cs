using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class ManpowerCalculator
    {
        public static int HpPerBody(PieceDefinition definition)
        {
            if (definition == null || definition.ManpowerCost <= 0)
                return definition?.MaxHp ?? 1;
            return definition.MaxHp / definition.ManpowerCost;
        }

        public static int ComputeFieldingRequirement(BoardState board, ContentRegistry content)
        {
            if (board == null)
                return 0;

            return board.Pieces
                .Where(p => CountsTowardFielding(p.Definition))
                .Sum(p => p.Definition.ManpowerCost);
        }

        public static int ComputeCasualties(IReadOnlyList<CombatantState> playerCombatants)
        {
            if (playerCombatants == null || playerCombatants.Count == 0)
                return 0;

            int total = 0;
            foreach (var c in playerCombatants)
            {
                if (c?.Definition == null || c.Definition.ManpowerCost <= 0)
                    continue;
                if (c.DamageTakenThisFight <= 0 && c.IsAlive)
                    continue;

                if (!c.IsAlive)
                {
                    total += c.Definition.ManpowerCost;
                    continue;
                }

                int hpPerBody = HpPerBody(c.Definition);
                int bodies = hpPerBody > 0 ? c.DamageTakenThisFight / hpPerBody : 0;
                total += Math.Min(c.Definition.ManpowerCost, bodies);
            }

            return total;
        }

        public static int ComputeUpkeep(BoardState board, ContentRegistry content) =>
            ComputeFieldingRequirement(board, content);

        public static bool CanStartBattle(BoardState board, int manpower, ContentRegistry content) =>
            manpower >= ComputeFieldingRequirement(board, content);

        private static bool CountsTowardFielding(PieceDefinition definition) =>
            PieceTagQueries.HasTag(definition, GameTagIds.Combatant)
            || PieceTagQueries.HasTag(definition, GameTagIds.Hq);
    }
}
