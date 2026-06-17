namespace DeadManZone.Core.Shop
{
    public static class ShopSlotLayoutResolver
    {
        public const int BaselineSlotCount = 8;
        public const int BonusSlotCount = 4;
        public const int MaxSlotCount = 12;

        public static (int columns, int rows) GetGridShape(int slotCount)
        {
            if (slotCount <= 8)
                return (4, 2);

            return (4, 3);
        }
    }
}
