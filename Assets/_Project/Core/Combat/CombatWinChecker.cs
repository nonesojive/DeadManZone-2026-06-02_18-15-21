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
            bool enemyAlive = HasActiveFighters(enemyCombatants);
            bool playerAlive = HasActiveFighters(playerCombatants);

            // Mutual annihilation is a DRAW that counts as a player WIN — including
            // boss credit. Director decision 2026-07-12: burning your whole army to
            // stop the enemy's is a pyrrhic victory, not a stalemate. Intentional; do
            // not "fix" playerWon here.
            if (!enemyAlive && !playerAlive)
                return (true, true, true);

            if (!enemyAlive)
                return (true, true, false);

            if (!playerAlive)
                return (true, false, false);

            return (false, false, false);
        }

        // A side fights on while it has any unbroken, living unit — routed survivors don't count (ADR-0005).
        private static bool HasActiveFighters(IEnumerable<CombatantState> combatants) =>
            combatants.Any(c => c.IsActive && c.Definition.MaxHp > 0);
    }
}
