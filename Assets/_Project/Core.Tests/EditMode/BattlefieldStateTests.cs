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
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor());

            var enemyLayout = TestBoards.Layout;
            var enemy = new BoardState(enemyLayout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor());

            var battlefield = BattlefieldState.FromBoards(player, enemy);
            var enemyCell = battlefield.FindCell(enemy.Pieces.First().InstanceId);

            Assert.AreEqual(battlefield.Layout.EnemyOriginX + 1, enemyCell.Position.X);
        }

        [Test]
        public void FromBoards_MirrorsEnemyHqToFarRear()
        {
            var player = new BoardState(TestBoards.Layout);
            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "enemy_hq");

            var battlefield = BattlefieldState.FromBoards(player, enemy);
            var hqCell = battlefield.FindCell("enemy_hq");

            Assert.AreEqual(battlefield.Layout.EnemyOriginX + 7, hqCell.Position.X);

            foreach (var cell in TestPieces.HqPiece().Shape.GetCells(hqCell.Position))
                Assert.Less(cell.X, battlefield.Layout.TotalWidth);
        }

        [Test]
        public void ZoneMap_EnemyFrontFacesNeutralColumns()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var zones = BattlefieldZoneMap.Create(layout, TestBoards.Layout);

            Assert.AreEqual(ZoneType.Front, zones[layout.EnemyOriginX, 0]);
            Assert.AreEqual(ZoneType.Rear, zones[layout.TotalWidth - 1, 0]);
            Assert.AreEqual(ZoneType.Front, zones[layout.PlayerHalfWidth - 1, 0]);
            Assert.AreEqual(ZoneType.Rear, zones[0, 0]);
        }

        [Test]
        public void CombatBoardLayout_Uses6x6HalvesAndNeutralBand()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.CombatLayout);
            Assert.AreEqual(6, layout.PlayerHalfWidth);
            Assert.AreEqual(5, layout.NeutralWidth);
            Assert.AreEqual(17, layout.TotalWidth);
            Assert.AreEqual(11, layout.EnemyOriginX);
        }
    }
}
