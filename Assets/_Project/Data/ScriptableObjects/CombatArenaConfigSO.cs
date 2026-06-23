using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Config")]
    public sealed class CombatArenaConfigSO : ScriptableObject
    {
        [Header("Presentation mode")]
        [Tooltip("Legacy3D keeps Synty models and perspective camera. TopTroops2D uses ortho camera and sprite units.")]
        public CombatArenaVisualMode visualMode = CombatArenaVisualMode.Legacy3D;

        [Header("Grid → world (meters)")]
        public float cellWidth = 1.8f;
        public float cellDepth = 1.8f;

        [Header("Top Troops prototype (combat rework v3)")]
        [Tooltip("Zone-tinted cube cells, cliffs, and sandbags like the TopTroopsCombat prototype.")]
        public bool useTopTroopsProceduralBattlefield = true;
        [Tooltip("Capsule soldiers when no combatArenaPrefab is assigned.")]
        public bool useProceduralUnitVisuals = false;
        [Tooltip("Units march in world space toward engagement goals instead of waiting on sparse grid-step events.")]
        public bool useTopTroopsFreeChaseMovement = true;
        [Tooltip("Multiplier over sim grid-step pace for free chase (1.2 = 20% faster than anchor advance).")]
        public float topTroopsChaseSpeedMultiplier = 1.2f;
        [Tooltip("How many grid cells presentation may run ahead of sim anchor while marching.")]
        public float topTroopsChaseMaxLeadCells = 2f;
        [Tooltip("Bright sky-blue background instead of grim trench fog.")]
        public bool useTopTroopsBrightSky = false;
        public Color topTroopsPlayerZoneColor = new(0.54f, 0.44f, 0.30f);
        public Color topTroopsNeutralZoneColor = new(0.46f, 0.42f, 0.36f);
        public Color topTroopsEnemyZoneColor = new(0.42f, 0.34f, 0.30f);
        public Color topTroopsSkyColor = new(0.24f, 0.21f, 0.18f);

        [Header("2D arena")]
        [Tooltip("Peak height of arced rifle/cannon tracers in world units.")]
        public float projectileArcHeight = 0.6f;
        [Tooltip("Oblique pitch for orthographic TopTroops2D camera (same convention as cameraElevationDegrees).")]
        public float orthoCameraElevationDegrees = 52f;
        [Tooltip("Orbit yaw for 2D mode (same convention as cameraAzimuthDegrees). 270 = player left, enemy right.")]
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
        [Tooltip("Visible brown dirt tiles aligned to sim cells.")]
        public bool showCheckerboardGrid = true;
        public Color gridLightCellColor = new(0.72f, 0.56f, 0.38f);
        public Color gridDarkCellColor = new(0.50f, 0.37f, 0.26f);
        public Color gridBackdropColor = new(0.16f, 0.12f, 0.09f);
        [Range(0f, 0.12f)]
        [Tooltip("Gap between cells — exposes backdrop as grid lines.")]
        public float gridCellInset = 0.045f;
        [Tooltip("Slight lift above ground plane to avoid z-fighting.")]
        public float gridYOffset = 0.04f;

        [Header("Motion")]
        [Tooltip("Legacy fallback when sim speed resolves to zero.")]
        public float moveLerpSeconds = 0.4f;
        [Tooltip("Scales sim-synced walk speed. Below 1 keeps units moving between sim steps.")]
        [Range(0.5f, 1.5f)]
        public float moveSpeedPresentationScale = 1.1f;
        [Tooltip("Keeps walk animation alive briefly after reaching a cell.")]
        public float moveMarchGraceSeconds = 2.4f;
        public float attackLungeSeconds = 0.15f;
        public float attackLungeDistance = 0.35f;

        [Header("Transition")]
        public float unitSpawnFadeSeconds = 0.35f;

        [Header("Units")]
        [Tooltip("Applied on top of each piece's combatArenaModelScale.")]
        public float unitModelScaleMultiplier = 1.25f;
        public float defaultUnitModelHeight = 1.85f;
        public float defaultVehicleModelHeight = 1.8f;

        [Header("Environment — Synty")]
        [Tooltip("When enabled, uses the Synty ground prefab instead of a primitive plane.")]
        public bool useSyntyTerrain = true;
        [Tooltip("Grim trench atmosphere profile. When set, overrides legacy environment defaults.")]
        public CombatArenaAtmosphereProfileSO atmosphereProfile;
        [Tooltip("Spawns bunker wall props around the board edge. Superseded by CombatArenaBackdrop when atmosphere profile enables backdrop.")]
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
