using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatMovementRules
    {
        /// <summary>
        /// Units keep marching until they reach their role engagement goal — not merely when an enemy enters attack range.
        /// </summary>
        public static bool ShouldAttemptMove(
            CombatantState mover,
            IReadOnlyList<CombatantState> enemies,
            GridCoord engagementGoal) =>
            mover.IsActive
            && PieceCombatRules.ParticipatesInCombat(mover.Definition)
            && mover.EffectiveMovementSpeed != 0
            && !mover.AnchorPosition.Equals(engagementGoal);

        public static bool HasEnemyInRange(CombatantState mover, IReadOnlyList<CombatantState> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive)
                    continue;

                if (CombatRange.IsInRange(mover.AnchorPosition, enemy.AnchorPosition, mover.Definition.AttackRange))
                    return true;
            }

            return false;
        }
    }
}
