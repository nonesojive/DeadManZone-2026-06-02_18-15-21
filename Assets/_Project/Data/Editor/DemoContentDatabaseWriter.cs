using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class DemoContentDatabaseWriter
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone";

        public static void Write(
            PieceDefinitionSO[] pieces,
            FactionSO[] factions,
            EnemyTemplateSO[] enemies)
        {
            var path = $"{Root}/ContentDatabase.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ContentDatabase>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ContentDatabase>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            WriteArray(so, "pieces", pieces);
            WriteArray(so, "factions", factions);
            WriteArray(so, "enemyTemplates", enemies);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteArray<T>(SerializedObject so, string propertyName, T[] items) where T : UnityEngine.Object
        {
            var prop = so.FindProperty(propertyName);
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
