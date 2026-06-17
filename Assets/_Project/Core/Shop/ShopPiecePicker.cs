using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Shop
{
    public static class ShopPiecePicker
    {
        public static PieceDefinition PickWeighted(
            IReadOnlyList<PieceDefinition> pool,
            IReadOnlyList<string> preferredCombatRoles,
            float preferredRoleWeight,
            Rng rng)
        {
            if (pool == null || pool.Count == 0)
                return null;

            if (pool.Count == 1)
                return pool[0];

            float total = 0f;
            var weights = new float[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                weights[i] = MatchesPreferredRole(pool[i], preferredCombatRoles)
                    ? preferredRoleWeight
                    : 1f;
                total += weights[i];
            }

            float roll = rng.NextFloat() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return pool[i];
            }

            return pool[^1];
        }

        private static bool MatchesPreferredRole(PieceDefinition piece, IReadOnlyList<string> preferredRoles)
        {
            if (preferredRoles == null || preferredRoles.Count == 0)
                return false;

            for (int i = 0; i < preferredRoles.Count; i++)
            {
                if (PieceTagQueries.HasCombatRoleTag(piece, preferredRoles[i]))
                    return true;
            }

            return false;
        }
    }
}
