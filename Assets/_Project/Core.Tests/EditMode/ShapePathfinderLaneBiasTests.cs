using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ShapePathfinderLaneBiasTests
    {
        private static readonly IReadOnlyList<GridCoord> SingleCellOffsets =
            CombatFootprint.ComputeOffsets(TestPieces.RifleSquad().Shape, rotation: 0);

        [Test]
        public void FindStep_FrontlinePrefersStayingOnSpawnLane_WhenBothMovesAdvance()
        {
            var layout = new BattlefieldLayout(
                playerHalfWidth: 9,
                neutralWidth: 0,
                enemyHalfWidth: 9,
                height: 10);
            var occupancy = new CombatOccupancyGrid();

            var step = ShapePathfinder.FindStep(
                currentAnchor: new GridCoord(3, 5),
                goalAnchor: new GridCoord(9, 6),
                shapeOffsets: SingleCellOffsets,
                moverInstanceId: "mover",
                occupancy: occupancy,
                layout: layout,
                spawnAnchorY: 5,
                preferLaneHold: true);

            Assert.IsNotNull(step);
            Assert.AreEqual(
                new GridCoord(4, 5),
                step.Value,
                "Frontline lane hold should choose forward movement over lane drift when both reduce heuristic.");
        }
    }
}
