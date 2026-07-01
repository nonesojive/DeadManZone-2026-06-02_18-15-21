using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class UiThemeEditor
    {
        public const string ThemeAssetPath = "Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset";

        [MenuItem(DeadManZoneEditorMenus.Ui + "Create Default UI Theme")]
        public static void CreateDefaultUiTheme()
        {
            EnsureThemeAsset();
            AssetDatabase.SaveAssets();
            Debug.Log($"UI theme ready at {ThemeAssetPath}");
        }

        public static UiThemeSO EnsureThemeAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UiThemeSO>(ThemeAssetPath);
            if (existing != null)
                return existing;

            EnsureFolder("Assets/_Project/Data/Resources/DeadManZone");
            var theme = ScriptableObject.CreateInstance<UiThemeSO>();
            theme.ApplyIronVanguardDefaults();
            AssetDatabase.CreateAsset(theme, ThemeAssetPath);
            return theme;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
