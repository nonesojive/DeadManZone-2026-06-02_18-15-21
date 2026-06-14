#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Data.Editor
{
    public static class SyntyArtBatchRunner
    {
        [MenuItem("DeadManZone/Synty/Apply Full Synty Art Pass")]
        public static void ApplyFullSyntyArtPass()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                Debug.LogWarning(
                    "URP is not assigned. Run DeadManZone → Rendering → Setup URP For Project first.");
            }

            SyntyArenaPrefabGenerator.GenerateAll();
            SyntyArtCatalogFactory.CreateSyntySandboxArtCatalog();
            SandboxIconSnapshotter.SnapshotAllIconsFromPrefabs(forceResnapshot: true);
            SandboxArtAssigner.ApplySandboxArtPass();
            SandboxArtAssigner.ValidateSandboxArtCoverage();
            Debug.Log(
                "Full Synty art pass complete. Enter Play Mode, start a fight, and verify combat visuals. " +
                "Do not run any batch URP/Lit material conversion tools.");
        }
    }
}
#endif
