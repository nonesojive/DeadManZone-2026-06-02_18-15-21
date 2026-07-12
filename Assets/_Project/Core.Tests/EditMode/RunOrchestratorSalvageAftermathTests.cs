using System.Collections.Generic;
using DeadManZone.Core;
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
        public void CompleteFight_SetsLastEnemyFactionIdAndSalvageChance()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(_orchestrator, _database);

            _orchestrator.ChooseFightOption(1);
            var chosen = _orchestrator.State.FightOptions[1];
            var template = _database.GetEnemyTemplate(chosen.TemplateFightNumber);
            Assert.IsNotNull(template, "chosen option's enemy template required.");

            _orchestrator.BeginCombat();
            RunCombatToCompletion();

            Assert.AreEqual(RunPhase.Aftermath, _orchestrator.State.Phase);
            Assert.AreEqual(template.enemyFactionId, _orchestrator.State.LastEnemyFactionId);
            Assert.Greater(_orchestrator.State.SalvageChancePercent, 0);
        }

        [Test]
        public void CompleteFight_ScalesSalvageChanceByKillShare()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(_orchestrator, _database);

            _orchestrator.ChooseFightOption(1);
            _orchestrator.BeginCombat();
            var final = RunCombatToCompletion();
            Assert.IsNotNull(final, "combat must complete");

            // CompleteCombat stamps the fight's kill share; SyncSalvageChancePercent
            // (re-run by every RefreshShop) applies it on top of the board-derived
            // chance — routed enemies escaped with their gear (ADR-0005).
            int expectedShare = SalvageChanceCalculator.KillSharePercent(
                final.EnemyKilled, final.EnemyRouted);
            Assert.AreEqual(expectedShare, _orchestrator.State.LastFightSalvageKillPercent);

            var faction = _database.GetFaction(FactionIds.IronmarchUnion);
            int unscaled = SalvageChanceCalculator.Compute(
                faction.baseSalvageChancePercent,
                SalvageBoardBoostAggregator.SumBoardBoost(_orchestrator.GetCombatBoard()));
            Assert.AreEqual(
                SalvageChanceCalculator.ApplyKillShare(unscaled, expectedShare),
                _orchestrator.State.SalvageChancePercent);
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

        /// <summary>Drives the fight to its end; returns the completed step (null if the
        /// phase left Combat some other way).</summary>
        private CombatAdvanceResult RunCombatToCompletion()
        {
            while (_orchestrator.State.Phase == RunPhase.Combat)
            {
                SubmitCombatCommandsForCurrentWindow();
                var step = _orchestrator.AdvanceCombat();
                if (step.Status == CombatAdvanceStatus.Completed)
                {
                    _orchestrator.FinalizePendingCombat();
                    return step;
                }
            }

            return null;
        }

        private void SubmitCombatCommandsForCurrentWindow()
        {
            if (_orchestrator.State.Combat == null || !_orchestrator.State.Combat.AwaitingCommand)
                return;

            var checkpointIndex = _orchestrator.State.Combat.CheckpointsFired - 1;
            int authority = _orchestrator.State.Combat.Authority > 0
                ? _orchestrator.State.Combat.Authority
                : _orchestrator.State.Combat.Requisition;

            var commands = VerticalSliceTestFixtures.BuildAggressiveCommandsForCheckpoint(
                _orchestrator.GetPlayerBoard(),
                checkpointIndex,
                authority);

            _orchestrator.SubmitCombatCommands(commands);
        }
    }
}
