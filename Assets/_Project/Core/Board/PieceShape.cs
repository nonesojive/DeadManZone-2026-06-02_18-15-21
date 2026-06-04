using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class PieceShape
    {
        private readonly GridCoord[] _cells;

        public PieceShape(IEnumerable<GridCoord> cells)
        {
            _cells = cells.ToArray();
        }

        public IEnumerable<GridCoord> GetCells(GridCoord anchor) =>
            GetCells(anchor, PieceRotation.R0);

        public IEnumerable<GridCoord> GetCells(GridCoord anchor, PieceRotation rotation)
        {
            foreach (var cell in _cells)
            {
                var rotated = ShapeTransforms.RotateOffset(cell, rotation);
                yield return new GridCoord(anchor.X + rotated.X, anchor.Y + rotated.Y);
            }
        }
    }
}
