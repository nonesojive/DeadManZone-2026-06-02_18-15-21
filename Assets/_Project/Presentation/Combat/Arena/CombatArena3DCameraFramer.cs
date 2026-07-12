using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Fight-start framing for the 3D arena's perspective camera. The scene-authored pose
    /// (tuned for readability) is the HOME MINIMUM; when a deployment is wider than it
    /// covers, the camera shifts laterally to the occupied hull's x-center and dollies
    /// backward along its own forward axis — never forward of home, rotation and fov
    /// untouched — until every occupied point sits inside the viewport with a margin.
    /// Units only converge inward after the opening (player marches +x, enemy −x), so the
    /// fight-start hull contains the whole fight. Idle punch-in camera absorbs the new
    /// pose as its home automatically.
    /// </summary>
    public static class CombatArena3DCameraFramer
    {
        /// <summary>Compute the framed camera position. Pure given the camera's rotation,
        /// fov and aspect — the camera is only used for projection math and is restored.</summary>
        public static Vector3 ComputeFramedPosition(
            Camera camera,
            Vector3 homePosition,
            IReadOnlyList<Vector3> worldPoints,
            float viewportMargin = 0.06f,
            float maxPullbackMeters = 40f)
        {
            if (camera == null || worldPoints == null || worldPoints.Count == 0)
                return homePosition;

            // Lateral: center on the occupied hull (strip is world-X aligned).
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                minX = Mathf.Min(minX, worldPoints[i].x);
                maxX = Mathf.Max(maxX, worldPoints[i].x);
            }

            var basePosition = new Vector3((minX + maxX) * 0.5f, homePosition.y, homePosition.z);
            var backward = -camera.transform.forward;

            var savedPosition = camera.transform.position;
            try
            {
                if (FitsAt(camera, basePosition, worldPoints, viewportMargin))
                    return basePosition;

                // Binary search the smallest pullback that fits.
                float lo = 0f, hi = maxPullbackMeters;
                if (!FitsAt(camera, basePosition + backward * hi, worldPoints, viewportMargin))
                {
                    Debug.LogWarning(
                        "[Combat3D] Camera framer hit max pullback without fitting every unit — " +
                        "framing at max. Check the authored pose/fov against the battlefield size.");
                    return basePosition + backward * hi;
                }

                for (int i = 0; i < 24; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (FitsAt(camera, basePosition + backward * mid, worldPoints, viewportMargin))
                        hi = mid;
                    else
                        lo = mid;
                }

                return basePosition + backward * hi;
            }
            finally
            {
                camera.transform.position = savedPosition;
            }
        }

        private static bool FitsAt(
            Camera camera, Vector3 position, IReadOnlyList<Vector3> points, float margin)
        {
            camera.transform.position = position;
            for (int i = 0; i < points.Count; i++)
            {
                var vp = camera.WorldToViewportPoint(points[i]);
                if (vp.z <= camera.nearClipPlane ||
                    vp.x < margin || vp.x > 1f - margin ||
                    vp.y < margin || vp.y > 1f - margin)
                    return false;
            }

            return true;
        }
    }
}
