using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    internal static class VisualStudioUiTab
    {
        private static readonly (string header, string[] propertyNames)[] ColorGroups =
        {
            ("Surfaces", new[] { "backgroundColor", "panelColor", "cardColor" }),
            ("Accents", new[] { "accentColor", "accentMutedColor", "dangerColor", "sellZoneColor" }),
            ("Text", new[] { "textPrimary", "textSecondary", "textOnAccent" }),
            ("Buttons", new[] { "buttonNormal", "buttonHighlighted", "buttonPressed" }),
            (
                "Board zones",
                new[]
                {
                    "rearZoneColor", "supportZoneColor", "frontZoneColor", "neutralZoneColor",
                    "specialTileColor", "tileHoverColor", "invalidPlacementColor"
                }),
            ("Shop lanes", new[] { "generalLaneTint", "engineersLaneTint", "requisitionLaneTint" }),
            ("Piece categories", new[] { "unitTint", "buildingTint", "hybridTint" }),
            ("Combat overlay", new[] { "combatOverlayColor", "combatBannerColor" })
        };

        public static void Draw(
            VisualProfileSO profile,
            bool autoApply,
            VisualStudioWindow.VisualStudioCallbacks callbacks)
        {
            EditorGUILayout.LabelField("UI & Board Theme", EditorStyles.boldLabel);

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select a visual profile on the Presets tab.", MessageType.Warning);
                return;
            }

            if (profile.uiTheme == null)
            {
                EditorGUILayout.HelpBox("Profile has no UiThemeSO. Create a default profile first.", MessageType.Warning);
                return;
            }

            var themeSerialized = new SerializedObject(profile.uiTheme);
            themeSerialized.Update();

            EditorGUI.BeginChangeCheck();

            foreach (var (header, propertyNames) in ColorGroups)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

                foreach (var propertyName in propertyNames)
                {
                    var property = themeSerialized.FindProperty(propertyName);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, true);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                themeSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile.uiTheme);
                EditorUtility.SetDirty(profile);

                if (autoApply)
                    VisualProfileEditorUtility.ApplyToOpenScene(profile);

                callbacks.Repaint?.Invoke();
            }
            else
            {
                themeSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Changes update board zone tiles, HUD panels, and canvas backgrounds when Auto-apply is enabled.",
                MessageType.None);
        }
    }
}
