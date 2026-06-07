using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SynergyEngineTests
    {
        [Test]
        public void AdjacentSupply_GrantsDamageBonus()
        {
            var supply = TestPieces.CreateUnit("supply", tags: new[] { GameKeywords.Supply });
            var rifle = TestPieces.CreateUnit("rifle", tags: new[] { GameKeywords.Infantry, GameTags.Combatant });
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            board.TryPlace(supply, new GridCoord(0, 0), "supply_1");
            board.TryPlace(rifle, new GridCoord(1, 0), "rifle_1");

            var placed = board.Pieces.ToList();
            var riflePiece = placed.First(p => p.InstanceId == "rifle_1");
            var synergy = SynergyEngine.ComputeForCombatant(board, riflePiece, placed);
            Assert.GreaterOrEqual(synergy.DamageBonus, 1);
        }
    }
}
