using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class SalvageChanceCalculatorTests
    {
        [Test]
        public void Compute_ReturnsBasePlusBoardBoost()
        {
            Assert.AreEqual(25, SalvageChanceCalculator.Compute(10, 15));
        }

        [Test]
        public void Compute_IsSameForAnyCombatOutcome()
        {
            int chance = SalvageChanceCalculator.Compute(10, 5);
            Assert.AreEqual(15, chance);
        }

        [Test]
        public void Compute_CappedAt50()
        {
            Assert.AreEqual(50, SalvageChanceCalculator.Compute(10, 40));
        }

        [Test]
        public void SumBoardBoost_AddsBonusAndFlagBoost()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var piece = new PieceDefinition
            {
                Id = "salvage_booster",
                DisplayName = "Salvage Booster",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                SalvageChanceBonus = 3,
                ShopModifiers = ShopModifierFlags.SalvageChanceBoost5
            };
            Assert.IsTrue(hq.TryPlace(piece, new GridCoord(0, 0)).Success);

            Assert.AreEqual(8, SalvageBoardBoostAggregator.SumBoardBoost(hq));
        }
    }
}
