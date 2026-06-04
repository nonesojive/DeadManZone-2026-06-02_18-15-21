using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatAbilityExecutorTests
    {
        [Test]
        public void GrenadeLob_DealsExplosiveAoE()
        {
            var source = new CombatantState
            {
                InstanceId = "grenade_1",
                Definition = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.GrenadeLob),
                Position = new GridCoord(0, 0),
                CurrentHp = 100
            };
            var enemy = new CombatantState
            {
                InstanceId = "enemy_1",
                Definition = TestPieces.RifleSquad(),
                Position = new GridCoord(1, 0),
                CurrentHp = 100
            };
            var log = new CombatEventLog();
            var board = TestBoards.StandardPlayer();

            var result = CombatAbilityExecutor.Execute(
                GrantedAbility.GrenadeLob,
                source.InstanceId,
                board,
                new[] { source },
                new[] { enemy },
                log,
                CombatPhase.Deployment,
                targetCell: enemy.Position);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(log.Events.Exists(e => e.ActionType == "grenade_lob"));
        }

        [Test]
        public void CannonBlast_OnlyValidOnGrindPause()
        {
            Assert.IsFalse(CombatAbilityExecutor.CanUseAtPause(GrantedAbility.CannonBlast, CombatPhase.Deployment));
            Assert.IsTrue(CombatAbilityExecutor.CanUseAtPause(GrantedAbility.CannonBlast, CombatPhase.Grind));
        }
    }
}
