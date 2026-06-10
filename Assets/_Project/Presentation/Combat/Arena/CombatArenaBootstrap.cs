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
        }

        private void ConfigureCamera()
        {
            if (arenaCamera == null)
            {
                var camGo = new GameObject("ArenaCamera");
                camGo.transform.SetParent(transform, false);
                arenaCamera = camGo.AddComponent<Camera>();
                arenaCamera.tag = "MainCamera";
            }

            if (config == null)
                return;

            arenaCamera.fieldOfView = config.fieldOfView;
            float elev = config.cameraElevationDegrees * Mathf.Deg2Rad;
            float azim = config.cameraAzimuthDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(
                Mathf.Cos(elev) * Mathf.Cos(azim),
                Mathf.Sin(elev),
                Mathf.Cos(elev) * Mathf.Sin(azim)) * config.cameraDistance;

            arenaCamera.transform.position = offset;
            arenaCamera.transform.LookAt(Vector3.zero);

            if (unitsRoot == null)
            {
                var root = new GameObject("UnitsRoot");
                root.transform.SetParent(transform, false);
                unitsRoot = root.transform;
            }
        }
    }
}
