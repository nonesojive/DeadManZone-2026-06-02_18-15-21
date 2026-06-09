using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SynergyEngineTests
    {
        [Test]
        public void EmptyRuleCatalog_ProducesZeroBonuses()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsFalse(snapshot.TryGet("rifle_1", out var result) && result.DamageBonus > 0);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterRelocate()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var initialSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsFalse(initialSnapshot.TryGet("rifle_1", out var initialRifleResult) && initialRifleResult.DamageBonus > 0);

            var moved = board.TryRelocate("rifle_1", TestBoards.FrontLineAnchor(0), PieceRotation.R0);
            Assert.IsTrue(moved.Success, moved.Reason);

            var movedSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsFalse(movedSnapshot.TryGet("rifle_1", out var movedRifleResult) && movedRifleResult.DamageBonus > 0);
        }
    }
}
