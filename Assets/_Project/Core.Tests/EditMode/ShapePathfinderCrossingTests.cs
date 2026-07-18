using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-17 owner-diagnosed "cost-greedy into suicide" fix (see
    /// TickCombatRunTransportTests.TransportTarget_ToFarSideBottomMiddle_CrossesNeutralColumnEarly_NotAfterDescending
    /// for the full-sim reproduction of the owner's video). This file pins the exact rule at the
    /// shared seam: ShapePathfinder.FindStep's straightLineBias parameter.
    ///
    /// Rule (documented here, implemented in ShapePathfinder.GreedyStepTowardGoal): with
    /// straightLineBias on, a step that still needs to cross the neutral column always beats a
    /// same-side step while the crossing is pending, regardless of per-step charge cost. Once
    /// fully across, the remaining X/Y distance interleaves proportionally (larger-remaining-axis
    /// first), and cost/coordinate order are only the final tiebreakers — identical to the
    /// straightLineBias-off (default) behavior used everywhere else, so normal engagement AI and
    /// lane-holding formations are untouched by this fix.</summary>
    public sealed class ShapePathfinderCrossingTests
    {
        private static readonly IReadOnlyList<GridCoord> SingleCell = new[] { new GridCoord(0, 0) };

        // 12-wide arena mapping: 5 player / 2 neutral / 5 enemy columns, 8 rows tall.
        private static BattlefieldLayout Arena() =>
            new BattlefieldLayout(playerHalfWidth: 5, neutralWidth: 2, enemyHalfWidth: 5, height: 8);

        [Test]
        public void StraightLineBias_CrossesNeutralColumnEarly_NotAfterDescendingOwnSide()
        {
            var layout = Arena();
            var occupancy = new CombatOccupancyGrid();
            // Top-right of the player half (2 cells from the neutral band) — the owner's video.
            var current = new GridCoord(2, 0);
            // Bottom-middle of the ENEMY half: EnemyOriginX=7, half is X 7..11, bottom row is
            // Height-1=7.
            var goal = new GridCoord(9, 7);

            int firstCrossingStep = -1;
            for (int step = 0; step < 20 && !current.Equals(goal); step++)
            {
                var next = ShapePathfinder.FindStep(
                    current,
                    goal,
                    SingleCell,
                    "ark_1",
                    occupancy,
                    layout,
                    spawnAnchorY: 0,
                    preferLaneHold: true,
                    straightLineBias: true);

                Assert.IsNotNull(next, $"step {step}: pathfinder must always find a step on an empty board");
                current = next.Value;

                if (firstCrossingStep < 0 && layout.IsNeutralColumn(current.X))
                    firstCrossingStep = step;
            }

            Assert.IsTrue(current.Equals(goal), "the transport must actually reach its target on an empty board");
            Assert.GreaterOrEqual(firstCrossingStep, 0, "the path never enters the neutral column at all");
            Assert.LessOrEqual(firstCrossingStep, 2,
                "must start crossing the neutral column within the first few steps, not after fully " +
                "descending its own side (the owner's video: 2 cells toward midfield, then 90 degrees " +
                "straight down the wire, dead before ever crossing)");
        }

        [Test]
        public void StraightLineBias_Off_KeepsLegacyCostFirstTieBreak_ReproducesTheBug()
        {
            var layout = Arena();
            var occupancy = new CombatOccupancyGrid();
            var current = new GridCoord(2, 0);
            var goal = new GridCoord(9, 7);

            // Without straightLineBias (the default — every non-transport-run mover, and a
            // transport before this fix), the legacy cheaper-cost-first tie-break still applies:
            // 2 cheap steps along the spawn row (still cheaper than paying the neutral crossing
            // cost), then it turns and descends the full remaining Y distance — because once off
            // the spawn row, "further down" (100 + lane-bias penalty) stays cheaper than
            // "crossing" (200 + lane-bias penalty) all the way to the target's row. This is the
            // exact mechanism behind the owner's video and pins that this fix does not change
            // default (bias-off) behavior at all.
            int firstCrossingStep = -1;
            for (int step = 0; step < 20 && !current.Equals(goal); step++)
            {
                var next = ShapePathfinder.FindStep(
                    current,
                    goal,
                    SingleCell,
                    "ark_1",
                    occupancy,
                    layout,
                    spawnAnchorY: 0,
                    preferLaneHold: true);

                Assert.IsNotNull(next, $"step {step}: pathfinder must always find a step on an empty board");
                current = next.Value;

                if (firstCrossingStep < 0 && layout.IsNeutralColumn(current.X))
                    firstCrossingStep = step;
            }

            Assert.IsTrue(current.Equals(goal), "the transport must still reach its target eventually");
            Assert.GreaterOrEqual(firstCrossingStep, 8,
                "legacy (bias-off) behavior descends its own side almost fully before ever crossing — " +
                "this is the bug the straightLineBias fix targets, unchanged here by design");
        }
    }
}
