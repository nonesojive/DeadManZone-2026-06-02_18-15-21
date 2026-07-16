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

        // ---------------------------------------------------------------
        // 2026-07-15 faction-roster-v1 §2.2: Grand Battery's Rolling Barrage — bigger area
        // strike than MortarShot, damage scales with the army's artillery-tag count.
        // ---------------------------------------------------------------

        [Test]
        public void RollingBarrage_DamageScalesWithArtilleryCount()
        {
            var source = new CombatantState
            {
                InstanceId = "battery_1",
                Definition = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.RollingBarrage),
                AnchorPosition = new GridCoord(0, 0),
                CurrentHp = 100
            };
            var enemy = new CombatantState
            {
                InstanceId = "enemy_1",
                Definition = TestPieces.RifleSquad(),
                AnchorPosition = new GridCoord(1, 0),
                CurrentHp = 1000
            };
            var board = TestBoards.StandardPlayer();

            var withoutArtillery = new CombatantState { InstanceId = "enemy_1", Definition = TestPieces.RifleSquad(), AnchorPosition = new GridCoord(1, 0), CurrentHp = 1000 };
            CombatAbilityExecutor.Execute(
                GrantedAbility.RollingBarrage, source.InstanceId, board, new[] { source }, new[] { withoutArtillery },
                new CombatEventLog(), logSegment: 0, logTick: 0, targetCell: withoutArtillery.AnchorPosition);
            int damageWithoutArtillery = 1000 - withoutArtillery.CurrentHp;

            var withArtillery = new CombatantState { InstanceId = "enemy_1", Definition = TestPieces.RifleSquad(), AnchorPosition = new GridCoord(1, 0), CurrentHp = 1000 };
            CombatAbilityExecutor.Execute(
                GrantedAbility.RollingBarrage, source.InstanceId, board, new[] { source }, new[] { withArtillery },
                new CombatEventLog(), logSegment: 0, logTick: 0, targetCell: withArtillery.AnchorPosition, artilleryCount: 3);
            int damageWithArtillery = 1000 - withArtillery.CurrentHp;

            Assert.Greater(damageWithArtillery, damageWithoutArtillery, "3 artillery pieces should out-damage 0");
        }

        [Test]
        public void RollingBarrage_HasWiderRadiusThanMortarShot()
        {
            var source = new CombatantState
            {
                InstanceId = "battery_1",
                Definition = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.RollingBarrage),
                AnchorPosition = new GridCoord(0, 0),
                CurrentHp = 100
            };
            var center = new CombatantState { InstanceId = "center", Definition = TestPieces.RifleSquad(), AnchorPosition = new GridCoord(5, 5), CurrentHp = 1000 };
            // 2 cells from center: inside MortarShot's radius-1 splash? No — outside radius 1,
            // inside RollingBarrage's radius 2.
            var farTarget = new CombatantState { InstanceId = "far", Definition = TestPieces.RifleSquad(), AnchorPosition = new GridCoord(7, 5), CurrentHp = 1000 };
            var board = TestBoards.StandardPlayer();

            CombatAbilityExecutor.Execute(
                GrantedAbility.RollingBarrage, source.InstanceId, board, new[] { source }, new[] { center, farTarget },
                new CombatEventLog(), logSegment: 0, logTick: 0, targetCell: center.AnchorPosition);

            Assert.Less(farTarget.CurrentHp, 1000, "RollingBarrage's radius 2 must reach 2 cells out");
        }

        // ---------------------------------------------------------------
        // 2026-07-15 faction-roster-v1 §4 (🟡 ledger, now wired): HQ-board buildings can grant
        // pause-window abilities. Artillery Park's Ranging Barrage is the consumer.
        // ---------------------------------------------------------------

        [Test]
        public void Execute_SourceOnHqBoard_FallsBackToHqLookup()
        {
            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            var artilleryPark = TestPieces.With(TestPieces.CommandOutpost(), grantedAbility: GrantedAbility.MortarShot);
            hqBoard.TryPlace(artilleryPark, new GridCoord(0, 0), "artillery_park_1");

            var enemy = new CombatantState
            {
                InstanceId = "enemy_1",
                Definition = TestPieces.RifleSquad(),
                AnchorPosition = new GridCoord(0, 0),
                CurrentHp = 100
            };
            var board = TestBoards.StandardPlayer();

            var result = CombatAbilityExecutor.Execute(
                GrantedAbility.MortarShot,
                "artillery_park_1",
                board,
                playerCombatants: new CombatantState[0],
                enemyCombatants: new[] { enemy },
                log: new CombatEventLog(),
                logSegment: 0,
                logTick: 0,
                targetCell: enemy.AnchorPosition,
                hqBoard: hqBoard);

            Assert.IsTrue(result.Success);
            Assert.Less(enemy.CurrentHp, 100);
        }

        [Test]
        public void Execute_SourceNotOnCombatOrHqBoard_Fails()
        {
            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            var board = TestBoards.StandardPlayer();

            var result = CombatAbilityExecutor.Execute(
                GrantedAbility.MortarShot,
                "nonexistent",
                board,
                playerCombatants: new CombatantState[0],
                enemyCombatants: new CombatantState[0],
                log: new CombatEventLog(),
                logSegment: 0,
                logTick: 0,
                hqBoard: hqBoard);

            Assert.IsFalse(result.Success);
        }
    }
}
