using System;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-13 rarity-standardized-pricing spec: the three price
    /// constants, salvage refund math (including the Dust Scourge x1.25 bonus and
    /// int truncation), and the append-only-enum throw guard.</summary>
    public sealed class RarityPricingTests
    {
        [Test]
        public void BaseCost_ReturnsTheThreeTierConstants()
        {
            Assert.AreEqual(10, RarityPricing.BaseCost(Rarity.Common));
            Assert.AreEqual(15, RarityPricing.BaseCost(Rarity.Uncommon));
            Assert.AreEqual(25, RarityPricing.BaseCost(Rarity.Rare));
        }

        [Test]
        public void BaseCost_ThrowsOnUnknownRarity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityPricing.BaseCost((Rarity)999));
        }

        [TestCase(Rarity.Common, 5)]
        [TestCase(Rarity.Uncommon, 7)]
        [TestCase(Rarity.Rare, 12)]
        public void SalvageCalculator_RefundsHalfBaseCost_WithIntTruncation(Rarity rarity, int expectedRefund)
        {
            var piece = new PieceDefinition { Rarity = rarity };
            var refund = SalvageCalculator.Compute(piece);
            Assert.AreEqual(expectedRefund, refund.Supplies);
        }

        [TestCase(Rarity.Common, 6)]     // 10 * 0.5 = 5   -> 5 * 1.25 = 6.25   -> 6
        [TestCase(Rarity.Uncommon, 8)]   // 15 * 0.5 = 7   -> 7 * 1.25 = 8.75   -> 8
        [TestCase(Rarity.Rare, 15)]      // 25 * 0.5 = 12  -> 12 * 1.25 = 15.0  -> 15
        public void SalvageCalculator_DustScourgeBonus_AppliesAfterTruncationAndTruncatesAgain(
            Rarity rarity, int expectedRefund)
        {
            var piece = new PieceDefinition { Rarity = rarity };
            var refund = SalvageCalculator.Compute(piece, FactionIds.DustScourge);
            Assert.AreEqual(expectedRefund, refund.Supplies);
        }
    }
}
