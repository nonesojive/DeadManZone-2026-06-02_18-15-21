using DeadManZone.Core;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Shop
{
    /// <summary>Computes resource refunds when selling pieces during the build phase.</summary>
    public static class SalvageCalculator
    {
        public const float SuppliesRefundRatio = 0.5f;
        public const float AuthorityRefundRatio = 0.5f;
        public const float ManpowerRefundRatio = 0.25f;

        public readonly struct SalvageRefund
        {
            public int Supplies { get; init; }
            public int Authority { get; init; }
            public int Manpower { get; init; }
        }

        /// <summary>2026-07-15 faction-roster-v1 §1.4: a mercenary sells for 0 across the
        /// board — Supplies, Authority, AND Manpower, full stop (the merc surcharge already
        /// paid the premium; there's no salvage value left to recoup).</summary>
        public static SalvageRefund Compute(PieceDefinition piece, string factionId = null, bool isMercenary = false)
        {
            if (isMercenary)
                return default;

            int supplies = (int)(RarityPricing.BaseCost(piece.Rarity) * SuppliesRefundRatio);
            int authority = (int)(piece.RequisitionCost * AuthorityRefundRatio);
            int manpower = 0;

            // Faction salvage bonus: Dust Scourge gets +25% supplies from salvage.
            if (factionId == FactionIds.DustScourge)
                supplies = (int)(supplies * 1.25f);

            return new SalvageRefund
            {
                Supplies = supplies,
                Authority = authority,
                Manpower = manpower
            };
        }
    }
}
