using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Parabolic projectile path for 2D arena tracers.</summary>
    public static class CombatArena2DProjectileArc
    {
        public static Vector3 Sample(Vector3 from, Vector3 to, float t, float arcHeight)
        {
            t = Mathf.Clamp01(t);
            Vector3 linear = Vector3.Lerp(from, to, t);
            float lift = 4f * arcHeight * t * (1f - t);
            return linear + Vector3.up * lift;
        }

        public static float MidpointHeight(Vector3 from, Vector3 to, float arcHeight) =>
            Sample(from, to, 0.5f, arcHeight).y;
    }
}
