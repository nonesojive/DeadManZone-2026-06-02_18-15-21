using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Frames orthographic 2D arena camera: same orbit as 3D framer, viewport-fit ortho size
    /// so square cells project square and the board is not squished.
    /// </summary>
    public static class CombatArenaOrthographicFramer
    {
        private const int SolveIterations = 32;
        private const float MinOrthoSize = 1f;
        private const float MaxOrthoSize = 120f;
        private const float OrbitDistance = 40f;

        public static void Frame(Camera camera, BattlefieldLayout layout, CombatArenaConfigSO config)
        {
            if (camera == null || layout == null || config == null)
                return;

            camera.orthographic = true;
            camera.fieldOfView = config.fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.topTroopsSkyColor;

            Vector3[] samplePoints = CombatArenaCameraFramer.GetGroundSamplePoints(
                layout, config.cellWidth, config.cellDepth);
            var lookAt = new Vector3(0f, 0f, config.lookAtDepthOffset);

            CombatArenaCameraFramer.ApplyOrbit(
                camera,
                lookAt,
                config.orthoCameraElevationDegrees,
                config.orthoCameraAzimuthDegrees,
                OrbitDistance);

            float horizontalTarget = 1f - (config.horizontalViewportPadding * 2f);
            horizontalTarget = Mathf.Clamp(horizontalTarget, 0.55f, 0.98f);
            float verticalTarget = config.autoFrameVerticalFill
                ? Mathf.Clamp(config.verticalViewportFill, 0.45f, 0.85f)
                : 0.75f;

            float sizeForWidth = SolveOrthoSize(camera, samplePoints, horizontalTarget, vertical: false);
            float sizeForHeight = SolveOrthoSize(camera, samplePoints, verticalTarget, vertical: true);
            float orthoSize = Mathf.Max(sizeForWidth, sizeForHeight) * config.cameraDistanceScale;
            camera.orthographicSize = Mathf.Clamp(orthoSize, MinOrthoSize, MaxOrthoSize);

            if (config.autoFrameVerticalPosition)
            {
                float halfDepth = layout.Height * config.cellDepth * 0.5f;
                lookAt.z = CombatArenaCameraFramer.SolveLookAtDepth(
                    camera,
                    lookAt,
                    config,
                    samplePoints,
                    OrbitDistance,
                    halfDepth);
                CombatArenaCameraFramer.ApplyOrbit(
                    camera,
                    lookAt,
                    config.orthoCameraElevationDegrees,
                    config.orthoCameraAzimuthDegrees,
                    OrbitDistance);
            }
        }

        private static float SolveOrthoSize(
            Camera camera,
            Vector3[] samplePoints,
            float targetSpan,
            bool vertical)
        {
            float low = MinOrthoSize;
            float high = MaxOrthoSize;

            for (int i = 0; i < SolveIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                camera.orthographicSize = mid;

                float span = vertical
                    ? CombatArenaCameraFramer.MeasureVerticalViewportSpan(camera, samplePoints)
                    : CombatArenaCameraFramer.MeasureHorizontalViewportSpan(camera, samplePoints);

                // Larger ortho size → board occupies less of the viewport.
                if (span > targetSpan)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) * 0.5f;
        }
    }
}
