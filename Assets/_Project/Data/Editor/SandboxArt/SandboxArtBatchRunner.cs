#if UNITY_EDITOR
using UnityEditor;

namespace DeadManZone.Data.Editor
{
    /// <summary>Runs the full sandbox art pipeline for batchmode / CI.</summary>
    public static class SandboxArtBatchRunner
    {
        [MenuItem(DeadManZoneEditorMenus.Art + "Run Full Sandbox Art Pipeline")]
        public static void RunFromMenu() => RunFullPipeline();

        public static void RunFullPipeline()
        {
            SandboxArtDefaultCatalogFactory.CreateDefaultSandboxArtCatalog();
            GrokBatch2IconImporter.ImportBatch2Icons();
            SandboxIconSnapshotter.SnapshotMissingIconsFromPrefabs();

            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog != null)
                SandboxArtAssigner.EnsureMissingIconFiles(catalog);

            SandboxArtAssigner.ApplySandboxArtPass();
            SandboxArtAssigner.ValidateSandboxArtCoverage();
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
