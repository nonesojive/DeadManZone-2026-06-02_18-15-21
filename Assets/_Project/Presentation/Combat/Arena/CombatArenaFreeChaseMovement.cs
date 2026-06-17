using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Velocity-based pursuit for Top Troops-style march. Avoids stop-and-go from
    /// reaching a short leash point ahead of the sparse sim anchor.
    /// </summary>
    public static class CombatArenaFreeChaseMovement
    {
        public const float DefaultStopRadius = 0.2f;

        public static bool ShouldKeepMarching(Vector3 currentWorld, Vector3 chaseWorld, float stopRadius = DefaultStopRadius)
        {
            Vector3 delta = chaseWorld - currentWorld;
            delta.y = 0f;
            return delta.sqrMagnitude > stopRadius * stopRadius;
        }

        public static Vector3 ComputeStep(
            Vector3 currentWorld,
            Vector3 simWorld,
            Vector3 chaseWorld,
            float speed,
            float deltaTime,
            float cellWidth,
            float maxLeadCells)
        {
            if (speed <= 0f || deltaTime <= 0f || cellWidth <= 0f)
                return currentWorld;

            Vector3 toChase = chaseWorld - currentWorld;
            toChase.y = 0f;
            if (toChase.sqrMagnitude < 0.0004f)
                return currentWorld;

            Vector3 moveDir = toChase.normalized;
            Vector3 simFlat = new(simWorld.x, 0f, simWorld.z);
            Vector3 posFlat = new(currentWorld.x, 0f, currentWorld.z);
            float aheadOfSim = Vector3.Dot(posFlat - simFlat, moveDir);
            float maxAhead = cellWidth * Mathf.Max(1f, maxLeadCells);

            if (aheadOfSim > maxAhead)
            {
                float excess = aheadOfSim - maxAhead;
                float dampen = Mathf.Clamp01(excess / cellWidth);
                Vector3 pullTowardSim = (simFlat - posFlat).normalized;
                moveDir = Vector3.Slerp(moveDir, pullTowardSim, dampen * 0.35f).normalized;
            }

            return currentWorld + moveDir * (speed * deltaTime);
        }

        public static Vector3 ClampToSimLead(
            Vector3 proposedWorld,
            Vector3 simWorld,
            float cellWidth,
            float maxLeadCells)
        {
            Vector3 simFlat = new(simWorld.x, 0f, simWorld.z);
            Vector3 proposedFlat = new(proposedWorld.x, 0f, proposedWorld.z);
            float maxDist = cellWidth * Mathf.Max(1f, maxLeadCells);
            Vector3 offset = proposedFlat - simFlat;
            if (offset.sqrMagnitude <= maxDist * maxDist)
                return proposedWorld;

            Vector3 clampedFlat = simFlat + offset.normalized * maxDist;
            return new Vector3(clampedFlat.x, proposedWorld.y, clampedFlat.z);
        }
    }
}
