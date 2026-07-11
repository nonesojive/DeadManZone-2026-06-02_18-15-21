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
    /// Builds Assets/_Project/Scenes/Combat3D_Demo.unity: spike-look environment (warm key /
    /// cool ambient, P0_Grade volume, graybox trench dressing), a perspective camera, and the
    /// combat arena rig (bootstrap/director/presenter/pool) configured for ToonInk3D visuals
    /// with the Phase-0 rifleman model, a freshly generated AnimatorController, and side rings.
    /// The spike scene/controller stay throwaway â€” everything generated lands under _Project.
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
        private const string GroundMaterialPath = GeneratedFolder + "/Combat3D_Ground.mat";
        private const string SandbagMaterialPath = GeneratedFolder + "/Combat3D_Sandbag.mat";
        private const string CombatRendererPath = "Assets/_Project/Settings/Rendering/DeadManZone_CombatRenderer.asset";

        // Roster archetypes: Meshy 12k units copied under _Project (idle/walk/die GLBs sharing
        // one rig per unit), mapped to the ContentDatabase piece id the sim uses. No
        // grenade_thrower piece exists in content â€” that model stands in for the mortar team.
        // A unit with broken/missing GLBs is skipped with a warning â€” actors with its piece id
        // fall back to the default rifleman visuals instead of blocking the scene build.
        private static readonly (string folder, string pieceId)[] RosterUnits =
        {
            ("bulwark_squad", "bulwark_squad"),
            ("field_medic", "field_medic"),
            ("grenade_thrower", "ironclad_mortars"),
        };

        private static string RosterModelFolder(string unitFolder) =>
            GeneratedFolder + "/Models/" + unitFolder;

        // Proven Phase-0 spike assets (referenced, never modified).
        private const string IdleGlbPath = "Assets/_Phase0Spike/Models/enlisted_rifleman_12k.glb";
        private const string WalkGlbPath = "Assets/_Phase0Spike/Models/enlisted_rifleman_12k_walk.glb";
        private const string DieGlbPath = "Assets/_Phase0Spike/Models/enlisted_rifleman_12k_die.glb";
        private const string PlayerMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Player.mat";
        private const string EnemyMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Enemy.mat";
        private const string FallbackUnitMaterialPath = "Assets/_Phase0Spike/Materials/ToonInk_Meshy.mat";
        private const string RingBluePath = "Assets/_Phase0Spike/Materials/RingBlue.mat";
        private const string RingRedPath = "Assets/_Phase0Spike/Materials/RingRed.mat";
        private const string GradeProfilePath = "Assets/_Phase0Spike/Materials/P0_Grade.asset";

        [MenuItem("DeadManZone/Combat3D/Build Combat3D Demo Scene")]
        public static void BuildDemoScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Combat3D] Exit Play mode before building the demo scene.");
                return;
            }

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
            var ringBlue = LoadOrFlag<Material>(RingBluePath, missing);
            var ringRed = LoadOrFlag<Material>(RingRedPath, missing);
            var gradeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradeProfilePath);
            if (gradeProfile == null)
                Debug.LogWarning($"[Combat3D] Grade volume profile missing at {GradeProfilePath} â€” scene will render ungraded.");

            var idleClip = FindClip(IdleGlbPath, missing, "idle");
            var walkClip = FindClip(WalkGlbPath, missing, "walk");
            var dieClip = FindClip(DieGlbPath, missing, "dead", "die");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    "[Combat3D] Aborting â€” required Phase-0 spike assets are missing:\n - " +
                    string.Join("\n - ", missing));
                return;
            }

            // --- Generated assets under _Project (spike stays throwaway). ---
            EnsureFolder(GeneratedFolder);
            var controller = BuildAnimatorController(
                idleClip, walkClip, dieClip, IdleClipPath, WalkClipPath, DieClipPath, ControllerPath);
            var archetypes = BuildRosterArchetypes();
            var config = BuildDemoArenaConfig();
            var groundMat = EnsureUnlitLitMaterial(GroundMaterialPath, new Color(0.30f, 0.28f, 0.25f));
            var sandbagMat = EnsureUnlitLitMaterial(SandbagMaterialPath, new Color(0.44f, 0.39f, 0.29f));

            // --- Scene ---
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Combat3D] Scene build cancelled (unsaved scene).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironmentLighting();
            var camera = CreateCamera();
            CreateKeyLight();
            CreateGlobalVolume(gradeProfile);
            CreateGraybox(groundMat, sandbagMat);
            CreateArenaRig(camera, config, controller, idleModel, playerMat, enemyMat, ringBlue, ringRed, archetypes);

            EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Combat3D] Demo scene saved to {ScenePath}. Open it and press Play â€” " +
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
        /// living next to the unit's GLBs under Combat3D/Models/&lt;unit&gt;/.</summary>
        private static List<(string pieceId, GameObject model, AnimatorController controller)>
            BuildRosterArchetypes()
        {
            var archetypes = new List<(string, GameObject, AnimatorController)>();
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
                archetypes.Add((pieceId, model, controller));
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

        private static Material EnsureUnlitLitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
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
            EditorUtility.SetDirty(tuned);
            return tuned;
        }

        // --- Scene construction (values lifted from Assets/_Phase0Spike/Scenes/Phase0_Spike.unity) ---

        private static void ApplyEnvironmentLighting()
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
            RenderSettings.fogDensity = 0.018f; // spike used 0.028 on a smaller field
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("ArenaCamera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.15f); // spike value
            // Close enough that units read as inked figures (the fullscreen interior-ink
            // edge-detect saturates tiny characters into solid silhouettes when pulled back).
            go.transform.position = new Vector3(0f, 8.5f, -12.0f);
            go.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

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
                Debug.LogWarning("[Combat3D] Active render pipeline is not URP â€” camera keeps the default renderer (no interior ink/SSAO).");
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
                             $"{AssetDatabase.GetAssetPath(pipeline)} â€” camera keeps the default renderer (no interior ink/SSAO).");
            return -1;
        }

        private static void CreateKeyLight()
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

        private static void CreateGlobalVolume(VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile; // P0_Grade (null tolerated; warned above)
        }

        private static void CreateGraybox(Material groundMat, Material sandbagMat)
        {
            var root = new GameObject("Environment");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Graybox_Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(4.2f, 1f, 2.4f); // 42m x 24m, board is ~30.6 x 10.8
            if (groundMat != null)
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Simple sandbag-wall boxes echoing the spike trench dressing. Kept off the
            // marching lanes (units fight along the field's middle rows).
            (Vector3 pos, float yaw, Vector3 scale)[] walls =
            {
                (new Vector3(-2.1f, 0.25f, -3.9f), 8f, new Vector3(2.2f, 0.5f, 0.6f)),
                (new Vector3(2.3f, 0.25f, -3.7f), -6f, new Vector3(2.2f, 0.5f, 0.6f)),
                (new Vector3(-1.8f, 0.25f, 4.1f), -10f, new Vector3(2.4f, 0.5f, 0.6f)),
                (new Vector3(2.0f, 0.25f, 4.2f), 5f, new Vector3(2.0f, 0.5f, 0.6f)),
                (new Vector3(-7.4f, 0.25f, 3.9f), 24f, new Vector3(1.8f, 0.5f, 0.55f)),
                (new Vector3(7.6f, 0.25f, -4.0f), -21f, new Vector3(1.8f, 0.5f, 0.55f)),
            };

            foreach (var (pos, yaw, scale) in walls)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Sandbag_Wall";
                wall.transform.SetParent(root.transform, false);
                wall.transform.localPosition = pos;
                wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                wall.transform.localScale = scale;
                if (sandbagMat != null)
                    wall.GetComponent<MeshRenderer>().sharedMaterial = sandbagMat;
            }
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
            List<(string pieceId, GameObject model, AnimatorController controller)> archetypes)
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
            rig.AddComponent<CombatArenaAudioPresenter>();

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

                var archetypeArray = so.FindProperty("archetypes");
                archetypeArray.arraySize = archetypes.Count;
                for (int i = 0; i < archetypes.Count; i++)
                {
                    var element = archetypeArray.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("pieceId").stringValue = archetypes[i].pieceId;
                    element.FindPropertyRelative("model").objectReferenceValue = archetypes[i].model;
                    element.FindPropertyRelative("controller").objectReferenceValue = archetypes[i].controller;
                }
            });

            var driver = rig.AddComponent<Combat3DDemoDriver>();
            SetSerialized(driver, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("presenter").objectReferenceValue = presenter;
                so.FindProperty("arenaLoader").objectReferenceValue = loader;
            });

            // Feel pass: punch-in camera beats + pooled muzzle-flash VFX (arena spec Â§1/Â§6).
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

        private static void SetSerialized(Component component, Action<SerializedObject> apply)
        {
            var serialized = new SerializedObject(component);
            apply(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
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
