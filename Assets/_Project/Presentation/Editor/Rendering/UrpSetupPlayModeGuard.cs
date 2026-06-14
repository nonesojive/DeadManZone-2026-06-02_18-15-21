#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Editor
{
    [InitializeOnLoad]
    internal static class UrpSetupPlayModeGuard
    {
        static UrpSetupPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (GraphicsSettings.defaultRenderPipeline != null)
                return;

            Debug.LogWarning(
                "DeadManZone: URP package is installed but no Render Pipeline Asset is assigned. " +
                "Combat Synty materials will look broken until you run " +
                "DeadManZone → Rendering → Setup URP For Project.");
        }
    }
}
#endif
