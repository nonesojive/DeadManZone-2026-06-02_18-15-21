using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadManZone.Presentation.UI
{
    /// <summary>Loads card prefabs from project paths during editor play mode.</summary>
    internal static class CardPrefabRuntimeLoader
    {
        public static GameObject LoadPrefab(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            return Resources.Load<GameObject>(ToResourcesPath(assetPath));
#endif
        }

        private static string ToResourcesPath(string assetPath)
        {
            const string resourcesRoot = "Assets/Resources/";
            if (!assetPath.StartsWith(resourcesRoot))
                return null;

            var relative = assetPath.Substring(resourcesRoot.Length);
            var extension = relative.LastIndexOf('.');
            return extension > 0 ? relative.Substring(0, extension) : relative;
        }
    }
}
