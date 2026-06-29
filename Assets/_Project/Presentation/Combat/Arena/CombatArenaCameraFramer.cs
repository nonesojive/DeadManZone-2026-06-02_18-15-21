using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Camera-math helpers shared by the 2D orthographic framer: ground sampling, oblique orbit
    /// placement, and viewport-span/center measurement for fit solving.
    /// </summary>
    public static class CombatArenaCameraFramer
    {
        private const int SolveIterations = 32;

        public static Vector3[] GetGroundSamplePoints(BattlefieldLayout layout, float cellWidth, float cellDepth)
        {
            float halfWidth = layout.TotalWidth * cellWidth * 0.5f;
            float halfDepth = layout.Height * cellDepth * 0.5f;

            return new[]
            {
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(halfWidth, 0f, 0f),
                new Vector3(0f, 0f, halfDepth),
                new Vector3(0f, 0f, -halfDepth)
            };
        }

        public static float MeasureHorizontalViewportSpan(Camera camera, Vector3[] worldPoints)
        {
            if (!TryMeasureViewportBounds(camera, worldPoints, out float minX, out float maxX, out _, out _))
                return 0f;

            return maxX - minX;
        }

        public static float MeasureVerticalViewportSpan(Camera camera, Vector3[] worldPoints)
        {
            if (!TryMeasureViewportBounds(camera, worldPoints, out _, out _, out float minY, out float maxY))
                return 0f;

            return maxY - minY;
        }

        public static float MeasureVerticalViewportCenter(Camera camera, Vector3[] worldPoints)
        {
            if (!TryMeasureViewportBounds(camera, worldPoints, out _, out _, out float minY, out float maxY))
                return 0.5f;

            return (minY + maxY) * 0.5f;
        }

        internal static float SolveLookAtDepth(
            Camera camera,
            Vector3 lookAt,
            CombatArenaConfigSO config,
            Vector3[] samplePoints,
            float distance,
            float halfDepth)
        {
            float targetCenter = Mathf.Clamp(config.boardVerticalViewportCenter, 0.25f, 0.75f);
            float low = -halfDepth * 0.75f;
            float high = halfDepth * 0.75f;

            for (int i = 0; i < SolveIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                var candidate = new Vector3(lookAt.x, lookAt.y, mid);
                ApplyOrbit(camera, candidate, config.orthoCameraElevationDegrees, config.orthoCameraAzimuthDegrees, distance);
                float centerY = MeasureVerticalViewportCenter(camera, samplePoints);

                if (centerY > targetCenter)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) * 0.5f;
        }

        internal static void ApplyOrbit(
            Camera camera,
            Vector3 lookAt,
            float elevationDegrees,
            float azimuthDegrees,
            float distance)
        {
            float elevation = elevationDegrees * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(azimuth)) * distance;

            camera.transform.position = lookAt + offset;
            camera.transform.LookAt(lookAt);
        }

        private static bool TryMeasureViewportBounds(
            Camera camera,
            Vector3[] worldPoints,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;

            foreach (var point in worldPoints)
            {
                var viewport = camera.WorldToViewportPoint(point);
                if (viewport.z <= 0f)
                    continue;

                minX = Mathf.Min(minX, viewport.x);
                maxX = Mathf.Max(maxX, viewport.x);
                minY = Mathf.Min(minY, viewport.y);
                maxY = Mathf.Max(maxY, viewport.y);
            }

            return !float.IsInfinity(minX) && !float.IsInfinity(maxX);
        }
    }
}
