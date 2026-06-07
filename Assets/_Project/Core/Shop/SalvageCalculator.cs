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

        public static SalvageRefund Compute(PieceDefinition piece, string factionId = null)
        {
            int supplies = (int)(piece.GoldCost * SuppliesRefundRatio);
            int authority = (int)(piece.RequisitionCost * AuthorityRefundRatio);
            int manpower = (int)(piece.ManpowerCost * ManpowerRefundRatio);

            // Faction salvage bonus: Dust Scourge gets +25% supplies from salvage.
            if (factionId == "dust_scourge")
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
