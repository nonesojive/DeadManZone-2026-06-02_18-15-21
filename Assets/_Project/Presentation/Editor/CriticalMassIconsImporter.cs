using DeadManZone.Presentation.UI;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CriticalMassIconsImporter
    {
        private const string SourceFolder = "Assets/critmassicons";
        private const string AssetPath = "Assets/_Project/Data/Resources/DeadManZone/CriticalMassIcons.asset";

        [MenuItem("DeadManZone/Build Critical Mass Icons Asset")]
        public static void Build()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceFolder });
            var sprites = new System.Collections.Generic.List<Sprite>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            sprites.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            var asset = AssetDatabase.LoadAssetAtPath<CriticalMassIconsSO>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CriticalMassIconsSO>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var so = new SerializedObject(asset);
            var icons = so.FindProperty("icons");
            icons.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                icons.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"Critical Mass Icons asset updated with {sprites.Count} sprites.");
        }
    }
}
