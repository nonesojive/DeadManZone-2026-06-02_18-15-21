using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatRangeTests
    {
        [Test]
        public void SelectTarget_SkipsOutOfRangeEnemies()
        {
            var attacker = new CombatantState
            {
                InstanceId = "a1",
                Definition = TestPieces.With(
                    TestPieces.RifleSquad(),
                    attackRange: AttackRangeTier.Short),
                Position = new GridCoord(0, 0),
                CurrentHp = 10
            };
            var near = new CombatantState
            {
                InstanceId = "e1",
                Definition = TestPieces.RifleSquad(),
                Position = new GridCoord(1, 0),
                CurrentHp = 10
            };
            var far = new CombatantState
            {
                InstanceId = "e2",
                Definition = TestPieces.RifleSquad(),
                Position = new GridCoord(5, 0),
                CurrentHp = 3
            };

            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { far, near },
                TacticType.DisciplinedFire);

            Assert.AreEqual("e1", target.InstanceId);
        }
    }
}
