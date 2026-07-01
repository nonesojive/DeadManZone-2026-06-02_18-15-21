using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BuildBoardTagCounterTests
    {
        [Test]
        public void CountTags_SumsAcrossHqAndCombatBoards()
        {
            var combat = new BoardState(TestBoards.Layout);
            combat.TryPlace(TestPieces.WithTags(command: true), new GridCoord(4, 4), "c1");
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.WithTags(command: true, building: true), new GridCoord(0, 0), "h1");

            var boards = new BuildBoardSet { Combat = combat, Hq = hq };
            Assert.AreEqual(2, BuildBoardTagCounter.Count(boards, GameTagIds.Command));
        }
    }
}
