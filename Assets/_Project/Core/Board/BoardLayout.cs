using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class BoardLayout
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<GridCoord> SpecialTiles { get; }

        private readonly ZoneType[,] _zones;

        private BoardLayout(int width, int height, ZoneType[,] zones, List<GridCoord> specialTiles)
        {
            Width = width;
            Height = height;
            _zones = zones;
            SpecialTiles = specialTiles;
        }

        public static BoardLayout CreateFromZoneMap(int width, int height, ZoneType[,] zones, GridCoord[] specialTiles = null) =>
            new BoardLayout(width, height, zones, specialTiles?.ToList() ?? new List<GridCoord>());

        public static BoardLayout CreateHorizontalZones(
            int width,
            int height,
            int rearCols,
            int supportCols,
            GridCoord[] specialTiles)
        {
            var zones = new ZoneType[width, height];
            for (int x = 0; x < width; x++)
            {
                var zone = x < rearCols ? ZoneType.Rear
                    : x < rearCols + supportCols ? ZoneType.Support
                    : ZoneType.Front;

                for (int y = 0; y < height; y++)
                    zones[x, y] = zone;
            }

            return new BoardLayout(width, height, zones, specialTiles.ToList());
        }

        [Obsolete("Use CreateHorizontalZones for column-based zones (Rear left, Front right).")]
        public static BoardLayout CreateStandard(
            int width,
            int height,
            int rearRows,
            int supportRows,
            GridCoord[] specialTiles)
        {
            var zones = new ZoneType[width, height];
            for (int y = 0; y < height; y++)
            {
                var zone = y < rearRows ? ZoneType.Rear
                    : y < rearRows + supportRows ? ZoneType.Support
                    : ZoneType.Front;

                for (int x = 0; x < width; x++)
                    zones[x, y] = zone;
            }

            return new BoardLayout(width, height, zones, specialTiles.ToList());
        }

        public ZoneType GetZone(GridCoord coord) => _zones[coord.X, coord.Y];

        public bool IsSpecialTile(GridCoord coord) =>
            SpecialTiles.Any(tile => tile.X == coord.X && tile.Y == coord.Y);

        /// <summary>Column counts for horizontal Rear|Support|Front layouts.</summary>
        public void GetHorizontalZoneColumns(out int rearCols, out int supportCols)
        {
            rearCols = 0;
            supportCols = 0;
            for (int x = 0; x < Width; x++)
            {
                switch (_zones[x, 0])
                {
                    case ZoneType.Rear:
                        rearCols++;
                        break;
                    case ZoneType.Support:
                        supportCols++;
                        break;
                }
            }
        }
    }
}
