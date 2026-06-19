using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ArmyStrengthCalculatorTests
    {
        [Test]
        public void EmptyBoard_ReturnsZeroTotals()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.AreEqual(0, snapshot.BaseTotal);
            Assert.AreEqual(0, snapshot.EffectiveTotal);
        }

        [Test]
        public void FieldingPieces_SumRatings()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var rifle = TestPieces.RifleSquad();
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.HqPiece(), TestBoards.SupportLineAnchor(2), "hq_1").Success);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            int expected = PieceCombatRating.ComputeBase(rifle)
                + PieceCombatRating.ComputeBase(TestPieces.HqPiece());
            Assert.AreEqual(expected, snapshot.BaseTotal);
            Assert.AreEqual(expected, snapshot.EffectiveTotal);
        }

        [Test]
        public void AdjacentSynergy_IncreasesEffectiveTotal()
        {
            var command = TestPieces.CreateUnit(
                "command",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Command });
            var artillery = TestPieces.CreateUnit(
                "artillery",
                combatRole: GameTagIds.Artillery,
                systemTag: GameTagIds.Combatant);

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(command, TestBoards.SupportLineAnchor(0), "command_1").Success);
            Assert.IsTrue(board.TryPlace(artillery, TestBoards.SupportLineAnchor(1), "artillery_1").Success);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.Greater(snapshot.EffectiveTotal, snapshot.BaseTotal);
            Assert.Greater(snapshot.SynergyBonus, 0);
        }

        [Test]
        public void BuildingsExcluded_FromTotals()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(TestPieces.CommandBunker(), TestBoards.SupportLineAnchor(0), "bunker_1").Success);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.AreEqual(0, snapshot.BaseTotal);
        }
    }
}
