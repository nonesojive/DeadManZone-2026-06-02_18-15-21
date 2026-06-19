using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatFormationSlots
    {
        private static readonly int[] BlockedFallbackOffsets = { -1, 1, -2, 2, -3, 3 };

        public static GridCoord ResolveFrontlineGoal(
            CombatantState mover,
            IReadOnlyList<CombatantState> frontlineAllies,
            IReadOnlyList<CombatantState> enemies,
            BattlefieldLayout layout)
        {
            if (mover == null || enemies == null || enemies.Count == 0 || layout == null)
                return mover?.AnchorPosition ?? default;

            int enemyFrontX = GetFrontColumnX(enemies[0].Side, enemies);
            int contactX = mover.Side == CombatSide.Player ? enemyFrontX - 1 : enemyFrontX + 1;
            int preferredLaneY = mover.SpawnAnchorY;
            var preferredGoal = new GridCoord(contactX, preferredLaneY);
            bool preferredBlocked = IsBlockedByFriendlyAnchor(preferredGoal, mover.InstanceId, frontlineAllies);

            int laneY = preferredBlocked
                ? preferredLaneY
                : ResolveLaneY(mover, frontlineAllies);
            var goal = new GridCoord(contactX, laneY);

            if (IsCandidateOpen(goal, mover.InstanceId, frontlineAllies, layout))
                return goal;

            for (int i = 0; i < BlockedFallbackOffsets.Length; i++)
            {
                var candidate = new GridCoord(contactX, laneY + BlockedFallbackOffsets[i]);
                if (IsCandidateOpen(candidate, mover.InstanceId, frontlineAllies, layout))
                    return candidate;
            }

            return mover.AnchorPosition;
        }

        public static int ResolveLaneY(CombatantState mover, IReadOnlyList<CombatantState> frontlineAllies)
        {
            if (mover == null)
                return 0;

            int preferred = mover.SpawnAnchorY;
            if (frontlineAllies == null)
                return preferred;

            for (int i = 0; i < frontlineAllies.Count; i++)
            {
                var ally = frontlineAllies[i];
                if (ally == null || !ally.IsAlive || ally.InstanceId == mover.InstanceId)
                    continue;
                if (ally.SpawnAnchorY != preferred)
                    continue;
                if (string.CompareOrdinal(ally.InstanceId, mover.InstanceId) < 0)
                    return preferred + 1;
            }

            return preferred;
        }

        public static int ResolveRearSpreadY(int slotIndex, int slotCount, int friendlyMinY, int friendlyMaxY)
        {
            if (friendlyMinY > friendlyMaxY)
            {
                int swap = friendlyMinY;
                friendlyMinY = friendlyMaxY;
                friendlyMaxY = swap;
            }

            if (slotCount <= 1)
                return (friendlyMinY + friendlyMaxY) / 2;

            int clampedIndex = Math.Clamp(slotIndex, 0, slotCount - 1);
            float t = clampedIndex / (float)(slotCount - 1);
            return friendlyMinY + (int)Math.Round(t * (friendlyMaxY - friendlyMinY));
        }

        private static bool IsCandidateOpen(
            GridCoord candidate,
            string moverInstanceId,
            IReadOnlyList<CombatantState> allies,
            BattlefieldLayout layout)
        {
            if (!IsInLayout(candidate, layout))
                return false;

            return !IsBlockedByFriendlyAnchor(candidate, moverInstanceId, allies);
        }

        private static bool IsInLayout(GridCoord candidate, BattlefieldLayout layout) =>
            candidate.Y >= 0
            && candidate.Y < layout.Height
            && candidate.X >= 0
            && candidate.X < layout.TotalWidth;

        private static bool IsBlockedByFriendlyAnchor(
            GridCoord goal,
            string moverInstanceId,
            IReadOnlyList<CombatantState> allies)
        {
            if (allies == null)
                return false;

            for (int i = 0; i < allies.Count; i++)
            {
                var ally = allies[i];
                if (ally == null || !ally.IsAlive || ally.InstanceId == moverInstanceId)
                    continue;
                if (ally.AnchorPosition.Equals(goal))
                    return true;
            }

            return false;
        }

        private static int GetFrontColumnX(CombatSide side, IReadOnlyList<CombatantState> combatants)
        {
            int frontColumn = side == CombatSide.Player ? int.MinValue : int.MaxValue;
            bool found = false;

            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (combatant == null || !combatant.IsAlive)
                    continue;

                int x = combatant.AnchorPosition.X;
                if (side == CombatSide.Player)
                {
                    if (x > frontColumn)
                        frontColumn = x;
                }
                else
                {
                    if (x < frontColumn)
                        frontColumn = x;
                }

                found = true;
            }

            return found ? frontColumn : 0;
        }
    }
}
