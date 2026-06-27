using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Deterministic bob/squash/lean for static-sprite unit locomotion placeholders.</summary>
    internal static class CombatUnitProceduralJog
    {
        internal const float WalkCycleSpeed = 10f;
        internal const float IdleCycleSpeed = 6f;
        internal const float WalkBobAmplitude = 0.04f;
        internal const float IdleBobAmplitude = 0.02f;

        internal static float CycleSpeed(bool walking) => walking ? WalkCycleSpeed : IdleCycleSpeed;

        internal static float BobAmplitude(bool walking) => walking ? WalkBobAmplitude : IdleBobAmplitude;

        internal static float EvaluateBobY(float bobTime, bool walking) =>
            Mathf.Sin(bobTime) * BobAmplitude(walking);

        internal static Vector3 EvaluateSquadScale(float bobTime, bool walking)
        {
            if (!walking)
                return Vector3.one;

            float squash = Mathf.Abs(Mathf.Sin(bobTime));
            return new Vector3(1f + squash * 0.03f, 1f - squash * 0.04f, 1f);
        }

        internal static float EvaluateLeanDegrees(float bobTime, bool walking)
        {
            if (!walking)
                return 0f;

            return Mathf.Sin(bobTime) * 2f;
        }

        internal static float PhaseOffset(int soldierIndex) => soldierIndex * 0.35f;

        internal static float EvaluateAnchorBobY(float bobTime, bool walking, int soldierIndex)
        {
            float phase = bobTime + PhaseOffset(soldierIndex);
            return Mathf.Sin(phase) * (walking ? 0.012f : 0.006f);
        }
    }
}
