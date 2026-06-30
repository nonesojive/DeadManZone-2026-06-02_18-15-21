using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class EnemyPlacementLayoutTests
    {
        [Test]
        public void RemapLegacyAnchor_ShiftsNineWideEnemyColumnsOntoSixWideBoard()
        {
            var anchor = EnemyPlacementLayout.RemapLegacyAnchor(6, 4, boardSize: 6);
            Assert.AreEqual(new GridCoord(3, 4), anchor);
        }

        [Test]
        public void RemapLegacyAnchor_ClampsYToBoardHeight()
        {
            var anchor = EnemyPlacementLayout.RemapLegacyAnchor(5, 6, boardSize: 6);
            Assert.AreEqual(new GridCoord(5, 5), anchor);
        }
    }
}
