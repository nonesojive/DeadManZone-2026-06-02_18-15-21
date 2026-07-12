#if UNITY_EDITOR
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat.Arena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/CombatArena3D.unity — the 3D arena the RUN FLOW loads
    /// additively (GameScenes.ResolveCombatArenaScene picks it when the shared
    /// CombatArenaConfig is in ToonInk3D mode). Same environment/lighting/rig assets as the
    /// Combat3D demo scene (Combat3DDemoSceneBootstrap.PrepareSceneAssets), but the rig
    /// carries ONLY scene-side components: bootstrap, unit-visual installer, VFX presenter,
    /// punch-in camera, audio presenter + ambience. Director / arena presenter / scene
    /// loader / tactics window live on the Run scene's CombatFlowPresenter object —
    /// cross-scene refs can't serialize, so CombatFlowPresenter wires the punch-in camera
    /// at load time and the presenter resolves scene-side audio/VFX via the bootstrap.
    /// No demo driver: the real run drives combat.
    /// </summary>
    public static class CombatArena3DSceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/CombatArena3D.unity";
        private const string SharedConfigPath =
            "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";

        [MenuItem("DeadManZone/Combat3D/Build CombatArena3D Scene (Run Flow)")]
        public static void BuildArenaScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Combat3D] Exit Play mode before building the arena scene.");
                return;
            }

            var assets = Combat3DDemoSceneBootstrap.PrepareSceneAssets();
            if (assets == null)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Combat3D] Scene build cancelled (unsaved scene).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Combat3DDemoSceneBootstrap.ApplyEnvironmentLighting();
            // No AudioListener: EnterArenaMode moves the Run scene's listener here at load.
            var camera = Combat3DDemoSceneBootstrap.CreateCamera(includeAudioListener: false);
            Combat3DDemoSceneBootstrap.CreateKeyLight();
            Combat3DDemoSceneBootstrap.CreateGlobalVolume(assets.GradeProfile);
            CombatEnvironmentBuilder.Build();
            CreateArenaRig(camera, assets);

            Combat3DDemoSceneBootstrap.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Combat3D] Run-flow arena scene saved to {ScenePath} and added to Build Settings.");
        }

        /// <summary>Scene-side rig only — see class summary for what stays in the Run scene.</summary>
        private static void CreateArenaRig(Camera camera, Combat3DDemoSceneBootstrap.SceneAssets assets)
        {
            var rig = new GameObject("CombatArena3D");

            var unitsRoot = new GameObject("UnitsRoot");
            unitsRoot.transform.SetParent(rig.transform, false);
            var buildingsRoot = new GameObject("BuildingsRoot");
            buildingsRoot.transform.SetParent(rig.transform, false);

            var bootstrap = rig.AddComponent<CombatArenaBootstrap>();
            Combat3DDemoSceneBootstrap.SetSerialized(bootstrap, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
                so.FindProperty("unitsRoot").objectReferenceValue = unitsRoot.transform;
                so.FindProperty("buildingsRoot").objectReferenceValue = buildingsRoot.transform;
                so.FindProperty("config").objectReferenceValue = assets.Config;
            });

            var installer = rig.AddComponent<CombatUnitVisual3DInstaller>();
            Combat3DDemoSceneBootstrap.SetSerialized(installer, so =>
            {
                so.FindProperty("unitModel").objectReferenceValue = assets.IdleModel;
                so.FindProperty("animatorController").objectReferenceValue = assets.Controller;
                so.FindProperty("playerUnitMaterial").objectReferenceValue = assets.PlayerMat;
                so.FindProperty("enemyUnitMaterial").objectReferenceValue = assets.EnemyMat;
                so.FindProperty("playerRingMaterial").objectReferenceValue = assets.RingBlue;
                so.FindProperty("enemyRingMaterial").objectReferenceValue = assets.RingRed;
                so.FindProperty("riflePrefab").objectReferenceValue = assets.RiflePrefab;
                Combat3DDemoSceneBootstrap.WriteArchetypes(so, assets.Archetypes);
            });

            // One-shot SFX presenter: CombatArenaPresenter prefers this bootstrap-side one
            // over the Run scene's default (which has no 3D audio set).
            var arenaAudio = rig.AddComponent<CombatArenaAudioPresenter>();
            Combat3DDemoSceneBootstrap.SetSerialized(arenaAudio, so =>
            {
                so.FindProperty("audioSet").objectReferenceValue = assets.AudioSet;
            });

            if (assets.AmbienceLoop != null)
            {
                var ambience = new GameObject("AmbienceBed");
                ambience.transform.SetParent(rig.transform, false);
                var ambienceSource = ambience.AddComponent<AudioSource>();
                ambienceSource.clip = assets.AmbienceLoop;
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = true;
                ambienceSource.volume = 0.16f;
                ambienceSource.spatialBlend = 0f;
            }

            // Director/presenter are runtime-wired by CombatFlowPresenter after the load.
            var punchIn = rig.AddComponent<CombatArenaPunchInCamera>();
            Combat3DDemoSceneBootstrap.SetSerialized(punchIn, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });

            var vfx = rig.AddComponent<Combat3DVfxPresenter>();
            Combat3DDemoSceneBootstrap.SetSerialized(vfx, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });
        }

        // ---------------------------------------------------------------- mode repair

        /// <summary>ToonInk3D is the only renderer; this just repairs a config asset that
        /// still carries an obsolete mode int and makes sure the scene is in Build Settings.</summary>
        [MenuItem("DeadManZone/Combat3D/Switch Combat To 3D Arena")]
        public static void SwitchCombatTo3D()
        {
            var config = LoadSharedConfig();
            if (config == null)
                return;

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Combat3D] {ScenePath} missing — run 'Build CombatArena3D Scene (Run Flow)' first.");
                return;
            }

            config.visualMode = CombatArenaVisualMode.ToonInk3D;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EnsureSceneInBuildSettings();
            Debug.Log("[Combat3D] Shared CombatArenaConfig set to ToonInk3D — run combat loads " +
                      $"{GameScenes.CombatArena3D}.");
        }

        private static CombatArenaConfigSO LoadSharedConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(SharedConfigPath);
            if (config == null)
                Debug.LogError($"[Combat3D] Shared config missing at {SharedConfigPath}.");
            return config;
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var entry in scenes)
            {
                if (entry.path == ScenePath)
                    return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Combat3D] CombatArena3D added to Build Settings.");
        }
    }
}
#endif
