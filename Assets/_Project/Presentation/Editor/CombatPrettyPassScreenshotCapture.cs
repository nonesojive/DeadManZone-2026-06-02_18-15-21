#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>Captures combat screenshots during Play mode for visual scorecard evidence.</summary>
    public static class CombatPrettyPassScreenshotCapture
    {
        private const string OutputFolder = "Assets/_Project/Art/QA/CombatPrettyPass";

        [MenuItem("DeadManZone/Combat Arena/Pretty Combat Pass — Capture Screenshot")]
        public static void CaptureScreenshot()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode and start combat before capturing a screenshot.");
                return;
            }

            EnsureFolder(OutputFolder);
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"combat_prettypass_{timestamp}.png";
            string fullPath = Path.Combine(OutputFolder, fileName);

            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"Combat screenshot saved to {fullPath}. Refresh Project window to view.");
        }

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
#endif
