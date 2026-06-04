using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatWinCheckerTests
    {
        [Test]
        public void PlayerWins_WhenEnemyHQDestroyed()
        {
            var player = new List<CombatantState>
            {
                Make("p1", TestPieces.RifleSquad(), alive: true)
            };
            var enemy = new List<CombatantState>
            {
                Make("hq", TestPieces.HqPiece(), alive: false),
                Make("e1", TestPieces.RifleSquad(), alive: true)
            };

            var (over, playerWon, isDraw) = CombatWinChecker.Evaluate(player, enemy);
            Assert.IsTrue(over);
            Assert.IsTrue(playerWon);
            Assert.IsFalse(isDraw);
        }

        [Test]
        public void NoInstantWin_WhenEnemyHasNoHqTag()
        {
            var player = new List<CombatantState> { Make("p1", TestPieces.RifleSquad(), alive: true) };
            var enemy = new List<CombatantState> { Make("e1", TestPieces.RifleSquad(), alive: true) };

            var (over, _, isDraw) = CombatWinChecker.Evaluate(player, enemy);
            Assert.IsFalse(over);
            Assert.IsFalse(isDraw);
        }

        private static CombatantState Make(string id, PieceDefinition def, bool alive)
        {
            return new CombatantState
            {
                InstanceId = id,
                Definition = def,
                CurrentHp = alive ? def.MaxHp : 0
            };
        }
    }
}
