using System.IO;
using System.Linq;
using DeadManZone.Data.UnitCreation;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class UnitPersistenceService
    {
        private const string PieceRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";
        private const string DatabasePath = "Assets/_Project/Data/Resources/DeadManZone/ContentDatabase.asset";

        public static bool IdAssetExists(string id) =>
            !string.IsNullOrWhiteSpace(id) && File.Exists(GetPieceAssetPath(id));

        public static bool IsRegisteredInDatabase(string id, ContentDatabase database)
        {
            if (database == null || string.IsNullOrWhiteSpace(id))
                return false;

            return database.Pieces.Any(p => p != null && p.id == id);
        }

        public static bool TrySave(UnitCreationDraft draft, out string errorMessage)
        {
            errorMessage = null;
            var validation = UnitCreationValidator.Validate(
                draft,
                idExistsInProject: draft.Mode == UnitCreatorMode.Create && IdAssetExists(draft.id),
                idRegisteredInDatabase: false);

            if (validation.HasErrors)
            {
                errorMessage = string.Join("\n", validation.Messages
                    .Where(m => m.Severity == ValidationSeverity.Error)
                    .Select(m => m.Message));
                return false;
            }

            EnsureFolder(PieceRoot);
            var path = GetPieceAssetPath(draft.id);
            var piece = draft.Mode == UnitCreatorMode.Edit
                ? AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path)
                : null;

            if (piece == null)
            {
                piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
                AssetDatabase.CreateAsset(piece, path);
            }

            draft.ApplyTo(piece, writeRegistration: true);
            EditorUtility.SetDirty(piece);

            if (draft.addToContentDatabase)
                RegisterInDatabase(piece);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(piece);
            Debug.Log($"Unit '{draft.id}' saved to {path}.", piece);
            return true;
        }

        public static bool TryDelete(string pieceId, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                errorMessage = "Piece id is required.";
                return false;
            }

            var path = GetPieceAssetPath(pieceId);
            var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
            if (piece == null)
            {
                errorMessage = $"No piece asset found at {path}.";
                return false;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete Unit",
                    $"Delete '{pieceId}' from the project and ContentDatabase?",
                    "Delete",
                    "Cancel"))
            {
                errorMessage = "Cancelled.";
                return false;
            }

            RemoveFromDatabase(piece);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Deleted unit '{pieceId}'.");
            return true;
        }

        public static string GetPieceAssetPath(string id) => $"{PieceRoot}/{id}.asset";

        private static void RegisterInDatabase(PieceDefinitionSO piece)
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"ContentDatabase not found at {DatabasePath}.");
                return;
            }

            var so = new SerializedObject(database);
            var piecesProp = so.FindProperty("pieces");
            int existingIndex = -1;
            for (int i = 0; i < piecesProp.arraySize; i++)
            {
                var entry = piecesProp.GetArrayElementAtIndex(i).objectReferenceValue as PieceDefinitionSO;
                if (entry != null && entry.id == piece.id)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                piecesProp.GetArrayElementAtIndex(existingIndex).objectReferenceValue = piece;
            }
            else
            {
                piecesProp.InsertArrayElementAtIndex(piecesProp.arraySize);
                piecesProp.GetArrayElementAtIndex(piecesProp.arraySize - 1).objectReferenceValue = piece;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void RemoveFromDatabase(PieceDefinitionSO piece)
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            if (database == null)
                return;

            var so = new SerializedObject(database);
            var piecesProp = so.FindProperty("pieces");
            for (int i = piecesProp.arraySize - 1; i >= 0; i--)
            {
                var entry = piecesProp.GetArrayElementAtIndex(i).objectReferenceValue as PieceDefinitionSO;
                if (entry == piece || (entry != null && entry.id == piece.id))
                    piecesProp.DeleteArrayElementAtIndex(i);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
