using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BuildingIncomeRulesTests
    {
        [Test]
        public void SupplyDepot_AddsFiveFlatSupplies()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0));
            hq.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 1));
            var boards = new BuildBoardSet { Hq = hq };

            Assert.AreEqual(10, BuildingIncomeRules.SumSuppliesFlatBonus(boards));
        }

        // OfficerQuarters_AuthorityScalesWithCommandTagsOnBothBoards removed: 2026-07-15
        // faction-roster-v1 (§2.2) cut officer_quarters from the IronMarch roster with no
        // direct replacement (see BuildingIncomeRules.SumAuthorityFromBuildings). The
        // "+1 Authority per Command piece on both boards" rule it exercised no longer
        // exists — command_outpost's flat +1/round is IronMarch's only Authority building.

        [Test]
        public void CommandOutpost_AddsOneAuthority()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.CommandOutpost(), new GridCoord(0, 0));
            var boards = new BuildBoardSet { Hq = hq };

            Assert.AreEqual(1, BuildingIncomeRules.SumAuthorityFromBuildings(boards));
        }
    }
}
