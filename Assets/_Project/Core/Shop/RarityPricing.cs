using System;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// A piece's Supplies price is derived from its rarity, not authored per piece
    /// (2026-07-13 rarity-standardized-pricing spec). One table for all categories —
    /// units, structures and economy buildings share it; building viability is tuned
    /// via yields, never via per-piece price exceptions. Moving a tier price here is
    /// the intended tuning surface.
    /// </summary>
    public static class RarityPricing
    {
        public const int Common = 10;
        public const int Uncommon = 15;
        public const int Rare = 25;

        public static int BaseCost(Rarity rarity) => rarity switch
        {
            Rarity.Common => Common,
            Rarity.Uncommon => Uncommon,
            Rarity.Rare => Rare,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity tier.")
        };
    }
}
