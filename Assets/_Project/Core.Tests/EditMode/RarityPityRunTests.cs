using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>M3 pity through the orchestrator (both Generate call sites: the
    /// round's RefreshShop and every reroll), save round-trip, and seeded-run
    /// determinism with pity active. Content-agnostic on purpose: assets may not
    /// carry rarity until the next content regen, so these tests assert the
    /// counter's TRANSITIONS against whatever the shop actually shows (the forced
    /// rare guarantee itself is covered at the generator level with synthetic
    /// registries in ShopGeneratorRarityTests).</summary>
    public sealed class RarityPityRunTests
    {
        private ContentDatabase _database;
        private ContentRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
            }

            _registry = _database.BuildRegistry();
            SaveManager.DeleteSave();
        }

        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        [Test]
        public void RoundRoll_CountsAsABatch_AndTracksRareAppearance()
        {
            var orchestrator = StartRun(4242);

            bool rareShown = ShopGenerator.ContainsRareOrAbove(
                orchestrator.State.Shop.Offers, _registry);
            Assert.AreEqual(rareShown ? 0 : 1, orchestrator.State.RarePityBatches,
                "the initial round roll is a counted batch: reset on a rare, else 1");
        }

        [Test]
        public void EveryReroll_CountsAsABatch_IncrementingOrResetting()
        {
            var orchestrator = StartRun(4242);

            for (int i = 0; i < 5; i++)
            {
                int before = orchestrator.State.RarePityBatches;
                orchestrator.State.Supplies = 999;
                orchestrator.State.Authority = 99;
                Assert.IsTrue(orchestrator.TryRerollShop(), $"reroll {i}");

                bool rareShown = ShopGenerator.ContainsRareOrAbove(
                    orchestrator.State.Shop.Offers, _registry);
                Assert.AreEqual(
                    rareShown ? 0 : before + 1,
                    orchestrator.State.RarePityBatches,
                    $"reroll {i}: batch-with-rare resets, rare-less batch increments");
            }
        }

        [Test]
        public void PityAndSalvageBoost_SurviveTheSaveRoundTrip()
        {
            var orchestrator = StartRun(555);
            orchestrator.State.RarePityBatches = 5;
            orchestrator.State.SalvageHardBoost = true;
            orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(5, reloaded.State.RarePityBatches);
            Assert.IsTrue(reloaded.State.SalvageHardBoost);
        }

        [Test]
        public void OlderV9Saves_DeserializeWithZeroPity()
        {
            // Additive fields on schema v9: a save written before M3 has no keys.
            var state = RunSaveSerializer.FromJson("{\"SaveSchemaVersion\":9}");

            Assert.AreEqual(0, state.RarePityBatches);
            Assert.IsFalse(state.SalvageHardBoost);
        }

        [Test]
        public void SeededRun_WithSameRerollSequence_ProducesIdenticalOfferStreams()
        {
            var first = CollectOfferStream(runSeed: 777, rerolls: 4, out int pityA);
            SaveManager.DeleteSave();
            var second = CollectOfferStream(runSeed: 777, rerolls: 4, out int pityB);

            CollectionAssert.AreEqual(first, second,
                "pity is state-derived, not extra randomness — same seed + same " +
                "reroll sequence must replay the identical offer stream");
            Assert.AreEqual(pityA, pityB);
        }

        // ---- fixtures ----

        private RunOrchestrator StartRun(int runSeed)
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed);
            return orchestrator;
        }

        private List<string> CollectOfferStream(int runSeed, int rerolls, out int finalPity)
        {
            var orchestrator = StartRun(runSeed);
            var stream = new List<string> { Fingerprint(orchestrator.State.Shop.Offers) };

            for (int i = 0; i < rerolls; i++)
            {
                orchestrator.State.Supplies = 999;
                orchestrator.State.Authority = 99;
                Assert.IsTrue(orchestrator.TryRerollShop(), $"reroll {i}");
                stream.Add(Fingerprint(orchestrator.State.Shop.Offers));
            }

            finalPity = orchestrator.State.RarePityBatches;
            return stream;
        }

        private static string Fingerprint(IEnumerable<ShopOffer> offers) =>
            string.Join(";", offers
                .OrderBy(o => o.SlotIndex)
                .Select(o => $"{o.SlotIndex}:{o.PieceId}:{o.GoldPrice}"));
    }
}
