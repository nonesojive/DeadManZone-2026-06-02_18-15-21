using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class MusterCalculatorTests
    {
        [Test]
        public void ComputeMuster_IncludesFactionBaseline()
        {
            var board = TestBoards.EmptyBuildingBoard();
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

        [Test]
        public void SupplySynergy_AdjacentPair_AddsSynergyBonus()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var supplyPiece = SupplierTaggedSupplyPiece();
            Assert.IsTrue(hq.TryPlace(supplyPiece, new GridCoord(0, 0), "supply_1").Success, "supply_1 placement");
            Assert.IsTrue(hq.TryPlace(supplyPiece, new GridCoord(0, 1), "supply_2").Success, "supply_2 placement");

            int muster = MusterCalculator.Compute(hq, baseMusterPerShop: 10);
            Assert.AreEqual(10 + MusterCalculator.SupplySynergyMusterBonus, muster);
        }

        private static PieceDefinition SupplierTaggedSupplyPiece() => new()
        {
            Id = "supplier_supply",
            DisplayName = "Supplier Supply",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            SynergyTags = new[] { GameTagIds.Supplier },
            MusterPerShop = 0
        };
    }
}
