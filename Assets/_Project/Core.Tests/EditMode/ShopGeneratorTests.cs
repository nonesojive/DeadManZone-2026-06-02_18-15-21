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
    public class ShopGeneratorTests
    {
        private static BoardLayout DefaultLayout() => TestBoards.Layout;

        private static BoardState BuildBoardWithSupplyDepot() => TestBoards.WithSupplyDepot();

        [Test]
        public void DefaultBoard_GeneratesSixVisibleOffers()
        {
            var board = new BoardState(DefaultLayout());
            var registry = CreateRoleTestRegistry();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 42);

            Assert.AreEqual(ShopSlotLayoutResolver.VisibleOfferSlotCount, shop.Offers.Count);
        }

        [Test]
        public void SupplyDepot_AppliesGoldDiscount()
        {
            var board = BuildBoardWithSupplyDepot();
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, factionId: FactionIds.IronVanguard, round: 2, seed: 999);

            Assert.That(shop.Modifiers.GoldDiscountPercent, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void SameSeed_ProducesSameOffers()
        {
            var board = new BoardState(DefaultLayout());
            var registry = CreateRoleTestRegistry();
            var generator = new ShopGenerator(registry);

            var shopA = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 42);
            var shopB = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 42);

            Assert.AreEqual(shopA.Offers.Count, shopB.Offers.Count);
            for (int i = 0; i < shopA.Offers.Count; i++)
            {
                Assert.AreEqual(shopA.Offers[i].PieceId, shopB.Offers[i].PieceId);
                Assert.AreEqual(shopA.Offers[i].SlotIndex, shopB.Offers[i].SlotIndex);
            }
        }

        [Test]
        public void CommandBunker_DoesNotAddExtraShopSlots()
        {
            var board = TestBoards.WithCommandBunker();

            var registry = CreateRoleTestRegistry();
            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 100);

            Assert.AreEqual(ShopSlotLayoutResolver.VisibleOfferSlotCount, shop.Offers.Count);
            Assert.That(shop.Modifiers.ExtraGeneralSlots, Is.EqualTo(1));
        }

        [Test]
        public void GoldDiscount_ReducesOfferGoldPrice()
        {
            var boardWithout = new BoardState(DefaultLayout());
            var boardWith = BuildBoardWithSupplyDepot();
            var registry = CreateRoleTestRegistry();
            var generator = new ShopGenerator(registry);

            var shopWithout = generator.Generate(boardWithout, FactionIds.IronVanguard, round: 1, seed: 50);
            var shopWith = generator.Generate(boardWith, FactionIds.IronVanguard, round: 1, seed: 50);

            var generalWithout = shopWithout.Offers.First(o => o.GoldPrice > 0);
            var matchingWith = shopWith.Offers.First(o => o.PieceId == generalWithout.PieceId);

            Assert.That(matchingWith.GoldPrice, Is.LessThan(generalWithout.GoldPrice));
        }

        [Test]
        public void FieldWorkshop_GuaranteesEngineerOffer()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(TestPieces.FieldWorkshop(), new GridCoord(0, 0)).Success);
            var board = new BuildBoardSet
            {
                Combat = new BoardState(DefaultLayout()),
                Hq = hq
            }.ToAggregateBoard();

            var registry = new ContentRegistry();
            registry.Register(TestPieces.RifleSquad(), ShopLane.Offensive);
            registry.Register(TestPieces.CommandBunker(), ShopLane.Defensive);
            registry.Register(TestPieces.FieldWorkshop(), ShopLane.Defensive);

            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 7);

            Assert.That(shop.Offers.Any(o => o.Lane == ShopLane.Defensive), Is.True);
            Assert.IsTrue(shop.Modifiers.GuaranteeEngineerOffer);
        }

        [Test]
        public void Generate_NeverUsesSpecialtyLane()
        {
            var board = new BoardState(DefaultLayout());
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(board, FactionIds.IronVanguard, round: 1, seed: 42);

            Assert.IsFalse(shop.Offers.Any(o => o.Lane == ShopLane.Specialty));
        }

        private static ContentRegistry CreateRoleTestRegistry()
        {
            var registry = new ContentRegistry();
            registry.Register(CreateFactionUnit("offensive_assault_a", GameTagIds.Assault), ShopLane.Offensive);
            registry.Register(CreateFactionUnit("offensive_assault_b", GameTagIds.Assault), ShopLane.Offensive);
            registry.Register(CreateFactionUnit("offensive_assault_c", GameTagIds.Assault), ShopLane.Offensive);
            registry.Register(CreateFactionUnit("offensive_assault_d", GameTagIds.Assault), ShopLane.Offensive);
            registry.Register(CreateFactionUnit("defensive_support_a", GameTagIds.Support), ShopLane.Defensive);
            registry.Register(CreateFactionUnit("defensive_support_b", GameTagIds.Support), ShopLane.Defensive);
            registry.Register(CreateFactionUnit("defensive_support_c", GameTagIds.Support), ShopLane.Defensive);
            registry.Register(CreateFactionUnit("defensive_support_d", GameTagIds.Support), ShopLane.Defensive);
            return registry;
        }

        private static PieceDefinition CreateFactionUnit(string id, string combatRole) => new()
        {
            Id = id,
            DisplayName = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            CombatRole = combatRole,
            FactionId = FactionIds.IronVanguard,
            GoldCost = 5,
            MaxHp = 10
        };
    }
}
