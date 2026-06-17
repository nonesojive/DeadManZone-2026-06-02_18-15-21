using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaFreeChaseMovementTests
    {
        [Test]
        public void ComputeStep_KeepsMoving_WhenAheadOfSimButBelowMaxLead()
        {
            const float cell = 1.8f;
            var sim = Vector3.zero;
            var current = new Vector3(1.1f * cell, 0f, 0f);
            var chase = new Vector3(20f, 0f, 0f);

            var next = CombatArenaFreeChaseMovement.ComputeStep(
                current,
                sim,
                chase,
                speed: 4.5f,
                deltaTime: 0.016f,
                cellWidth: cell,
                maxLeadCells: 4f);

            Assert.Greater(Vector3.Distance(current, next), 0.001f);
        }

        [Test]
        public void ComputeStep_DoesNotStopAtOneCellLead()
        {
            const float cell = 1.8f;
            var sim = Vector3.zero;
            var current = new Vector3(cell * 1.14f, 0f, 0f);
            var chase = new Vector3(cell * 10f, 0f, 0f);

            var next = CombatArenaFreeChaseMovement.ComputeStep(
                current,
                sim,
                chase,
                speed: 4.5f,
                deltaTime: 0.016f,
                cellWidth: cell,
                maxLeadCells: 4f);

            Assert.Greater(next.x, current.x);
        }

        [Test]
        public void ShouldKeepMarching_ReturnsFalse_WhenOnChaseGoal()
        {
            var current = new Vector3(1f, 0f, 2f);
            var chase = new Vector3(1f, 0f, 2.1f);

            Assert.IsFalse(CombatArenaFreeChaseMovement.ShouldKeepMarching(current, chase));
        }
    }
}
