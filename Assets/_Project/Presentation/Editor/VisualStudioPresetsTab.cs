using System.IO;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    internal static class VisualStudioPresetsTab
    {
        private static bool _shareUiThemeOnDuplicate;

        public static void Draw(
            VisualProfileSO profile,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            EditorGUILayout.LabelField("Visual Presets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var selected = (VisualProfileSO)EditorGUILayout.ObjectField(
                "Editing Profile",
                profile,
                typeof(VisualProfileSO),
                false);

            if (EditorGUI.EndChangeCheck() && selected != null)
                callbacks.OnProfileChanged?.Invoke(selected);

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Assign or create a VisualProfileSO to begin.", MessageType.Warning);
                if (GUILayout.Button("Create Default Profile"))
                {
                    var created = VisualProfilePresetFactory.EnsureDefaultProfile();
                    callbacks.OnProfileChanged?.Invoke(created);
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            var displayName = EditorGUILayout.TextField("Preset Name", profile.displayName);
            if (EditorGUI.EndChangeCheck())
            {
                profile.displayName = displayName;
                EditorUtility.SetDirty(profile);
            }

            EditorGUILayout.Space(4f);
            _shareUiThemeOnDuplicate = EditorGUILayout.ToggleLeft(
                "Share UI theme when duplicating (link instead of copy)",
                _shareUiThemeOnDuplicate);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Duplicate Preset"))
            {
                var duplicate = DuplicatePreset(profile, _shareUiThemeOnDuplicate);
                if (duplicate != null)
                    callbacks.OnProfileChanged?.Invoke(duplicate);
            }

            if (GUILayout.Button("Delete"))
            {
                if (DeletePreset(profile))
                    callbacks.OnProfileChanged?.Invoke(VisualProfileEditorUtility.GetActiveProfile());
            }

            if (GUILayout.Button("Apply Preset"))
            {
                VisualProfileEditorUtility.SetActiveProfile(profile);
                VisualProfileEditorUtility.ApplyToOpenScene(profile);
            }

            if (GUILayout.Button("Open in Project"))
            {
                EditorGUIUtility.PingObject(profile);
                Selection.activeObject = profile;
            }

            EditorGUILayout.EndHorizontal();

            DrawPresetList(profile, autoApply, callbacks);
            DrawStarterPresetsSection();
        }

        private static void DrawPresetList(
            VisualProfileSO current,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            if (!AssetDatabase.IsValidFolder(VisualProfilePresetFactory.PresetsFolder))
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Saved Presets", EditorStyles.boldLabel);

            var guids = AssetDatabase.FindAssets("t:VisualProfileSO", new[] { VisualProfilePresetFactory.PresetsFolder });
            if (guids.Length == 0)
            {
                EditorGUILayout.HelpBox("No presets in the Presets folder yet. Use Create Starter Presets below.", MessageType.None);
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(path);
                if (preset == null)
                    continue;

                var isActive = preset == current;
                EditorGUILayout.BeginHorizontal();

                var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(preset.displayName, style, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", GUILayout.Width(56f)))
                    callbacks.OnProfileChanged?.Invoke(preset);

                if (GUILayout.Button("Apply", GUILayout.Width(56f)))
                {
                    VisualProfileEditorUtility.SetActiveProfile(preset);
                    VisualProfileEditorUtility.ApplyToOpenScene(preset);
                    callbacks.OnProfileChanged?.Invoke(preset);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawStarterPresetsSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Factory", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Starter Presets"))
            {
                var source = VisualProfilePresetFactory.EnsureDefaultProfile();
                VisualProfilePresetFactory.CreateStarterPresets(source);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Starter presets ready under {VisualProfilePresetFactory.PresetsFolder}");
            }
        }

        private static VisualProfileSO DuplicatePreset(VisualProfileSO source, bool shareUiTheme)
        {
            if (source == null)
                return null;

            EnsureFolder(VisualProfilePresetFactory.PresetsFolder);

            var copyName = $"{source.displayName} Copy";
            var fileStem = SanitizeFileName(copyName);
            var profilePath = AssetDatabase.GenerateUniqueAssetPath(
                $"{VisualProfilePresetFactory.PresetsFolder}/{fileStem}VisualProfile.asset");

            var prefix = Path.GetFileNameWithoutExtension(profilePath).Replace("VisualProfile", string.Empty);
            var theme = shareUiTheme
                ? source.uiTheme
                : CopySubAsset(source.uiTheme, $"{VisualProfilePresetFactory.PresetsFolder}/{prefix}UiTheme.asset");
            var mainMenuAtmosphere = CopySubAsset(
                source.mainMenuAtmosphere,
                $"{VisualProfilePresetFactory.PresetsFolder}/{prefix}MainMenuAtmosphere.asset");
            var runAtmosphere = CopySubAsset(
                source.runAtmosphere,
                $"{VisualProfilePresetFactory.PresetsFolder}/{prefix}RunAtmosphere.asset");
            var mainMenuLighting = CopySubAsset(
                source.mainMenuLighting,
                $"{VisualProfilePresetFactory.PresetsFolder}/{prefix}MainMenuLighting.asset");

            var profile = ScriptableObject.CreateInstance<VisualProfileSO>();
            profile.displayName = copyName;
            profile.uiTheme = theme;
            profile.mainMenuAtmosphere = mainMenuAtmosphere;
            profile.mainMenuLighting = mainMenuLighting;
            profile.runAtmosphere = runAtmosphere;
            profile.postProcessProfile = source.postProcessProfile;

            AssetDatabase.CreateAsset(profile, profilePath);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log($"Duplicated preset to {profilePath}");
            return profile;
        }

        private static bool DeletePreset(VisualProfileSO profile)
        {
            if (profile == null)
                return false;

            var path = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(path)
                || path == VisualProfilePresetFactory.DefaultProfilePath
                || path == VisualProfilePresetFactory.RuntimeProfilePath)
            {
                Debug.LogWarning("Cannot delete the default or runtime visual profile.");
                return false;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete Preset",
                    $"Delete preset '{profile.displayName}' and its copied sub-assets?",
                    "Delete",
                    "Cancel"))
                return false;

            DeleteIfOwnedSubAsset(profile.uiTheme, path);
            DeleteIfOwnedSubAsset(profile.mainMenuAtmosphere, path);
            DeleteIfOwnedSubAsset(profile.runAtmosphere, path);
            DeleteIfOwnedSubAsset(profile.mainMenuLighting, path);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void DeleteIfOwnedSubAsset(Object asset, string profilePath)
        {
            if (asset == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)
                || !assetPath.StartsWith(VisualProfilePresetFactory.PresetsFolder))
                return;

            AssetDatabase.DeleteAsset(assetPath);
        }

        private static T CopySubAsset<T>(T source, string destinationPath) where T : Object
        {
            if (source == null)
                return null;

            var existing = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            if (existing != null)
                return existing;

            var copy = Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(destinationPath);
            AssetDatabase.CreateAsset(copy, destinationPath);
            return copy;
        }

        private static string SanitizeFileName(string name) =>
            name.Replace(" ", string.Empty);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
