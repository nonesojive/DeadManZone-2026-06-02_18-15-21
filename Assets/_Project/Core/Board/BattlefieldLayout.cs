namespace DeadManZone.Core.Board
{
    public sealed class BattlefieldLayout
    {
        public int PlayerHalfWidth { get; }
        public int NeutralWidth { get; }
        public int EnemyHalfWidth { get; }
        public int Height { get; }

        public int TotalWidth => PlayerHalfWidth + NeutralWidth + EnemyHalfWidth;

        public int NeutralStartX => PlayerHalfWidth;
        public int EnemyOriginX => PlayerHalfWidth + NeutralWidth;

        public BattlefieldLayout(int playerHalfWidth, int neutralWidth, int enemyHalfWidth, int height)
        {
            PlayerHalfWidth = playerHalfWidth;
            NeutralWidth = neutralWidth;
            EnemyHalfWidth = enemyHalfWidth;
            Height = height;
        }

        public static BattlefieldLayout FromPlayerBoard(BoardLayout playerLayout, int neutralColumns = 2) =>
            new BattlefieldLayout(playerLayout.Width, neutralColumns, playerLayout.Width, playerLayout.Height);

        public bool IsNeutralColumn(int x) =>
            x >= NeutralStartX && x < NeutralStartX + NeutralWidth;

        public bool IsPlayerHalf(int x) => x >= 0 && x < PlayerHalfWidth;

        public bool IsEnemyHalf(int x) => x >= EnemyOriginX && x < TotalWidth;
    }
}
