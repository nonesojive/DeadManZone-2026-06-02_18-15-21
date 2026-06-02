using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Tests
{
    public static class TestBoards
    {
        public static BoardLayout Layout =>
            BoardLayout.CreateStandard(
                width: 8,
                height: 6,
                rearRows: 2,
                supportRows: 2,
                specialTiles: new[] { new GridCoord(1, 2), new GridCoord(4, 2) });

        public static BoardState StandardPlayer()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 4));
            return board;
        }

        public static BoardState StandardEnemy()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 4));
            return board;
        }

        public static BoardState WithCommandBunker()
        {
            var board = new BoardState(Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            board.TryPlace(TestPieces.RifleSquad(), new GridCoord(2, 4));
            return board;
        }

        public static BoardState StrongPlayerVsWeakEnemy()
        {
            var player = new BoardState(Layout);
            player.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 4));
            player.TryPlace(TestPieces.RifleSquad(), new GridCoord(2, 4), instanceId: "player_rifle_2");

            var enemy = new BoardState(Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), new GridCoord(0, 4));

            return player;
        }

        public static BoardState WeakEnemyOnly()
        {
            var enemy = new BoardState(Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), new GridCoord(0, 4));
            return enemy;
        }
    }
}
