using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>M1 Dread-clock run flow: dread grants, boss triggering, boss stages,
    /// victory by third boss, and twisted-fight restore determinism.</summary>
    public sealed class DreadBossRunTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
            }

            SaveManager.DeleteSave();
        }

        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        [Test]
        public void BossRoster_EveryStageBoardBuildsAndEscalates()
        {
            var registry = _database.BuildRegistry();
            foreach (var boss in BossRoster.All)
            {
                int previousCount = 0;
                for (int stage = 0; stage < DreadRules.BossCount; stage++)
                {
                    var board = BossRoster.BuildStageBoard(boss, stage, registry);
                    Assert.Greater(board.Pieces.Count, 0, $"{boss.BossId} stage {stage}");
                    Assert.GreaterOrEqual(board.Pieces.Count, previousCount,
                        $"{boss.BossId} stages should escalate in size");
                    previousCount = board.Pieces.Count;
                }
            }
        }

        [Test]
        public void NormalWin_GrantsExactlyDreadPerWin()
        {
            var orchestrator = StartRun(runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            SaveSteamrollerBoard(orchestrator);

            bool won = RunFightToCompletion(orchestrator);

            Assert.IsTrue(won, "steamroller board should win fight 1");
            Assert.AreEqual(DreadRules.DreadPerWin, orchestrator.State.Dread);
            Assert.AreEqual(0, orchestrator.State.BossesDefeated);
            Assert.AreEqual(2, orchestrator.State.FightIndex, "FightIndex stays the plain counter");
        }

        [Test]
        public void Loss_GrantsZeroDread()
        {
            var orchestrator = StartRun(runSeed: 555);
            // An empty combat board is an immediate, deterministic defeat.
            bool won = RunFightToCompletion(orchestrator);

            Assert.IsFalse(won);
            Assert.AreEqual(0, orchestrator.State.Dread);
            Assert.AreEqual(1, orchestrator.State.FightIndex);
            Assert.AreNotEqual(RunPhase.Victory, orchestrator.State.Phase);
        }

        [Test]
        public void BossFight_TriggersAtThreshold_NotBefore()
        {
            var below = StartRun(runSeed: 777);
            below.State.Dread = DreadRules.NextThreshold(0) - 1;
            Assert.IsFalse(below.IsBossFightPending);
            below.ChooseFightOption(1);
            below.BeginCombat();
            Assert.IsNull(below.State.Combat.BossId);
            Assert.IsNull(below.State.Combat.ActiveTwistId);

            SaveManager.DeleteSave();
            var at = StartRun(runSeed: 777);
            at.State.Dread = DreadRules.NextThreshold(0);
            Assert.IsTrue(at.IsBossFightPending);
            var expectedBoss = BossRoster.Get(at.GetBossOrder()[0]);
            at.BeginCombat();
            Assert.AreEqual(expectedBoss.BossId, at.State.Combat.BossId);
            Assert.AreEqual(expectedBoss.TwistId, at.State.Combat.ActiveTwistId);
            Assert.AreEqual(expectedBoss.EnemyFactionId, at.State.LastEnemyFactionId,
                "salvage targeting must key on the boss's pool");
        }

        [Test]
        public void BossWin_GrantsNoDread_AndAdvancesBossTrack()
        {
            var orchestrator = StartRun(runSeed: 888);
            orchestrator.State.Dread = 6; // BEFORE the fixture: it mirrors the upcoming (boss) board
            SaveSteamrollerBoard(orchestrator);

            Assert.IsTrue(orchestrator.IsBossFightPending);
            bool won = RunFightToCompletion(orchestrator);

            Assert.IsTrue(won, "steamroller board should beat a stage-1 boss army");
            Assert.AreEqual(6, orchestrator.State.Dread, "boss wins grant no Dread");
            Assert.AreEqual(1, orchestrator.State.BossesDefeated);
            Assert.IsFalse(orchestrator.IsBossFightPending, "next threshold is 12");
            Assert.AreEqual(RunPhase.Aftermath, orchestrator.State.Phase);
        }

        [Test]
        public void ThirdBossWin_SetsVictory()
        {
            var orchestrator = StartRun(runSeed: 999);
            orchestrator.State.Dread = 18; // BEFORE the fixture: it mirrors the upcoming (boss) board
            orchestrator.State.BossesDefeated = 2;
            SaveSteamrollerBoard(orchestrator);

            Assert.IsTrue(orchestrator.IsBossFightPending);
            bool won = RunFightToCompletion(orchestrator);

            Assert.IsTrue(won, "steamroller board should beat a stage-3 boss army");
            Assert.AreEqual(3, orchestrator.State.BossesDefeated);
            Assert.AreEqual(RunPhase.Victory, orchestrator.State.Phase);
        }

        [Test]
        public void BossLoss_KeepsBossPendingForNextCombat()
        {
            var orchestrator = StartRun(runSeed: 1111);
            orchestrator.State.Dread = 6;
            string firstBossId = orchestrator.GetBossOrder()[0];

            // Empty board: deterministic loss against the boss.
            bool won = RunFightToCompletion(orchestrator);
            Assert.IsFalse(won);

            Assert.AreEqual(6, orchestrator.State.Dread);
            Assert.AreEqual(0, orchestrator.State.BossesDefeated);
            Assert.IsTrue(orchestrator.IsBossFightPending, "the boss awaits the next combat");

            orchestrator.DismissAftermath();
            orchestrator.BeginCombat();
            Assert.AreEqual(firstBossId, orchestrator.State.Combat.BossId,
                "the same boss returns after a loss");
        }

        [Test]
        public void HighDread_ClampsToLastAuthoredTemplate_NoThrow()
        {
            var orchestrator = StartRun(runSeed: 2222);
            orchestrator.State.Dread = 40; // fight-equivalent 21, far past the authored 10
            orchestrator.State.BossesDefeated = 3; // no boss pending: normal template path

            var board = orchestrator.GetUpcomingEnemyBoard();
            Assert.NotNull(board, "difficulty must clamp to the last authored template");
            Assert.Greater(board.Pieces.Count, 0);

            var lastTemplate = _database.GetEnemyTemplate(RunOrchestrator.MaxFights);
            Assert.AreEqual(lastTemplate.previewTag, orchestrator.GetNextEnemyPreviewTag());
        }

        [Test]
        public void TwistedBossFight_RestoreReproducesIdenticalEventLog()
        {
            var live = RunBossFightLog(runSeed: 4242, reloadMidFight: false);
            var restored = RunBossFightLog(runSeed: 4242, reloadMidFight: true);

            CollectionAssert.AreEqual(live, restored,
                "a restored twisted fight must replay the exact event log");
        }

        private List<string> RunBossFightLog(int runSeed, bool reloadMidFight)
        {
            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(orchestrator, _database);
            orchestrator.State.Dread = 6;
            orchestrator.State.Manpower = 9999;

            orchestrator.BeginCombat();
            Assert.NotNull(orchestrator.State.Combat.BossId, "fight must be a boss fight");
            Assert.NotNull(orchestrator.State.Combat.ActiveTwistId);

            var step = orchestrator.AdvanceCombat();
            if (reloadMidFight)
            {
                orchestrator.SaveAndExit();
                orchestrator = new RunOrchestrator(_database);
                Assert.IsTrue(orchestrator.TryLoadSavedRun());
                Assert.NotNull(orchestrator.State.Combat.ActiveTwistId,
                    "twist id must survive the save round-trip");
                step = orchestrator.AdvanceCombat();
            }

            while (step.Status != CombatAdvanceStatus.Completed)
                step = orchestrator.AdvanceCombat();

            Assert.NotNull(step.EventLog);
            return step.EventLog.Events
                .Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}")
                .ToList();
        }

        private RunOrchestrator StartRun(int runSeed)
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed);
            return orchestrator;
        }

        /// <summary>A deliberately overwhelming board: MIRROR the upcoming enemy's own
        /// combined-arms composition twice over, then fill every remaining cell with
        /// riflemen. Probed live 2026-07-12: mono-unit walls (even 36 bulwarks) LOSE to
        /// the stage-3 boss army — late fights require combined arms, so the fixture
        /// out-arms the enemy with its own comp instead of guessing one. Manpower topped.</summary>
        private void SaveSteamrollerBoard(RunOrchestrator orchestrator)
        {
            var board = orchestrator.GetCombatBoard();
            var upcoming = orchestrator.GetUpcomingEnemyBoard();
            Assert.NotNull(upcoming, "an upcoming enemy board must exist");

            int placed = 0;
            foreach (var piece in upcoming.Pieces)
            {
                int copies = 0;
                for (int y = 0; y < board.Layout.Height && copies < 2; y++)
                    for (int x = 0; x < board.Layout.Width && copies < 2; x++)
                        if (board.TryPlace(piece.Definition, new GridCoord(x, y), $"steam_m{placed}_{y}_{x}").Success)
                        {
                            copies++;
                            placed++;
                        }
            }

            var filler = _database.Pieces.First(p => p.id == "conscript_rifleman").ToCore();
            for (int y = 0; y < board.Layout.Height; y++)
                for (int x = 0; x < board.Layout.Width; x++)
                    if (board.TryPlace(filler, new GridCoord(x, y), $"steam_f_{y}_{x}").Success)
                        placed++;

            Assert.GreaterOrEqual(placed, 12, "steamroller board should field a combined-arms mass");
            orchestrator.SaveCombatBoard(board);
            orchestrator.State.Manpower = 9999;
        }

        /// <summary>Drives one full fight; returns whether the player won. Leaves the run
        /// in whatever phase CompleteCombat resolved to.</summary>
        private static bool RunFightToCompletion(RunOrchestrator orchestrator)
        {
            Assert.IsTrue(orchestrator.CanStartBattle(out string reason), reason);
            // Normal front by default (M2); boss rounds ignore the choice.
            if (orchestrator.State.FightOptions is { Count: > 0 }
                && orchestrator.State.ChosenFightOption < 0)
                orchestrator.ChooseFightOption(1);
            orchestrator.BeginCombat();

            var step = orchestrator.AdvanceCombat();
            while (step.Status != CombatAdvanceStatus.Completed)
                step = orchestrator.AdvanceCombat();

            bool won = step.PlayerWon;
            orchestrator.FinalizePendingCombat();
            return won;
        }
    }
}
