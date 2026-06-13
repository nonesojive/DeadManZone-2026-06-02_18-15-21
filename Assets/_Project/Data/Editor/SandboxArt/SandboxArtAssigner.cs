#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SandboxArtAssigner
    {
        private const string PiecesRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

        [MenuItem("DeadManZone/Art/Apply Sandbox Art Pass")]
        public static void ApplySandboxArtPass()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("SandboxArtCatalog missing. Run Create Default Sandbox Art Catalog first.");
                return;
            }

            EnsureMissingIconFiles(catalog);

            int applied = 0;
            foreach (var entry in catalog.entries)
            {
                if (ApplyEntry(entry))
                    applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Sandbox art pass applied to {applied}/{catalog.entries.Length} pieces.");
        }

        [MenuItem("DeadManZone/Art/Validate Sandbox Art Coverage")]
        public static void ValidateSandboxArtCoverage()
        {
            if (AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath) == null)
            {
                Debug.LogError("SandboxArtCatalog missing.");
                return;
            }

            int issues = 0;
            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = LoadPiece(pieceId);
                if (piece == null)
                {
                    Debug.LogWarning($"Missing piece asset: {pieceId}");
                    issues++;
                    continue;
                }

                if (piece.icon == null)
                {
                    Debug.LogWarning($"{pieceId}: icon not assigned");
                    issues++;
                }

                if (SandboxArtRoster.RequiresCombatArenaPrefab(piece.category)
                    && piece.combatArenaPrefab == null)
                {
                    Debug.LogWarning($"{pieceId}: combatArenaPrefab required for {piece.category}");
                    issues++;
                }
            }

            Debug.Log(issues == 0
                ? "Sandbox art coverage: OK (25/25)"
                : $"Sandbox art coverage: {issues} issue(s) — run Apply Sandbox Art Pass");
        }

        internal static void EnsureMissingIconFiles(SandboxArtCatalogSO catalog)
        {
            for (var i = 0; i < catalog.entries.Length; i++)
            {
                var entry = catalog.entries[i];
                if (string.IsNullOrEmpty(entry.iconAssetPath))
                    continue;

                if (SandboxArtAssetPaths.FileExists(entry.iconAssetPath))
                    continue;

                if (entry.snapshotIconFromPrefab)
                    continue;

                SandboxArtSpriteImporter.WritePlaceholderIcon(entry.iconAssetPath, entry.pieceId, i);
            }
        }

        private static bool ApplyEntry(SandboxArtEntry entry)
        {
            var piece = LoadPiece(entry.pieceId);
            if (piece == null)
                return false;

            if (!string.IsNullOrEmpty(entry.iconAssetPath) && SandboxArtAssetPaths.FileExists(entry.iconAssetPath))
            {
                SandboxArtSpriteImporter.ConfigureSpriteImporter(entry.iconAssetPath);
                piece.icon = AssetDatabase.LoadAssetAtPath<Sprite>(entry.iconAssetPath);
            }

            if (!string.IsNullOrEmpty(entry.combatArenaPrefabPath))
            {
                piece.combatArenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.combatArenaPrefabPath);
                piece.combatArenaModelScale = entry.combatArenaModelScale > 0f ? entry.combatArenaModelScale : 1f;
                piece.combatArenaModelHeight = entry.combatArenaModelHeight;
            }

            EditorUtility.SetDirty(piece);
            return piece.icon != null;
        }

        private static PieceDefinitionSO LoadPiece(string pieceId) =>
            AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PiecesRoot}/{pieceId}.asset");
    }
}
#endif
