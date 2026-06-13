using System.Linq;
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

        private static BoardState BuildBoardWithSupplyDepot()
        {
            var board = new BoardState(DefaultLayout());
            board.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0));
            return board;
        }

        [Test]
        public void SupplyDepot_AppliesGoldDiscount()
        {
            var board = BuildBoardWithSupplyDepot();
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, factionId: "iron_vanguard", round: 2, seed: 999);

            Assert.That(shop.Modifiers.GoldDiscountPercent, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void SameSeed_ProducesSameOffers()
        {
            var board = new BoardState(DefaultLayout());
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);

            var shopA = generator.Generate(board, "iron_vanguard", round: 1, seed: 42);
            var shopB = generator.Generate(board, "iron_vanguard", round: 1, seed: 42);

            Assert.AreEqual(shopA.Offers.Count, shopB.Offers.Count);
            for (int i = 0; i < shopA.Offers.Count; i++)
            {
                Assert.AreEqual(shopA.Offers[i].PieceId, shopB.Offers[i].PieceId);
                Assert.AreEqual(shopA.Offers[i].Lane, shopB.Offers[i].Lane);
            }
        }

        [Test]
        public void CommandBunker_AddsExtraGeneralSlot()
        {
            var board = new BoardState(DefaultLayout());
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));

            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, "iron_vanguard", round: 1, seed: 100);

            Assert.That(shop.Modifiers.ExtraGeneralSlots, Is.EqualTo(1));
        }

        [Test]
        public void GoldDiscount_ReducesOfferGoldPrice()
        {
            var boardWithout = new BoardState(DefaultLayout());
            var boardWith = BuildBoardWithSupplyDepot();
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);

            var shopWithout = generator.Generate(boardWithout, "iron_vanguard", round: 1, seed: 50);
            var shopWith = generator.Generate(boardWith, "iron_vanguard", round: 1, seed: 50);

            var generalWithout = shopWithout.Offers.First(o => o.Lane == ShopLane.Offensive && o.GoldPrice > 0);
            var matchingWith = shopWith.Offers.First(o =>
                o.Lane == ShopLane.Offensive && o.PieceId == generalWithout.PieceId);

            Assert.That(matchingWith.GoldPrice, Is.LessThan(generalWithout.GoldPrice));
        }

        [Test]
        public void FieldWorkshop_GuaranteesEngineerOffer()
        {
            var board = new BoardState(DefaultLayout());
            board.TryPlace(TestPieces.FieldWorkshop(), new GridCoord(1, 0));

            var registry = new ContentRegistry();
            registry.Register(TestPieces.RifleSquad(), ShopLane.Offensive);
            registry.Register(TestPieces.CommandBunker(), ShopLane.Offensive);

            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, "iron_vanguard", round: 1, seed: 7);

            Assert.That(shop.Offers.Any(o => o.Lane == ShopLane.Defensive), Is.True);
            Assert.IsTrue(shop.Modifiers.GuaranteeEngineerOffer);
        }

        [Test]
        public void SpecialtyLocked_ReturnsNoSpecialtyOffers()
        {
            var board = new BoardState(DefaultLayout());
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(board, "iron_vanguard", round: 1, seed: 42);

            Assert.That(shop.Offers.Any(o => o.Lane == ShopLane.Specialty), Is.False);
        }

        [Test]
        public void SpecialtyUnlocked_RollsSpecialtyOffers()
        {
            var board = new BoardState(DefaultLayout());
            var registry = TestContentRegistry.Create();
            var generator = new ShopGenerator(registry);

            var shop = generator.Generate(board, "iron_vanguard", round: 1, seed: 42, specialtyUnlocked: true);

            Assert.That(shop.Offers.Any(o => o.Lane == ShopLane.Specialty), Is.True);
        }

        [Test]
        public void SpecialtyLane_EmptyBoard_BiasesTowardAssaultOrTankPool()
        {
            var board = new BoardState(DefaultLayout());
            var registry = new ContentRegistry();
            registry.Register(TestPieces.CreateUnit("assault_special", combatRole: GameTagIds.Assault), ShopLane.Specialty);
            registry.Register(TestPieces.CreateUnit("support_special", combatRole: GameTagIds.Support), ShopLane.Specialty);

            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(board, "iron_vanguard", round: 1, seed: 7, specialtyUnlocked: true);
            var specialtyOffers = shop.Offers.Where(o => o.Lane == ShopLane.Specialty).ToList();

            Assert.That(specialtyOffers, Is.Not.Empty);
            Assert.IsTrue(specialtyOffers.All(o => o.PieceId == "assault_special"));
        }
    }
}
