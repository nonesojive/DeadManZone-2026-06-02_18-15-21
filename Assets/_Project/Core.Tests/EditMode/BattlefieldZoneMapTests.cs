using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BattlefieldZoneMapTests
    {
        [Test]
        public void PlayerHalf_UsesFourThreeTwoColumns()
        {
            var playerLayout = TestBoards.Layout;
            var battlefield = BattlefieldLayout.FromPlayerBoard(playerLayout);
            var zones = BattlefieldZoneMap.Create(battlefield, playerLayout);

            Assert.AreEqual(ZoneType.Rear, zones[0, 0]);
            Assert.AreEqual(ZoneType.Rear, zones[3, 0]);
            Assert.AreEqual(ZoneType.Support, zones[4, 0]);
            Assert.AreEqual(ZoneType.Support, zones[6, 0]);
            Assert.AreEqual(ZoneType.Front, zones[7, 0]);
            Assert.AreEqual(ZoneType.Front, zones[8, 0]);
        }

        [Test]
        public void EnemyHalf_FrontFacesNeutral_WithFourThreeTwoColumns()
        {
            var playerLayout = TestBoards.Layout;
            var layout = BattlefieldLayout.FromPlayerBoard(playerLayout);
            var zones = BattlefieldZoneMap.Create(layout, playerLayout);

            Assert.AreEqual(ZoneType.Front, zones[layout.EnemyOriginX, 0]);
            Assert.AreEqual(ZoneType.Front, zones[layout.EnemyOriginX + 1, 0]);
            Assert.AreEqual(ZoneType.Support, zones[layout.EnemyOriginX + 2, 0]);
            Assert.AreEqual(ZoneType.Rear, zones[layout.TotalWidth - 1, 0]);
        }

        [Test]
        public void NeutralColumns_UseNeutralZone()
        {
            var playerLayout = TestBoards.Layout;
            var layout = BattlefieldLayout.FromPlayerBoard(playerLayout);
            var zones = BattlefieldZoneMap.Create(layout, playerLayout);

            Assert.AreEqual(ZoneType.Neutral, zones[layout.NeutralStartX, 0]);
            Assert.AreEqual(ZoneType.Neutral, zones[layout.NeutralStartX + layout.NeutralWidth - 1, 0]);
        }
    }
}
