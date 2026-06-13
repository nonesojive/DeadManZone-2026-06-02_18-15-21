using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class RunOrchestratorSalvageAftermathTests
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
        public void CompleteFight_SetsLastEnemyFactionIdAndSalvageChance()
        {
            var board = VerticalSliceTestFixtures.BuildGauntletBoard(_database);
            _orchestrator.StartNewRun("iron_vanguard", runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            _orchestrator.SavePlayerBoard(board);

            var template = _database.GetEnemyTemplate(1);
            Assert.IsNotNull(template, "Fight 1 enemy template required.");

            _orchestrator.BeginCombat();
            RunCombatToCompletion();

            Assert.AreEqual(RunPhase.Aftermath, _orchestrator.State.Phase);
            Assert.AreEqual(template.enemyFactionId, _orchestrator.State.LastEnemyFactionId);
            Assert.Greater(_orchestrator.State.SalvageChancePercent, 0);
        }

        [Test]
        public void SalvageAftermathHelper_CountsUniqueDestroyedEnemyTypes()
        {
            var enemyBoard = new BoardSnapshot
            {
                Pieces = new List<PlacedPieceRecord>
                {
                    new() { InstanceId = "enemy_a", PieceId = "conscript_rifleman" },
                    new() { InstanceId = "enemy_b", PieceId = "field_medic" }
                }
            };

            var log = new CombatEventLog();
            log.Append(0, 1, "enemy_a", "destroyed", "player_1", 0);
            log.Append(0, 2, "enemy_a", "destroyed", "player_2", 0);
            log.Append(0, 3, "enemy_b", "destroyed", "player_1", 0);

            Assert.AreEqual(2, SalvageAftermathHelper.CountDestroyedEnemyTypes(log, enemyBoard));
        }

        private void RunCombatToCompletion()
        {
            while (_orchestrator.State.Phase == RunPhase.Combat)
            {
                SubmitCombatCommandsForCurrentWindow();
                var step = _orchestrator.AdvanceCombat();
                if (step.Status == CombatAdvanceStatus.Completed)
                {
                    _orchestrator.FinalizePendingCombat();
                    break;
                }
            }
        }

        private void SubmitCombatCommandsForCurrentWindow()
        {
            if (_orchestrator.State.Combat == null || !_orchestrator.State.Combat.AwaitingCommand)
                return;

            var checkpointIndex = _orchestrator.State.Combat.CheckpointsFired - 1;
            var commands = new List<PhaseCommand>
            {
                new()
                {
                    AfterCheckpoint = checkpointIndex,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            };

            int authority = _orchestrator.State.Combat.Authority > 0
                ? _orchestrator.State.Combat.Authority
                : _orchestrator.State.Combat.Requisition;

            foreach (var cmd in _orchestrator.GetAvailableCommands())
            {
                if (cmd.Type != CommandType.UseAbility || cmd.RequisitionCost > authority)
                    continue;

                commands.Add(new PhaseCommand
                {
                    AfterCheckpoint = checkpointIndex,
                    Type = CommandType.UseAbility,
                    Ability = cmd.Ability,
                    SourcePieceId = cmd.SourcePieceId,
                    Cost = cmd.RequisitionCost
                });
                authority -= cmd.RequisitionCost;
            }

            _orchestrator.SubmitCombatCommands(commands);
        }
    }
}
