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

        [Test]
        public void MediumInfantry_MovesBeforeFirstPause()
        {
            int moves = CountMovesBeforeFirstPause(MovementSpeedTier.Medium);
            Assert.GreaterOrEqual(moves, 2, "Medium infantry should advance at least two cells before the first pause.");
        }

        [Test]
        public void HighTier_OutpacesLowTierBeforeFirstPause()
        {
            int high = CountMovesBeforeFirstPause(MovementSpeedTier.High);
            int low = CountMovesBeforeFirstPause(MovementSpeedTier.Low);
            Assert.Greater(high, low);
        }

        private static int CountMovesBeforeFirstPause(MovementSpeedTier movementSpeed)
        {
            var player = new BoardState(TestBoards.Layout);
            var piece = TestPieces.With(TestPieces.RifleSquad(), movementSpeed: movementSpeed);
            player.TryPlace(piece, new GridCoord(4, 5), instanceId: "player_rifle");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.CombatFieldHq(), new GridCoord(0, 4), instanceId: "enemy_hq");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), instanceId: "enemy_rifle");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(new List<PhaseCommand>());

            return run.Log.Events.Count(e =>
                e.ActorId == "player_rifle"
                && e.ActionType == "move"
                && e.Segment == 0);
        }
    }
}
