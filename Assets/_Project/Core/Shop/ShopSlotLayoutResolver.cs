namespace DeadManZone.Core.Shop
{
    public static class ShopSlotLayoutResolver
    {
        public const int GridColumns = 3;
        public const int GridRows = 3;
        public const int VisibleOfferSlotCount = 6;
        public const int ReservedSlotStartIndex = 6;
        public const int BaselineSlotCount = 9;
        public const int BonusSlotCount = 4;
        public const int MaxSlotCount = BaselineSlotCount + BonusSlotCount;

        public static bool RollsOffers(ShopSlotProfile profile) =>
            profile != null && profile.Kind != ShopSlotKind.ReservedAbility;

        /// <summary>Full shop grid footprint (includes hidden reserved row).</summary>
        public static (int columns, int rows) GetGridShape(int slotCount) =>
            (GridColumns, GridRows);

        /// <summary>Rows/columns used to size offer cards (excludes hidden ability row).</summary>
        public static (int columns, int rows) GetVisibleGridShape(int visibleOfferCount)
        {
            int count = visibleOfferCount < 1 ? 1 : visibleOfferCount;
            int rows = (count + GridColumns - 1) / GridColumns;
            int maxVisibleRows = GridRows - 1;
            if (rows > maxVisibleRows)
                rows = maxVisibleRows;

            return (GridColumns, rows);
        }
    }
}
