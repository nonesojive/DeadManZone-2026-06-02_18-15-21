using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;

namespace DeadManZone.Presentation.DragDrop
{
    public enum DragSourceKind
    {
        ShopOffer,
        ReservesPiece,
        BoardPiece
    }

    public sealed class DragPayload
    {
        public DragSourceKind SourceKind { get; set; }
        public string OfferId { get; set; }
        public string PieceId { get; set; }
        public string ReservesInstanceId { get; set; }
        public string BoardInstanceId { get; set; }
        public GridCoord BoardAnchor { get; set; }
        public PieceRotation Rotation { get; set; } = PieceRotation.R0;
        public PieceDefinition Definition { get; set; }
        public ShopOffer Offer { get; set; }
    }

    public static class PieceRotationUtil
    {
        public static PieceRotation RotateClockwise(PieceRotation rotation) =>
            rotation switch
            {
                PieceRotation.R0 => PieceRotation.R90,
                PieceRotation.R90 => PieceRotation.R180,
                PieceRotation.R180 => PieceRotation.R270,
                PieceRotation.R270 => PieceRotation.R0,
                _ => PieceRotation.R0
            };

        public static PieceRotation RotateCounterClockwise(PieceRotation rotation) =>
            rotation switch
            {
                PieceRotation.R0 => PieceRotation.R270,
                PieceRotation.R90 => PieceRotation.R0,
                PieceRotation.R180 => PieceRotation.R90,
                PieceRotation.R270 => PieceRotation.R180,
                _ => PieceRotation.R0
            };
    }
}
