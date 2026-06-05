using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class FightRewardTableTests
    {
        [Test]
        public void GetReward_Fights1Through3_UseTutorialSupplies()
        {
            Assert.AreEqual(100, FightRewardTable.GetReward(1).Supplies);
            Assert.AreEqual(105, FightRewardTable.GetReward(2).Supplies);
            Assert.AreEqual(110, FightRewardTable.GetReward(3).Supplies);
        }

        [Test]
        public void GetReward_Fight4_KeepsExistingCurve()
        {
            Assert.AreEqual(22, FightRewardTable.GetReward(4).Supplies);
        }

        [Test]
        public void GetReward_Draw_HalvesTutorialSupplies()
        {
            Assert.AreEqual(50, FightRewardTable.GetReward(1, isDraw: true).Supplies);
            Assert.AreEqual(52, FightRewardTable.GetReward(2, isDraw: true).Supplies);
        }
    }
}
