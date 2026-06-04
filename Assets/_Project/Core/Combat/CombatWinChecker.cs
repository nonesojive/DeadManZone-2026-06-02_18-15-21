using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatWinChecker
    {
        public static (bool fightOver, bool playerWon, bool isDraw) Evaluate(
            IReadOnlyList<CombatantState> playerCombatants,
            IReadOnlyList<CombatantState> enemyCombatants)
        {
            bool playerHasHq = playerCombatants.Any(c => c.HasTag(GameTags.Hq));
            bool enemyHasHq = enemyCombatants.Any(c => c.HasTag(GameTags.Hq));

            if (enemyHasHq && !HasLivingTag(enemyCombatants, GameTags.Hq))
                return (true, true, false);

            if (playerHasHq && !HasLivingTag(playerCombatants, GameTags.Hq))
                return (true, false, false);

            bool enemyCombatantsAlive = HasLivingTag(enemyCombatants, GameTags.Combatant);
            bool playerCombatantsAlive = HasLivingTag(playerCombatants, GameTags.Combatant);

            if (!enemyCombatantsAlive && !playerCombatantsAlive)
                return (true, true, true);

            if (!enemyCombatantsAlive)
                return (true, true, false);

            if (!playerCombatantsAlive)
                return (true, false, false);

            return (false, false, false);
        }

        private static bool HasLivingTag(IEnumerable<CombatantState> combatants, string tag) =>
            combatants.Any(c => c.IsAlive && c.HasTag(tag));
    }
}
