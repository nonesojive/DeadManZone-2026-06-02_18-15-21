using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatFootprint
    {
        public static IReadOnlyList<GridCoord> ComputeOffsets(PieceShape shape, int rotation)
        {
            var pieceRotation = ToPieceRotation(rotation);
            return shape.GetCells(new GridCoord(0, 0), pieceRotation).ToList();
        }

        public static IReadOnlyList<GridCoord> ComputeOccupiedCells(
            GridCoord anchor,
            IReadOnlyList<GridCoord> offsets)
        {
            var cells = new List<GridCoord>(offsets.Count);
            foreach (var offset in offsets)
                cells.Add(new GridCoord(anchor.X + offset.X, anchor.Y + offset.Y));

            return cells;
        }

        private static PieceRotation ToPieceRotation(int rotation) =>
            rotation switch
            {
                0 => PieceRotation.R0,
                1 => PieceRotation.R90,
                2 => PieceRotation.R180,
                3 => PieceRotation.R270,
                _ => (PieceRotation)(rotation * 90)
            };
    }
}
