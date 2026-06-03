using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatWinChecker
    {
        public static (bool fightOver, bool playerWon) Evaluate(
            IReadOnlyList<CombatantState> playerCombatants,
            IReadOnlyList<CombatantState> enemyCombatants)
        {
            bool playerHasHq = playerCombatants.Any(c => c.HasTag(GameTags.Hq));
            bool enemyHasHq = enemyCombatants.Any(c => c.HasTag(GameTags.Hq));

            if (enemyHasHq && !HasLivingTag(enemyCombatants, GameTags.Hq))
                return (true, true);

            if (playerHasHq && !HasLivingTag(playerCombatants, GameTags.Hq))
                return (true, false);

            bool enemyCombatantsAlive = HasLivingTag(enemyCombatants, GameTags.Combatant);
            bool playerCombatantsAlive = HasLivingTag(playerCombatants, GameTags.Combatant);

            if (!enemyCombatantsAlive)
                return (true, true);

            if (!playerCombatantsAlive)
                return (true, false);

            return (false, false);
        }

        private static bool HasLivingTag(IEnumerable<CombatantState> combatants, string tag) =>
            combatants.Any(c => c.IsAlive && c.HasTag(tag));
    }
}
