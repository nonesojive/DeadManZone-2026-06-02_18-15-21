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
                layout,
                CombatSegment.Opening);
            var neutral = CombatMovement.GetStepChargeCost(
                new GridCoord(8, 5),
                new GridCoord(9, 5),
                layout,
                CombatSegment.Opening);

            Assert.AreEqual(CombatMovementSpeed.NormalStepChargeCost, normal);
            Assert.AreEqual(CombatMovementSpeed.NeutralStepChargeCost, neutral);
            Assert.AreEqual(normal * 2, neutral);
        }

        [Test]
        public void MediumInfantry_MovesTwoToThreeCellsDuringOpening()
        {
            int moves = CountOpeningMoves(MovementSpeedTier.Medium);
            Assert.GreaterOrEqual(moves, 2);
            Assert.LessOrEqual(moves, 3);
        }

        [Test]
        public void HighTier_OutpacesLowTierDuringOpening()
        {
            int high = CountOpeningMoves(MovementSpeedTier.High);
            int low = CountOpeningMoves(MovementSpeedTier.Low);
            Assert.Greater(high, low);
        }

        private static int CountOpeningMoves(MovementSpeedTier movementSpeed)
        {
            var player = new BoardState(TestBoards.Layout);
            var piece = TestPieces.With(TestPieces.RifleSquad(), movementSpeed: movementSpeed);
            player.TryPlace(piece, new GridCoord(4, 5), instanceId: "player_rifle");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), instanceId: "enemy_hq");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), instanceId: "enemy_rifle");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(new List<PhaseCommand>());

            return run.Log.Events.Count(e =>
                e.ActorId == "player_rifle"
                && e.ActionType == "move"
                && e.Phase == CombatPhase.Deployment);
        }
    }
}
