using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Config")]
    public sealed class CombatArenaConfigSO : ScriptableObject
    {
        [Header("Grid → world (meters)")]
        public float cellWidth = 1.8f;
        public float cellDepth = 1.8f;

        [Header("Camera — Top Troops style")]
        [Tooltip("Downward pitch. Top Troops uses a steep oblique angle (~48–52°).")]
        public float cameraElevationDegrees = 50f;
        [Tooltip("Orbit around the field. 270 = player front line toward the bottom of the screen.")]
        public float cameraAzimuthDegrees = 270f;
        [Tooltip("Used when Auto Frame Width is disabled.")]
        public float cameraDistance = 28f;
        [Tooltip("Narrow FOV keeps grid lines nearly parallel like Top Troops.")]
        public float fieldOfView = 38f;
        [Tooltip("When enabled, distance is solved so board left/right edges touch the screen.")]
        public bool autoFrameWidth = true;
        [Range(0f, 0.15f)]
        public float horizontalViewportPadding = 0f;
        [Tooltip("When enabled, shifts the look target so the board sits in the middle vertical band.")]
        public bool autoFrameVerticalPosition = true;
        [Range(0.25f, 0.75f)]
        [Tooltip("Target vertical center of the board in viewport space (0.44 ≈ Top Troops).")]
        public float boardVerticalViewportCenter = 0.44f;
        [Tooltip("Manual look-at shift along field depth when vertical auto-frame is off.")]
        public float lookAtDepthOffset = 0f;
        [Tooltip("When enabled, uses the saved manual camera transform below instead of auto framing.")]
        public bool useManualCameraPose = false;
        [Tooltip("Exact camera world position saved from play-mode tuning.")]
        public Vector3 manualCameraWorldPosition = Vector3.zero;
        [Tooltip("Look-at point used with manual camera pose.")]
        public Vector3 lookAtWorld = Vector3.zero;

        [Header("Motion")]
        public float moveLerpSeconds = 0.4f;
        public float attackLungeSeconds = 0.15f;
        public float attackLungeDistance = 0.35f;

        [Header("Transition")]
        public float unitSpawnFadeSeconds = 0.35f;
    }
}
