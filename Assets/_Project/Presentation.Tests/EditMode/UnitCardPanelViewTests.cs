using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Run;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class UnitCardPanelViewTests
    {
        [Test]
        public void UsesBuildingCard_OnlyForHqBoardPieces()
        {
            Assert.IsTrue(UnitCardPanelView.UsesBuildingCard(new PieceDefinition
            {
                Primary = GameTagIds.Building,
                Category = PieceCategory.Building
            }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                Category = PieceCategory.Unit
            }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(new PieceDefinition
            {
                Primary = GameTagIds.Structure,
                Category = PieceCategory.Unit
            }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(null));
        }

        [Test]
        public void UsesBuildingCard_StructurePrimary_UsesUnitCardEvenIfCategoryBuilding()
        {
            // ponytail: category alone must not pick building card; board placement is the source of truth.
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(new PieceDefinition
            {
                Primary = GameTagIds.Structure,
                Category = PieceCategory.Building
            }));
        }
    }
}
