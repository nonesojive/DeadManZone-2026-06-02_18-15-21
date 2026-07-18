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
        private const int FrontlineLaneBiasPenalty = 4;
        private const int RearLaneBiasPenalty = 1;

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
            int? spawnAnchorY = null,
            bool preferLaneHold = true,
            bool straightLineBias = false)
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
                spawnAnchorY,
                preferLaneHold,
                straightLineBias);
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

        /// <summary>2026-07-17 owner-diagnosed "cost-greedy into suicide" fix: with
        /// straightLineBias off (the default — normal engagement AI), tie-breaking is unchanged
        /// from before this fix (cheapest charge cost, then coordinate order) — no regression to
        /// existing lane-holding/formation behavior.
        /// With straightLineBias on (currently: a transport beelining to a registered target),
        /// a step that reduces distance to the target's SIDE of the neutral column always beats
        /// a same-side step while a crossing is still pending — the 2x neutral charge cost must
        /// not out-vote a straight line to a far-side target and detour the mover down its own
        /// side first. Once fully across, remaining X/Y distance interleaves proportionally
        /// (whichever axis has more distance left goes first, like a Bresenham line), and cost is
        /// only the final tiebreaker, same as always.</summary>
        private static GridCoord? GreedyStepTowardGoal(
            GridCoord currentAnchor,
            GridCoord goalAnchor,
            IReadOnlyList<GridCoord> shapeOffsets,
            string moverInstanceId,
            CombatOccupancyGrid occupancy,
            BattlefieldLayout layout,
            int? spawnAnchorY,
            bool preferLaneHold,
            bool straightLineBias)
        {
            int currentHeuristic = Manhattan(currentAnchor, goalAnchor);
            bool crossingPending = straightLineBias && NeedsCrossing(currentAnchor.X, goalAnchor.X, layout);
            int remainingX = System.Math.Abs(goalAnchor.X - currentAnchor.X);
            int remainingY = System.Math.Abs(goalAnchor.Y - currentAnchor.Y);

            GridCoord? best = null;
            int bestHeuristic = currentHeuristic;
            int bestCrossingRank = int.MaxValue;
            int bestAxisRemaining = int.MinValue;
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

                bool isXStep = neighbor.X != currentAnchor.X;
                int cost = GetStepCost(currentAnchor, neighbor, layout, spawnAnchorY, preferLaneHold);
                int crossingRank = crossingPending && isXStep ? 0 : 1;
                int axisRemaining = isXStep ? remainingX : remainingY;

                bool better;
                if (best == null || heuristic < bestHeuristic)
                    better = true;
                else if (heuristic > bestHeuristic)
                    better = false;
                else if (straightLineBias && crossingRank != bestCrossingRank)
                    better = crossingRank < bestCrossingRank;
                else if (straightLineBias && axisRemaining != bestAxisRemaining)
                    better = axisRemaining > bestAxisRemaining;
                else if (cost != bestCost)
                    better = cost < bestCost;
                else
                    better = CompareCoords(neighbor, best ?? neighbor) < 0;

                if (better)
                {
                    bestHeuristic = heuristic;
                    bestCrossingRank = crossingRank;
                    bestAxisRemaining = axisRemaining;
                    bestCost = cost;
                    best = neighbor;
                }
            }

            return best;
        }

        /// <summary>True while currentX still needs to travel through the neutral band to reach
        /// goalX's side — i.e. a straight-line mover isn't "across" yet. Symmetric for either
        /// direction of travel; false once currentX has fully entered goalX's half (or goalX
        /// isn't actually on the far side at all).</summary>
        private static bool NeedsCrossing(int currentX, int goalX, BattlefieldLayout layout)
        {
            if (currentX == goalX)
                return false;

            return goalX > currentX
                ? currentX < layout.EnemyOriginX && goalX >= layout.EnemyOriginX
                : currentX >= layout.NeutralStartX && goalX < layout.NeutralStartX;
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
            int? spawnAnchorY,
            bool preferLaneHold)
        {
            int cost = CombatMovement.GetStepChargeCost(from, to, layout);
            if (!spawnAnchorY.HasValue || to.Y == spawnAnchorY.Value)
                return cost;

            int laneBiasPenalty = preferLaneHold ? FrontlineLaneBiasPenalty : RearLaneBiasPenalty;
            cost += laneBiasPenalty;

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
