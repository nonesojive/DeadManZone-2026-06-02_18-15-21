using DeadManZone.Presentation.MainMenu;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Builds the cinematic MainMenu scene (camera, fog, SlimUI shell).
    /// Manual edits to MainMenu.unity are overwritten by Refresh Main Menu Scene.
    /// </summary>
    internal static class CinematicMenuSceneBuilder
    {
        private const string MainMenuCamControllerPath =
            "Assets/SlimUI/Modern Menu 1/Animations/Camera/MainMenuCam.controller";

        internal static void BuildAndSave(string scenePath)
        {
            var theme = UiThemeSceneStyling.LoadTheme();
            var menuTheme = MenuThemeEditor.EnsureMenuTheme();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureEventSystem();

            var menuEnvironment = CinematicMenuEnvironmentBuilder.Build();
            MainMenuBackdropBuilder.TryBuild();
            CreateVisualProfileApplier(menuEnvironment.transform);
            var menuCamera = CreateMenuCamera(out var animator);
            var ui = CinematicMenuUiBuilder.BuildAndWire(menuCamera, theme, menuTheme);
            MenuSceneSetup.CreateRunManager();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"DeadManZone: cinematic MainMenu saved to {scenePath}");
        }

        private static Camera CreateMenuCamera(out Animator animator)
        {
            var cameraGo = new GameObject("MenuCamera");
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.035f, 0.03f, 1f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;

            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<MainMenuCameraDirector>();
            cameraGo.transform.position = new Vector3(0f, 1.35f, -5.2f);
            cameraGo.transform.rotation = Quaternion.Euler(1.1f, 47.87f, 0f);

            animator = cameraGo.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MainMenuCamControllerPath);
            if (controller != null)
                animator.runtimeAnimatorController = controller;
            else
                Debug.LogWarning($"MainMenuCam controller not found at {MainMenuCamControllerPath}");

            return camera;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void CreateVisualProfileApplier(Transform menuEnvironmentRoot)
        {
            var go = new GameObject("VisualProfile");
            var applier = go.AddComponent<VisualProfileApplier>();
            VisualProfilePresetFactory.EnsureDefaultProfile();
            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath);
            var serialized = new SerializedObject(applier);
            serialized.FindProperty("profile").objectReferenceValue = runtimeProfile;
            serialized.FindProperty("sceneKind").enumValueIndex = (int)VisualProfileSceneKind.MainMenu;
            serialized.FindProperty("menuEnvironmentRoot").objectReferenceValue = menuEnvironmentRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
