using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaBootstrap : MonoBehaviour
    {
        private const float PlaneMeshSize = 10f;
        private const float PerimeterPropOffset = 1.8f;
        private const int PerimeterPropCount = 8;
        private const string DefaultPerimeterPrefabPath =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Bunker_Wall_01.prefab";

        [SerializeField] private Camera arenaCamera;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform buildingsRoot;
        [SerializeField] private Transform groundRoot;
        [SerializeField] private CombatArenaConfigSO config;

        private Transform _propsRoot;
        private bool _usingSyntyGround;
        private bool _usingFlatGround;
        private bool _usingGridBackdrop;

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

            EnsureGround();
            ConfigureCamera();
            ApplyEnvironment();
        }

        public void FrameBattlefield(BattlefieldLayout layout)
        {
            if (layout == null || config == null)
                return;

            EnsureGround();
            ConfigureCamera();

            if (arenaCamera != null)
                CombatArenaCameraFramer.Frame(arenaCamera, layout, config);

            if (config.useTopTroopsProceduralBattlefield)
            {
                ApplyTopTroopsBattlefield(layout);
                return;
            }

            FitGroundToLayout(layout);
            SpawnPerimeterProps(layout);
            CombatArenaGridView.Build(transform, layout, config);
            CombatArenaBackdrop.Build(transform, layout, config, config?.atmosphereProfile);
        }

        private void ApplyTopTroopsBattlefield(BattlefieldLayout layout)
        {
            HideGroundForProceduralBattlefield();
            ApplyTopTroopsSky();

            TopTroopsBattlefieldBuilder.Build(
                transform,
                layout,
                config.cellWidth,
                config.cellDepth,
                TopTroopsBattlefieldPalette.FromConfig(config));
        }

        private void HideGroundForProceduralBattlefield()
        {
            if (groundRoot == null)
                return;

            var renderer = groundRoot.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        private void ApplyTopTroopsSky()
        {
            if (config == null || !config.useTopTroopsBrightSky || arenaCamera == null)
                return;

            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = config.topTroopsSkyColor;
        }

        private void FitGroundToLayout(BattlefieldLayout layout)
        {
            if (groundRoot == null || config == null)
                return;

            float padding = config.showCheckerboardGrid
                ? Mathf.Max(1.05f, config.groundPadding * 0.78f)
                : config.groundPadding > 0f ? config.groundPadding : 1f;
            float boardWidth = layout.TotalWidth * config.cellWidth * padding;
            float boardDepth = layout.Height * config.cellDepth * padding;

            groundRoot.localScale = Vector3.one;
            if (CombatArenaVisualPlacement.TryMeasureMeshFootprint(groundRoot, out float meshWidth, out float meshDepth))
            {
                groundRoot.localScale = new Vector3(boardWidth / meshWidth, 1f, boardDepth / meshDepth);
                return;
            }

            groundRoot.localScale = new Vector3(boardWidth / PlaneMeshSize, 1f, boardDepth / PlaneMeshSize);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ApplyEnvironment()
        {
            if (config != null
                && config.useSyntySkybox
                && (config.atmosphereProfile == null || !config.atmosphereProfile.enableFog)
                && !string.IsNullOrEmpty(config.syntySkyboxMaterialPath))
            {
                var skybox = SyntyRuntimeAssetLoader.LoadMaterial(config.syntySkyboxMaterialPath);
                if (CombatArenaMaterialUtility.IsMaterialRenderable(skybox))
                {
                    RenderSettings.skybox = skybox;
                    if (arenaCamera != null)
                        arenaCamera.clearFlags = CameraClearFlags.Skybox;
                    return;
                }
            }

            RenderSettings.skybox = null;
            if (config?.atmosphereProfile != null)
                CombatArenaAtmosphereController.Ensure(transform)
                    .Apply(config.atmosphereProfile, config, arenaCamera);
            else
                CombatArenaEnvironment.Apply(config, transform, arenaCamera);
        }

        private void EnsureGround()
        {
            if (config != null && config.showCheckerboardGrid && TryEnsureGridBackdropGround())
                return;

            if (config != null && config.useSyntyTerrain && TryEnsureSyntyGround())
                return;

            EnsurePrimitiveGround();
        }

        private bool TryEnsureGridBackdropGround()
        {
            if (groundRoot != null && (_usingSyntyGround || !_usingGridBackdrop))
            {
                DestroyGroundObject(groundRoot.gameObject);
                groundRoot = null;
            }

            if (groundRoot == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
                ground.transform.SetParent(transform, false);

                var collider = ground.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                groundRoot = ground.transform;
                _usingSyntyGround = false;
                _usingFlatGround = false;
                _usingGridBackdrop = true;
            }

            var renderer = groundRoot.GetComponent<Renderer>();
            if (renderer != null)
                CombatArenaMaterialUtility.ApplySolidGroundMaterial(renderer, config.gridBackdropColor);

            return true;
        }

        private bool TryEnsureSyntyGround()
        {
            if (config.useFlatTexturedGround && TryEnsureFlatTexturedGround())
                return true;

            var prefab = config.syntyGroundPrefab != null
                ? config.syntyGroundPrefab
                : SyntyRuntimeAssetLoader.LoadPrefab(config.syntyGroundPrefabPath);
            if (prefab == null)
                return false;

            if (groundRoot != null && (_usingFlatGround || !_usingSyntyGround))
            {
                DestroyGroundObject(groundRoot.gameObject);
                groundRoot = null;
                _usingGridBackdrop = false;
            }

            if (groundRoot == null)
            {
                var instance = Instantiate(prefab, transform, false);
                instance.name = "ArenaGround";
                groundRoot = instance.transform;
                _usingSyntyGround = true;
                _usingFlatGround = false;
                CombatArenaFxCull.RemoveTransparentFxRenderers(instance);
            }

            return true;
        }

        private bool TryEnsureFlatTexturedGround()
        {
            var material = config.syntyGroundMaterial != null
                ? config.syntyGroundMaterial
                : SyntyRuntimeAssetLoader.LoadMaterial(config.syntyGroundMaterialPath);
            if (!CombatArenaMaterialUtility.IsMaterialRenderable(material))
                return false;

            if (groundRoot != null && !_usingFlatGround)
            {
                DestroyGroundObject(groundRoot.gameObject);
                groundRoot = null;
                _usingGridBackdrop = false;
            }

            if (groundRoot == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
                ground.transform.SetParent(transform, false);

                var collider = ground.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                groundRoot = ground.transform;
                _usingSyntyGround = true;
                _usingFlatGround = true;
            }

            var groundRenderer = groundRoot.GetComponent<Renderer>();
            if (groundRenderer != null)
                groundRenderer.sharedMaterial = material;

            return true;
        }

        private void EnsurePrimitiveGround()
        {
            _usingSyntyGround = false;
            _usingFlatGround = false;
            _usingGridBackdrop = false;

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
                CombatArenaMaterialUtility.ApplyFallbackGroundMaterial(groundRenderer);
        }

        private void SpawnPerimeterProps(BattlefieldLayout layout)
        {
            if (config == null || !config.spawnPerimeterProps)
                return;

            if (config.atmosphereProfile != null && config.atmosphereProfile.enableBackdrop)
                return;

            var prefab = SyntyRuntimeAssetLoader.LoadPrefab(DefaultPerimeterPrefabPath);
            if (prefab == null)
                return;

            EnsurePropsRoot();
            ClearPropsRoot();

            float halfWidth = layout.TotalWidth * config.cellWidth * 0.5f;
            float halfDepth = layout.Height * config.cellDepth * 0.5f;
            float offset = PerimeterPropOffset;

            var positions = new[]
            {
                new Vector3(-halfWidth - offset, 0f, halfDepth + offset),
                new Vector3(0f, 0f, halfDepth + offset),
                new Vector3(halfWidth + offset, 0f, halfDepth + offset),
                new Vector3(halfWidth + offset, 0f, 0f),
                new Vector3(halfWidth + offset, 0f, -halfDepth - offset),
                new Vector3(0f, 0f, -halfDepth - offset),
                new Vector3(-halfWidth - offset, 0f, -halfDepth - offset),
                new Vector3(-halfWidth - offset, 0f, 0f)
            };

            for (int i = 0; i < PerimeterPropCount; i++)
            {
                var instance = Instantiate(prefab, _propsRoot, false);
                instance.name = $"PerimeterProp_{i + 1}";
                instance.transform.localPosition = positions[i];
                instance.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
        }

        private void EnsurePropsRoot()
        {
            if (_propsRoot != null)
                return;

            var existing = transform.Find("ArenaProps");
            if (existing != null)
            {
                _propsRoot = existing;
                return;
            }

            var propsGo = new GameObject("ArenaProps");
            propsGo.transform.SetParent(transform, false);
            _propsRoot = propsGo.transform;
        }

        private void ClearPropsRoot()
        {
            if (_propsRoot == null)
                return;

            for (int i = _propsRoot.childCount - 1; i >= 0; i--)
                DestroyGroundObject(_propsRoot.GetChild(i).gameObject);
        }

        private static void DestroyGroundObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
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
            arenaCamera.allowHDR = false;
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

            EnsureLighting();
        }

        private void EnsureLighting()
        {
            try
            {
                if (config?.atmosphereProfile != null)
                    CombatArenaAtmosphereController.Ensure(transform)
                        .Apply(config.atmosphereProfile, config, arenaCamera);
                else
                    CombatArenaEnvironment.Apply(config, transform, arenaCamera);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CombatArena] Atmosphere setup failed; falling back to legacy environment. {ex.Message}");
                CombatArenaEnvironment.Apply(config, transform, arenaCamera);
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
