using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class AuthorityCalculatorTests
    {
        [Test]
        public void ComputeRoundPool_EmptyBoard_ReturnsHqBase()
        {
            var board = new BoardState(TestBoards.Layout);
            Assert.AreEqual(2, AuthorityCalculator.ComputeRoundPool(board));
        }

        [Test]
        public void ComputeRoundPool_AddsOnePerCommandBuilding()
        {
            var board = TestBoards.WithCommandBunker();
            Assert.AreEqual(3, AuthorityCalculator.ComputeRoundPool(board));
        }

        [Test]
        public void ComputeRoundPool_CountsEachCommandBuilding()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            var outpost = new PieceDefinition
            {
                Id = "officer_quarters",
                DisplayName = "Officer Quarters",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                CommandActions = CommandActionFlags.ChangeStance,
                MaxHp = 12
            };
            hq.TryPlace(outpost, new GridCoord(0, 2), "outpost_1");
            var board = new BuildBoardSet
            {
                Combat = new BoardState(TestBoards.Layout),
                Hq = hq
            }.ToAggregateBoard();
            Assert.AreEqual(4, AuthorityCalculator.ComputeRoundPool(board));
        }
    }
}
