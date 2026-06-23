using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopSlotLayoutResolverTests
    {
        [Test]
        public void Resolve_DefaultConfig_HasEightBaselineSlots()
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
            Assert.AreEqual(4, layout[4].SlotIndex);
            Assert.AreEqual(ShopSlotKind.BaselineDefensive, layout[4].Kind);
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

            Assert.AreEqual(8, layout.Count);
        }

        [Test]
        public void GetGridShape_EightOffers_IsFourByTwo()
        {
            var (columns, rows) = ShopSlotLayoutResolver.GetGridShape(8);
            Assert.AreEqual(4, columns);
            Assert.AreEqual(2, rows);
        }

        [Test]
        public void GetGridShape_TwelveOffers_IsFourByThree()
        {
            var (columns, rows) = ShopSlotLayoutResolver.GetGridShape(12);
            Assert.AreEqual(4, columns);
            Assert.AreEqual(3, rows);
        }
    }
}
