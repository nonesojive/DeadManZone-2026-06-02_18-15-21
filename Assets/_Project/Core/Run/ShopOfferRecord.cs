using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Run
{
    public sealed class ShopOfferRecord
    {
        public string OfferId { get; set; }
        public ShopLane Lane { get; set; }
        public int SlotIndex { get; set; }
        public string PieceId { get; set; }
        public int GoldPrice { get; set; }
        public int RequisitionPrice { get; set; }
        public bool IsSalvaged { get; set; }

        public static ShopOfferRecord FromOffer(ShopOffer offer) =>
            offer == null
                ? null
                : new ShopOfferRecord
                {
                    OfferId = offer.OfferId,
                    Lane = offer.Lane,
                    SlotIndex = offer.SlotIndex,
                    PieceId = offer.PieceId,
                    GoldPrice = offer.GoldPrice,
                    RequisitionPrice = offer.RequisitionPrice,
                    IsSalvaged = offer.IsSalvaged
                };

        public ShopOffer ToOffer() =>
            new ShopOffer
            {
                OfferId = OfferId,
                Lane = Lane,
                SlotIndex = SlotIndex,
                PieceId = PieceId,
                GoldPrice = GoldPrice,
                RequisitionPrice = RequisitionPrice,
                IsSalvaged = IsSalvaged
            };
    }
}
