using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class RoundIncomeCalculatorTests
    {
        [Test]
        public void ComputeSuppliesIncome_ReturnsBaseWhenBoardEmpty()
        {
            var boards = BoardsFrom(TestBoards.EmptyBuildingBoard());
            Assert.AreEqual(10, RoundIncomeCalculator.ComputeSuppliesIncome(10, 0, boards));
        }

        [Test]
        public void ComputeBoardSuppliesBonus_IsZeroWithoutQualifyingBoard()
        {
            CriticalMassDefaultRules.RegisterWithCatalog();
            Assert.AreEqual(0, RoundIncomeCalculator.ComputeBoardSuppliesBonus(10, BoardsFrom(TestBoards.EmptyBuildingBoard())));
        }

        [Test]
        public void ComputeManpowerIncome_IncludesDepotMuster()
        {
            var combat = new BoardState(TestBoards.CombatLayout);
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0)).Success);
            var boards = new BuildBoardSet { Combat = combat, Hq = hq };

            Assert.AreEqual(15, RoundIncomeCalculator.ComputeManpowerIncome(12, boards));
        }

        [Test]
        public void ComputeSalvageChancePreview_AddsCombatBoardBoost()
        {
            var combat = new BoardState(TestBoards.CombatLayout);
            var piece = new PieceDefinition
            {
                Id = "salvage_booster",
                DisplayName = "Salvage Booster",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                SalvageChanceBonus = 3,
                ShopModifiers = ShopModifierFlags.SalvageChanceBoost5
            };
            Assert.IsTrue(combat.TryPlace(piece, new GridCoord(0, 0)).Success);

            Assert.AreEqual(18, RoundIncomeCalculator.ComputeSalvageChancePreview(10, combat));
        }

        [Test]
        public void FormatIncomeLabel_PrefixesPlusForNonNegative()
        {
            Assert.AreEqual("+22", RoundIncomeCalculator.FormatIncomeLabel(22));
            Assert.AreEqual("+0", RoundIncomeCalculator.FormatIncomeLabel(0));
        }

        private static BuildBoardSet BoardsFrom(BoardState hqBoard) =>
            new() { Hq = hqBoard };
    }
}
