using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Computes role-specific movement goal anchors (Top Troops engagement positions).
    /// </summary>
    public static class RoleEngagement
    {
        public static GridCoord ComputeGoal(
            CombatantState combatant,
            IReadOnlyList<CombatantState> allies,
            IReadOnlyList<CombatantState> enemies,
            BattlefieldLayout layout)
        {
            if (combatant == null || combatant.Definition == null)
                return combatant?.AnchorPosition ?? default;

            var aliveEnemies = CollectAlive(enemies);
            if (aliveEnemies.Count == 0)
                return combatant.AnchorPosition;

            var role = combatant.Definition.CombatRole;
            var bias = CombatRoleProfile.ResolveBias(role);

            if (IsInfantryPrimary(combatant) || role == GameTagIds.Assault || bias == CombatRoleTargetingBias.NearestFront)
                return NearestFrontEnemyGoal(combatant.AnchorPosition, aliveEnemies);

            if (role == GameTagIds.Artillery || bias == CombatRoleTargetingBias.Furthest)
                return ArtilleryGoal(combatant, allies, aliveEnemies);

            if (role == GameTagIds.Sniper)
                return SniperGoal(combatant, aliveEnemies);

            if (role == GameTagIds.Support || bias == CombatRoleTargetingBias.LowestMaxHpRearPreferred)
                return SupportGoal(combatant, allies);

            if (bias == CombatRoleTargetingBias.NoAttack)
                return combatant.AnchorPosition;

            return NearestEnemyGoal(combatant.AnchorPosition, aliveEnemies);
        }

        private static bool IsInfantryPrimary(CombatantState combatant) =>
            string.Equals(combatant.Definition.Primary, GameTagIds.Infantry, StringComparison.OrdinalIgnoreCase)
            || combatant.HasTag(GameTagIds.Infantry);

        private static GridCoord NearestFrontEnemyGoal(GridCoord from, IReadOnlyList<CombatantState> enemies)
        {
            int frontColumn = GetEnemyFrontColumnX(enemies);
            var frontEnemies = FilterByColumn(enemies, frontColumn);
            return SelectNearestPosition(from, frontEnemies);
        }

        private static GridCoord ArtilleryGoal(
            CombatantState combatant,
            IReadOnlyList<CombatantState> allies,
            IReadOnlyList<CombatantState> enemies)
        {
            var nearestEnemy = SelectNearestCombatant(combatant.AnchorPosition, enemies);
            if (nearestEnemy == null)
                return combatant.AnchorPosition;

            int maxRange = CombatRange.GetRangeCells(combatant.Definition.AttackRange);
            var aliveAllies = CollectAlive(allies, combatant.InstanceId);
            int friendlyFront = aliveAllies.Count > 0
                ? GetFriendlyFrontColumnX(combatant.Side, aliveAllies)
                : combatant.AnchorPosition.X;
            int goalX = combatant.Side == CombatSide.Player
                ? System.Math.Min(nearestEnemy.AnchorPosition.X - maxRange, friendlyFront)
                : System.Math.Max(nearestEnemy.AnchorPosition.X + maxRange, friendlyFront);

            return new GridCoord(goalX, combatant.AnchorPosition.Y);
        }

        private static GridCoord SniperGoal(CombatantState combatant, IReadOnlyList<CombatantState> enemies)
        {
            var inRange = FilterInRange(combatant, enemies);
            if (inRange.Count > 0)
                return SelectLowestMaxHpRearPreferred(inRange).AnchorPosition;

            int rearColumn = GetEnemyRearColumnX(enemies);
            var rearEnemies = FilterByColumn(enemies, rearColumn);
            if (rearEnemies.Count > 0)
                return SelectNearestPosition(combatant.AnchorPosition, rearEnemies);

            return NearestEnemyGoal(combatant.AnchorPosition, enemies);
        }

        private static GridCoord SupportGoal(CombatantState combatant, IReadOnlyList<CombatantState> allies)
        {
            var aliveAllies = CollectAlive(allies, combatant.InstanceId);
            if (aliveAllies.Count == 0)
                return combatant.AnchorPosition;

            int friendlyFront = GetFriendlyFrontColumnX(combatant.Side, aliveAllies);
            bool isBehindFront = combatant.Side == CombatSide.Player
                ? combatant.AnchorPosition.X <= friendlyFront
                : combatant.AnchorPosition.X >= friendlyFront;
            if (isBehindFront)
                return combatant.AnchorPosition;

            int rearColumn = GetFriendlyRearColumnX(combatant.Side, aliveAllies);
            return new GridCoord(rearColumn, combatant.AnchorPosition.Y);
        }

        private static GridCoord NearestEnemyGoal(GridCoord from, IReadOnlyList<CombatantState> enemies) =>
            SelectNearestCombatant(from, enemies).AnchorPosition;

        private static List<CombatantState> CollectAlive(
            IReadOnlyList<CombatantState> combatants,
            string excludeInstanceId = null)
        {
            var alive = new List<CombatantState>(combatants?.Count ?? 0);
            if (combatants == null)
                return alive;

            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (combatant == null || !combatant.IsAlive)
                    continue;

                if (excludeInstanceId != null && combatant.InstanceId == excludeInstanceId)
                    continue;

                alive.Add(combatant);
            }

            return alive;
        }

        private static List<CombatantState> FilterInRange(CombatantState combatant, IReadOnlyList<CombatantState> enemies)
        {
            var inRange = new List<CombatantState>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (CombatRange.IsInRange(
                        combatant.AnchorPosition,
                        enemy.AnchorPosition,
                        combatant.Definition.AttackRange))
                {
                    inRange.Add(enemy);
                }
            }

            return inRange;
        }

        private static List<CombatantState> FilterByColumn(IReadOnlyList<CombatantState> combatants, int columnX)
        {
            var filtered = new List<CombatantState>(combatants.Count);
            for (int i = 0; i < combatants.Count; i++)
            {
                if (combatants[i].AnchorPosition.X == columnX)
                    filtered.Add(combatants[i]);
            }

            return filtered;
        }

        private static CombatantState SelectNearestCombatant(GridCoord from, IReadOnlyList<CombatantState> candidates)
        {
            CombatantState best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                int distance = CombatRange.Manhattan(from, candidate.AnchorPosition);
                if (best == null
                    || distance < bestDistance
                    || (distance == bestDistance && CompareByInstanceId(candidate, best) < 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static GridCoord SelectNearestPosition(GridCoord from, IReadOnlyList<CombatantState> candidates) =>
            SelectNearestCombatant(from, candidates).AnchorPosition;

        private static CombatantState SelectLowestMaxHpRearPreferred(IReadOnlyList<CombatantState> candidates)
        {
            int rearColumn = GetEnemyRearColumnX(candidates);
            CombatantState bestRear = null;
            CombatantState bestOverall = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (IsBetterLowMaxHp(candidate, bestOverall))
                    bestOverall = candidate;

                if (candidate.AnchorPosition.X == rearColumn && IsBetterLowMaxHp(candidate, bestRear))
                    bestRear = candidate;
            }

            return bestRear ?? bestOverall;
        }

        private static bool IsBetterLowMaxHp(CombatantState candidate, CombatantState currentBest)
        {
            if (candidate == null)
                return false;

            if (currentBest == null)
                return true;

            int candidateMaxHp = candidate.Definition?.MaxHp ?? int.MaxValue;
            int currentBestMaxHp = currentBest.Definition?.MaxHp ?? int.MaxValue;
            if (candidateMaxHp != currentBestMaxHp)
                return candidateMaxHp < currentBestMaxHp;

            return CompareByInstanceId(candidate, currentBest) < 0;
        }

        private static int GetEnemyFrontColumnX(IReadOnlyList<CombatantState> enemies) =>
            GetFrontColumnX(enemies[0].Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player, enemies);

        private static int GetEnemyRearColumnX(IReadOnlyList<CombatantState> enemies) =>
            GetRearColumnX(enemies[0].Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player, enemies);

        private static int GetFriendlyFrontColumnX(CombatSide side, IReadOnlyList<CombatantState> allies) =>
            GetFrontColumnX(side, allies);

        private static int GetFriendlyRearColumnX(CombatSide side, IReadOnlyList<CombatantState> allies) =>
            GetRearColumnX(side, allies);

        private static int GetFrontColumnX(CombatSide side, IReadOnlyList<CombatantState> combatants)
        {
            int frontColumn = side == CombatSide.Player ? int.MinValue : int.MaxValue;
            for (int i = 0; i < combatants.Count; i++)
            {
                int x = combatants[i].AnchorPosition.X;
                if (side == CombatSide.Player)
                {
                    if (x > frontColumn)
                        frontColumn = x;
                }
                else if (x < frontColumn)
                {
                    frontColumn = x;
                }
            }

            return frontColumn;
        }

        private static int GetRearColumnX(CombatSide side, IReadOnlyList<CombatantState> combatants)
        {
            int rearColumn = side == CombatSide.Player ? int.MaxValue : int.MinValue;
            for (int i = 0; i < combatants.Count; i++)
            {
                int x = combatants[i].AnchorPosition.X;
                if (side == CombatSide.Player)
                {
                    if (x < rearColumn)
                        rearColumn = x;
                }
                else if (x > rearColumn)
                {
                    rearColumn = x;
                }
            }

            return rearColumn;
        }

        private static int CompareByInstanceId(CombatantState left, CombatantState right) =>
            StringComparer.Ordinal.Compare(left?.InstanceId ?? string.Empty, right?.InstanceId ?? string.Empty);
    }
}
