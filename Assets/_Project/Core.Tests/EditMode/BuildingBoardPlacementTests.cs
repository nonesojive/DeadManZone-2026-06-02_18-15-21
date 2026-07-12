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
            // Since the starting-loadout feature, a fresh combat board carries ONLY the
            // faction's authored starting units — nothing bought, nothing else.
            Assert.IsTrue(
                orchestrator.GetCombatBoard().Pieces.All(p => p.InstanceId.StartsWith("start_")),
                "a fresh combat board holds only starting-loadout pieces");
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
            var outpost = database.Pieces.First(p => p.id == "command_outpost").ToCore();
            // Find a free HQ anchor — starting-loadout buildings occupy authored cells.
            Core.Common.GridCoord? free = null;
            for (int y = 0; y < buildingBoard.Layout.Height && free == null; y++)
                for (int x = 0; x < buildingBoard.Layout.Width && free == null; x++)
                    if (buildingBoard.CanPlace(outpost, new Core.Common.GridCoord(x, y)))
                        free = new Core.Common.GridCoord(x, y);
            Assert.IsTrue(free.HasValue, "the HQ board should still have room for another building");
            Assert.IsTrue(buildingBoard.TryPlace(outpost, free.Value, "outpost_test").Success);
            Assert.IsFalse(orchestrator.GetCombatBoard().TryPlace(outpost, free.Value, "outpost_fail").Success);
        }
    }
}
