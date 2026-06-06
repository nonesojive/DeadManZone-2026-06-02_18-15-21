using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    internal static class VisualStudioAtmosphereTab
    {
        internal enum Mode
        {
            MainMenu,
            RunScene
        }

        public static void Draw(
            VisualProfileSO profile,
            Mode mode,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            var title = mode == Mode.MainMenu ? "Main Menu Atmosphere" : "Run Scene Atmosphere";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select a visual profile on the Presets tab.", MessageType.Warning);
                return;
            }

            if (mode == Mode.MainMenu)
                DrawMainMenu(profile, autoApply, callbacks);
            else
                DrawRunScene(profile, autoApply, callbacks);
        }

        private static void DrawMainMenu(
            VisualProfileSO profile,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            DrawAtmosphereBlock(profile.mainMenuAtmosphere, "Main Menu Atmosphere", profile, autoApply, callbacks);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Main Menu Lighting", EditorStyles.boldLabel);

            if (profile.mainMenuLighting == null)
            {
                EditorGUILayout.HelpBox("Profile has no MenuLightingSO assigned.", MessageType.Warning);
                return;
            }

            var lightingSerialized = new SerializedObject(profile.mainMenuLighting);
            lightingSerialized.Update();

            EditorGUI.BeginChangeCheck();
            DrawLightingList(lightingSerialized);

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Capture From Scene"))
                CaptureFromScene(profile);

            if (GUILayout.Button("Select Light"))
                SelectFirstLightInScene(profile.mainMenuLighting);

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                lightingSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile.mainMenuLighting);
                EditorUtility.SetDirty(profile);
                TryAutoApply(profile, autoApply);
                callbacks.Repaint?.Invoke();
            }
            else
            {
                lightingSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void DrawRunScene(
            VisualProfileSO profile,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            DrawAtmosphereBlock(profile.runAtmosphere, "Run Atmosphere", profile, autoApply, callbacks);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Refresh UI Theme in Scene"))
                VisualProfileEditorUtility.ApplyToOpenScene(profile);
        }

        private static void DrawAtmosphereBlock(
            SceneAtmosphereSO atmosphere,
            string label,
            VisualProfileSO profile,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            if (atmosphere == null)
            {
                EditorGUILayout.HelpBox($"{label} asset is not assigned.", MessageType.Warning);
                return;
            }

            var atmosphereSerialized = new SerializedObject(atmosphere);
            atmosphereSerialized.Update();

            EditorGUI.BeginChangeCheck();

            var iterator = atmosphereSerialized.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Capture Atmosphere From Render Settings"))
            {
                atmosphere.CopyFromCurrentRenderSettings();
                EditorUtility.SetDirty(atmosphere);
                EditorUtility.SetDirty(profile);
                TryAutoApply(profile, autoApply);
            }

            if (EditorGUI.EndChangeCheck())
            {
                atmosphereSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(atmosphere);
                EditorUtility.SetDirty(profile);
                TryAutoApply(profile, autoApply);
                callbacks.Repaint?.Invoke();
            }
            else
            {
                atmosphereSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void DrawLightingList(SerializedObject lightingSerialized)
        {
            var lightsProp = lightingSerialized.FindProperty("lights");
            if (lightsProp == null)
                return;

            for (var i = 0; i < lightsProp.arraySize; i++)
            {
                var element = lightsProp.GetArrayElementAtIndex(i);
                var nameProp = element.FindPropertyRelative("lightName");
                var foldoutLabel = nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
                    ? nameProp.stringValue
                    : $"Light {i}";

                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, foldoutLabel, true);
                if (!element.isExpanded)
                    continue;

                EditorGUI.indentLevel++;
                DrawLightEntry(element, foldoutLabel);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawLightEntry(SerializedProperty element, string lightName)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("lightType"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("color"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("intensity"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("range"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("localPosition"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("eulerRotation"));

            if (GUILayout.Button($"Select {lightName} in Scene"))
                SelectLightInScene(lightName);
        }

        private static void CaptureFromScene(VisualProfileSO profile)
        {
            var menuEnvironment = FindMenuEnvironment();
            if (menuEnvironment == null)
            {
                Debug.LogWarning("MenuEnvironment not found in the open scene.");
                return;
            }

            profile.mainMenuLighting?.CaptureFromEnvironment(menuEnvironment.transform);
            profile.mainMenuAtmosphere?.CopyFromCurrentRenderSettings();

            if (profile.mainMenuLighting != null)
                EditorUtility.SetDirty(profile.mainMenuLighting);
            if (profile.mainMenuAtmosphere != null)
                EditorUtility.SetDirty(profile.mainMenuAtmosphere);

            EditorUtility.SetDirty(profile);
            VisualProfileEditorUtility.ApplyToOpenScene(profile);
        }

        private static void SelectFirstLightInScene(MenuLightingSO lighting)
        {
            if (lighting?.lights == null || lighting.lights.Count == 0)
            {
                Debug.LogWarning("No lights defined in MenuLightingSO.");
                return;
            }

            SelectLightInScene(lighting.lights[0].lightName);
        }

        private static void SelectLightInScene(string lightName)
        {
            if (string.IsNullOrEmpty(lightName))
                return;

            var menuEnvironment = FindMenuEnvironment();
            if (menuEnvironment == null)
            {
                Debug.LogWarning("MenuEnvironment not found in the open scene.");
                return;
            }

            var lightTransform = menuEnvironment.transform.Find(lightName);
            if (lightTransform == null)
            {
                Debug.LogWarning($"Light '{lightName}' not found under MenuEnvironment.");
                return;
            }

            Selection.activeGameObject = lightTransform.gameObject;
            EditorGUIUtility.PingObject(lightTransform.gameObject);
        }

        private static GameObject FindMenuEnvironment() => GameObject.Find("MenuEnvironment");

        private static void TryAutoApply(VisualProfileSO profile, bool autoApply)
        {
            if (autoApply)
                VisualProfileEditorUtility.ApplyToOpenScene(profile);
        }
    }
}
