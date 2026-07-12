using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;
#if UNITY_URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Bootstraps the ToonInk3D combat arena: camera plumbing and unit/building roots.
    /// The scene authors its own perspective camera pose, lighting, and grade.</summary>
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
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private Vector3? _authoredCameraHome;

        /// <summary>Fight-start framing: dolly/shift from the scene-authored pose (kept
        /// as the minimum) so every occupied cell is on screen — real deployments can span
        /// the full 17-column strip, which the authored close framing does not cover.</summary>
        public void FrameBattlefield3D(BattlefieldState battlefield, CombatGridMapper mapper)
        {
            if (arenaCamera == null || battlefield == null || mapper == null)
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

            EnsureUrpCameraData(arenaCamera);

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
