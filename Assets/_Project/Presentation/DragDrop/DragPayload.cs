using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;

namespace DeadManZone.Presentation.DragDrop
{
    public enum DragSourceKind
    {
        ShopOffer,
        BenchPiece,
        BoardPiece
    }

    public sealed class DragPayload
    {
        public DragSourceKind SourceKind { get; set; }
        public string OfferId { get; set; }
        public string PieceId { get; set; }
        public int BenchIndex { get; set; } = -1;
        public string BoardInstanceId { get; set; }
        public GridCoord BoardAnchor { get; set; }
        public PieceDefinition Definition { get; set; }
        public ShopOffer Offer { get; set; }
    }
}
