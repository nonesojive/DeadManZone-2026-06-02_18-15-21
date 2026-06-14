using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ShopSlotLayoutResolverTests
    {
        [Test]
        public void Resolve_DefaultBoard_HasSixBaselineSlots()
        {
            var board = new BoardState(TestBoards.Layout);
            var registry = TestContentRegistry.Create();
            var layout = ShopSlotLayoutResolver.Resolve(board, "iron_vanguard", registry, new ShopModifiers());

            Assert.AreEqual(ShopSlotLayoutResolver.BaselineSlotCount, layout.Count);
            Assert.AreEqual(0, layout[0].SlotIndex);
            Assert.AreEqual(ShopSlotKind.BaselineOffensive, layout[0].Kind);
            Assert.AreEqual(3, layout[3].SlotIndex);
            Assert.AreEqual(ShopSlotKind.BaselineDefensive, layout[3].Kind);
        }

        [Test]
        public void GetGridShape_SixOffers_IsThreeByTwo()
        {
            var (columns, rows) = ShopSlotLayoutResolver.GetGridShape(6);
            Assert.AreEqual(3, columns);
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
