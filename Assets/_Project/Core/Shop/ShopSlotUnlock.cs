namespace DeadManZone.Core.Shop
{
    public readonly struct ShopSlotUnlock
    {
        public int SlotIndex { get; init; }
        public ShopSlotProfile Profile { get; init; }
    }
}
