namespace DeadManZone.Core.Shop
{
    public sealed class ShopSlotDefinition
    {
        public int SlotIndex { get; init; }
        public ShopSlotKind Kind { get; init; }
        public ShopLane PoolLane { get; init; }

        public ShopSlotDefinition(int slotIndex, ShopSlotKind kind, ShopLane poolLane)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            PoolLane = poolLane;
        }
    }
}
