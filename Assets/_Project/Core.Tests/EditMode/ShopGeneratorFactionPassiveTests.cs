using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 W1b: Crimson Assembly's "Ahead of Schedule"
    /// rarity-odds shift and the global salvage pity timer (§1.5), at the ShopGenerator
    /// level with synthetic registries (no faction content pass required).</summary>
    public sealed class ShopGeneratorFactionPassiveTests
    {
        private static BoardState EmptyBoard() => new BoardState(TestBoards.Layout);

        private static PieceDefinition Piece(
            string id,
            Rarity rarity,
            string factionId,
            string combatRole = null) => new()
            {
                Id = id,
                DisplayName = id,
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                CombatRole = combatRole ?? GameTagIds.Assault,
                FactionId = factionId,
                MaxHp = 10,
                Rarity = rarity
            };

        // ---- Crimson rarity-odds shift ----

        private static ContentRegistry MixedRarityRegistryFor(params string[] factionIds)
        {
            var registry = new ContentRegistry();
            foreach (var factionId in factionIds)
            {
                for (int i = 0; i < 3; i++)
                    registry.Register(Piece($"{factionId}_common_{i}", Rarity.Common, factionId), ShopLane.Offensive);
                for (int i = 0; i < 2; i++)
                    registry.Register(Piece($"{factionId}_uncommon_{i}", Rarity.Uncommon, factionId), ShopLane.Offensive);
                registry.Register(Piece($"{factionId}_rare_0", Rarity.Rare, factionId), ShopLane.Offensive);
            }

            return registry;
        }

        [Test]
        public void Crimson_RollsRarerOffers_ThanAnotherFaction_AtTheSameFightEquivalent()
        {
            var registry = MixedRarityRegistryFor(FactionIds.IronmarchUnion, FactionIds.CrimsonAssembly);
            var generator = new ShopGenerator(registry);

            int CountRares(string factionId)
            {
                int rares = 0;
                // round 8: IronMarch reads the 7-8 row (10% rare); Crimson's +1 pushes it
                // into the 9+ row (15% rare) — a comfortably large, stable gap over 300 seeds.
                for (int seed = 1; seed <= 300; seed++)
                {
                    var shop = generator.Generate(EmptyBoard(), factionId, round: 8, seed: seed);
                    rares += shop.Offers.Count(o => registry.GetById(o.PieceId).Rarity == Rarity.Rare);
                }

                return rares;
            }

            int ironMarchRares = CountRares(FactionIds.IronmarchUnion);
            int crimsonRares = CountRares(FactionIds.CrimsonAssembly);

            Assert.That(crimsonRares, Is.GreaterThan(ironMarchRares),
                $"Crimson must roll odds as if FightEquivalent+1 (IronMarch {ironMarchRares}, Crimson {crimsonRares})");
        }

        [Test]
        public void Crimson_Price_UsesTheRealFightEquivalent_NotTheBoostedOne()
        {
            // Both factions draw an identical Common pool at the same round: the Dread
            // price tax must be identical (`round - 1`) even though Crimson's ODDS are
            // computed off round+1 — price and odds must not share the same lookup.
            var registry = new ContentRegistry();
            registry.Register(Piece("iron_common", Rarity.Common, FactionIds.IronmarchUnion), ShopLane.Offensive);
            registry.Register(Piece("crimson_common", Rarity.Common, FactionIds.CrimsonAssembly), ShopLane.Offensive);
            var generator = new ShopGenerator(registry);

            const int round = 4;
            var ironShop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion, round, seed: 1);
            var crimsonShop = generator.Generate(EmptyBoard(), FactionIds.CrimsonAssembly, round, seed: 1);

            var ironOffer = ironShop.Offers.First(o => o.PieceId == "iron_common");
            var crimsonOffer = crimsonShop.Offers.First(o => o.PieceId == "crimson_common");

            Assert.AreEqual(ironOffer.GoldPrice, crimsonOffer.GoldPrice,
                "same rarity/round/discount must price identically regardless of the odds passive");
        }

        // ---- Salvage pity timer ----

        [Test]
        public void SalvagePity_ForcesASalvageOffer_AtTheDryBatchThreshold()
        {
            const string enemyFaction = "crimson_legion";
            var registry = RichPlayerRegistry(FactionIds.IronmarchUnion, enemyFaction);
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(
                EmptyBoard(), FactionIds.IronmarchUnion, round: 1, seed: 1,
                lastEnemyFactionId: enemyFaction,
                salvageChancePercent: 0, // odds alone would never roll Salvage — isolates the pity force
                salvagePityBatches: FactionPassives.SalvagePityDryBatchThresholdDefault);

            Assert.IsTrue(shop.Offers.Any(o => o.IsSalvaged),
                "the dry-batch threshold must force a salvage-source offer even at 0% salvage chance");
        }

        [Test]
        public void SalvagePity_BelowThreshold_DoesNotForceAnything()
        {
            const string enemyFaction = "crimson_legion";
            var registry = RichPlayerRegistry(FactionIds.IronmarchUnion, enemyFaction);
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(
                EmptyBoard(), FactionIds.IronmarchUnion, round: 1, seed: 1,
                lastEnemyFactionId: enemyFaction,
                salvageChancePercent: 0,
                salvagePityBatches: FactionPassives.SalvagePityDryBatchThresholdDefault - 1);

            Assert.IsFalse(shop.Offers.Any(o => o.IsSalvaged),
                "one short of the threshold: nothing forced, 0% salvage chance rolls no salvage, " +
                "and a well-stocked own-faction/neutral pool never exhausts into the fallback chain");
        }

        [Test]
        public void SalvagePity_DustScourgeForcesAtTwo_NotFour()
        {
            const string enemyFaction = "crimson_legion";
            var registry = RichPlayerRegistry(FactionIds.DustScourge, enemyFaction);
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(
                EmptyBoard(), FactionIds.DustScourge, round: 1, seed: 1,
                lastEnemyFactionId: enemyFaction,
                salvageChancePercent: 0,
                salvagePityBatches: FactionPassives.SalvagePityDryBatchThresholdDustScourge);

            Assert.IsTrue(shop.Offers.Any(o => o.IsSalvaged),
                "Dust Scourge's dry-batch threshold is 2, not the global 4");
        }

        /// <summary>Enough distinct player-faction pieces in BOTH lanes (3 offensive + 2
        /// defensive, matching the 5 visible slots) plus a neutral piece per lane, so the
        /// pre-existing "no duplicate piece within a batch" rule never exhausts the
        /// Faction/Neutral pools into the Salvage fallback on its own — isolating what
        /// these tests actually want to assert: the PITY mechanism alone.</summary>
        private static ContentRegistry RichPlayerRegistry(string playerFactionId, string enemyFactionId)
        {
            var registry = new ContentRegistry();
            for (int i = 0; i < 3; i++)
                registry.Register(Piece($"{playerFactionId}_off_{i}", Rarity.Common, playerFactionId), ShopLane.Offensive);
            for (int i = 0; i < 2; i++)
                registry.Register(Piece($"{playerFactionId}_def_{i}", Rarity.Common, playerFactionId), ShopLane.Defensive);
            registry.Register(Piece("neutral_off", Rarity.Common, "neutral"), ShopLane.Offensive);
            registry.Register(Piece("neutral_def", Rarity.Common, "neutral"), ShopLane.Defensive);
            registry.Register(Piece("enemy_off", Rarity.Common, enemyFactionId), ShopLane.Offensive);
            return registry;
        }
    }
}
