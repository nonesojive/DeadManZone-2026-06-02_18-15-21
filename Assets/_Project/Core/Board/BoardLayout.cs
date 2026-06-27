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
        public BoardKind Kind { get; }
        public bool UsesZones { get; }
        public IReadOnlyList<GridCoord> SpecialTiles { get; }
        public IReadOnlyList<GridCoord> BlockedCells { get; }

        private readonly ZoneType[,] _zones;
        private readonly HashSet<(int X, int Y)> _blocked;

        private BoardLayout(
            int width,
            int height,
            BoardKind kind,
            bool usesZones,
            ZoneType[,] zones,
            List<GridCoord> specialTiles,
            List<GridCoord> blockedCells)
        {
            Width = width;
            Height = height;
            Kind = kind;
            UsesZones = usesZones;
            _zones = zones;
            SpecialTiles = specialTiles;
            BlockedCells = blockedCells;
            _blocked = new HashSet<(int, int)>(blockedCells.Select(c => (c.X, c.Y)));
        }

        public static BoardLayout CreateUnzoned(
            int width,
            int height,
            BoardKind kind,
            GridCoord[] blockedCells = null,
            GridCoord[] specialTiles = null)
        {
            var zones = new ZoneType[width, height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                zones[x, y] = ZoneType.Support;

            return new BoardLayout(
                width,
                height,
                kind,
                usesZones: false,
                zones,
                specialTiles?.ToList() ?? new List<GridCoord>(),
                blockedCells?.ToList() ?? new List<GridCoord>());
        }

        public static BoardLayout CreateCombatBoard(int size = 6, GridCoord[] specialTiles = null) =>
            CreateUnzoned(size, size, BoardKind.Combat, specialTiles: specialTiles);

        public static BoardLayout CreateHqBoard(
            int width,
            int height,
            GridCoord[] blockedCells = null,
            GridCoord[] specialTiles = null) =>
            CreateUnzoned(width, height, BoardKind.Hq, blockedCells, specialTiles);

        public static BoardLayout CreateFromZoneMap(int width, int height, ZoneType[,] zones, GridCoord[] specialTiles = null) =>
            new BoardLayout(
                width,
                height,
                BoardKind.Combat,
                usesZones: true,
                zones,
                specialTiles?.ToList() ?? new List<GridCoord>(),
                new List<GridCoord>());

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

            return new BoardLayout(
                width,
                height,
                BoardKind.Combat,
                usesZones: true,
                zones,
                specialTiles.ToList(),
                new List<GridCoord>());
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

            return new BoardLayout(
                width,
                height,
                BoardKind.Combat,
                usesZones: true,
                zones,
                specialTiles.ToList(),
                new List<GridCoord>());
        }

        public bool IsBlocked(GridCoord coord) => _blocked.Contains((coord.X, coord.Y));

        public bool IsWithinBounds(GridCoord coord) =>
            coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height;

        public bool IsPlaceableCell(GridCoord coord) =>
            IsWithinBounds(coord) && !IsBlocked(coord);

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
