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
        public void FromPlayerBoard_TotalWidth_IsSeventeen_ForCombatBoard()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.CombatLayout);
            Assert.AreEqual(5, layout.NeutralWidth);
            Assert.AreEqual(17, layout.TotalWidth);
            Assert.AreEqual(11, layout.EnemyOriginX);
            Assert.AreEqual(6, layout.PlayerHalfWidth);
        }
    }
}
