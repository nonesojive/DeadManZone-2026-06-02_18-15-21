using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
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
            var board = BuildGauntletTestBoard();
            int runSeed = FindWinningSeed(board, startSeed: 42);

            _orchestrator.StartNewRun("iron_vanguard", runSeed: runSeed);
            _orchestrator.SavePlayerBoard(board);

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

        private BoardState BuildGauntletTestBoard()
        {
            var faction = _database.GetFaction("iron_vanguard");
            var board = new BoardState(faction.CreateBoardLayout());

            TryPlacePiece(board, "field_gun_nest", new Core.Common.GridCoord(0, 0), "gun_1");
            TryPlacePiece(board, "supply_depot", new Core.Common.GridCoord(2, 0), "depot_1");
            TryPlacePiece(board, "command_bunker", new Core.Common.GridCoord(1, 2), "bunker_1");
            TryPlacePiece(board, "mortar_crew", new Core.Common.GridCoord(4, 3), "mortar_1");
            TryPlacePiece(board, "rifle_squad", new Core.Common.GridCoord(0, 4), "rifle_1");
            TryPlacePiece(board, "diesel_walker", new Core.Common.GridCoord(2, 4), "walker_1");
            TryPlacePiece(board, "mg_team", new Core.Common.GridCoord(5, 4), "mg_1");
            TryPlacePiece(board, "mobile_artillery", new Core.Common.GridCoord(6, 3), "artillery_1");

            return board;
        }

        private void TryPlacePiece(BoardState board, string pieceId, Core.Common.GridCoord anchor, string instanceId)
        {
            var piece = _database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }

        private int FindWinningSeed(BoardState board, int startSeed)
        {
            for (int seed = startSeed; seed < startSeed + 50; seed++)
            {
                if (BoardBeatsGauntlet(board, seed))
                    return seed;
            }

            Assert.Fail("Gauntlet test board did not win all fights on any seed in range.");
            return startSeed;
        }

        private bool BoardBeatsGauntlet(BoardState board, int runSeed)
        {
            var faction = _database.GetFaction("iron_vanguard");
            var registry = _database.BuildRegistry();

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var enemy = _database.GetEnemyTemplate(fight).BuildBoard(faction, registry);
                var commands = BuildAggressiveCommands(board);
                var result = new CombatResolver().Resolve(
                    board,
                    enemy,
                    runSeed + fight * 1000,
                    commands,
                    requisition: 8);

                if (!result.PlayerWon)
                    return false;
            }

            return true;
        }

        private static List<PhaseCommand> BuildAggressiveCommands(BoardState board)
        {
            var commands = new List<PhaseCommand>();
            string bunkerId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "command_bunker")?.InstanceId;
            string depotId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "supply_depot")?.InstanceId;

            if (bunkerId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
            }

            if (depotId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = depotId,
                    Cost = 1
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = depotId,
                    Cost = 1
                });
            }

            return commands;
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
