using System;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    internal static class MenuThemeEditor
    {
        internal const string MenuThemeAssetPath = "Assets/_Project/Data/Menu/DeadManZoneMenuTheme.asset";

        internal static ScriptableObject EnsureMenuTheme(UiThemeSO uiTheme = null)
        {
            uiTheme ??= UiThemeEditor.EnsureThemeAsset();
            var themedDataType = FindThemedUiDataType();
            if (themedDataType == null)
            {
                Debug.LogWarning("SlimUI ThemedUIData type not found. SlimUI button theming will use package defaults.");
                return null;
            }

            EnsureFolder("Assets/_Project/Data/Menu");
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MenuThemeAssetPath);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance(themedDataType);
                AssetDatabase.CreateAsset(existing, MenuThemeAssetPath);
            }

            ApplyUiThemeColors(existing, uiTheme);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void ApplyUiThemeColors(ScriptableObject themedData, UiThemeSO uiTheme)
        {
            var serialized = new SerializedObject(themedData);
            SetColor(serialized, "custom1.graphic1", uiTheme.accentColor);
            SetColor32(serialized, "custom1.text1", uiTheme.textPrimary);
            SetColor(serialized, "custom2.graphic2", uiTheme.accentMutedColor);
            SetColor32(serialized, "custom2.text2", uiTheme.textSecondary);
            SetColor(serialized, "custom3.graphic3", uiTheme.buttonHighlighted);
            SetColor32(serialized, "custom3.text3", uiTheme.textOnAccent);
            SetColor(serialized, "currentColor", uiTheme.accentColor);
            SetColor32(serialized, "textColor", uiTheme.textPrimary);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(SerializedObject obj, string propertyPath, Color color)
        {
            var property = obj.FindProperty(propertyPath);
            if (property != null)
                property.colorValue = color;
        }

        private static void SetColor32(SerializedObject obj, string propertyPath, Color color)
        {
            var property = obj.FindProperty(propertyPath);
            if (property != null)
                property.colorValue = color;
        }

        private static Type FindThemedUiDataType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("SlimUI.ModernMenu.ThemedUIData");
                if (type != null)
                    return type;
            }

            return null;
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
