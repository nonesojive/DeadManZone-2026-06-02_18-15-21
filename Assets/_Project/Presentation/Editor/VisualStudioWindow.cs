using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public sealed class VisualStudioWindow : EditorWindow
    {
        internal const string AutoApplyPrefKey = "DMZ_VisualStudio_AutoApply";
        internal const string RunScenePath = MenuSceneSetup.ScenesFolder + "/Run.unity";

        private VisualProfileSO _profile;
        private VisualStudioTab _activeTab = VisualStudioTab.Presets;
        private Vector2 _scrollPosition;

        internal enum VisualStudioTab
        {
            Presets,
            UiBoard,
            MainMenu,
            RunScene,
            Preview
        }

        internal sealed class VisualStudioCallbacks
        {
            public System.Action<VisualProfileSO> OnProfileChanged;
            public System.Action Repaint;
        }

        [MenuItem("DeadManZone/Visual Studio")]
        public static void Open()
        {
            var window = GetWindow<VisualStudioWindow>("Visual Studio");
            window.minSize = new Vector2(380f, 440f);
            window.Show();
        }

        private void OnEnable()
        {
            _profile = VisualProfileEditorUtility.GetActiveProfile();
        }

        private void OnGUI()
        {
            if (_profile == null)
                _profile = VisualProfileEditorUtility.GetActiveProfile();

            DrawHelpBox();
            DrawToolbar();

            _activeTab = (VisualStudioTab)GUILayout.Toolbar((int)_activeTab, GetTabLabels());
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var callbacks = new VisualStudioCallbacks
            {
                OnProfileChanged = profile =>
                {
                    _profile = profile;
                    Repaint();
                },
                Repaint = Repaint
            };

            var autoApply = EditorPrefs.GetBool(AutoApplyPrefKey, true);

            switch (_activeTab)
            {
                case VisualStudioTab.Presets:
                    VisualStudioPresetsTab.Draw(_profile, autoApply, callbacks);
                    break;
                case VisualStudioTab.UiBoard:
                    VisualStudioUiTab.Draw(_profile, autoApply, callbacks);
                    break;
                case VisualStudioTab.MainMenu:
                    VisualStudioAtmosphereTab.Draw(_profile, VisualStudioAtmosphereTab.Mode.MainMenu, autoApply, callbacks);
                    break;
                case VisualStudioTab.RunScene:
                    VisualStudioAtmosphereTab.Draw(_profile, VisualStudioAtmosphereTab.Mode.RunScene, autoApply, callbacks);
                    break;
                case VisualStudioTab.Preview:
                    DrawPreviewTab(autoApply);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private static string[] GetTabLabels() =>
            new[] { "Presets", "UI & Board", "Main Menu", "Run Scene", "Preview" };

        private static void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "DeadManZone Visual Studio edits visual presets (UI palette, board zones, menu atmosphere, and lighting) "
                + "with live Edit Mode preview. Enable Auto-apply on the Preview tab to push changes into the open scene immediately.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Apply to Scene", EditorStyles.toolbarButton))
                VisualProfileEditorUtility.ApplyToOpenScene(_profile);

            if (GUILayout.Button("Save Assets", EditorStyles.toolbarButton))
                VisualProfileEditorUtility.SaveProfileAssets();

            if (GUILayout.Button("Revert Unsaved", EditorStyles.toolbarButton))
                VisualProfileEditorUtility.RevertUnsaved(_profile);

            if (GUILayout.Button("Sync SlimUI", EditorStyles.toolbarButton))
                MenuThemeEditor.EnsureMenuTheme(_profile?.uiTheme);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewTab(bool autoApply)
        {
            EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);

            var scene = EditorSceneManager.GetActiveScene();
            var sceneLabel = scene.IsValid() ? scene.name : "(none)";
            EditorGUILayout.LabelField("Open Scene", sceneLabel);

            EditorGUI.BeginChangeCheck();
            var newAutoApply = EditorGUILayout.ToggleLeft("Auto-apply changes to open scene", autoApply);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(AutoApplyPrefKey, newAutoApply);

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open MainMenu"))
                OpenScene(MenuSceneSetup.MainMenuPath);

            if (GUILayout.Button("Open Run"))
                OpenScene(RunScenePath);
            EditorGUILayout.EndHorizontal();

            if (_profile != null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Active Profile", _profile.displayName);
            }
        }

        private static void OpenScene(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"Scene not found at {path}. Run DeadManZone/Setup Main Menu & Run Scenes first.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }
    }
}
