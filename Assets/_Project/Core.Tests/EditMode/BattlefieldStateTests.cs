using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BattlefieldStateTests
    {
        [Test]
        public void FromBoards_OffsetsEnemyToRightHalf()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), new GridCoord(6, 2));

            var enemyLayout = TestBoards.Layout;
            var enemy = new BoardState(enemyLayout);
            enemy.TryPlace(TestPieces.RifleSquad(), new GridCoord(6, 2));

            var battlefield = BattlefieldState.FromBoards(player, enemy);
            var enemyCell = battlefield.FindCell(enemy.Pieces.First().InstanceId);

            Assert.AreEqual(battlefield.Layout.EnemyOriginX + 6, enemyCell.Position.X);
        }

        [Test]
        public void NeutralColumns_AreBetweenHalves()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            Assert.IsTrue(layout.IsNeutralColumn(layout.NeutralStartX));
            Assert.IsFalse(layout.IsNeutralColumn(0));
        }
    }
}
