using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SalvageCalculatorTests
    {
        [Test]
        public void Compute_ReturnsHalfSuppliesByDefault()
        {
            // Rarity.Common (default) => RarityPricing.BaseCost = 10, refund (int)5.
            var piece = new PieceDefinition { RequisitionCost = 2, ManpowerCost = 4 };
            var refund = SalvageCalculator.Compute(piece);
            Assert.AreEqual(5, refund.Supplies);
            Assert.AreEqual(1, refund.Authority);
            Assert.AreEqual(0, refund.Manpower);
        }

        [Test]
        public void DustScourge_GetsSalvageBonus()
        {
            // Common base 10 -> 5 refund -> x1.25 = 6.25, truncated to 6.
            var piece = new PieceDefinition();
            var refund = SalvageCalculator.Compute(piece, FactionIds.DustScourge);
            Assert.AreEqual(6, refund.Supplies);
        }
    }
}
