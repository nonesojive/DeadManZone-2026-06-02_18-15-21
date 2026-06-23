using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class SalvageShopGeneratorTests
    {
        private const string PlayerFactionId = FactionIds.IronVanguard;
        private const string EnemyFactionId = "FactionIds.DustScourge";

        private static ContentRegistry CreateSalvageRegistry()
        {
            var registry = new ContentRegistry();
            registry.Register(CreateUnit("iron_offensive_a", PlayerFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("iron_offensive_b", PlayerFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("iron_offensive_c", PlayerFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("iron_offensive_d", PlayerFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("dust_offensive_a", EnemyFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("dust_offensive_b", EnemyFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("dust_offensive_c", EnemyFactionId), ShopLane.Offensive);
            registry.Register(CreateUnit("dust_offensive_d", EnemyFactionId), ShopLane.Offensive);
            registry.Register(CreateBuilding("iron_defensive_a", PlayerFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("iron_defensive_b", PlayerFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("iron_defensive_c", PlayerFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("iron_defensive_d", PlayerFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("dust_defensive_a", EnemyFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("dust_defensive_b", EnemyFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("dust_defensive_c", EnemyFactionId), ShopLane.Defensive);
            registry.Register(CreateBuilding("dust_defensive_d", EnemyFactionId), ShopLane.Defensive);
            return registry;
        }

        private static PieceDefinition CreateUnit(string id, string factionId) => new()
        {
            Id = id,
            DisplayName = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            GoldCost = 5,
            FactionId = factionId
        };

        private static PieceDefinition CreateBuilding(string id, string factionId) => new()
        {
            Id = id,
            DisplayName = id,
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            GoldCost = 4,
            FactionId = factionId,
            CombatRole = GameTagIds.Support
        };

        [Test]
        public void HighSalvageLeg_YieldsMostlySalvagedOffers()
        {
            var board = new BoardState(TestBoards.Layout);
            var registry = CreateSalvageRegistry();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(
                board,
                PlayerFactionId,
                round: 1,
                seed: 42,
                lastEnemyFactionId: EnemyFactionId,
                salvageChancePercent: 90);

            Assert.That(shop.Offers, Is.Not.Empty);
            int salvaged = shop.Offers.Count(o => o.IsSalvaged);
            Assert.That(salvaged, Is.GreaterThan(shop.Offers.Count / 2));
            Assert.IsTrue(shop.Offers.Where(o => o.IsSalvaged).All(o =>
                registry.GetById(o.PieceId).FactionId == EnemyFactionId));
        }

        [Test]
        public void ZeroSalvageChance_YieldsNoSalvagedOffers()
        {
            var board = new BoardState(TestBoards.Layout);
            var registry = CreateSalvageRegistry();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(
                board,
                PlayerFactionId,
                round: 1,
                seed: 42,
                lastEnemyFactionId: EnemyFactionId,
                salvageChancePercent: 0);

            Assert.IsFalse(shop.Offers.Any(o => o.IsSalvaged));
        }

        [Test]
        public void SameSeed_ProducesSameSalvageOffers()
        {
            var board = new BoardState(TestBoards.Layout);
            var registry = CreateSalvageRegistry();
            var generator = new ShopGenerator(registry);

            var shopA = generator.Generate(
                board,
                PlayerFactionId,
                round: 1,
                seed: 77,
                lastEnemyFactionId: EnemyFactionId,
                salvageChancePercent: 50);

            var shopB = generator.Generate(
                board,
                PlayerFactionId,
                round: 1,
                seed: 77,
                lastEnemyFactionId: EnemyFactionId,
                salvageChancePercent: 50);

            Assert.AreEqual(shopA.Offers.Count, shopB.Offers.Count);
            for (int i = 0; i < shopA.Offers.Count; i++)
            {
                Assert.AreEqual(shopA.Offers[i].PieceId, shopB.Offers[i].PieceId);
                Assert.AreEqual(shopA.Offers[i].IsSalvaged, shopB.Offers[i].IsSalvaged);
            }
        }

        [Test]
        public void SalvagePool_ExcludesPlayerFaction()
        {
            var registry = new ContentRegistry();
            registry.Register(CreateUnit("shared_faction_unit", PlayerFactionId), ShopLane.Offensive);

            var pool = SalvageShopPool.GetPool(
                registry,
                ShopLane.Offensive,
                lastEnemyFactionId: PlayerFactionId,
                playerFactionId: PlayerFactionId,
                fightIndex: 1);

            Assert.IsEmpty(pool);
        }
    }
}
