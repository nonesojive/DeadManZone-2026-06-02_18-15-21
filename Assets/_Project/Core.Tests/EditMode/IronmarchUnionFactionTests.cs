using DeadManZone.Core.Board;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class IronmarchUnionFactionTests
    {
        [Test]
        public void ComputeSuppliesIncome_EmptyBoard_IsFactionBaselineOnly()
        {
            var boards = new BuildBoardSet { Hq = TestBoards.EmptyBuildingBoard() };

            Assert.AreEqual(
                10,
                RoundIncomeCalculator.ComputeSuppliesIncome(factionBaselineSupplies: 10, boards));
        }

        [Test]
        public void ComputeSuppliesIncome_IncludesFactionBaselineAndBoardBonus()
        {
            const int factionBaselineSupplies = 10;
            var boards = new BuildBoardSet { Hq = TestBoards.EmptyBuildingBoard() };
            int boardBonus = RoundIncomeCalculator.ComputeBoardSuppliesBonus(factionBaselineSupplies, boards);
            int expected = factionBaselineSupplies + boardBonus;

            Assert.AreEqual(
                expected,
                RoundIncomeCalculator.ComputeSuppliesIncome(factionBaselineSupplies, boards));
        }
    }
}
