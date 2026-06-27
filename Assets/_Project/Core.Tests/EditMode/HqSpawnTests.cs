using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class HqSpawnTests
    {
        [Test]
        public void StartNewRun_DoesNotAutoPlaceHqOnCombatBoard()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun(FactionIds.IronVanguard, runSeed: 1);
            var combat = orchestrator.GetCombatBoard();
            Assert.IsFalse(combat.Pieces.Any(p => PieceTagQueries.HasTag(p.Definition, GameTagIds.Hq)));
        }

        [Test]
        public void Buildings_PlaceOnHqBoard_NotCombat()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun(FactionIds.IronVanguard, runSeed: 1);
            var hqBoard = orchestrator.GetHqBoard();
            var radio = database.Pieces.First(p => p.id == "radio_array").ToCore();
            Assert.IsTrue(hqBoard.TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_test").Success);
            Assert.IsFalse(orchestrator.GetCombatBoard().TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_fail").Success);
        }
    }
}
