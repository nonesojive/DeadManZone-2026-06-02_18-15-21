using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
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
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            Assert.AreEqual(3, AuthorityCalculator.ComputeRoundPool(board));
        }

        [Test]
        public void ComputeRoundPool_CountsEachCommandBuilding()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            var radio = new PieceDefinition
            {
                Id = "radio_array",
                DisplayName = "Radio Array",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Tags = new[] { "Command" },
                MaxHp = 12
            };
            board.TryPlace(radio, new GridCoord(3, 0), "radio_1");
            Assert.AreEqual(4, AuthorityCalculator.ComputeRoundPool(board));
        }
    }
}
