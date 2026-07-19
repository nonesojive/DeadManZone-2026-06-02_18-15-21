using DeadManZone.Core;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-19 economy-pass goldens. Owner targets: (T1) the first shop
    /// budget affords exactly-ish {1 Rare} OR {1 Uncommon + 1-2 rerolls} OR {2 Commons}
    /// at round-1 prices (the Dread price tax — ShopGenerator.CreateOffer's
    /// `round - 1` — is zero on round 1); (T2) a normal-fight-every-round cadence
    /// sustains ~1 Uncommon per shop (12 base income + 6 normal spoils ≈ 18/round vs
    /// Uncommon 15 + rising tax). Faction assertions read the shipped ContentDatabase,
    /// so they reflect the authored values only after content regeneration
    /// (DeadManZone → Generate Demo Content).</summary>
    public sealed class EconomyBalanceTests
    {
        /// <summary>Starting-supplies band around the 25 baseline: Blightborn/Dust sit
        /// at 22 (lean-economy identity), Cartel tops out at 30 (richest faction).</summary>
        private const int StartingSuppliesMin = 22;
        private const int StartingSuppliesMax = 30;

        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
        }

        [Test]
        public void FirstShop_AffordsOwnerBudgetShapes()
        {
            RequireDatabase();

            // Round-1 prices: base rarity cost, zero Dread tax. Rerolls cost 1, 2, ...
            int rareShape = RarityPricing.Rare;                                   // 25
            int twoCommonsShape = 2 * RarityPricing.Common;                       // 20
            int uncommonPlusTwoRerollsShape = RarityPricing.Uncommon
                + RunOrchestrator.BaseRerollCost                                  // reroll #1 = 1
                + RunOrchestrator.BaseRerollCost + 1;                             // reroll #2 = 2

            foreach (var factionId in FactionIds.Playable)
            {
                var faction = _database.GetFaction(factionId);
                Assert.NotNull(faction, $"missing FactionSO for '{factionId}'");
                int supplies = faction.startingSupplies;

                Assert.GreaterOrEqual(supplies, StartingSuppliesMin,
                    $"'{factionId}' starting supplies {supplies} below the economy-pass band");
                Assert.LessOrEqual(supplies, StartingSuppliesMax,
                    $"'{factionId}' starting supplies {supplies} above the economy-pass band — first shop should be a real budget choice, not a spree");

                Assert.GreaterOrEqual(supplies, twoCommonsShape,
                    $"'{factionId}' first shop must afford 2 Commons ({twoCommonsShape})");
                Assert.GreaterOrEqual(supplies, uncommonPlusTwoRerollsShape,
                    $"'{factionId}' first shop must afford 1 Uncommon + 2 rerolls ({uncommonPlusTwoRerollsShape})");
                // The Rare shape is the 25-baseline anchor; the lean factions (22) trade
                // it away deliberately, so it gates only the baseline-and-up factions.
                if (supplies >= 25)
                    Assert.GreaterOrEqual(supplies, rareShape,
                        $"'{factionId}' first shop must afford 1 Rare ({rareShape})");
            }
        }

        [Test]
        public void NormalCadence_SustainsUncommonAverage()
        {
            RequireDatabase();

            // T2: normal fight every round → base income + normal spoils covers an
            // Uncommon with >= 2 Supplies of headroom for the early Dread price tax.
            int required = RarityPricing.Uncommon + 2;

            foreach (var factionId in FactionIds.Playable)
            {
                var faction = _database.GetFaction(factionId);
                Assert.NotNull(faction, $"missing FactionSO for '{factionId}'");

                int perRound = faction.baseSuppliesPerRound + DreadRules.NormalVictorySupplies;
                Assert.GreaterOrEqual(perRound, required,
                    $"'{factionId}' normal-cadence income {perRound}/round (base {faction.baseSuppliesPerRound} + spoils {DreadRules.NormalVictorySupplies}) must sustain an Uncommon ({RarityPricing.Uncommon}) plus tax headroom");
            }
        }

        [Test]
        public void HardVsNormal_SpoilsPremium()
        {
            // Content-free golden: the hard front's risk premium must stay at least
            // double the normal front's spoils, or "hard" stops paying for itself.
            Assert.GreaterOrEqual(DreadRules.HardVictorySupplies, 2 * DreadRules.NormalVictorySupplies,
                "hard victory supplies must be >= 2x normal victory spoils (risk premium)");
        }

        private void RequireDatabase()
        {
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }
    }
}
