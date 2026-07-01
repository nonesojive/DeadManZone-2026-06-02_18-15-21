using DeadManZone.Core.Board;
using DeadManZone.Presentation.Run;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class UnitCardPanelViewTests
    {
        [Test]
        public void UsesBuildingCard_OnlyForBuildingCategory()
        {
            Assert.IsTrue(UnitCardPanelView.UsesBuildingCard(
                new PieceDefinition { Category = PieceCategory.Building }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(
                new PieceDefinition { Category = PieceCategory.Unit }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(
                new PieceDefinition { Category = PieceCategory.Hybrid }));
            Assert.IsFalse(UnitCardPanelView.UsesBuildingCard(null));
        }
    }
}
