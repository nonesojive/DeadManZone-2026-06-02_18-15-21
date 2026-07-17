using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.9/§2.4: candidate pool for the Cartel mercenary shop
    /// slot — any off-faction FIGHTER (not building/structure), drawn from the whole
    /// registry rather than gated on the last-fought enemy (mercs are a standing contract
    /// offer, not battlefield spoils — that's the distinction from the salvage source).
    /// </summary>
    public static class MercenaryPoolBuilder
    {
        public static List<PieceDefinition> BuildCandidates(ContentRegistry registry, string playerFactionId) =>
            registry.GetPool(ShopLane.Offensive)
                .Concat(registry.GetPool(ShopLane.Defensive))
                .Where(p => IsEligible(p, playerFactionId))
                .ToList();

        private static bool IsEligible(PieceDefinition piece, string playerFactionId) =>
            piece != null
            && piece.FactionId != playerFactionId
            && piece.FactionId != OffFactionRules.NeutralFactionId
            && OffFactionRules.IsFighter(piece);
    }
}
