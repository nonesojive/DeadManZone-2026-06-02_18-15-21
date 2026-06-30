using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopSlotLayoutResolverTests
    {
        [Test]
        public void Resolve_DefaultConfig_HasNineBaselineSlotsIncludingReservedRow()
        {
            var board = new BoardState(TestBoards.Layout);
            var config = ShopConfig.CreateDefault();
            var context = new ShopUnlockContext
            {
                Board = board,
                FactionId = FactionIds.IronVanguard,
                Registry = TestContentRegistry.Create(),
                Modifiers = new ShopModifiers()
            };

            var layout = ShopSlotProfileResolver.ResolveActiveSlots(
                config,
                ShopSlotUnlockRegistry.Empty,
                context);

            Assert.AreEqual(ShopSlotLayoutResolver.BaselineSlotCount, layout.Count);
            Assert.AreEqual(0, layout[0].SlotIndex);
            Assert.AreEqual(ShopSlotKind.BaselineOffensive, layout[0].Kind);
            Assert.AreEqual(3, layout[3].SlotIndex);
            Assert.AreEqual(ShopSlotKind.BaselineDefensive, layout[3].Kind);
            Assert.AreEqual(ShopSlotKind.ReservedAbility, layout[6].Kind);
        }

        [Test]
        public void CommandBunker_DoesNotAddExtraSlots()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            var context = new ShopUnlockContext
            {
                Board = board,
                FactionId = FactionIds.IronVanguard,
                Registry = TestContentRegistry.Create(),
                Modifiers = ShopGenerator.ComputeModifiers(board)
            };

            var layout = ShopSlotProfileResolver.ResolveActiveSlots(
                ShopConfig.CreateDefault(),
                ShopSlotUnlockRegistry.Empty,
                context);

            Assert.AreEqual(ShopSlotLayoutResolver.BaselineSlotCount, layout.Count);
        }

        [Test]
        public void GetGridShape_SixVisibleOffers_IsThreeByTwo()
        {
            var (columns, rows) = ShopSlotLayoutResolver.GetVisibleGridShape(6);
            Assert.AreEqual(3, columns);
            Assert.AreEqual(2, rows);
        }

        [Test]
        public void GetGridShape_FullShop_IsThreeByThree()
        {
            var (columns, rows) = ShopSlotLayoutResolver.GetGridShape(9);
            Assert.AreEqual(3, columns);
            Assert.AreEqual(3, rows);
        }

        [Test]
        public void ReservedSlots_DoNotRollOffers()
        {
            var board = new BoardState(TestBoards.Layout);
            var shop = new ShopGenerator(TestContentRegistry.Create())
                .Generate(board, FactionIds.IronVanguard, round: 1, seed: 1);

            Assert.IsFalse(shop.Offers.Any(o => o.SlotIndex >= ShopSlotLayoutResolver.ReservedSlotStartIndex));
        }
    }
}
