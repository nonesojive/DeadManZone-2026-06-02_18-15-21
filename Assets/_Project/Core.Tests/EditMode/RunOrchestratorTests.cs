using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests
{
    public class RunOrchestratorTests
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
        public void StartNewRun_SetsBuildPhaseAndShop()
        {
            _orchestrator.StartNewRun("iron_vanguard");

            Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase);
            Assert.AreEqual(1, _orchestrator.State.FightIndex);
            Assert.Greater(_orchestrator.State.Shop.Offers.Count, 0);
        }

        [Test]
        public void SaveRoundTrip_PreservesFightProgress()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 1234);
            _orchestrator.State.FightIndex = 2;
            _orchestrator.State.Gold = 77;
            _orchestrator.SaveAndExit();

            var loaded = new RunOrchestrator(_database);
            Assert.IsTrue(loaded.TryLoadSavedRun());
            Assert.AreEqual(2, loaded.State.FightIndex);
            Assert.AreEqual(77, loaded.State.Gold);
        }

        [Test]
        public void FullCombatLoop_CanReachVictoryWithStrongBoard()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 42);
            PlaceStrongBoard();

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                _orchestrator.BeginCombat();

                while (_orchestrator.State.Phase == RunPhase.Combat)
                {
                    if (_orchestrator.GetAvailableCommands().Count > 0)
                    {
                        var cmd = _orchestrator.GetAvailableCommands().First();
                        _orchestrator.SubmitCombatCommand(new PhaseCommand
                        {
                            AfterPhase = _orchestrator.State.Combat.CompletedPhase,
                            Type = cmd.Type,
                            Stance = StanceType.AllOutAssault,
                            SourcePieceId = cmd.SourcePieceId,
                            Cost = cmd.RequisitionCost
                        });
                    }

                    var step = _orchestrator.AdvanceCombat();
                    if (step.Status == CombatAdvanceStatus.Completed)
                        break;
                }

                if (_orchestrator.State.Phase == RunPhase.Victory)
                    break;

                Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase, $"Fight {fight} should return to build.");
            }

            Assert.AreEqual(RunPhase.Victory, _orchestrator.State.Phase);
        }

        private void PlaceStrongBoard()
        {
            var board = _orchestrator.GetPlayerBoard();
            var rifle = _database.Pieces.First(p => p.id == "rifle_squad").ToCore();
            var bunker = _database.Pieces.First(p => p.id == "command_bunker").ToCore();

            board.TryPlace(bunker, new Core.Common.GridCoord(0, 0), "bunker_1");
            board.TryPlace(rifle, new Core.Common.GridCoord(0, 4), "rifle_1");
            board.TryPlace(rifle, new Core.Common.GridCoord(2, 4), "rifle_2");
            board.TryPlace(rifle, new Core.Common.GridCoord(4, 4), "rifle_3");
            _orchestrator.SavePlayerBoard(board);
        }
    }
}
