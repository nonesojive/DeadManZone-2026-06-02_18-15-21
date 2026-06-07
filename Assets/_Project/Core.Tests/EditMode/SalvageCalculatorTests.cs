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
            var piece = new PieceDefinition { GoldCost = 10, RequisitionCost = 2, ManpowerCost = 4 };
            var refund = SalvageCalculator.Compute(piece);
            Assert.AreEqual(5, refund.Supplies);
            Assert.AreEqual(1, refund.Authority);
            Assert.AreEqual(0, refund.Manpower);
        }

        [Test]
        public void DustScourge_GetsSalvageBonus()
        {
            var piece = new PieceDefinition { GoldCost = 10 };
            var refund = SalvageCalculator.Compute(piece, "dust_scourge");
            Assert.AreEqual(6, refund.Supplies);
        }
    }
}
