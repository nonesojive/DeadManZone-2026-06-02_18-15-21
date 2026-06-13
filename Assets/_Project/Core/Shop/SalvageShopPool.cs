using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    public static class SalvageShopPool
    {
        public static IReadOnlyList<PieceDefinition> GetPool(
            ContentRegistry registry,
            ShopLane lane,
            string lastEnemyFactionId,
            string playerFactionId,
            int fightIndex)
        {
            return registry.GetPool(lane)
                .Where(p => p.FactionId == lastEnemyFactionId)
                .Where(p => p.FactionId != playerFactionId)
                .ToList();
        }
    }
}
