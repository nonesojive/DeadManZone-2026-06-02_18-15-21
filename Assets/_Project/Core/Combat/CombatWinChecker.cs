using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    public static class CombatWinChecker
    {
        public static (bool fightOver, bool playerWon, bool isDraw) Evaluate(
            IReadOnlyList<CombatantState> playerCombatants,
            IReadOnlyList<CombatantState> enemyCombatants)
        {
            bool enemyAlive = HasLivingFighters(enemyCombatants);
            bool playerAlive = HasLivingFighters(playerCombatants);

            if (!enemyAlive && !playerAlive)
                return (true, true, true);

            if (!enemyAlive)
                return (true, true, false);

            if (!playerAlive)
                return (true, false, false);

            return (false, false, false);
        }

        private static bool HasLivingFighters(IEnumerable<CombatantState> combatants) =>
            combatants.Any(c => c.IsAlive && c.Definition.MaxHp > 0);
    }
}
