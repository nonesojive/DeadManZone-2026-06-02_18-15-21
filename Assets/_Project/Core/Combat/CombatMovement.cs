using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatMovement
    {
        public static int NeutralMoveCost => 2;
        public static int NormalMoveCost => 1;

        public static int GetMoveCost(GridCoord from, GridCoord to, BattlefieldLayout layout, CombatSegment segment)
        {
            if (layout.IsNeutralColumn(to.X) || layout.IsNeutralColumn(from.X))
                return NeutralMoveCost;

            return NormalMoveCost;
        }

        public static GridCoord? StepTowardTarget(
            GridCoord current,
            GridCoord target,
            BattlefieldLayout layout,
            CombatSegment segment,
            HashSet<GridCoord> blocked)
        {
            var neighbors = GetNeighbors(current, layout);
            GridCoord? best = null;
            int bestDist = int.MaxValue;

            foreach (var next in neighbors)
            {
                if (blocked.Contains(next))
                    continue;

                int dist = Manhattan(next, target);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = next;
                }
            }

            return best;
        }

        private static IEnumerable<GridCoord> GetNeighbors(GridCoord coord, BattlefieldLayout layout)
        {
            var candidates = new[]
            {
                new GridCoord(coord.X - 1, coord.Y),
                new GridCoord(coord.X + 1, coord.Y),
                new GridCoord(coord.X, coord.Y - 1),
                new GridCoord(coord.X, coord.Y + 1)
            };

            foreach (var c in candidates)
            {
                if (c.X < 0 || c.X >= layout.TotalWidth || c.Y < 0 || c.Y >= layout.Height)
                    continue;

                yield return c;
            }
        }

        private static int Manhattan(GridCoord a, GridCoord b) =>
            System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }
}
