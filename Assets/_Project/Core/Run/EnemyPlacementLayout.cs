using System;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Run
{
    /// <summary>Maps legacy 9-wide demo anchors onto the current square combat board.</summary>
    public static class EnemyPlacementLayout
    {
        public static GridCoord RemapLegacyAnchor(int x, int y, int boardSize)
        {
            if (boardSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(boardSize));

            // ponytail: demo enemies used x=6–8 from the old 9-wide grid; shift −3 for 6-wide boards.
            if (x >= 6)
                x -= 3;

            x = Math.Clamp(x, 0, boardSize - 1);
            y = Math.Clamp(y, 0, boardSize - 1);
            return new GridCoord(x, y);
        }
    }
}
