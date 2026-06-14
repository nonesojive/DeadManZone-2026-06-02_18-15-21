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

        [Header("Units")]
        [Tooltip("Applied on top of each piece's combatArenaModelScale.")]
        public float unitModelScaleMultiplier = 1.25f;
        public float defaultUnitModelHeight = 1.85f;

        [Header("Environment — Synty")]
        [Tooltip("When enabled, uses the Synty ground prefab instead of a primitive plane.")]
        public bool useSyntyTerrain = true;
        [Tooltip("Spawns bunker wall props around the board edge. Off by default — walls block the oblique camera.")]
        public bool spawnPerimeterProps = false;
        [Tooltip("When enabled, applies the Synty skybox material. Off by default — bright skydomes wash out Built-in RP combat.")]
        public bool useSyntySkybox = false;
        [Tooltip("Scales ground mesh beyond board edges so the camera never sees empty space.")]
        [Range(1f, 2f)]
        public float groundPadding = 1.35f;
        [Tooltip("Soft distance fog hides ground edges and adds trench atmosphere.")]
        public bool enableArenaFog = true;
        [Range(0.005f, 0.06f)]
        public float fogDensity = 0.022f;
        [Tooltip("Large dirt tile — small tiles look like a floating diamond from the oblique camera.")]
        public string syntyGroundPrefabPath =
            "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_Large_01.prefab";
        [Tooltip("When enabled, uses a flat plane with the Synty dirt material instead of a ground mesh prefab.")]
        public bool useFlatTexturedGround = true;

        [Header("Environment — direct references (required for player builds)")]
        [Tooltip("Direct reference to the flat-ground material. Used first; the path below is only an editor fallback. " +
                 "Path-based loading does NOT work in player builds, so this must be assigned for builds to show the ground.")]
        public Material syntyGroundMaterial;
        [Tooltip("Direct reference to the ground mesh prefab. Used first; the path below is only an editor fallback.")]
        public GameObject syntyGroundPrefab;

        public string syntyGroundMaterialPath =
            "Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat";
        public string syntySkyboxMaterialPath =
            "Assets/Synty/PolygonApocalypse/Materials/Misc/Skydome_Day_01.mat";
        public string fallbackUnitPrefabPath =
            "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Rifle.prefab";
        public string fallbackBuildingPrefabPath =
            "Assets/_Project/Art/Synty/Arena/Buildings/ArenaBuilding_SupplyDepot.prefab";
        public string fallbackHqPrefabPath =
            "Assets/_Project/Art/Synty/Arena/Buildings/ArenaBuilding_Hq.prefab";
    }
}
