using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Config")]
    public sealed class CombatArenaConfigSO : ScriptableObject
    {
        [Header("Presentation mode")]
        [Tooltip("ToonInk3D is the only live renderer; the enum keeps its obsolete values for asset int-serialization safety.")]
        public CombatArenaVisualMode visualMode = CombatArenaVisualMode.ToonInk3D;

        [Header("Grid → world (meters)")]
        public float cellWidth = 1.8f;
        public float cellDepth = 1.8f;

        [Header("Top Troops prototype (combat rework v3)")]
        [Tooltip("Zone-tinted cube cells, cliffs, and sandbags like the TopTroopsCombat prototype. Dead 2D flag; still written by the Combat3D demo bootstrap.")]
        public bool useTopTroopsProceduralBattlefield = true;
        [Tooltip("Units march in world space toward engagement goals instead of waiting on sparse grid-step events.")]
        public bool useTopTroopsFreeChaseMovement = true;
        [Tooltip("Multiplier over sim grid-step pace for free chase. Keep at 1 (or below) — any value " +
            "above 1 makes the presentation anchor-follow arrive before the next sim tick and idle " +
            "until it, which is the 'step-and-settle' bug (2026-07-17 fluidity pass).")]
        public float topTroopsChaseSpeedMultiplier = 1f;
        [Tooltip("How many grid cells presentation may run ahead of sim anchor while marching.")]
        public float topTroopsChaseMaxLeadCells = 2f;

        [Header("Orthographic framing (CombatArenaCameraFramer ortho path)")]
        [Tooltip("Oblique pitch for an orthographic camera (same convention as cameraElevationDegrees).")]
        public float orthoCameraElevationDegrees = 52f;
        [Tooltip("Orbit yaw for orthographic framing (same convention as cameraAzimuthDegrees). 270 = player left, enemy right.")]
        public float orthoCameraAzimuthDegrees = 270f;

        [Header("Camera — Top Troops style")]
        [Tooltip("Downward pitch. Top Troops uses a steep oblique angle (~48–52°).")]
        public float cameraElevationDegrees = 52f;
        [Tooltip("Orbit around the field. 270 = player front line toward the bottom of the screen.")]
        public float cameraAzimuthDegrees = 270f;
        [Tooltip("Used when Auto Frame Width is disabled.")]
        public float cameraDistance = 28f;
        [Tooltip("Narrow FOV keeps grid lines nearly parallel like Top Troops.")]
        public float fieldOfView = 42f;
        [Tooltip("When enabled, distance is solved so board left/right edges touch the screen.")]
        public bool autoFrameWidth = true;
        [Range(0f, 0.15f)]
        public float horizontalViewportPadding = 0f;
        [Tooltip("When enabled, shifts the look target so the board sits in the middle vertical band.")]
        public bool autoFrameVerticalPosition = true;
        [Range(0.25f, 0.75f)]
        [Tooltip("Target vertical center of the board in viewport space (0.50+ shifts field upward).")]
        public float boardVerticalViewportCenter = 0.50f;
        [Tooltip("When enabled, zooms in until the board fills more vertical screen space.")]
        public bool autoFrameVerticalFill = true;
        [Range(0.45f, 0.85f)]
        [Tooltip("Target vertical span of the board in viewport space.")]
        public float verticalViewportFill = 0.58f;
        [Range(0.7f, 1.2f)]
        [Tooltip("Multiplier applied after auto framing — lower zooms in (larger board).")]
        public float cameraDistanceScale = 0.88f;
        [Tooltip("Manual look-at shift along field depth when vertical auto-frame is off.")]
        public float lookAtDepthOffset = 0f;
        [Tooltip("When enabled, uses the saved manual camera transform below instead of auto framing.")]
        public bool useManualCameraPose = false;
        [Tooltip("Exact camera world position saved from play-mode tuning.")]
        public Vector3 manualCameraWorldPosition = Vector3.zero;
        [Tooltip("Look-at point used with manual camera pose.")]
        public Vector3 lookAtWorld = Vector3.zero;

        [Header("Battlefield grid — Top Troops checkerboard")]
        [Tooltip("Dead 2D flag; still written by the Combat3D demo bootstrap.")]
        public bool showCheckerboardGrid = true;

        [Header("Motion")]
        [Tooltip("Legacy fallback when sim speed resolves to zero.")]
        public float moveLerpSeconds = 0.4f;
        [Tooltip("Scales sim-synced walk speed. Below 1 keeps units moving between sim steps.")]
        [Range(0.5f, 1.5f)]
        public float moveSpeedPresentationScale = 1.1f;
        [Tooltip("Keeps walk animation alive briefly after reaching a cell.")]
        public float moveMarchGraceSeconds = 2.4f;

        [Header("Environment — Synty (dead 2D flags; still written by the demo bootstrap / logged by URP setup)")]
        [Tooltip("When enabled, uses the Synty ground prefab instead of a primitive plane.")]
        public bool useSyntyTerrain = true;
        [Tooltip("Spawns bunker wall props around the board edge.")]
        public bool spawnPerimeterProps = false;
        [Tooltip("When enabled, applies the Synty skybox material.")]
        public bool useSyntySkybox = false;
        [Tooltip("Soft distance fog hides ground edges and adds trench atmosphere.")]
        public bool enableArenaFog = true;
    }
}
