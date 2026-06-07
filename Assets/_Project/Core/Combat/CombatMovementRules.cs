using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatMovementRules
    {
        public static bool ShouldAttemptMove(CombatantState mover, IReadOnlyList<CombatantState> enemies) =>
            mover.IsAlive
            && mover.HasTag(GameTagIds.Combatant)
            && mover.Definition.MovementSpeed != MovementSpeedTier.None
            && !HasEnemyInRange(mover, enemies);

        public static bool HasEnemyInRange(CombatantState mover, IReadOnlyList<CombatantState> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                if (CombatRange.IsInRange(mover.Position, enemy.Position, mover.Definition.AttackRange))
                    return true;
            }

            return false;
        }

        public static GridCoord SelectNearestEnemyPosition(GridCoord from, IReadOnlyList<CombatantState> enemies) =>
            enemies
                .Where(e => e.IsAlive)
                .OrderBy(e => CombatRange.Manhattan(from, e.Position))
                .ThenBy(e => e.InstanceId)
                .First()
                .Position;
    }
}
