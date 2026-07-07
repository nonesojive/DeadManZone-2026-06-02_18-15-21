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

        private void Awake()
        {
            Instance = this;
            if (config == null)
                config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");

            ConfigureCamera();
            TopTroopsAtmosphere.Apply(config, transform, arenaCamera);
#if UNITY_URP_PRESENT
            CombatArenaPostFx.Ensure(transform);
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void FrameBattlefield(BattlefieldLayout layout)
        {
            if (layout == null || config == null)
                return;

            ConfigureCamera();
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
            arenaCamera.orthographic = true;

            EnsureUrpCameraData(arenaCamera);

            if (RenderSettings.skybox == null)
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
