using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Shop
{
    public static class ShopPoolFilter
    {
        public static PieceDefinition PickWeighted(
            IReadOnlyList<PieceDefinition> pool,
            int fightIndex,
            Rng rng,
            string playerFactionId = "iron_vanguard")
        {
            if (pool == null || pool.Count == 0)
                return null;

            float neutralWeight = GetNeutralWeight(fightIndex);
            float factionWeight = 1f - neutralWeight;
            float total = 0f;
            var weights = new float[pool.Count];

            for (int i = 0; i < pool.Count; i++)
            {
                bool isNeutral = pool[i].FactionId == "neutral";
                weights[i] = isNeutral ? neutralWeight : factionWeight;
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

        public static float GetNeutralWeight(int fightIndex) => fightIndex switch
        {
            <= 3 => 0.85f,
            <= 6 => 0.55f,
            _ => 0.25f
        };
    }
}
