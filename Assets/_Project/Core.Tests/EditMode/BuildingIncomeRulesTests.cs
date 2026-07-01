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

        [Test]
        public void OfficerQuarters_AuthorityScalesWithCommandTagsOnBothBoards()
        {
            var combat = new BoardState(TestBoards.CombatLayout);
            combat.TryPlace(TestPieces.WithTags(command: true), new GridCoord(4, 4), "c1");
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.WithTags(command: true, building: true), new GridCoord(0, 0), "h1");
            hq.TryPlace(TestPieces.OfficerQuarters(), new GridCoord(0, 2), "quarters");

            var boards = new BuildBoardSet { Combat = combat, Hq = hq };
            Assert.AreEqual(2, BuildingIncomeRules.SumAuthorityFromBuildings(boards));
        }

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
