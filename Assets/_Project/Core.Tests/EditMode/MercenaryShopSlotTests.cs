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
    /// <summary>2026-07-15 faction-roster-v1 §1.9/§2.4/§4 Cartel mercenary shop slot:
    /// CartelMercenarySlotProvider wired through the IShopSlotUnlockProvider seam,
    /// ShopGenerator.RollMercenarySlot/CreateMercenaryOffer. Synthetic registries so this
    /// wave doesn't depend on the Cartel content pass (W2).</summary>
    public sealed class MercenaryShopSlotTests
    {
        private static BoardState EmptyBoard() => new BoardState(TestBoards.Layout);

        private static ShopGenerator MakeGenerator(ContentRegistry registry) =>
            new ShopGenerator(
                registry,
                unlockRegistry: new ShopSlotUnlockRegistry(new IShopSlotUnlockProvider[]
                {
                    new CartelMercenarySlotProvider()
                }));

        private static PieceDefinition Piece(
            string id,
            Rarity rarity,
            string factionId,
            string primary = null,
            PieceCategory category = PieceCategory.Unit) => new()
            {
                Id = id,
                DisplayName = id,
                Category = category,
                Primary = primary ?? GameTagIds.Infantry,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                CombatRole = GameTagIds.Assault,
                FactionId = factionId,
                MaxHp = 10,
                Rarity = rarity
            };

        [Test]
        public void CartelFaction_GetsAMercenaryOffer_AtBonusSlotNine()
        {
            var registry = new ContentRegistry();
            registry.Register(Piece("cartel_common", Rarity.Common, FactionIds.CartelOfEchoes), ShopLane.Offensive);
            registry.Register(Piece("ironmarch_fighter", Rarity.Common, FactionIds.IronmarchUnion), ShopLane.Offensive);
            registry.Register(Piece("dust_fighter", Rarity.Uncommon, FactionIds.DustScourge), ShopLane.Offensive);
            registry.Register(Piece("neutral_fighter", Rarity.Common, "neutral"), ShopLane.Offensive);

            var generator = MakeGenerator(registry);
            var shop = generator.Generate(EmptyBoard(), FactionIds.CartelOfEchoes, round: 1, seed: 1);

            var mercOffer = shop.Offers.SingleOrDefault(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex);
            Assert.IsNotNull(mercOffer, "Cartel must get an offer at the mercenary bonus slot");
            Assert.IsTrue(mercOffer.IsMercenary);
            Assert.IsFalse(mercOffer.IsSalvaged, "mercenary and salvage are distinct sources");

            var piece = registry.GetById(mercOffer.PieceId);
            Assert.AreNotEqual(FactionIds.CartelOfEchoes, piece.FactionId, "must be OFF-faction");
            Assert.AreNotEqual("neutral", piece.FactionId, "neutral is not a mercenary source");
        }

        [Test]
        public void NonCartelFaction_NeverGetsTheMercenarySlot()
        {
            var registry = new ContentRegistry();
            registry.Register(Piece("ironmarch_fighter", Rarity.Common, FactionIds.IronmarchUnion), ShopLane.Offensive);
            registry.Register(Piece("dust_fighter", Rarity.Common, FactionIds.DustScourge), ShopLane.Offensive);

            var generator = MakeGenerator(registry);
            var shop = generator.Generate(EmptyBoard(), FactionIds.IronmarchUnion, round: 1, seed: 1);

            Assert.IsFalse(shop.Offers.Any(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex),
                "the merc slot provider must be a no-op for any faction other than Cartel");
            Assert.IsFalse(shop.Offers.Any(o => o.IsMercenary));
        }

        [Test]
        public void MercenaryPool_ExcludesBuildingsAndStructures()
        {
            var registry = new ContentRegistry();
            // Only off-faction non-fighters registered — the merc slot must find nothing.
            registry.Register(
                Piece("dust_building", Rarity.Common, FactionIds.DustScourge, category: PieceCategory.Building),
                ShopLane.Offensive);
            registry.Register(
                Piece("dust_structure", Rarity.Common, FactionIds.DustScourge, primary: GameTagIds.Structure),
                ShopLane.Offensive);

            var generator = MakeGenerator(registry);
            var shop = generator.Generate(EmptyBoard(), FactionIds.CartelOfEchoes, round: 1, seed: 1);

            Assert.IsFalse(shop.Offers.Any(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex),
                "no fighter candidates: the slot must roll nothing rather than a building/structure");
        }

        [Test]
        public void MercenaryOffer_Price_IsBaseCostPlusSurchargePlusDreadTax()
        {
            var registry = new ContentRegistry();
            registry.Register(Piece("dust_fighter", Rarity.Uncommon, FactionIds.DustScourge), ShopLane.Offensive);
            registry.Register(Piece("cartel_common", Rarity.Common, FactionIds.CartelOfEchoes), ShopLane.Offensive);

            var generator = MakeGenerator(registry);
            const int round = 5;
            var shop = generator.Generate(EmptyBoard(), FactionIds.CartelOfEchoes, round, seed: 1);

            var mercOffer = shop.Offers.Single(o => o.SlotIndex == CartelMercenarySlotProvider.SlotIndex);
            int baseGold = RarityPricing.BaseCost(Rarity.Uncommon);
            int expected = baseGold + baseGold * FactionPassives.MercenarySurchargePercent / 100 + (round - 1);

            Assert.AreEqual(expected, mercOffer.GoldPrice);
        }
    }
}
