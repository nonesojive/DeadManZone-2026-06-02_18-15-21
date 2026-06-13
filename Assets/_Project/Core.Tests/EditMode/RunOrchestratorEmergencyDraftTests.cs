using System.Linq;
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
                Assert.Ignore("Generated ContentDatabase not found. Run DeadManZone/Generate Vertical Slice Content first.");
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
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 777);
            var board = _orchestrator.GetPlayerBoard();
            var rifle = _database.Pieces.First(p => p.id == "rifle_squad").ToCore();
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.FrontLineAnchor(), "rifle_1").Success);
            _orchestrator.SavePlayerBoard(board);

            var registry = ContentRegistryProvider.Build(_database);
            int upkeep = ManpowerCalculator.ComputeUpkeep(_orchestrator.GetPlayerBoard(), registry);
            _orchestrator.State.Manpower = 5;
            int expectedShortfall = upkeep - 5;

            Assert.IsFalse(_orchestrator.CanStartBattle(out _));
            Assert.IsTrue(_orchestrator.TryEmergencyDraft());
            Assert.AreEqual(5 + expectedShortfall, _orchestrator.State.Manpower);
            Assert.AreEqual(upkeep, _orchestrator.State.Manpower);
            Assert.IsTrue(_orchestrator.State.EmergencyDraftUsed);
            Assert.IsTrue(_orchestrator.CanStartBattle(out _));
            Assert.IsFalse(_orchestrator.TryEmergencyDraft());
            Assert.AreEqual(upkeep, _orchestrator.State.Manpower);
        }
    }
}
