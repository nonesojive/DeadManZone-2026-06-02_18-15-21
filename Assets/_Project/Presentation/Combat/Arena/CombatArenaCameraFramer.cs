using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Positions the arena camera in a Top Troops-style oblique view:
    /// board width fills the screen, player front line toward the bottom.
    /// </summary>
    public static class CombatArenaCameraFramer
    {
        private const int SolveIterations = 32;
        private const float MinDistance = 2f;
        private const float MaxDistance = 250f;

        public static void Frame(Camera camera, BattlefieldLayout layout, CombatArenaConfigSO config)
        {
            if (camera == null || layout == null || config == null)
                return;

            camera.fieldOfView = config.fieldOfView;

            if (config.useManualCameraPose)
            {
                if (config.manualCameraWorldPosition.sqrMagnitude > 0.01f)
                {
                    var manualPose = new CombatArenaCameraPose
                    {
                        WorldPosition = config.manualCameraWorldPosition,
                        LookAt = config.lookAtWorld,
                        FieldOfView = config.fieldOfView
                    };
                    manualPose.ApplyTo(camera);
                }
                else
                {
                    // Orbit params saved via asset/tuner before world position was added.
                    ApplyOrbit(
                        camera,
                        config.lookAtWorld,
                        config.cameraElevationDegrees,
                        config.cameraAzimuthDegrees,
                        config.cameraDistance);
                }

                return;
            }

            var samplePoints = GetGroundSamplePoints(layout, config.cellWidth, config.cellDepth);
            var lookAt = new Vector3(0f, 0f, config.lookAtDepthOffset);

            float distance = config.autoFrameWidth
                ? SolveDistance(camera, lookAt, config, samplePoints)
                : config.cameraDistance;

            if (config.autoFrameVerticalPosition)
            {
                float halfDepth = layout.Height * config.cellDepth * 0.5f;
                for (int pass = 0; pass < 3; pass++)
                {
                    lookAt.z = SolveLookAtDepth(
                        camera,
                        lookAt,
                        config,
                        samplePoints,
                        distance,
                        halfDepth);

                    if (!config.autoFrameWidth)
                        break;

                    distance = SolveDistance(camera, lookAt, config, samplePoints);
                }
            }
            else if (config.autoFrameWidth)
            {
                distance = SolveDistance(camera, lookAt, config, samplePoints);
            }

            ApplyOrbit(camera, lookAt, config.cameraElevationDegrees, config.cameraAzimuthDegrees, distance);
        }

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

        public static float MeasureVerticalViewportCenter(Camera camera, Vector3[] worldPoints)
        {
            if (!TryMeasureViewportBounds(camera, worldPoints, out _, out _, out float minY, out float maxY))
                return 0.5f;

            return (minY + maxY) * 0.5f;
        }

        internal static float SolveDistance(
            Camera camera,
            Vector3 lookAt,
            CombatArenaConfigSO config,
            Vector3[] samplePoints)
        {
            float targetSpan = 1f - (config.horizontalViewportPadding * 2f);
            targetSpan = Mathf.Clamp(targetSpan, 0.5f, 1f);

            float low = MinDistance;
            float high = MaxDistance;

            for (int i = 0; i < SolveIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                ApplyOrbit(camera, lookAt, config.cameraElevationDegrees, config.cameraAzimuthDegrees, mid);
                float span = MeasureHorizontalViewportSpan(camera, samplePoints);

                if (span > targetSpan)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) * 0.5f;
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
                ApplyOrbit(camera, candidate, config.cameraElevationDegrees, config.cameraAzimuthDegrees, distance);
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
