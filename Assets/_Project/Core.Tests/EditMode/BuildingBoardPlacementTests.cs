using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BuildingBoardPlacementTests
    {
        [Test]
        public void StartNewRun_CombatBoardStartsEmpty()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 1);
            Assert.IsEmpty(orchestrator.GetCombatBoard().Pieces);
        }

        [Test]
        public void Buildings_PlaceOnBuildingBoard_NotCombat()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 1);
            var buildingBoard = orchestrator.GetHqBoard();
            var radio = database.Pieces.First(p => p.id == "radio_array").ToCore();
            Assert.IsTrue(buildingBoard.TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_test").Success);
            Assert.IsFalse(orchestrator.GetCombatBoard().TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_fail").Success);
        }
    }
}
