using DeadManZone.Core.Board;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class IronmarchUnionFactionTests
    {
        [Test]
        public void ComputeSuppliesIncome_IncludesFightRewardFactionBaselineAndBoardBonus()
        {
            const int fightRewardSupplies = 20;
            const int factionBaselineSupplies = 10;
            var boards = new BuildBoardSet { Hq = TestBoards.EmptyBuildingBoard() };
            int boardBonus = RoundIncomeCalculator.ComputeBoardSuppliesBonus(fightRewardSupplies, boards);
            int expected = fightRewardSupplies + factionBaselineSupplies + boardBonus;

            Assert.AreEqual(
                expected,
                RoundIncomeCalculator.ComputeSuppliesIncome(fightRewardSupplies, factionBaselineSupplies, boards));
        }
    }
}
