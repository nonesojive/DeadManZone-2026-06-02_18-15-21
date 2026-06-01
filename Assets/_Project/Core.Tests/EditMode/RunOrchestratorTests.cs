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
                    SubmitCombatCommandsForCurrentWindow();

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
            var depot = _database.Pieces.First(p => p.id == "supply_depot").ToCore();
            var mg = _database.Pieces.First(p => p.id == "mg_team").ToCore();
            var walker = _database.Pieces.First(p => p.id == "diesel_walker").ToCore();

            // Economy + commands (bunker on faction special tile at y=2)
            board.TryPlace(depot, new Core.Common.GridCoord(0, 3), "depot_1");
            board.TryPlace(bunker, new Core.Common.GridCoord(1, 2), "bunker_1");

            // Front line — supply depot adjacent to rifles grants +1 damage in combat
            board.TryPlace(rifle, new Core.Common.GridCoord(0, 4), "rifle_1");
            board.TryPlace(walker, new Core.Common.GridCoord(2, 4), "walker_1");
            board.TryPlace(rifle, new Core.Common.GridCoord(4, 4), "rifle_2");
            board.TryPlace(mg, new Core.Common.GridCoord(5, 4), "mg_1");

            _orchestrator.SavePlayerBoard(board);
        }

        private void SubmitCombatCommandsForCurrentWindow()
        {
            var available = _orchestrator.GetAvailableCommands();
            if (available.Count == 0)
                return;

            int budget = _orchestrator.GetPrimaryActionBudget();
            int submitted = 0;
            var completedPhase = _orchestrator.State.Combat.CompletedPhase;

            foreach (var cmd in available)
            {
                if (submitted >= budget)
                    break;

                if (cmd.Type == CommandType.SpendRequisitionBuff &&
                    _orchestrator.State.Combat.Requisition < cmd.RequisitionCost)
                    continue;

                _orchestrator.SubmitCombatCommand(new PhaseCommand
                {
                    AfterPhase = completedPhase,
                    Type = cmd.Type,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = cmd.SourcePieceId,
                    Cost = cmd.RequisitionCost
                });
                submitted++;
            }
        }
    }
}
