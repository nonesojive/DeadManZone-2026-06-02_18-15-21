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

            return stance switch
            {
                StanceType.FocusWeakest => alive.OrderBy(e => e.CurrentHp).ThenBy(e => e.InstanceId).First(),
                StanceType.HoldTheLine => alive
                    .OrderByDescending(e => e.Definition.Category == PieceCategory.Building)
                    .ThenBy(e => e.CurrentHp)
                    .ThenBy(e => e.InstanceId)
                    .First(),
                StanceType.SupportPriority => alive
                    .OrderByDescending(e => e.Definition.Category == PieceCategory.Building)
                    .ThenByDescending(e => e.CurrentHp)
                    .ThenBy(e => e.InstanceId)
                    .First(),
                StanceType.AllOutAssault => alive.OrderByDescending(e => e.CurrentHp).ThenBy(e => e.InstanceId).First(),
                _ => alive.OrderBy(e => e.InstanceId).First()
            };
        }
    }
}
