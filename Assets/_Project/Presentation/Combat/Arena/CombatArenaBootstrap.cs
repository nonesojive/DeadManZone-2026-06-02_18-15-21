using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform groundRoot;
        [SerializeField] private CombatArenaConfigSO config;

        public Camera ArenaCamera => arenaCamera;
        public Transform UnitsRoot => unitsRoot;
        public CombatArenaConfigSO Config => config;

        public static CombatArenaBootstrap Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (config == null)
                config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");

            EnsureGround();
            ConfigureCamera();
        }

        public void FrameBattlefield(BattlefieldLayout layout)
        {
            if (layout == null || config == null)
                return;

            EnsureGround();
            ConfigureCamera();

            if (arenaCamera != null)
                CombatArenaCameraFramer.Frame(arenaCamera, layout, config);

            FitGroundToLayout(layout);
        }

        private void FitGroundToLayout(BattlefieldLayout layout)
        {
            if (groundRoot == null || config == null)
                return;

            const float planeMeshSize = 10f;
            float boardWidth = layout.TotalWidth * config.cellWidth;
            float boardDepth = layout.Height * config.cellDepth;
            groundRoot.localScale = new Vector3(boardWidth / planeMeshSize, 1f, boardDepth / planeMeshSize);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureGround()
        {
            if (groundRoot == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
                ground.transform.SetParent(transform, false);
                ground.transform.localScale = new Vector3(3f, 1f, 2f);
                groundRoot = ground.transform;
            }

            var groundRenderer = groundRoot.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                groundMat.color = new Color(0.28f, 0.24f, 0.18f);
                groundRenderer.sharedMaterial = groundMat;
            }
        }

        private void ConfigureCamera()
        {
            if (arenaCamera == null)
            {
                var camGo = new GameObject("ArenaCamera");
                camGo.transform.SetParent(transform, false);
                arenaCamera = camGo.AddComponent<Camera>();
            }

            arenaCamera.tag = "MainCamera";
            arenaCamera.depth = 10f;
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = new Color(0.12f, 0.11f, 0.10f);

            if (unitsRoot == null)
            {
                var root = new GameObject("UnitsRoot");
                root.transform.SetParent(transform, false);
                unitsRoot = root.transform;
            }

            EnsureLighting();
        }

        private void EnsureLighting()
        {
            if (transform.Find("ArenaLight") != null)
                return;

            var lightGo = new GameObject("ArenaLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.88f);
        }
    }
}
