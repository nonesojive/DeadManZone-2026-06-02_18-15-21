using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>M2 Fight Option run flow through the orchestrator: round-start
    /// generation, persistence, choice validation, tier economy (easy Authority
    /// debit / tiered Dread / hard materiel package), and option-fight restore
    /// determinism. Follows the DreadBossRunTests fixture patterns.</summary>
    public sealed class FightOptionRunTests
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
        public void StartNewRun_GeneratesThreeSeededOptions()
        {
            var orchestrator = StartRun(runSeed: 4242);
            var options = orchestrator.State.FightOptions;

            Assert.AreEqual(3, options.Count);
            Assert.AreEqual(-1, orchestrator.State.ChosenFightOption);
            Assert.AreEqual(FightOptionTier.Easy, options[0].Tier);
            Assert.AreEqual(FightOptionTier.Normal, options[1].Tier);
            Assert.AreEqual(FightOptionTier.Hard, options[2].Tier);
            Assert.NotNull(options[2].ConditionId);

            // Round 1 at Dread 0: armies stay within ±1 of fight-equivalent 1.
            foreach (var option in options)
                Assert.That(option.TemplateFightNumber, Is.InRange(1, 2));

            // The exact rolls reproduce from the "options" stream and the authored templates.
            var expected = FightOptionGenerator.Generate(
                4242, orchestrator.State.FightIndex, orchestrator.State.Dread,
                BuildSources(orchestrator));
            Assert.AreEqual(Fingerprint(expected), Fingerprint(options));
        }

        [Test]
        public void Options_PersistAcrossSaveAndLoad()
        {
            var orchestrator = StartRun(runSeed: 555);
            string before = Fingerprint(orchestrator.State.FightOptions);
            orchestrator.ChooseFightOption(2);
            orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(before, Fingerprint(reloaded.State.FightOptions),
                "options must persist verbatim — never regenerate mid-round");
            Assert.AreEqual(2, reloaded.State.ChosenFightOption);
        }

        [Test]
        public void BeginCombat_WithoutAChoice_Throws()
        {
            var orchestrator = StartRun(runSeed: 777);
            SaveSteamrollerBoard(orchestrator);

            Assert.Throws<System.InvalidOperationException>(() => orchestrator.BeginCombat(),
                "the UI always chooses; BeginCombat must not silently pick a front");
        }

        [Test]
        public void ChooseFightOption_ValidatesIndexRange()
        {
            var orchestrator = StartRun(runSeed: 888);

            Assert.IsFalse(orchestrator.CanChooseOption(-1, out string reason));
            Assert.IsNotNull(reason);
            Assert.IsFalse(orchestrator.CanChooseOption(3, out _));
            Assert.Throws<System.InvalidOperationException>(() => orchestrator.ChooseFightOption(3));

            Assert.IsTrue(orchestrator.CanChooseOption(1, out _));
            orchestrator.ChooseFightOption(1);
            Assert.AreEqual(1, orchestrator.State.ChosenFightOption);
        }

        [Test]
        public void ChooseEasy_RejectedWhenAuthorityCannotCoverTheCost()
        {
            var orchestrator = StartRun(runSeed: 888);
            orchestrator.State.Authority = DreadRules.EasyAuthorityCost - 1;

            Assert.IsFalse(orchestrator.CanChooseOption(0, out string reason));
            Assert.That(reason, Does.Contain("Authority"));
            Assert.Throws<System.InvalidOperationException>(() => orchestrator.ChooseFightOption(0));

            orchestrator.State.Authority = DreadRules.EasyAuthorityCost;
            Assert.IsTrue(orchestrator.CanChooseOption(0, out _));
        }

        [Test]
        public void EasyChoice_DebitsAuthorityIntoTheCombatSnapshot()
        {
            var normal = StartRun(runSeed: 999);
            SaveSteamrollerBoard(normal);
            normal.ChooseFightOption(1);
            normal.BeginCombat();
            int normalRequisition = normal.State.Combat.Requisition;
            Assert.AreEqual(FightOptionTier.Normal, normal.State.Combat.ActiveTier);

            SaveManager.DeleteSave();
            var easy = StartRun(runSeed: 999);
            SaveSteamrollerBoard(easy);
            easy.ChooseFightOption(0);
            easy.BeginCombat();

            Assert.AreEqual(FightOptionTier.Easy, easy.State.Combat.ActiveTier);
            Assert.AreEqual(
                normalRequisition - DreadRules.EasyAuthorityCost,
                easy.State.Combat.Requisition,
                "Requisition must be the round pool minus the easy front's Authority cost");
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void Win_GrantsDreadByTier(int optionIndex, int expectedDread)
        {
            var orchestrator = StartRun(runSeed: 24_680);
            orchestrator.ChooseFightOption(optionIndex);
            SaveSteamrollerBoard(orchestrator);

            bool won = RunFightToCompletion(orchestrator);

            Assert.IsTrue(won, $"steamroller board should win the tier-{optionIndex} front");
            Assert.AreEqual(expectedDread, orchestrator.State.Dread);
        }

        [Test]
        public void HardWin_AwardsTheMaterielPackage()
        {
            var orchestrator = StartRun(runSeed: 24_680);
            orchestrator.ChooseFightOption(2);
            SaveSteamrollerBoard(orchestrator);
            int suppliesBefore = orchestrator.State.Supplies;
            int manpowerBefore = orchestrator.State.Manpower;

            bool won = RunFightToCompletion(orchestrator);
            Assert.IsTrue(won, "steamroller board should win the hard front");

            var report = orchestrator.State.LastBattleReport;
            Assert.AreEqual(
                suppliesBefore + report.SuppliesEarned + DreadRules.HardVictorySupplies,
                orchestrator.State.Supplies,
                "hard victory must add the supplies package on top of round income");
            Assert.AreEqual(
                manpowerBefore - report.ManpowerCasualties
                    + orchestrator.State.LastMusterGained + DreadRules.HardVictoryManpower,
                orchestrator.State.Manpower,
                "hard victory must add the manpower package on top of the muster");
        }

        [Test]
        public void Win_ClearsTheChoiceAndRegeneratesNextRoundsOptions()
        {
            var orchestrator = StartRun(runSeed: 24_680);
            orchestrator.ChooseFightOption(1);
            SaveSteamrollerBoard(orchestrator);

            Assert.IsTrue(RunFightToCompletion(orchestrator));

            Assert.AreEqual(-1, orchestrator.State.ChosenFightOption);
            Assert.AreEqual(3, orchestrator.State.FightOptions.Count);
            var expected = FightOptionGenerator.Generate(
                24_680, orchestrator.State.FightIndex, orchestrator.State.Dread,
                BuildSources(orchestrator));
            Assert.AreEqual(Fingerprint(expected), Fingerprint(orchestrator.State.FightOptions),
                "round 2 options key on the incremented FightIndex and new Dread");
        }

        [Test]
        public void ReFoughtLoss_SeesTheSameOptions()
        {
            var orchestrator = StartRun(runSeed: 1111);
            string before = Fingerprint(orchestrator.State.FightOptions);
            orchestrator.ChooseFightOption(1);

            // Empty combat board: a deterministic loss.
            bool won = RunFightToCompletion(orchestrator, chooseIfUnchosen: false);
            Assert.IsFalse(won);

            Assert.AreEqual(-1, orchestrator.State.ChosenFightOption, "the choice resets on loss");
            Assert.AreEqual(before, Fingerprint(orchestrator.State.FightOptions),
                "same FightIndex + Dread after a loss → the same fronts return (intended)");
        }

        [Test]
        public void BossPendingRound_GeneratesNoOptions_AndBossBranchStillRuns()
        {
            var orchestrator = StartRun(runSeed: 888);
            orchestrator.State.Dread = DreadRules.NextThreshold(0) - DreadRules.DreadFor(FightOptionTier.Normal);
            orchestrator.ChooseFightOption(1);
            SaveSteamrollerBoard(orchestrator);

            Assert.IsTrue(RunFightToCompletion(orchestrator), "steamroller board should win");
            Assert.IsTrue(orchestrator.IsBossFightPending, "the win must land on the threshold");
            Assert.IsEmpty(orchestrator.State.FightOptions,
                "boss-pending rounds offer no options — the Front Report shows the boss");

            orchestrator.DismissAftermath();
            orchestrator.State.Manpower = 9999;
            orchestrator.BeginCombat();
            Assert.NotNull(orchestrator.State.Combat.BossId, "the M1 boss branch runs unchanged");
            Assert.IsNull(orchestrator.State.Combat.ActiveTier, "boss fights carry no option tier");
        }

        [Test]
        public void HardConditionFight_RestoreReproducesIdenticalEventLog()
        {
            var live = RunOptionFightLog(runSeed: 4242, optionIndex: 2, reloadMidFight: false);
            var restored = RunOptionFightLog(runSeed: 4242, optionIndex: 2, reloadMidFight: true);

            CollectionAssert.AreEqual(live, restored,
                "a restored hard fight must re-apply its Battle Condition identically");
        }

        [Test]
        public void EasyFight_RestoreReproducesIdenticalEventLog()
        {
            var live = RunOptionFightLog(runSeed: 4242, optionIndex: 0, reloadMidFight: false);
            var restored = RunOptionFightLog(runSeed: 4242, optionIndex: 0, reloadMidFight: true);

            CollectionAssert.AreEqual(live, restored,
                "a restored easy fight must re-suppress the enemy fight-start engines");
        }

        // ---- fixtures (DreadBossRunTests patterns) ----

        private List<string> RunOptionFightLog(int runSeed, int optionIndex, bool reloadMidFight)
        {
            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(orchestrator, _database);
            orchestrator.State.Manpower = 9999;
            orchestrator.ChooseFightOption(optionIndex);

            var chosen = orchestrator.State.FightOptions[optionIndex];
            orchestrator.BeginCombat();
            Assert.AreEqual(chosen.Tier, orchestrator.State.Combat.ActiveTier);
            Assert.AreEqual(chosen.ConditionId, orchestrator.State.Combat.ActiveTwistId,
                "hard fights persist their condition; other tiers persist null");

            var step = orchestrator.AdvanceCombat();
            if (reloadMidFight)
            {
                orchestrator.SaveAndExit();
                orchestrator = new RunOrchestrator(_database);
                Assert.IsTrue(orchestrator.TryLoadSavedRun());
                Assert.AreEqual(chosen.Tier, orchestrator.State.Combat.ActiveTier,
                    "the tier must survive the save round-trip");
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

        private List<FightOptionArmySource> BuildSources(RunOrchestrator orchestrator)
        {
            var registry = _database.BuildRegistry();
            return _database.EnemyTemplates
                .Where(t => t != null)
                .Select(t => new FightOptionArmySource
                {
                    FightNumber = t.fightNumber,
                    EnemyFactionId = t.enemyFactionId,
                    BuildBoard = () => t.BuildBoard(orchestrator.Faction, registry)
                })
                .ToList();
        }

        private static string Fingerprint(IEnumerable<FightOptionRecord> options) =>
            string.Join(";", options.Select(o =>
                $"{o.Tier}|{o.EnemyFactionId}|{o.TemplateFightNumber}|{o.ConditionId}|{o.StrengthPreview}"));

        /// <summary>Mirror the upcoming enemy's composition twice over, then fill with
        /// riflemen (see DreadBossRunTests.SaveSteamrollerBoard). Choice-aware: run
        /// ChooseFightOption first so the mirror matches the chosen front.</summary>
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

        /// <summary>Drives one full fight; returns whether the player won.</summary>
        private static bool RunFightToCompletion(
            RunOrchestrator orchestrator,
            bool chooseIfUnchosen = true)
        {
            Assert.IsTrue(orchestrator.CanStartBattle(out string reason), reason);
            if (chooseIfUnchosen
                && orchestrator.State.FightOptions is { Count: > 0 }
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
