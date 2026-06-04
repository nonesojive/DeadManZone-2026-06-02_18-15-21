using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SpecialtyLaneUnlockTests
    {
        [Test]
        public void Unlocked_WhenCommandBunkerOnBoard()
        {
            var board = TestBoards.WithCommandBunker();
            Assert.IsTrue(SpecialtyLaneUnlock.IsUnlocked(board, "iron_vanguard"));
        }

        [Test]
        public void Locked_WhenEmptyBoard()
        {
            var board = new BoardState(TestBoards.Layout);
            Assert.IsFalse(SpecialtyLaneUnlock.IsUnlocked(board, "iron_vanguard"));
        }
    }
}
