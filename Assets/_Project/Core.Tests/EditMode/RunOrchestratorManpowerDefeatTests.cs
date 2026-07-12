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
    /// <summary>Manpower is run health (ADR-0005, M5): a fight that bleeds the army to
    /// zero ends the run in Defeat — on wins AND losses — but only AFTER the post-fight
    /// grants (muster, hard-victory package) get their chance to save it.</summary>
    public sealed class RunOrchestratorManpowerDefeatTests
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
        public void TearDown()
        {
            SaveManager.DeleteSave();
        }

        [Test]
        public void WonFight_ManpowerZeroAfterGrants_EndsRunInDefeat()
        {
            // Pass 1: learn the seeded fight's deterministic Manpower delta
            // (muster minus casualties; the chosen option 1 is a normal front).
            var probe = StartGauntletRun();
            int before = probe.State.Manpower;
            bool won = RunFightToCompletion(probe);
            if (!won)
                Assert.Ignore("seeded gauntlet board lost fight 1; win-path defeat not exercisable");
            Assert.AreEqual(RunPhase.Aftermath, probe.State.Phase,
                "healthy manpower keeps a won fight in Aftermath");
            int delta = probe.State.Manpower - before;

            // Pass 2: same seed replays the fight identically — start with exactly
            // enough Manpower that the grants land the army on zero.
            SaveManager.DeleteSave();
            var bledOut = StartGauntletRun();
            bledOut.State.Manpower = -delta;
            RunFightToCompletion(bledOut);

            Assert.AreEqual(0, bledOut.State.Manpower);
            Assert.AreEqual(RunPhase.Defeat, bledOut.State.Phase,
                "a win that bleeds Manpower to zero still ends the run");
        }

        [Test]
        public void LostFight_ManpowerZeroAfterGrants_EndsRunInDefeat()
        {
            // Pass 1: stage the deterministic loss and learn its Manpower delta.
            var probe = StartSingleUnitRun();
            int before = probe.State.Manpower;
            bool won = RunFightToCompletion(probe);
            if (won)
                Assert.Ignore("seeded single-unit board won fight 1; loss-path defeat not exercisable");
            int delta = probe.State.Manpower - before;

            // Pass 2: same seed, same staging — the muster must not save this one.
            SaveManager.DeleteSave();
            var bledOut = StartSingleUnitRun();
            bledOut.State.Manpower = -delta;
            RunFightToCompletion(bledOut);

            Assert.AreEqual(0, bledOut.State.Manpower);
            Assert.AreEqual(RunPhase.Defeat, bledOut.State.Phase,
                "a loss that bleeds Manpower to zero ends the run");
        }

        private RunOrchestrator StartGauntletRun()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(orchestrator, _database);
            orchestrator.ChooseFightOption(1);
            return orchestrator;
        }

        /// <summary>Blank board plus one cheap unit — the loss-staging premise from
        /// RunOrchestratorTests.DefeatReport_CarriesDamageTablesFromSim.</summary>
        private RunOrchestrator StartSingleUnitRun()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);

            var combat = orchestrator.GetCombatBoard();
            foreach (var piece in combat.Pieces.Where(p => p.InstanceId.StartsWith("start_")).ToList())
                combat.TryRemove(piece.InstanceId, out _);
            orchestrator.SaveCombatBoard(combat);

            var rifle = _database.Pieces.First(p => p.id == "conscript_rifleman").ToCore();
            var board = orchestrator.GetPlayerBoard();
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.CombatBoardAnchor(3, 3), "lone_rifle").Success);
            orchestrator.SaveCombatBoard(board);

            orchestrator.ChooseFightOption(1);
            return orchestrator;
        }

        private static bool RunFightToCompletion(RunOrchestrator orchestrator)
        {
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
