using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;

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

        public static BattlefieldLayout FromPlayerBoard(
            BoardLayout playerLayout,
            int? neutralColumns = null) =>
            new BattlefieldLayout(
                playerLayout.Width,
                neutralColumns ?? CombatBattlefieldConfig.NeutralColumnCount,
                playerLayout.Width,
                playerLayout.Height);

        public bool IsNeutralColumn(int x) =>
            x >= NeutralStartX && x < NeutralStartX + NeutralWidth;

        public bool IsPlayerHalf(int x) => x >= 0 && x < PlayerHalfWidth;

        public bool IsEnemyHalf(int x) => x >= EnemyOriginX && x < TotalWidth;

        /// <summary>Flip local X so enemy front faces the neutral columns.</summary>
        public static int MirrorLocalX(int localX, int halfWidth) => halfWidth - 1 - localX;

        public int MirrorEnemyLocalX(int localX) => MirrorLocalX(localX, EnemyHalfWidth);

        /// <summary>
        /// Mirror an enemy-board anchor so the full piece shape stays within the half-width.
        /// </summary>
        public static int MirrorEnemyAnchorX(
            int anchorX,
            PieceShape shape,
            int halfWidth,
            PieceRotation rotation = PieceRotation.R0) =>
            MirrorLocalX(anchorX + shape.GetMaxOffsetX(rotation), halfWidth);

        public int MirrorEnemyAnchorX(int anchorX, PieceShape shape, PieceRotation rotation = PieceRotation.R0) =>
            MirrorEnemyAnchorX(anchorX, shape, EnemyHalfWidth, rotation);
    }
}
