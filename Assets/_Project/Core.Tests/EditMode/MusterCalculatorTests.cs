using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class MusterCalculatorTests
    {
        [Test]
        public void ComputeMuster_IncludesFactionBaseline()
        {
            var board = TestBoards.HqOnly();
            int muster = MusterCalculator.Compute(board, baseMusterPerShop: 12);
            Assert.AreEqual(12, muster);
        }

        [Test]
        public void ComputeMuster_AddsPieceMusterPerShop()
        {
            var board = TestBoards.WithSupplyDepot();
            int muster = MusterCalculator.Compute(board, baseMusterPerShop: 12);
            Assert.AreEqual(15, muster);
        }

        [Test]
        public void ComputeMuster_NoSupplySynergyBonusUntilTagsReturn()
        {
            var board = TestBoards.WithTwoSupplyBuildings();
            int muster = MusterCalculator.Compute(board, baseMusterPerShop: 10);
            Assert.AreEqual(16, muster);
        }
    }
}
