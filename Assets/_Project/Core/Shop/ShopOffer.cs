namespace DeadManZone.Core.Shop
{
    public sealed class ShopOffer
    {
        public string OfferId { get; init; }
        public ShopLane Lane { get; init; }
        public string PieceId { get; init; }
        public int GoldPrice { get; init; }
        public int RequisitionPrice { get; init; }
    }
}
