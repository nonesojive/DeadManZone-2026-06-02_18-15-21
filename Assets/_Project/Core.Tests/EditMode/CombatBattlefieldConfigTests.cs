using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatBattlefieldConfigTests
    {
        [Test]
        public void NeutralColumnCount_IsSeven()
        {
            Assert.AreEqual(7, CombatBattlefieldConfig.NeutralColumnCount);
        }

        [Test]
        public void FromPlayerBoard_TotalWidth_IsTwentyFive()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            Assert.AreEqual(7, layout.NeutralWidth);
            Assert.AreEqual(25, layout.TotalWidth);
            Assert.AreEqual(16, layout.EnemyOriginX);
        }
    }
}
