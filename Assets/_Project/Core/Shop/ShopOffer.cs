namespace DeadManZone.Core.Shop
{
    public sealed class ShopOffer
    {
        public string OfferId { get; set; }
        public ShopLane Lane { get; set; }
        public ShopSlotKind SlotKind { get; set; }
        public int SlotIndex { get; set; }
        public string PieceId { get; set; }
        public int GoldPrice { get; set; }
        public int RequisitionPrice { get; set; }
        public bool IsSalvaged { get; set; }

        /// <summary>2026-07-16 faction-roster-v1 §1.4/§1.9: true for offers rolled by the
        /// Cartel mercenary slot (CartelMercenarySlotProvider). Distinct from IsSalvaged —
        /// a merc offer is never a "salvage" source roll. Carried onto the acquired
        /// PlacedPiece.IsMercenary at purchase.</summary>
        public bool IsMercenary { get; set; }
    }
}
