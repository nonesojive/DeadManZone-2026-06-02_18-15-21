using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;
#if UNITY_URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Bootstraps the 2D (Top Troops) combat arena: orthographic camera, roots, atmosphere.</summary>
    public sealed class CombatArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform buildingsRoot;
        [SerializeField] private CombatArenaConfigSO config;

        public Camera ArenaCamera => arenaCamera;
        public Transform UnitsRoot => unitsRoot;
        public Transform BuildingsRoot => buildingsRoot;
        public CombatArenaConfigSO Config => config;

        public static CombatArenaBootstrap Instance { get; private set; }

        /// <summary>ToonInk3D scenes author their own perspective camera, lighting, and grade;
        /// the 2D atmosphere/battlefield build and orthographic framing must not run.</summary>
        private bool Is3DMode =>
            config != null && config.visualMode == CombatArenaVisualMode.ToonInk3D;

        private void Awake()
        {
            Instance = this;
            if (config == null)
                config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");

            // Units interpolate toward their sim positions with Time.deltaTime, so a single
            // long frame (a GC collection, or a volley's burst of VFX) makes them teleport
            // forward. Cap the frame step so a hitch degrades to a brief slow-down instead of
            // a visible jump (the default 0.333 lets a stall advance a third of a second).
            Time.maximumDeltaTime = 0.05f;

            ConfigureCamera();
            if (!Is3DMode)
            {
                TopTroopsAtmosphere.Apply(config, transform, arenaCamera);
#if UNITY_URP_PRESENT
                CombatArenaPostFx.Ensure(transform);
#endif
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private Vector3? _authoredCameraHome;

        /// <summary>3D fight-start framing: dolly/shift from the scene-authored pose (kept
        /// as the minimum) so every occupied cell is on screen — real deployments can span
        /// the full 17-column strip, which the authored close framing does not cover.
        /// No-op in 2D mode (the orthographic framer owns that path).</summary>
        public void FrameBattlefield3D(BattlefieldState battlefield, CombatGridMapper mapper)
        {
            if (!Is3DMode || arenaCamera == null || battlefield == null || mapper == null)
                return;

            _authoredCameraHome ??= arenaCamera.transform.position;

            var points = new System.Collections.Generic.List<Vector3>();
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                var ground = mapper.ToWorld(cell.Position);
                points.Add(ground);
                points.Add(ground + Vector3.up * 2.2f); // unit head height incl. tall rigs
            }

            arenaCamera.transform.position = CombatArena3DCameraFramer.ComputeFramedPosition(
                arenaCamera, _authoredCameraHome.Value, points);
        }

        public void FrameBattlefield(BattlefieldLayout layout)
        {
            if (layout == null || config == null)
                return;

            ConfigureCamera();
            if (Is3DMode)
                return; // scene-authored camera pose/lighting; no 2D battlefield view.

            TopTroopsAtmosphere.Apply(config, transform, arenaCamera);
#if UNITY_URP_PRESENT
            CombatArenaPostFx.Ensure(transform);
#endif

            var mapper = new CombatGridMapper(layout, config.cellWidth, config.cellDepth);
            CombatArena2DBattlefieldView.Build(transform, layout, mapper, config, arenaCamera);

            if (arenaCamera != null)
                CombatArenaOrthographicFramer.Frame(arenaCamera, layout, config);
        }

        private void ConfigureCamera()
        {
            if (arenaCamera == null)
            {
                var camGo = new GameObject("ArenaCamera");
                camGo.transform.SetParent(transform, false);
                arenaCamera = camGo.AddComponent<Camera>();
            }

            arenaCamera.tag = "Untagged";
            arenaCamera.depth = 10f;
            arenaCamera.rect = new Rect(0f, 0f, 1f, 1f);
            arenaCamera.allowHDR = true; // let bloom pick up the bright additive muzzle/tracer VFX
            if (!Is3DMode)
                arenaCamera.orthographic = true;

            EnsureUrpCameraData(arenaCamera);

            if (!Is3DMode && RenderSettings.skybox == null)
            {
                arenaCamera.clearFlags = CameraClearFlags.SolidColor;
                arenaCamera.backgroundColor = new Color(0.12f, 0.11f, 0.10f);
            }

            if (unitsRoot == null)
            {
                var root = new GameObject("UnitsRoot");
                root.transform.SetParent(transform, false);
                unitsRoot = root.transform;
            }

            if (buildingsRoot == null)
            {
                var root = new GameObject("BuildingsRoot");
                root.transform.SetParent(transform, false);
                buildingsRoot = root.transform;
            }
        }

        private static void EnsureUrpCameraData(Camera camera)
        {
#if UNITY_URP_PRESENT
            if (camera == null || !CombatArenaMaterialUtility.IsUrpActive())
                return;

            var urpData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (urpData == null)
                urpData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            urpData.renderPostProcessing = true;
            urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
#endif
        }
    }
}
