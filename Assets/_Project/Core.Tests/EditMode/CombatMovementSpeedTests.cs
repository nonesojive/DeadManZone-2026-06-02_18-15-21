using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatMovementSpeedTests
    {
        [Test]
        public void NeutralStepCostsTwiceNormalGround()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var normal = CombatMovement.GetStepChargeCost(
                new GridCoord(4, 5),
                new GridCoord(5, 5),
                layout);
            var neutral = CombatMovement.GetStepChargeCost(
                new GridCoord(8, 5),
                new GridCoord(9, 5),
                layout);

            Assert.AreEqual(CombatMovementSpeed.NormalStepChargeCost, normal);
            Assert.AreEqual(CombatMovementSpeed.NeutralStepChargeCost, neutral);
            Assert.AreEqual(normal * 2, neutral);
        }

        [TestCase(0, 0)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        [TestCase(3, 4)]
        [TestCase(4, 5)]
        public void GetChargePerTick_MapsNumericSpeed(int speed, int expectedCharge)
        {
            Assert.AreEqual(expectedCharge, CombatMovementSpeed.GetChargePerTick(speed));
        }

        [Test]
        public void MediumInfantry_MovesBeforeFirstPause()
        {
            int moves = CountMovesBeforeFirstPause(movementSpeed: 2);
            Assert.GreaterOrEqual(moves, 2, "Medium infantry should advance at least two cells before the first pause.");
        }

        [Test]
        public void Speed4_OutpacesSpeed2_BeforeFirstPause()
        {
            var fastMoves = MoveEventsBeforeFirstPause(movementSpeed: 4);
            var slowMoves = MoveEventsBeforeFirstPause(movementSpeed: 2);
            Assert.IsNotEmpty(fastMoves);
            Assert.IsNotEmpty(slowMoves);
            Assert.Less(fastMoves[^1].Tick, slowMoves[^1].Tick,
                "Speed 4 should reach its pre-pause move depth sooner than speed 2.");
        }

        private static int CountMovesBeforeFirstPause(int movementSpeed) =>
            MoveEventsBeforeFirstPause(movementSpeed).Count;

        private static List<CombatEvent> MoveEventsBeforeFirstPause(int movementSpeed)
        {
            var player = new BoardState(TestBoards.Layout);
            var piece = TestPieces.With(TestPieces.RifleSquad(), movementSpeed: movementSpeed);
            player.TryPlace(piece, new GridCoord(4, 5), instanceId: "player_rifle");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.MultiCellRearBlocker(), new GridCoord(0, 4), instanceId: "enemy_blocker");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), instanceId: "enemy_rifle");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(new List<PhaseCommand>());

            return run.Log.Events
                .Where(e => e.ActorId == "player_rifle" && e.ActionType == "move" && e.Segment == 0)
                .ToList();
        }
    }
}
