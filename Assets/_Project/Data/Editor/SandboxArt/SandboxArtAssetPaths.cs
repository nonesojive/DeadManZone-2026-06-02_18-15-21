#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class SandboxArtAssetPaths
    {
        internal static bool FileExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            return File.Exists(ToAbsolute(assetPath));
        }

        internal static string ToAbsolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
#endif
