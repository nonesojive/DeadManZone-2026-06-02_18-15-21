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
        public void MortarShot_DealsExplosiveAoE()
        {
            var source = new CombatantState
            {
                InstanceId = "mortar_1",
                Definition = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot),
                AnchorPosition = new GridCoord(0, 0),
                CurrentHp = 100
            };
            var enemy = new CombatantState
            {
                InstanceId = "enemy_1",
                Definition = TestPieces.RifleSquad(),
                AnchorPosition = new GridCoord(1, 0),
                CurrentHp = 100
            };
            var log = new CombatEventLog();
            var board = TestBoards.StandardPlayer();

            var result = CombatAbilityExecutor.Execute(
                GrantedAbility.MortarShot,
                source.InstanceId,
                board,
                new[] { source },
                new[] { enemy },
                log,
                logSegment: 0,
                logTick: 0,
                targetCell: enemy.AnchorPosition);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(log.Events.Exists(e => e.ActionType == "mortar_shot"));
        }

        [Test]
        public void CannonBlast_OnlyValidOnSecondCheckpoint()
        {
            Assert.IsFalse(CombatAbilityExecutor.CanUseAtPause(GrantedAbility.CannonBlast, checkpointIndex: 0));
            Assert.IsTrue(CombatAbilityExecutor.CanUseAtPause(GrantedAbility.CannonBlast, checkpointIndex: 1));
        }
    }
}
