using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests
{
    /// <summary>M3 rarity in the shop generator: SO→Core plumbing, Dread-weighted
    /// tier rolls, lane fallback (down, never up), the pity force at the guarantee,
    /// and the hard-victory salvage upweight. All assertions run at fixed seeds.</summary>
    public sealed class ShopGeneratorRarityTests
    {
        private static BoardState EmptyBoard() => new BoardState(TestBoards.Layout);

        // ---- SO → Core plumbing ----

        [Test]
        public void PieceDefinitionSO_PlumbsRarityToCore_AndDefaultsCommon()
        {
            var so = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            try
            {
                so.id = "so_rarity_probe";
                so.displayName = "Probe";
                so.primary = GameTagIds.Infantry;
                so.combatRole = GameTagIds.Assault;
                so.systemTag = string.Empty;

                Assert.AreEqual(Rarity.Common, so.ToCore().Rarity,
                    "unregenerated assets must default Common and stay valid");

                so.rarity = Rarity.Rare;
                Assert.AreEqual(Rarity.Rare, so.ToCore().Rarity);
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        // ---- tier weighting / determinism ----

        [Test]
        public void SameSeedAndPity_ProduceIdenticalOffers()
        {
            var registry = MixedRarityRegistry();
            var generator = new ShopGenerator(registry);

            var a = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion,
                round: 4, seed: 42, rarePityBatches: 4);
            var b = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion,
                round: 4, seed: 42, rarePityBatches: 4);

            CollectionAssert.AreEqual(
                a.Offers.Select(o => $"{o.SlotIndex}:{o.PieceId}").ToList(),
                b.Offers.Select(o => $"{o.SlotIndex}:{o.PieceId}").ToList());
        }

        [Test]
        public void RareOffers_GetMoreFrequent_AsTheClockClimbs()
        {
            var registry = MixedRarityRegistry();
            var generator = new ShopGenerator(registry);

            int CountRareOffers(int round)
            {
                int rares = 0;
                for (int seed = 1; seed <= 200; seed++)
                {
                    var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion, round, seed);
                    rares += shop.Offers.Count(o => registry.GetById(o.PieceId).Rarity == Rarity.Rare);
                }

                return rares;
            }

            int early = CountRareOffers(round: 1);  // 2% rare tier
            int late = CountRareOffers(round: 10);  // 15% rare tier

            Assert.That(late, Is.GreaterThan(early * 2),
                $"rare offers must scale with the clock (early {early}, late {late})");
            // ~1200 offers per pass; the early rare share must stay near the table's 2%.
            Assert.That(early, Is.LessThan(120), $"early rare count {early}");
        }

        [Test]
        public void FilterTierWithFallback_FallsDown_NeverUp()
        {
            var common = Piece("c", Rarity.Common);
            var rare = Piece("r", Rarity.Rare);
            var pool = new List<PieceDefinition> { common, rare };

            CollectionAssert.AreEquivalent(new[] { rare },
                ShopGenerator.FilterTierWithFallback(pool, Rarity.Rare));
            CollectionAssert.AreEquivalent(new[] { common },
                ShopGenerator.FilterTierWithFallback(pool, Rarity.Uncommon),
                "an uncommon roll with no uncommons falls DOWN to common, never up to rare");
            CollectionAssert.AreEquivalent(new[] { common },
                ShopGenerator.FilterTierWithFallback(pool, Rarity.Common));

            // Last resort: nothing at or below the rolled tier keeps the slot stocked.
            var onlyRare = new List<PieceDefinition> { rare };
            CollectionAssert.AreEquivalent(new[] { rare },
                ShopGenerator.FilterTierWithFallback(onlyRare, Rarity.Common));
        }

        [Test]
        public void CommonOnlyLane_NeverServesAboveItsRoll()
        {
            // Pool has commons and rares but NO uncommons: every uncommon roll must
            // land on a common, so the rare share stays near the table's rare percent
            // (a promote-up bug would push it toward rare+uncommon combined).
            var registry = new ContentRegistry();
            for (int i = 0; i < 3; i++)
                registry.Register(Piece($"common_{i}", Rarity.Common), ShopLane.Offensive);
            registry.Register(Piece("rare_0", Rarity.Rare), ShopLane.Offensive);
            for (int i = 0; i < 3; i++)
                registry.Register(Piece($"def_common_{i}", Rarity.Common, GameTagIds.Support), ShopLane.Defensive);

            var generator = new ShopGenerator(registry);
            int rares = 0;
            int total = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion, round: 1, seed: seed);
                rares += shop.Offers.Count(o => registry.GetById(o.PieceId).Rarity == Rarity.Rare);
                total += shop.Offers.Count;
            }

            // Rare tier is 2% at round 1; 18% uncommon rolls falling UP would read ~20%.
            Assert.That(rares, Is.LessThan(total / 10),
                $"rare offers {rares}/{total} — uncommon rolls must fall down, not up");
        }

        // ---- pity force ----

        [Test]
        public void PityGuarantee_ForcesARareCapableSlot_AcrossManySeeds()
        {
            var registry = MixedRarityRegistry();
            var generator = new ShopGenerator(registry);

            for (int seed = 1; seed <= 40; seed++)
            {
                var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion,
                    round: 1, seed: seed,
                    rarePityBatches: RarityWeights.PityGuaranteeBatches);

                Assert.IsTrue(ShopGenerator.ContainsRareOrAbove(shop.Offers, registry),
                    $"seed {seed}: the guarantee batch must include a rare");
            }
        }

        [Test]
        public void PityGuarantee_WithNoRareCapableLane_StillFillsTheShop()
        {
            var registry = new ContentRegistry();
            for (int i = 0; i < 4; i++)
                registry.Register(Piece($"common_{i}", Rarity.Common), ShopLane.Offensive);
            for (int i = 0; i < 4; i++)
                registry.Register(Piece($"def_common_{i}", Rarity.Common, GameTagIds.Support), ShopLane.Defensive);

            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion,
                round: 1, seed: 7, rarePityBatches: 20);

            Assert.AreEqual(ShopSlotLayoutResolver.VisibleOfferSlotCount, shop.Offers.Count,
                "no rare anywhere: nothing is forced, offers still roll");
            Assert.IsFalse(ShopGenerator.ContainsRareOrAbove(shop.Offers, registry),
                "the orchestrator's counter keeps climbing in this case");
        }

        // ---- salvage quality ----

        [Test]
        public void HardVictorySalvage_UpweightsRarerSpoils()
        {
            const string enemyFaction = "crimson_assembly";
            var registry = MixedRarityRegistry();
            registry.Register(Piece("spoil_common", Rarity.Common, factionId: enemyFaction), ShopLane.Offensive);
            registry.Register(Piece("spoil_uncommon", Rarity.Uncommon, factionId: enemyFaction), ShopLane.Offensive);
            registry.Register(Piece("spoil_rare", Rarity.Rare, factionId: enemyFaction), ShopLane.Offensive);

            var generator = new ShopGenerator(registry);

            int CountRareSalvage(bool boost)
            {
                int rares = 0;
                for (int seed = 1; seed <= 300; seed++)
                {
                    var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion,
                        round: 1, seed: seed,
                        lastEnemyFactionId: enemyFaction,
                        salvageChancePercent: 50,
                        salvageRareBoost: boost);

                    rares += shop.Offers.Count(o =>
                        o.IsSalvaged && registry.GetById(o.PieceId).Rarity == Rarity.Rare);
                }

                return rares;
            }

            int normal = CountRareSalvage(boost: false);
            int boosted = CountRareSalvage(boost: true);

            Assert.That(boosted, Is.GreaterThan(normal),
                $"hard-victory salvage must skew rarer (normal {normal}, boosted {boosted})");
        }

        // ---- fixtures ----

        /// <summary>3 commons + 2 uncommons + 1 rare per lane, IronMarch faction.</summary>
        private static ContentRegistry MixedRarityRegistry()
        {
            var registry = new ContentRegistry();
            foreach (var (role, lane) in new[]
                     {
                         (GameTagIds.Assault, ShopLane.Offensive),
                         (GameTagIds.Support, ShopLane.Defensive)
                     })
            {
                for (int i = 0; i < 3; i++)
                    registry.Register(Piece($"{lane}_common_{i}", Rarity.Common, role), lane);
                for (int i = 0; i < 2; i++)
                    registry.Register(Piece($"{lane}_uncommon_{i}", Rarity.Uncommon, role), lane);
                registry.Register(Piece($"{lane}_rare_0", Rarity.Rare, role), lane);
            }

            return registry;
        }

        private static PieceDefinition Piece(
            string id,
            Rarity rarity,
            string combatRole = null,
            string factionId = null) => new()
            {
                Id = id,
                DisplayName = id,
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                CombatRole = combatRole ?? GameTagIds.Assault,
                FactionId = factionId ?? FactionIds.IronmarchUnion,
                MaxHp = 10,
                Rarity = rarity
            };
    }
}
