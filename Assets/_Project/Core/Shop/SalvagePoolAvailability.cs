using System.Linq;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.5 edge case: whether ANY salvage-source candidate
    /// exists right now (no last-fought enemy faction yet, or that faction has no pieces
    /// registered). While empty the salvage pity counter must HOLD rather than reset or
    /// climb — SalvagePityRules only decides forcing/reset once stock exists.
    /// </summary>
    public static class SalvagePoolAvailability
    {
        /// <summary>Mirrors ShopOfferPoolBuilder's own salvage-candidate filter (a Salvage
        /// candidate must belong to the enemy faction AND not also equal the player's own —
        /// relevant while enemy content mirrors the player faction, e.g. the current demo
        /// content's IronMarch-vs-IronMarch placeholder fights).</summary>
        public static bool IsEmpty(ContentRegistry registry, string lastEnemyFactionId, string playerFactionId)
        {
            if (registry == null || string.IsNullOrEmpty(lastEnemyFactionId) || lastEnemyFactionId == playerFactionId)
                return true;

            return !HasFactionPiece(registry, ShopLane.Offensive, lastEnemyFactionId)
                && !HasFactionPiece(registry, ShopLane.Defensive, lastEnemyFactionId);
        }

        private static bool HasFactionPiece(ContentRegistry registry, ShopLane lane, string factionId) =>
            registry.GetPool(lane).Any(p => p.FactionId == factionId);
    }
}
