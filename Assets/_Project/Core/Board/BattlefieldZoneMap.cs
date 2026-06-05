namespace DeadManZone.Core.Board
{
    /// <summary>
    /// Zone bands for the combined battlefield: player Rear|Support|Front, neutral, enemy Front|Support|Rear.
    /// </summary>
    public static class BattlefieldZoneMap
    {
        public static ZoneType[,] Create(BattlefieldLayout layout)
        {
            var (rearCols, supportCols) = DefaultZoneColumns(layout.PlayerHalfWidth);
            return Create(layout, rearCols, supportCols);
        }

        public static ZoneType[,] Create(BattlefieldLayout layout, BoardLayout playerBoardLayout)
        {
            playerBoardLayout.GetHorizontalZoneColumns(out int rearCols, out int supportCols);
            return Create(layout, rearCols, supportCols);
        }

        public static ZoneType[,] Create(BattlefieldLayout layout, int rearCols, int supportCols)
        {
            var zones = new ZoneType[layout.TotalWidth, layout.Height];
            for (int x = 0; x < layout.TotalWidth; x++)
            {
                var zone = ResolveZone(layout, x, rearCols, supportCols);
                for (int y = 0; y < layout.Height; y++)
                    zones[x, y] = zone;
            }

            return zones;
        }

        public static ZoneType ResolveZone(BattlefieldLayout layout, int x, int rearCols, int supportCols)
        {
            if (layout.IsPlayerHalf(x))
                return ZoneFromHorizontalColumns(rearCols, supportCols, x);

            if (layout.IsNeutralColumn(x))
                return ZoneType.Neutral;

            int enemyLocalX = x - layout.EnemyOriginX;
            return ZoneFromHorizontalColumns(
                rearCols,
                supportCols,
                BattlefieldLayout.MirrorLocalX(enemyLocalX, layout.EnemyHalfWidth));
        }

        private static (int rearCols, int supportCols) DefaultZoneColumns(int halfWidth)
        {
            int rearCols = halfWidth * 4 / 9;
            int supportCols = halfWidth * 3 / 9;
            if (rearCols + supportCols >= halfWidth)
            {
                rearCols = halfWidth / 3;
                supportCols = halfWidth / 3;
            }

            return (rearCols, supportCols);
        }

        private static ZoneType ZoneFromHorizontalColumns(int rearCols, int supportCols, int localX)
        {
            if (localX < rearCols)
                return ZoneType.Rear;

            if (localX < rearCols + supportCols)
                return ZoneType.Support;

            return ZoneType.Front;
        }
    }
}
