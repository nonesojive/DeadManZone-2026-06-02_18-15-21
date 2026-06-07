using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CriticalMassRulesTests
    {
        [Test]
        public void ThreeInfantryPrimary_GrantsDamageBonus()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                systemTag: GameTagIds.Combatant);

            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(0), "a").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(1), "b").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(2), "c").Success);

            var bonus = CriticalMassRules.EvaluateFightStart(board);
            Assert.GreaterOrEqual(bonus.DamageBonus, 2);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterPieceRemoved()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                systemTag: GameTagIds.Combatant);

            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(0), "a").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(1), "b").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(2), "c").Success);

            var snapshot = CriticalMassRules.EvaluateFightStart(board);
            Assert.IsTrue(board.TryRemove("c", out _));

            var reevaluated = CriticalMassRules.EvaluateFightStart(board);
            Assert.GreaterOrEqual(snapshot.DamageBonus, 2);
            Assert.AreEqual(0, reevaluated.DamageBonus);
        }
    }
}
