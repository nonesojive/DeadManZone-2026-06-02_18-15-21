namespace DeadManZone.Core.Shop
{
    public sealed class ShopOffer
    {
        public string OfferId { get; set; }
        public ShopLane Lane { get; set; }
        public string PieceId { get; set; }
        public int GoldPrice { get; set; }
        public int RequisitionPrice { get; set; }
    }
}
