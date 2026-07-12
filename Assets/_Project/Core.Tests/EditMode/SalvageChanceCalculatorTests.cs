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
        public void KillSharePercent_IsKillsOverRemovedEnemies()
        {
            Assert.AreEqual(75, SalvageChanceCalculator.KillSharePercent(enemyKilled: 3, enemyRouted: 1));
            Assert.AreEqual(50, SalvageChanceCalculator.KillSharePercent(enemyKilled: 2, enemyRouted: 2));
            Assert.AreEqual(100, SalvageChanceCalculator.KillSharePercent(enemyKilled: 5, enemyRouted: 0));
        }

        [Test]
        public void KillSharePercent_AllRoutedFight_YieldsZero()
        {
            Assert.AreEqual(0, SalvageChanceCalculator.KillSharePercent(enemyKilled: 0, enemyRouted: 4));
        }

        [Test]
        public void KillSharePercent_NoRemovals_IsNeutralHundred()
        {
            // Legacy saves / no finished fight this round: salvage at full chance.
            Assert.AreEqual(100, SalvageChanceCalculator.KillSharePercent(enemyKilled: 0, enemyRouted: 0));
        }

        [Test]
        public void ApplyKillShare_ScalesWithIntegerMath()
        {
            Assert.AreEqual(30, SalvageChanceCalculator.ApplyKillShare(chancePercent: 40, killSharePercent: 75));
            Assert.AreEqual(0, SalvageChanceCalculator.ApplyKillShare(chancePercent: 40, killSharePercent: 0));
            Assert.AreEqual(40, SalvageChanceCalculator.ApplyKillShare(chancePercent: 40, killSharePercent: 100));
            // Truncation is deliberate — integer math keeps seeded runs deterministic.
            Assert.AreEqual(3, SalvageChanceCalculator.ApplyKillShare(chancePercent: 10, killSharePercent: 33));
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
