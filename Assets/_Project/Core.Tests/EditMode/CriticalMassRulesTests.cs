using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CriticalMassRulesTests
    {
        [Test]
        public void ThreeInfantry_GrantsDamageBonus()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var infantry = TestPieces.CreateUnit("inf", tags: new[] { GameKeywords.Infantry, GameTags.Combatant });

            board.TryPlace(infantry, new GridCoord(0, 0), "a");
            board.TryPlace(infantry, new GridCoord(1, 0), "b");
            board.TryPlace(infantry, new GridCoord(2, 0), "c");

            var bonus = CriticalMassRules.Evaluate(board);
            Assert.GreaterOrEqual(bonus.DamageBonus, 2);
        }
    }
}
