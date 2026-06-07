using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatWinChecker
    {
        public static (bool fightOver, bool playerWon, bool isDraw) Evaluate(
            IReadOnlyList<CombatantState> playerCombatants,
            IReadOnlyList<CombatantState> enemyCombatants)
        {
            bool playerHasHq = playerCombatants.Any(c => c.HasTag(GameTagIds.Hq));
            bool enemyHasHq = enemyCombatants.Any(c => c.HasTag(GameTagIds.Hq));

            if (enemyHasHq && !HasLivingTag(enemyCombatants, GameTagIds.Hq))
                return (true, true, false);

            if (playerHasHq && !HasLivingTag(playerCombatants, GameTagIds.Hq))
                return (true, false, false);

            bool enemyCombatantsAlive = HasLivingTag(enemyCombatants, GameTagIds.Combatant);
            bool playerCombatantsAlive = HasLivingTag(playerCombatants, GameTagIds.Combatant);

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
