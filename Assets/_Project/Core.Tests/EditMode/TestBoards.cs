using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Tests
{
    public static class TestBoards
    {
        public const int DefaultWidth = 9;
        public const int DefaultHeight = 10;
        public const int DefaultRearCols = 4;
        public const int DefaultSupportCols = 3;

        public static BoardLayout Layout =>
            BoardLayout.CreateHorizontalZones(
                DefaultWidth,
                DefaultHeight,
                DefaultRearCols,
                DefaultSupportCols,
                specialTiles: new[]
                {
                    new GridCoord(1, 4),
                    new GridCoord(4, 4),
                    new GridCoord(7, 4)
                });

        /// <summary>Front zone anchor for unit placement on the default horizontal layout (columns 7-8).</summary>
        public static GridCoord FrontLineAnchor(int y = 5) => new(7, y);

        /// <summary>Support zone anchor for a 9-wide board with 3 rear and 3 support columns (columns 3-5).</summary>
        public static GridCoord SupportLineAnchor(int columnOffset = 0, int y = 0) => new(3 + columnOffset, y);

        public static BoardState StandardPlayer()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.RifleSquad(), FrontLineAnchor());
            return board;
        }

        public static BoardState StandardEnemy()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "enemy_hq");
            board.TryPlace(TestPieces.RifleSquad(), FrontLineAnchor(3), instanceId: "enemy_rifle_1");
            board.TryPlace(TestPieces.RifleSquad(), FrontLineAnchor(6), instanceId: "enemy_rifle_2");
            return board;
        }

        public static BoardState WithCommandBunker()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            board.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 2), instanceId: "hq_test");
            board.TryPlace(TestPieces.RifleSquad(), FrontLineAnchor(4));
            return board;
        }

        public static BoardState StrongPlayerVsWeakEnemy()
        {
            var player = new BoardState(Layout);
            player.TryPlace(TestPieces.RifleSquad(), FrontLineAnchor());
            player.TryPlace(TestPieces.RifleSquad(), new GridCoord(7, 4), instanceId: "player_rifle_2");

            var enemy = new BoardState(Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), FrontLineAnchor());

            return player;
        }

        public static BoardState WeakEnemyOnly()
        {
            var enemy = new BoardState(Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), FrontLineAnchor());
            return enemy;
        }

        public static BoardState WithHqAndRifle()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.FieldingHq(), new GridCoord(0, 2), instanceId: "hq_test");
            board.TryPlace(TestPieces.RifleSquadTenMan(), FrontLineAnchor(4));
            return board;
        }

        public static BoardState HqOnly()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "hq_test");
            return board;
        }

        public static BoardState WithSupplyDepot()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "hq_test");
            board.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0));
            return board;
        }

        public static BoardState WithTwoSupplyBuildings()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "hq_test");
            board.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0), instanceId: "supply_depot_1");
            board.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 1), instanceId: "supply_depot_2");
            return board;
        }
    }
}
