using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// A* pathfinding on anchor positions with full footprint validation.
    /// Returns only the first step toward the goal; if A* cannot reach the goal within
    /// its iteration budget, falls back to one greedy step that reduces Manhattan distance.
    /// </summary>
    public static class ShapePathfinder
    {
        private const int MaxIterations = 2048;

        private static readonly GridCoord[] NeighborDeltas =
        {
            new(-1, 0),
            new(1, 0),
            new(0, -1),
            new(0, 1)
        };

        public static GridCoord? FindStep(
            GridCoord currentAnchor,
            GridCoord goalAnchor,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout)
        {
            if (currentAnchor.Equals(goalAnchor))
                return null;

            if (TryFindPath(currentAnchor, goalAnchor, shapeOffsets, moverInstanceId, occupancy, layout, out var path)
                && path.Count >= 2)
            {
                return path[1];
            }

            return GreedyFallbackStep(
                currentAnchor,
                goalAnchor,
                shapeOffsets,
                moverInstanceId,
                occupancy,
                layout);
        }

        private static bool TryFindPath(
            GridCoord start,
            GridCoord goal,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout,
            out List<GridCoord> path)
        {
            path = null;

            var openSet = new List<GridCoord> { start };
            var cameFrom = new Dictionary<GridCoord, GridCoord>();
            var gScore = new Dictionary<GridCoord, int> { [start] = 0 };
            var closedSet = new HashSet<GridCoord>();

            int iterations = 0;
            while (openSet.Count > 0 && iterations++ < MaxIterations)
            {
                openSet.Sort(CompareOpenSetNodes(goal, gScore));
                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.Equals(goal))
                {
                    path = ReconstructPath(cameFrom, start, goal);
                    return true;
                }

                closedSet.Add(current);

                foreach (var neighbor in GetValidNeighbors(
                             current,
                             shapeOffsets,
                             moverInstanceId,
                             occupancy,
                             layout))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    int tentativeG = gScore[current]
                        + CombatMovement.GetStepChargeCost(current, neighbor, layout);

                    if (gScore.TryGetValue(neighbor, out var existingG) && tentativeG >= existingG)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }

            return false;
        }

        private static GridCoord? GreedyFallbackStep(
            GridCoord currentAnchor,
            GridCoord goalAnchor,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout)
        {
            int currentHeuristic = Manhattan(currentAnchor, goalAnchor);
            GridCoord? best = null;
            int bestHeuristic = currentHeuristic;

            foreach (var neighbor in GetValidNeighbors(
                         currentAnchor,
                         shapeOffsets,
                         moverInstanceId,
                         occupancy,
                         layout))
            {
                int heuristic = Manhattan(neighbor, goalAnchor);
                if (heuristic > bestHeuristic)
                    continue;

                if (best == null
                    || heuristic < bestHeuristic
                    || CompareCoords(neighbor, best.Value) < 0)
                {
                    bestHeuristic = heuristic;
                    best = neighbor;
                }
            }

            return best;
        }

        private static IEnumerable<GridCoord> GetValidNeighbors(
            GridCoord anchor,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout)
        {
            foreach (var delta in NeighborDeltas)
            {
                var neighbor = new GridCoord(anchor.X + delta.X, anchor.Y + delta.Y);
                if (!occupancy.CanPlace(moverInstanceId, neighbor, shapeOffsets, layout))
                    continue;

                yield return neighbor;
            }
        }

        private static List<GridCoord> ReconstructPath(
            IReadOnlyDictionary<GridCoord, GridCoord> cameFrom,
            GridCoord start,
            GridCoord goal)
        {
            var path = new List<GridCoord> { goal };
            var current = goal;

            while (!current.Equals(start))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static System.Comparison<GridCoord> CompareOpenSetNodes(
            GridCoord goal,
            IReadOnlyDictionary<GridCoord, int> gScore)
        {
            return (a, b) =>
            {
                int fA = gScore.GetValueOrDefault(a, int.MaxValue) + Manhattan(a, goal);
                int fB = gScore.GetValueOrDefault(b, int.MaxValue) + Manhattan(b, goal);
                if (fA != fB)
                    return fA.CompareTo(fB);

                int hA = Manhattan(a, goal);
                int hB = Manhattan(b, goal);
                if (hA != hB)
                    return hA.CompareTo(hB);

                int gA = gScore.GetValueOrDefault(a, int.MaxValue);
                int gB = gScore.GetValueOrDefault(b, int.MaxValue);
                if (gA != gB)
                    return gA.CompareTo(gB);

                return CompareCoords(a, b);
            };
        }

        private static int CompareCoords(GridCoord a, GridCoord b)
        {
            if (a.X != b.X)
                return a.X.CompareTo(b.X);

            return a.Y.CompareTo(b.Y);
        }

        private static int Manhattan(GridCoord a, GridCoord b) =>
            System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }
}
