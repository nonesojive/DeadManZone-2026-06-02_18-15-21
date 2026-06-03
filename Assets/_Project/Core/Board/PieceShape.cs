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

        public IEnumerable<GridCoord> GetCells(GridCoord anchor)
        {
            foreach (var cell in _cells)
                yield return new GridCoord(anchor.X + cell.X, anchor.Y + cell.Y);
        }
    }
}
