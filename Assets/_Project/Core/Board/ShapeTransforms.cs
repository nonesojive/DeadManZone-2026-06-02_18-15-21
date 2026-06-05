using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public static class ShapeTransforms
    {
        public static GridCoord RotateOffset(GridCoord local, PieceRotation rotation) =>
            rotation switch
            {
                PieceRotation.R0 => local,
                PieceRotation.R90 => new GridCoord(-local.Y, local.X),
                PieceRotation.R180 => new GridCoord(-local.X, -local.Y),
                PieceRotation.R270 => new GridCoord(local.Y, -local.X),
                _ => local
            };

        public static GridCoord InverseRotateOffset(GridCoord rotated, PieceRotation rotation) =>
            rotation switch
            {
                PieceRotation.R0 => rotated,
                PieceRotation.R90 => new GridCoord(rotated.Y, -rotated.X),
                PieceRotation.R180 => new GridCoord(-rotated.X, -rotated.Y),
                PieceRotation.R270 => new GridCoord(-rotated.Y, rotated.X),
                _ => rotated
            };
    }
}
