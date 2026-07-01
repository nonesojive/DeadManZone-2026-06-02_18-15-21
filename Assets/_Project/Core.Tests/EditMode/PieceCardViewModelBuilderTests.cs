using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class PieceCardViewModelBuilderTests
    {
        [Test]
        public void Build_HqBuilding_HidesCombatStats()
        {
            var piece = new PieceDefinition
            {
                Id = "supply_depot",
                DisplayName = "Supply Depot",
                Primary = GameTagIds.Building,
                Category = PieceCategory.Building,
                AttackType = AttackType.None,
                AttackRange = AttackRangeTier.Medium
            };

            var model = PieceCardViewModelBuilder.Build(piece);

            Assert.IsFalse(model.ShowCombatStats);
        }

        [Test]
        public void Build_CombatUnit_ShowsCombatStats()
        {
            var piece = TestPieces.With(
                TestPieces.RifleSquad(),
                attackType: AttackType.Ballistic,
                attackRange: AttackRangeTier.Long);

            var model = PieceCardViewModelBuilder.Build(piece);

            Assert.IsTrue(model.ShowCombatStats);
            Assert.AreEqual(8, model.AttackRangeValue);
        }
    }
}
