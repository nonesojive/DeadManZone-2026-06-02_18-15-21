using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class VisualProfileEditorUtility
    {
        public static VisualProfileSO GetActiveProfile()
        {
            var profile = Resources.Load<VisualProfileSO>(VisualProfileProvider.ResourcePath);
            return profile != null ? profile : VisualProfilePresetFactory.EnsureDefaultProfile();
        }

        public static void ApplyToOpenScene(VisualProfileSO profile)
        {
            UiThemeSceneRefresher.RefreshOpenScene(profile);

            if (profile != null)
                EditorUtility.SetDirty(profile);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        public static void SaveProfileAssets()
        {
            AssetDatabase.SaveAssets();
        }

        public static void SetActiveProfile(VisualProfileSO profile)
        {
            if (profile == null)
                return;

            var runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                VisualProfilePresetFactory.RuntimeProfilePath);
            if (runtimeProfile == null)
            {
                VisualProfilePresetFactory.EnsureDefaultProfile();
                runtimeProfile = AssetDatabase.LoadAssetAtPath<VisualProfileSO>(
                    VisualProfilePresetFactory.RuntimeProfilePath);
            }

            runtimeProfile.displayName = profile.displayName;
            runtimeProfile.uiTheme = profile.uiTheme;
            runtimeProfile.mainMenuAtmosphere = profile.mainMenuAtmosphere;
            runtimeProfile.mainMenuLighting = profile.mainMenuLighting;
            runtimeProfile.runAtmosphere = profile.runAtmosphere;
            runtimeProfile.postProcessProfile = profile.postProcessProfile;
            EditorUtility.SetDirty(runtimeProfile);
            VisualProfileProvider.InvalidateCache();
            UiThemeProvider.InvalidateCache();
        }

        public static void RevertUnsaved(VisualProfileSO profile)
        {
            if (profile == null)
                return;

            ReloadAsset(profile);
            ReloadAsset(profile.uiTheme);
            ReloadAsset(profile.mainMenuAtmosphere);
            ReloadAsset(profile.mainMenuLighting);
            ReloadAsset(profile.runAtmosphere);
            ApplyToOpenScene(GetActiveProfile());
        }

        private static void ReloadAsset(Object asset)
        {
            if (asset == null)
                return;

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
