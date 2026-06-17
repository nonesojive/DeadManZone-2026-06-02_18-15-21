using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    public static class ShopOfferPoolBuilder
    {
        public static List<PieceDefinition> BuildCandidates(
            ContentRegistry registry,
            ShopPoolBias poolBias,
            ShopOfferSource source,
            string playerFactionId,
            string lastEnemyFactionId)
        {
            var pool = registry.GetPool(poolBias.ToShopLane());
            if (pool.Count == 0)
                return new List<PieceDefinition>();

            string factionFilter = ResolveFactionFilter(source, playerFactionId, lastEnemyFactionId);
            if (factionFilter == null)
                return new List<PieceDefinition>();

            return pool
                .Where(p => p.FactionId == factionFilter)
                .Where(p => source != ShopOfferSource.Salvage || p.FactionId != playerFactionId)
                .ToList();
        }

        private static string ResolveFactionFilter(
            ShopOfferSource source,
            string playerFactionId,
            string lastEnemyFactionId) =>
            source switch
            {
                ShopOfferSource.Neutral => "neutral",
                ShopOfferSource.Faction => playerFactionId,
                ShopOfferSource.Salvage => string.IsNullOrEmpty(lastEnemyFactionId) ? null : lastEnemyFactionId,
                _ => playerFactionId
            };
    }
}
