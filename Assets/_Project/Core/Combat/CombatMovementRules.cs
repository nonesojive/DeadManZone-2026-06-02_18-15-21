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
            mover.IsAlive
            && PieceCombatRules.ParticipatesInCombat(mover.Definition)
            && mover.Definition.MovementSpeed != 0
            && !mover.AnchorPosition.Equals(engagementGoal);

        public static bool HasEnemyInRange(CombatantState mover, IReadOnlyList<CombatantState> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                if (CombatRange.IsInRange(mover.AnchorPosition, enemy.AnchorPosition, mover.Definition.AttackRange))
                    return true;
            }

            return false;
        }

        public static GridCoord SelectNearestEnemyPosition(GridCoord from, IReadOnlyList<CombatantState> enemies) =>
            enemies
                .Where(e => e.IsAlive)
                .OrderBy(e => CombatRange.Manhattan(from, e.AnchorPosition))
                .ThenBy(e => e.InstanceId)
                .First()
                .AnchorPosition;
    }
}
