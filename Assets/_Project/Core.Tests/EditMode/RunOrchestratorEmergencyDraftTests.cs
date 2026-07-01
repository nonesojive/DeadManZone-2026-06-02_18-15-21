using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class RunOrchestratorEmergencyDraftTests
    {
        private ContentDatabase _database;
        private RunOrchestrator _orchestrator;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
            }

            SaveManager.DeleteSave();
            _orchestrator = new RunOrchestrator(_database);
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
        }

        [Test]
        public void TryEmergencyDraft_WhenShortfall_AppliesManpowerOnce()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 777);
            var board = _orchestrator.GetPlayerBoard();
            var rifle = _database.Pieces.First(p => p.id == "conscript_rifleman").ToCore();
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.CombatBoardAnchor(5, 3), "rifle_1").Success);
            _orchestrator.SaveCombatBoard(board);

            var registry = ContentRegistryProvider.Build(_database);
            int upkeep = ManpowerCalculator.ComputeUpkeep(_orchestrator.GetPlayerBoard(), registry);
            int startingManpower = System.Math.Max(0, upkeep - 1);
            _orchestrator.State.Manpower = startingManpower;
            int expectedShortfall = upkeep - startingManpower;

            Assert.IsFalse(_orchestrator.CanStartBattle(out _));
            Assert.IsTrue(_orchestrator.TryEmergencyDraft());
            Assert.AreEqual(startingManpower + expectedShortfall, _orchestrator.State.Manpower);
            Assert.AreEqual(upkeep, _orchestrator.State.Manpower);
            Assert.IsTrue(_orchestrator.State.EmergencyDraftUsed);
            Assert.IsTrue(_orchestrator.CanStartBattle(out _));
            Assert.IsFalse(_orchestrator.TryEmergencyDraft());
            Assert.AreEqual(upkeep, _orchestrator.State.Manpower);
        }
    }
}
