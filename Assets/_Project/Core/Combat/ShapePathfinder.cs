using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Footprint-aware pathfinding for one combat tick step.
    /// Uses greedy movement when unobstructed, otherwise bounded BFS (no per-tick full A* sort).
    /// </summary>
    public static class ShapePathfinder
    {
        private const int MaxBfsExpansions = 128;
        private const int LaneBiasPenalty = 2;

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
            BattlefieldLayout layout,
            int? spawnAnchorY = null)
        {
            if (currentAnchor.Equals(goalAnchor))
                return null;

            var greedy = GreedyStepTowardGoal(
                currentAnchor,
                goalAnchor,
                shapeOffsets,
                moverInstanceId,
                occupancy,
                layout,
                spawnAnchorY);
            if (greedy != null)
                return greedy;

            if (TryBfsFirstStep(
                    currentAnchor,
                    goalAnchor,
                    shapeOffsets,
                    moverInstanceId,
                    occupancy,
                    layout,
                    out var bfsStep))
            {
                return bfsStep;
            }

            return null;
        }

        private static GridCoord? GreedyStepTowardGoal(
            GridCoord currentAnchor,
            GridCoord goalAnchor,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout,
            int? spawnAnchorY)
        {
            int currentHeuristic = Manhattan(currentAnchor, goalAnchor);
            GridCoord? best = null;
            int bestHeuristic = currentHeuristic;
            int bestCost = int.MaxValue;

            foreach (var neighbor in GetValidNeighbors(
                         currentAnchor,
                         shapeOffsets,
                         moverInstanceId,
                         occupancy,
                         layout))
            {
                int heuristic = Manhattan(neighbor, goalAnchor);
                if (heuristic >= currentHeuristic)
                    continue;

                int cost = GetStepCost(currentAnchor, neighbor, layout, spawnAnchorY);
                if (heuristic < bestHeuristic
                    || (heuristic == bestHeuristic && cost < bestCost)
                    || (heuristic == bestHeuristic && cost == bestCost && CompareCoords(neighbor, best ?? neighbor) < 0))
                {
                    bestHeuristic = heuristic;
                    bestCost = cost;
                    best = neighbor;
                }
            }

            return best;
        }

        private static bool TryBfsFirstStep(
            GridCoord start,
            GridCoord goal,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout,
            out GridCoord? firstStep)
        {
            firstStep = null;

            var queue = new Queue<GridCoord>();
            var cameFrom = new Dictionary<GridCoord, GridCoord>();
            var visited = new HashSet<GridCoord> { start };

            queue.Enqueue(start);
            int expansions = 0;

            while (queue.Count > 0 && expansions++ < MaxBfsExpansions)
            {
                var current = queue.Dequeue();
                if (current.Equals(goal))
                {
                    firstStep = FirstStepOnPath(cameFrom, start, goal);
                    return firstStep != null;
                }

                foreach (var neighbor in GetValidNeighbors(
                             current,
                             shapeOffsets,
                             moverInstanceId,
                             occupancy,
                             layout))
                {
                    if (!visited.Add(neighbor))
                        continue;

                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private static GridCoord? FirstStepOnPath(
            IReadOnlyDictionary<GridCoord, GridCoord> cameFrom,
            GridCoord start,
            GridCoord goal)
        {
            if (start.Equals(goal) || !cameFrom.ContainsKey(goal))
                return null;

            var current = goal;
            while (true)
            {
                var previous = cameFrom[current];
                if (previous.Equals(start))
                    return current;

                current = previous;
            }
        }

        private static int GetStepCost(
            GridCoord from,
            GridCoord to,
            BattlefieldLayout layout,
            int? spawnAnchorY)
        {
            int cost = CombatMovement.GetStepChargeCost(from, to, layout);
            if (spawnAnchorY.HasValue && System.Math.Abs(to.Y - spawnAnchorY.Value) > 1)
                cost += LaneBiasPenalty;

            return cost;
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
