using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public static class CombatTargeting
    {
        public static CombatantState SelectTarget(
            CombatantState attacker,
            IReadOnlyList<CombatantState> enemies,
            StanceType stance)
        {
            var alive = enemies.Where(e => e.IsAlive).ToList();
            if (alive.Count == 0)
                return null;

            var inRange = alive
                .Where(e => CombatRange.IsInRange(attacker.Position, e.Position, attacker.Definition.AttackRange))
                .ToList();
            if (inRange.Count == 0)
                return null;

            return stance switch
            {
                StanceType.FocusWeakest => inRange.OrderBy(e => e.CurrentHp).ThenBy(e => e.InstanceId).First(),
                StanceType.HoldTheLine => inRange
                    .OrderByDescending(e => e.Definition.Category == PieceCategory.Building)
                    .ThenBy(e => e.CurrentHp)
                    .ThenBy(e => e.InstanceId)
                    .First(),
                StanceType.SupportPriority => inRange
                    .OrderByDescending(e => e.Definition.Category == PieceCategory.Building)
                    .ThenByDescending(e => e.CurrentHp)
                    .ThenBy(e => e.InstanceId)
                    .First(),
                StanceType.AllOutAssault => inRange.OrderByDescending(e => e.CurrentHp).ThenBy(e => e.InstanceId).First(),
                _ => inRange.OrderBy(e => e.InstanceId).First()
            };
        }
    }
}
