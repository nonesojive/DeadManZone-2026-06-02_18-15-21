using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatBattlefieldConfigTests
    {
        [Test]
        public void NeutralColumnCount_IsFive()
        {
            Assert.AreEqual(5, CombatBattlefieldConfig.NeutralColumnCount);
        }

        [Test]
        public void FromPlayerBoard_TotalWidth_IsTwentyThree()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            Assert.AreEqual(5, layout.NeutralWidth);
            Assert.AreEqual(23, layout.TotalWidth);
            Assert.AreEqual(14, layout.EnemyOriginX);
        }
    }
}
