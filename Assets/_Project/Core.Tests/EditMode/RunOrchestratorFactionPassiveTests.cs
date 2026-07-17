using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 W1b, orchestrator-level integration coverage
    /// for the economy/shop passives: the Cartel mercenary slot (real acquisition +
    /// save round trip), the salvage pity counter (hold/persist), Blightborn's Despair
    /// Dividend, and Paradox's free first reroll. Only IronMarch/Dust Scourge/Cartel have
    /// a FactionSO today — Blightborn/Paradox are exercised by overriding State.FactionId
    /// after a normal IronMarch start (those code paths only read the string id, never the
    /// FactionSO), since no content pass exists for them yet.</summary>
    public sealed class RunOrchestratorFactionPassiveTests
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

        // ---- Cartel mercenary slot ----

        [Test]
        public void CartelRun_ShopOffersAMercenarySlot_AndAcquisitionMarksThePieceInstance()
        {
            var faction = _database.GetFaction(FactionIds.CartelOfEchoes);
            if (faction == null)
                Assert.Ignore("no Cartel of Echoes FactionSO in this content set.");

            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.CartelOfEchoes, runSeed: 111);

            var mercOffer = orchestrator.State.Shop.Offers
                .FirstOrDefault(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex);
            Assert.IsNotNull(mercOffer, "Cartel must roll a mercenary offer as long as any off-faction fighter is registered.");
            Assert.IsTrue(mercOffer.IsMercenary);

            orchestrator.State.Supplies = 9999;
            orchestrator.State.Authority = 99;
            Assert.IsTrue(orchestrator.TryAcquireOfferToReserves(mercOffer.OfferId, new GridCoord(0, 0)));

            var placed = orchestrator.GetReserves().Pieces.Single();
            Assert.IsTrue(placed.IsMercenary, "the acquired instance must carry the permanent mercenary flag");

            // Save round trip: the flag must survive JSON serialize/deserialize.
            orchestrator.SaveAndExit();
            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            var reloadedPlaced = reloaded.GetReserves().Pieces.Single();
            Assert.IsTrue(reloadedPlaced.IsMercenary, "IsMercenary must round-trip through the save schema");
        }

        [Test]
        public void MercenarySellsForZero_FromReserves()
        {
            var faction = _database.GetFaction(FactionIds.CartelOfEchoes);
            if (faction == null)
                Assert.Ignore("no Cartel of Echoes FactionSO in this content set.");

            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.CartelOfEchoes, runSeed: 222);

            var mercOffer = orchestrator.State.Shop.Offers
                .FirstOrDefault(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex);
            Assert.IsNotNull(mercOffer);

            orchestrator.State.Supplies = 9999;
            orchestrator.State.Authority = 99;
            Assert.IsTrue(orchestrator.TryAcquireOfferToReserves(mercOffer.OfferId, new GridCoord(0, 0)));

            int suppliesBeforeSell = orchestrator.State.Supplies;
            var placed = orchestrator.GetReserves().Pieces.Single();
            Assert.IsTrue(orchestrator.TrySellFromReserves(placed.InstanceId));

            Assert.AreEqual(suppliesBeforeSell, orchestrator.State.Supplies,
                "a mercenary must sell for 0 Supplies (and Authority/Manpower) — no salvage value left to recoup");
        }

        // ---- Salvage pity ----

        [Test]
        public void FreshRun_SalvagePityHoldsAtZero_WhileNoSalvageablePoolExists()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 4242);

            // Fresh run: no fight completed yet, so LastEnemyFactionId is unset — the
            // salvage pool is empty and the pity counter must HOLD at 0.
            Assert.AreEqual(0, orchestrator.State.SalvagePityBatches);

            orchestrator.State.Supplies = 999;
            orchestrator.State.Authority = 99;
            Assert.IsTrue(orchestrator.TryRerollShop());
            Assert.AreEqual(0, orchestrator.State.SalvagePityBatches,
                "the counter must not climb while there's nothing to salvage yet");
        }

        [Test]
        public void SalvagePityBatches_SurvivesTheSaveRoundTrip()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 555);
            orchestrator.State.SalvagePityBatches = 3;
            orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(3, reloaded.State.SalvagePityBatches);
        }

        [Test]
        public void OlderV9Saves_DeserializeWithZeroSalvagePity()
        {
            var state = RunSaveSerializer.FromJson("{\"SaveSchemaVersion\":9}");
            Assert.AreEqual(0, state.SalvagePityBatches);
        }

        // ---- Blightborn Despair Dividend / Paradox free reroll ----
        // No FactionSO exists yet for these two — the passives only ever read the string
        // State.FactionId, so overriding it after a normal start exercises the real code
        // path without needing content that lands in W2.

        [Test]
        public void DespairDividend_GrantsSuppliesPerRoutedEnemy_ForBlightbornOnly()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 333);
            orchestrator.State.FactionId = FactionIds.BlightbornPact;

            Assert.AreEqual(6, FactionPassives.DespairDividendSupplies(orchestrator.State.FactionId, 6));
            Assert.AreEqual(0, FactionPassives.DespairDividendSupplies(FactionIds.IronmarchUnion, 6));
        }

        [Test]
        public void ParadoxFreeReroll_FirstRerollIsFree_SubsequentAreNot()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 444);
            orchestrator.State.FactionId = FactionIds.ParadoxEngine;
            orchestrator.State.Supplies = 0;
            orchestrator.State.Authority = 99;

            Assert.AreEqual(0, orchestrator.ComputeRerollGoldCost(), "first reroll each Build must be free for Paradox");
            Assert.IsTrue(orchestrator.TryRerollShop(), "must succeed with 0 Supplies since the first reroll is free");
            Assert.AreEqual(0, orchestrator.State.Supplies, "the free reroll must not charge anything");

            Assert.AreEqual(RunOrchestrator.BaseRerollCost + 1, orchestrator.ComputeRerollGoldCost(),
                "the SECOND reroll this Build is not free");
        }

        [Test]
        public void NonParadoxFaction_RerollAlwaysCosts()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 445);

            Assert.AreEqual(RunOrchestrator.BaseRerollCost, orchestrator.ComputeRerollGoldCost());
        }
    }
}
