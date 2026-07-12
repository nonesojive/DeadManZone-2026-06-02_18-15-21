#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/Combat3D_Demo.unity: spike-look lighting (warm key /
    /// cool ambient, P0_Grade volume), the Trenchline battlefield environment
    /// (CombatEnvironmentBuilder — broken earth, trenchworks, wire, backdrop ring), and the
    /// combat arena rig (bootstrap/director/presenter/pool) configured for ToonInk3D visuals
    /// with the Phase-0 rifleman model, a freshly generated AnimatorController, and side rings.
    /// The spike scene/controller stay throwaway — everything generated lands under _Project.
    /// </summary>
    public static class Combat3DDemoSceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat3D_Demo.unity";
        private const string GeneratedFolder = "Assets/_Project/Combat3D";
        private const string ControllerPath = GeneratedFolder + "/RiflemanCombat3D.controller";
        private const string IdleClipPath = GeneratedFolder + "/Rifleman_Idle.anim";
        private const string WalkClipPath = GeneratedFolder + "/Rifleman_Walk.anim";
        private const string DieClipPath = GeneratedFolder + "/Rifleman_Die.anim";
        private const string ConfigPath = GeneratedFolder + "/CombatArena3DDemoConfig.asset";
        private const string CombatRendererPath = "Assets/_Project/Settings/Rendering/DeadManZone_CombatRenderer.asset";

        // Roster archetypes: Meshy 12k units copied under _Project (idle/walk/die GLBs sharing
        // one rig per unit), mapped to the ContentDatabase piece id the sim uses.
        // A unit with broken/missing GLBs is skipped with a warning - actors with its piece id
        // fall back to the default rifleman visuals instead of blocking the scene build.
        //
        // Coverage of the 11 combat-eligible (category Unit) pieces:
        // - conscript_rifleman + enlisted_rifleman: intentionally NOT listed. The default
        //   rifleman model/controller (IdleGlbPath etc., Models/enlisted_rifleman/) IS their
        //   visual; archetype entries would just duplicate those same assets.
        // - armored_transport, ironmarch_iron_horse, machine_gun_nest: awaiting reference
        //   images AND non-humanoid (vehicles/emplacement) — the Meshy humanoid rig pipeline
        //   doesn't fit them. They fall back to rifleman visuals by design until the owner
        //   decides their treatment.
        private static readonly (string folder, string pieceId)[] RosterUnits =
        {
            ("bulwark_squad", "bulwark_squad"),
            ("field_medic", "field_medic"),
            ("ironclad_mortars", "ironclad_mortars"),
            ("ironclad_field_marshal", "ironclad_field_marshal"),
            ("ironclad_marksman", "ironclad_marksman"),
            ("ironmarch_surgeon", "ironmarch_surgeon"),
        };

        // Non-humanoid units (owner spec, audit "non-humanoid treatment"): single static
        // model.glb from generate_unit.py --vehicle, rendered by CombatUnitVisual3DVehicle
        // (code-driven rumble/recoil/collapse — no rig, no clips, no rifle). Height =
        // authored silhouette height in meters (these are not 1.7 m infantry).
        // yaw = per-gen facing correction (Meshy side-view refs come out with arbitrary
        // authored forward); tuned live in Play mode, baked here.
        private static readonly (string folder, string pieceId, float height, float yaw)[] VehicleUnits =
        {
            // Facing verified per gen via isolated probes (identity rotation, Front view):
            // tank/transport authored +X → -90; nest barrel authored -X → +90.
            ("ironmarch_iron_horse", "ironmarch_iron_horse", 2.4f, -90f),
            ("armored_transport", "armored_transport", 2.5f, -90f),
            ("machine_gun_nest", "machine_gun_nest", 1.6f, 90f),
        };

        private static string RosterModelFolder(string unitFolder) =>
            GeneratedFolder + "/Models/" + unitFolder;

        // Default rifleman GLBs: _Project copies of the proven Phase-0 spike set
        // (enlisted_rifleman_12k*.glb), so the scene's models no longer depend on the
        // throwaway spike folder. Materials/grade below are still spike-sourced.
        private const string IdleGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/idle.glb";
        private const string WalkGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/walk.glb";
        private const string DieGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/die.glb";

        // Proven Phase-0 spike assets (referenced, never modified).
        private const string PlayerMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Player.mat";
        private const string EnemyMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Enemy.mat";
        private const string FallbackUnitMaterialPath = "Assets/_Phase0Spike/Materials/ToonInk_Meshy.mat";
        private const string GradeProfilePath = "Assets/_Phase0Spike/Materials/P0_Grade.asset";

        // Ring-fill health rings (replace the spike's flat RingBlue/RingRed discs): the base
        // ring IS the unit health display. Fill colors sampled from the spike ring palette;
        // rims lifted slightly so a near-dead unit's side still reads. Muted per bible §3.
        private const string RingFillShaderPath =
            "Assets/_Project/Presentation/Combat/Arena/Shaders/CombatRingFill.shader";
        private const string PlayerRingPath = GeneratedFolder + "/RingFill_Player.mat";
        private const string EnemyRingPath = GeneratedFolder + "/RingFill_Enemy.mat";

        /// <summary>Everything a Combat3D scene build needs, generated/validated once.
        /// Shared by the demo scene and the run-flow CombatArena3D scene builders.</summary>
        /// <summary>One installer archetype entry: humanoid (model + controller) or vehicle
        /// (static model, code-driven motion, per-unit silhouette height).</summary>
        internal struct ArchetypeSpec
        {
            public string PieceId;
            public GameObject Model;
            public AnimatorController Controller;
            public bool IsVehicle;
            public float VehicleHeight;
            public float VehicleYawOffsetDegrees;
        }

        internal sealed class SceneAssets
        {
            public GameObject IdleModel;
            public Material PlayerMat;
            public Material EnemyMat;
            public Material RingBlue;
            public Material RingRed;
            public VolumeProfile GradeProfile;
            public AnimatorController Controller;
            public List<ArchetypeSpec> Archetypes;
            public CombatArenaConfigSO Config;
            public GameObject RiflePrefab;
            public CombatArenaAudioSetSO AudioSet;
            public AudioClip AmbienceLoop;
        }

        /// <summary>Validate the spike/model assets and (re)generate the derived assets
        /// (materials, controllers, config, rifle, audio). Null + logged error on failure.</summary>
        internal static SceneAssets PrepareSceneAssets()
        {
            // --- Validate spike assets up front; friendly abort if the spike moved. ---
            var missing = new List<string>();
            var idleModel = LoadOrFlag<GameObject>(IdleGlbPath, missing);
            LoadOrFlag<GameObject>(WalkGlbPath, missing);
            LoadOrFlag<GameObject>(DieGlbPath, missing);
            // Tuned _Project copies of the spike unit materials. The spike defaults
            // (_ShadowColor 0.10) read as solid black at gameplay camera distance;
            // these lift the shadow band so the toon-ink shading stays readable.
            var playerMat = LoadOrCreateTunedUnitMaterial(
                PlayerMaterialPath, GeneratedFolder + "/Unit_Player_3D.mat");
            var enemyMat = LoadOrCreateTunedUnitMaterial(
                EnemyMaterialPath, GeneratedFolder + "/Unit_Enemy_3D.mat");
            var fallbackMat = AssetDatabase.LoadAssetAtPath<Material>(FallbackUnitMaterialPath);
            if (playerMat == null)
                playerMat = fallbackMat;
            if (enemyMat == null)
                enemyMat = fallbackMat;
            if (playerMat == null || enemyMat == null)
                missing.Add($"{PlayerMaterialPath} / {EnemyMaterialPath} (and fallback {FallbackUnitMaterialPath})");
            var ringFillShader = LoadOrFlag<Shader>(RingFillShaderPath, missing);
            var gradeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradeProfilePath);
            if (gradeProfile == null)
                Debug.LogWarning($"[Combat3D] Grade volume profile missing at {GradeProfilePath} — scene will render ungraded.");

            var idleClip = FindClip(IdleGlbPath, missing, "idle");
            var walkClip = FindClip(WalkGlbPath, missing, "walk");
            var dieClip = FindClip(DieGlbPath, missing, "dead", "die");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    "[Combat3D] Aborting — required Phase-0 spike assets are missing:\n - " +
                    string.Join("\n - ", missing));
                return null;
            }

            // --- Generated assets under _Project (spike stays throwaway). ---
            EnsureFolder(GeneratedFolder);
            // Placeholder SFX set + ambience bed (no real combat audio exists in the project yet).
            var (audioSet, ambienceLoop) = Combat3DPlaceholderAudioBuilder.EnsureAudioSet();
            return new SceneAssets
            {
                IdleModel = idleModel,
                PlayerMat = playerMat,
                EnemyMat = enemyMat,
                GradeProfile = gradeProfile,
                RingBlue = LoadOrCreateRingFillMaterial(
                    PlayerRingPath, ringFillShader,
                    fill: new Color(0.20f, 0.28f, 0.42f),   // spike RingBlue
                    rim: new Color(0.28f, 0.40f, 0.60f),
                    empty: new Color(0.09f, 0.10f, 0.13f)),
                RingRed = LoadOrCreateRingFillMaterial(
                    EnemyRingPath, ringFillShader,
                    fill: new Color(0.40f, 0.18f, 0.16f),   // spike RingRed
                    rim: new Color(0.56f, 0.25f, 0.21f),
                    empty: new Color(0.12f, 0.09f, 0.09f)),
                Controller = BuildAnimatorController(
                    idleClip, walkClip, dieClip, IdleClipPath, WalkClipPath, DieClipPath, ControllerPath),
                Archetypes = BuildRosterArchetypes(),
                Config = BuildDemoArenaConfig(),
                RiflePrefab = RiflePropBuilder.EnsurePrefab(),
                AudioSet = audioSet,
                AmbienceLoop = ambienceLoop
            };
        }

        [MenuItem("DeadManZone/Combat3D/Build Combat3D Demo Scene")]
        public static void BuildDemoScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Combat3D] Exit Play mode before building the demo scene.");
                return;
            }

            var assets = PrepareSceneAssets();
            if (assets == null)
                return;

            // --- Scene ---
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Combat3D] Scene build cancelled (unsaved scene).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironmentLighting();
            var camera = CreateCamera(includeAudioListener: true);
            CreateKeyLight();
            CreateGlobalVolume(assets.GradeProfile);
            CombatEnvironmentBuilder.Build();
            CreateArenaRig(camera, assets.Config, assets.Controller, assets.IdleModel, assets.PlayerMat,
                assets.EnemyMat, assets.RingBlue, assets.RingRed, assets.RiflePrefab, assets.Archetypes,
                assets.AudioSet, assets.AmbienceLoop);

            EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Combat3D] Demo scene saved to {ScenePath}. Open it and press Play — " +
                      "the 3v3 fight auto-starts through the Core sim.");
        }

        private static T LoadOrFlag<T>(string path, List<string> missing) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                missing.Add(path);
            return asset;
        }

        private static AnimationClip FindClip(string glbPath, List<string> missing, params string[] keywords)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(glbPath)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.Contains("__preview"))
                .ToList();

            foreach (string keyword in keywords)
            {
                var match = clips.FirstOrDefault(c =>
                    c.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null)
                    return match;
            }

            // Single-clip GLBs: take the only clip rather than failing on a rename.
            if (clips.Count == 1)
                return clips[0];

            missing.Add($"{glbPath} (no AnimationClip matching '{string.Join("/", keywords)}'; " +
                        $"found: {string.Join(", ", clips.Select(c => c.name))})");
            return null;
        }

        /// <summary>One archetype entry per roster unit whose GLBs imported cleanly:
        /// generated looped .anim copies + AnimatorController (same logic as the rifleman)
        /// living next to the unit's GLBs under Combat3D/Models/&lt;unit&gt;/. Vehicle units
        /// append after: static model.glb only, no controller.</summary>
        private static List<ArchetypeSpec> BuildRosterArchetypes()
        {
            var archetypes = new List<ArchetypeSpec>();
            foreach (var (unitFolder, pieceId) in RosterUnits)
            {
                string folder = RosterModelFolder(unitFolder);
                var unitMissing = new List<string>();
                var model = LoadOrFlag<GameObject>(folder + "/idle.glb", unitMissing);
                var idle = FindClip(folder + "/idle.glb", unitMissing, "idle");
                var walk = FindClip(folder + "/walk.glb", unitMissing, "walk");
                var die = FindClip(folder + "/die.glb", unitMissing, "dead", "die");

                if (unitMissing.Count > 0)
                {
                    Debug.LogWarning(
                        $"[Combat3D] Roster unit '{unitFolder}' skipped (falls back to rifleman visuals):\n - " +
                        string.Join("\n - ", unitMissing));
                    continue;
                }

                var controller = BuildAnimatorController(
                    idle, walk, die,
                    $"{folder}/{unitFolder}_Idle.anim",
                    $"{folder}/{unitFolder}_Walk.anim",
                    $"{folder}/{unitFolder}_Die.anim",
                    $"{folder}/{unitFolder}Combat3D.controller");
                archetypes.Add(new ArchetypeSpec
                {
                    PieceId = pieceId,
                    Model = model,
                    Controller = controller,
                });
            }

            foreach (var (unitFolder, pieceId, height, yaw) in VehicleUnits)
            {
                string path = RosterModelFolder(unitFolder) + "/model.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Debug.LogWarning(
                        $"[Combat3D] Vehicle unit '{unitFolder}' skipped — no {path} yet " +
                        "(run tools/meshy/generate_unit.py <unit> --vehicle); falls back to rifleman visuals.");
                    continue;
                }

                archetypes.Add(new ArchetypeSpec
                {
                    PieceId = pieceId,
                    Model = model,
                    IsVehicle = true,
                    VehicleHeight = height,
                    VehicleYawOffsetDegrees = yaw,
                });
            }

            return archetypes;
        }

        private static AnimatorController BuildAnimatorController(
            AnimationClip idleSource, AnimationClip walkSource, AnimationClip dieSource,
            string idleClipPath, string walkClipPath, string dieClipPath, string controllerPath)
        {
            // Writable copies so loop flags can be set (imported GLB sub-assets are read-only).
            var idle = SaveClipCopy(idleSource, idleClipPath, loop: true);
            var walk = SaveClipCopy(walkSource, walkClipPath, loop: true);
            var die = SaveClipCopy(dieSource, dieClipPath, loop: false);

            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            var walkState = stateMachine.AddState("Walk");
            walkState.motion = walk;
            var dieState = stateMachine.AddState("Die");
            dieState.motion = die;
            stateMachine.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            var anyToDie = stateMachine.AddAnyStateTransition(dieState);
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.08f;
            anyToDie.canTransitionToSelf = false;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip SaveClipCopy(AnimationClip source, string assetPath, bool loop)
        {
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(copy, assetPath);
            return copy;
        }

        private static CombatArenaConfigSO BuildDemoArenaConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.visualMode = CombatArenaVisualMode.ToonInk3D;
            config.cellWidth = 1.8f;
            config.cellDepth = 1.8f;
            // Reuse the v4 pacing: free-chase march + calibrated walk speed, untouched.
            config.useTopTroopsFreeChaseMovement = true;
            config.useTopTroopsProceduralBattlefield = false;
            config.showCheckerboardGrid = false;
            config.enableArenaFog = false; // scene RenderSettings own the fog
            config.useSyntyTerrain = false;
            config.spawnPerimeterProps = false;

            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>Copy a spike unit material to the generated folder (once) and apply the
        /// readability tune: lifted shadow band + softer rim ink for gameplay camera distance.</summary>
        private static Material LoadOrCreateTunedUnitMaterial(string sourcePath, string tunedPath)
        {
            var tuned = AssetDatabase.LoadAssetAtPath<Material>(tunedPath);
            if (tuned == null)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(sourcePath) == null)
                    return null;
                if (!AssetDatabase.CopyAsset(sourcePath, tunedPath))
                    return null;
                tuned = AssetDatabase.LoadAssetAtPath<Material>(tunedPath);
            }

            tuned.SetColor("_ShadowColor", new Color(0.45f, 0.44f, 0.50f));
            tuned.SetFloat("_ShadowThreshold", -0.15f);
            tuned.SetFloat("_InkStrength", 0.28f);
            tuned.SetFloat("_MidStrength", 0.45f);
            // Inverted-hull outline fragments into scribble on Meshy skinned normals; the
            // combat renderer's fullscreen depth/normal edge detect draws the clean
            // silhouette instead, so units run with the hull pass off.
            tuned.SetFloat("_OutlineWidth", 0f);
            EditorUtility.SetDirty(tuned);
            return tuned;
        }

        /// <summary>Generated DMZ/CombatRingFill material for one side. Colors reset on
        /// every build (same pattern as the tuned unit materials) so palette tweaks here
        /// propagate through a menu rebuild.</summary>
        private static Material LoadOrCreateRingFillMaterial(
            string path, Shader shader, Color fill, Color rim, Color empty)
        {
            if (shader == null)
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_FillColor", fill);
            material.SetColor("_RimColor", rim);
            material.SetColor("_EmptyColor", empty);
            material.SetFloat("_Fill", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // --- Scene construction (values lifted from Assets/_Phase0Spike/Scenes/Phase0_Spike.unity) ---

        internal static void ApplyEnvironmentLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // Lifted from the spike, then brightened so ToonInk's SampleSH term keeps
            // shadow-side unit detail readable at gameplay camera distance.
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.44f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.40f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.21f, 0.19f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.14f, 0.16f, 0.20f);
            RenderSettings.fogDensity = 0.022f; // spike used 0.028 on a smaller field; 0.022 sells backdrop depth without eating units (~20% fog at the far lane)
        }

        /// <param name="includeAudioListener">Demo scene: true (nothing else provides one).
        /// Run-flow arena scene: false — CombatArenaUiController.EnterArenaMode moves the
        /// Run scene's listener onto the arena camera; a serialized one would duplicate.</param>
        internal static Camera CreateCamera(bool includeAudioListener)
        {
            var go = new GameObject("ArenaCamera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            if (includeAudioListener)
                go.AddComponent<AudioListener>();
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Slightly above RenderSettings.fogColor: the fog line at the terrain crest is
            // key-lit ground mixed with fog, so a bg at raw fog color reads as a dark seam.
            // Solid grimdark sky per arena spec; the backdrop ring anchors the horizon.
            camera.backgroundColor = new Color(0.17f, 0.19f, 0.24f);
            // Trenchworks framing (candidates in docs/framing/, 2026-07-11): pulled back
            // from (0, 8.5, -12, pitch 30, fov 40) so the far parapets/craters/wire line
            // sit inside the frame; the shallower pitch lifts the backdrop instead of
            // adding dead foreground. Units stay ~80% of the old on-screen height —
            // still well inside the readable-ink range the previous close-up protected.
            go.transform.position = new Vector3(0f, 10f, -14f);
            go.transform.rotation = Quaternion.Euler(29f, 0f, 0f);

            // Combat-only rendering: the interior-ink + SSAO features live on
            // DeadManZone_CombatRenderer, not the shared forward renderer, so
            // non-combat scenes stay clean. Opt this camera into it by index.
            int rendererIndex = FindCombatRendererIndex();
            if (rendererIndex >= 0)
                camera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
            return camera;
        }

        /// <summary>Index of DeadManZone_CombatRenderer in the active URP asset's renderer
        /// list (serialized onto the camera, so the scene keeps working at runtime).</summary>
        private static int FindCombatRendererIndex()
        {
            var pipeline = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline)
                as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                Debug.LogWarning("[Combat3D] Active render pipeline is not URP — camera keeps the default renderer (no interior ink/SSAO).");
                return -1;
            }

            var rendererList = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            for (int i = 0; i < rendererList.arraySize; i++)
            {
                var data = rendererList.GetArrayElementAtIndex(i).objectReferenceValue;
                if (data != null && AssetDatabase.GetAssetPath(data) == CombatRendererPath)
                    return i;
            }

            Debug.LogWarning($"[Combat3D] {CombatRendererPath} is not in the renderer list of " +
                             $"{AssetDatabase.GetAssetPath(pipeline)} — camera keeps the default renderer (no interior ink/SSAO).");
            return -1;
        }

        internal static void CreateKeyLight()
        {
            var go = new GameObject("Key Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.80f); // warm key (spike value)
            light.intensity = 1.7f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            go.transform.rotation = Quaternion.Euler(40f, -52f, 0f); // spike value

            // Cool shadowless fill so the ToonInk shadow band never collapses to black.
            var fillGo = new GameObject("Fill Light");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.75f, 0.80f, 0.90f);
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(25f, 160f, 0f);
        }

        internal static void CreateGlobalVolume(VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile; // P0_Grade (null tolerated; warned above)
        }

        private static void CreateArenaRig(
            Camera camera,
            CombatArenaConfigSO config,
            AnimatorController controller,
            GameObject unitModel,
            Material playerMat,
            Material enemyMat,
            Material ringBlue,
            Material ringRed,
            GameObject riflePrefab,
            List<ArchetypeSpec> archetypes,
            CombatArenaAudioSetSO audioSet,
            AudioClip ambienceLoop)
        {
            var rig = new GameObject("Combat3DArena");

            var unitsRoot = new GameObject("UnitsRoot");
            unitsRoot.transform.SetParent(rig.transform, false);
            var buildingsRoot = new GameObject("BuildingsRoot");
            buildingsRoot.transform.SetParent(rig.transform, false);

            var bootstrap = rig.AddComponent<CombatArenaBootstrap>();
            SetSerialized(bootstrap, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
                so.FindProperty("unitsRoot").objectReferenceValue = unitsRoot.transform;
                so.FindProperty("buildingsRoot").objectReferenceValue = buildingsRoot.transform;
                so.FindProperty("config").objectReferenceValue = config;
            });

            var director = rig.AddComponent<CombatDirector>();

            // One-shot SFX presenter (CombatArenaPresenter finds it on the rig and drives
            // it from replayed muzzle/impact/death events). Demo-only placeholder set —
            // the presenter's Resources fallback is the shared 2D asset, left untouched.
            var arenaAudio = rig.AddComponent<CombatArenaAudioPresenter>();
            SetSerialized(arenaAudio, so =>
            {
                so.FindProperty("audioSet").objectReferenceValue = audioSet;
            });

            // Ambient bed: plain looping 2D source, kept well under the SFX (bible §3 —
            // the ambience is a floor, not a feature).
            if (ambienceLoop != null)
            {
                var ambience = new GameObject("AmbienceBed");
                ambience.transform.SetParent(rig.transform, false);
                var ambienceSource = ambience.AddComponent<AudioSource>();
                ambienceSource.clip = ambienceLoop;
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = true;
                ambienceSource.volume = 0.16f;
                ambienceSource.spatialBlend = 0f;
            }

            var presenter = rig.AddComponent<CombatArenaPresenter>();
            SetSerialized(presenter, so =>
            {
                so.FindProperty("combatDirector").objectReferenceValue = director;
            });

            var loader = rig.AddComponent<CombatArenaSceneLoader>();

            var installer = rig.AddComponent<CombatUnitVisual3DInstaller>();
            SetSerialized(installer, so =>
            {
                so.FindProperty("unitModel").objectReferenceValue = unitModel;
                so.FindProperty("animatorController").objectReferenceValue = controller;
                so.FindProperty("playerUnitMaterial").objectReferenceValue = playerMat;
                so.FindProperty("enemyUnitMaterial").objectReferenceValue = enemyMat;
                so.FindProperty("playerRingMaterial").objectReferenceValue = ringBlue;
                so.FindProperty("enemyRingMaterial").objectReferenceValue = ringRed;
                so.FindProperty("riflePrefab").objectReferenceValue = riflePrefab;

                WriteArchetypes(so, archetypes);
            });

            // Army health HUD: two opposing top-of-screen bars fed by the replay tracker.
            var armyHud = rig.AddComponent<CombatArmyHealthHud>();
            SetSerialized(armyHud, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
            });

            var driver = rig.AddComponent<Combat3DDemoDriver>();
            SetSerialized(driver, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("presenter").objectReferenceValue = presenter;
                so.FindProperty("arenaLoader").objectReferenceValue = loader;
                so.FindProperty("armyHud").objectReferenceValue = armyHud;
            });

            // Feel pass: punch-in camera beats + pooled muzzle-flash VFX (arena spec §1/§6).
            var punchIn = rig.AddComponent<CombatArenaPunchInCamera>();
            SetSerialized(punchIn, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("presenter").objectReferenceValue = presenter;
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });

            var vfx = rig.AddComponent<Combat3DVfxPresenter>();
            SetSerialized(vfx, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });
        }

        /// <summary>Serialize the installer's archetypes array (shared by the demo and the
        /// run-flow scene builders — keep the field writes in one place).</summary>
        internal static void WriteArchetypes(SerializedObject so, List<ArchetypeSpec> archetypes)
        {
            var archetypeArray = so.FindProperty("archetypes");
            archetypeArray.arraySize = archetypes.Count;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var element = archetypeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("pieceId").stringValue = archetypes[i].PieceId;
                element.FindPropertyRelative("model").objectReferenceValue = archetypes[i].Model;
                element.FindPropertyRelative("controller").objectReferenceValue = archetypes[i].Controller;
                element.FindPropertyRelative("isVehicle").boolValue = archetypes[i].IsVehicle;
                element.FindPropertyRelative("vehicleHeight").floatValue = archetypes[i].VehicleHeight;
                element.FindPropertyRelative("vehicleYawOffsetDegrees").floatValue =
                    archetypes[i].VehicleYawOffsetDegrees;
            }
        }

        internal static void SetSerialized(Component component, Action<SerializedObject> apply)
        {
            var serialized = new SerializedObject(component);
            apply(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
// EOF
     