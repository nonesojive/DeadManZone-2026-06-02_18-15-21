using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class SalvageChanceCalculatorTests
    {
        [Test]
        public void Defeat_ReturnsFactionBaseOnly()
        {
            int chance = SalvageChanceCalculator.Compute(
                baseSalvagePercent: 10,
                boardBoost: 15,
                outcome: FightOutcome.Defeat,
                destroyedUniqueTypes: 3);

            Assert.AreEqual(10, chance);
        }

        [Test]
        public void Victory_IncludesBoardBoostAndWinBonus()
        {
            int chance = SalvageChanceCalculator.Compute(
                baseSalvagePercent: 10,
                boardBoost: 10,
                outcome: FightOutcome.Victory,
                destroyedUniqueTypes: 2);

            // 10 base + 10 board + 10 win + 4 destroyed = 34
            Assert.AreEqual(34, chance);
        }

        [Test]
        public void Draw_TreatedSameAsVictory()
        {
            int victory = SalvageChanceCalculator.Compute(10, 5, FightOutcome.Victory, 0);
            int draw = SalvageChanceCalculator.Compute(10, 5, FightOutcome.Draw, 0);

            Assert.AreEqual(victory, draw);
        }

        [Test]
        public void Result_CappedAt50()
        {
            int chance = SalvageChanceCalculator.Compute(10, 40, FightOutcome.Victory, 5);

            Assert.AreEqual(50, chance);
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
