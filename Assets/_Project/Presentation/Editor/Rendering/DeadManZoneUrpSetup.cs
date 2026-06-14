#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Creates URP pipeline assets and assigns them project-wide.
    /// Run once after installing com.unity.render-pipelines.universal.
    /// </summary>
    public static class DeadManZoneUrpSetup
    {
        private const string SettingsFolder = "Assets/_Project/Settings/Rendering";
        private const string PipelineAssetPath = SettingsFolder + "/DeadManZone_URP.asset";
        private const string RendererAssetPath = SettingsFolder + "/DeadManZone_ForwardRenderer.asset";

        [MenuItem("DeadManZone/Rendering/Setup URP For Project")]
        public static void SetupUrpForProject()
        {
            EnsureFolder(SettingsFolder);

            var renderer = LoadOrCreateRenderer();
            var pipeline = LoadOrCreatePipeline(renderer);
            AssignPipelineEverywhere(pipeline);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DeadManZone URP Setup",
                "URP pipeline assets were created and assigned in Project Settings.\n\n" +
                "Next steps:\n" +
                "1. Synty → Package Helper → Install Packages (if prompted)\n" +
                "2. DeadManZone → Synty → Apply Full Synty Art Pass\n" +
                "3. DeadManZone → Rendering → Validate URP Setup\n\n" +
                "Do not batch-convert Synty materials to URP/Lit.",
                "OK");

            Debug.Log($"DeadManZone: URP assigned via {PipelineAssetPath}");
        }

        [MenuItem("DeadManZone/Rendering/Validate URP Setup")]
        public static void ValidateUrpSetup()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null)
            {
                Debug.LogError(
                    "URP is NOT active. GraphicsSettings.defaultRenderPipeline is null. " +
                    "Run DeadManZone → Rendering → Setup URP For Project.");
                return;
            }

            var config = Resources.Load<DeadManZone.Data.CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            var report = new System.Text.StringBuilder();
            report.AppendLine($"URP active: {pipeline.name}");

            if (config == null)
                report.AppendLine("WARN: CombatArenaConfig not found in Resources.");
            else
                report.AppendLine(
                    $"Combat config: useSyntyTerrain={config.useSyntyTerrain}, useSyntySkybox={config.useSyntySkybox}");

            if (!AssetDatabase.IsValidFolder("Assets/Synty"))
                report.AppendLine("WARN: Assets/Synty missing — re-import Synty packages.");
            else if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Synty/Arena"))
                report.AppendLine("WARN: Arena wrappers missing — run DeadManZone → Synty → Apply Full Synty Art Pass.");
            else
                report.AppendLine("Synty folders present. Native Synty shaders should render on URP without material conversion.");

            Debug.Log(report.ToString());
        }

        private static UniversalRendererData LoadOrCreateRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer != null)
                return renderer;

            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererAssetPath);
            return renderer;
        }

        private static UniversalRenderPipelineAsset LoadOrCreatePipeline(UniversalRendererData renderer)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            rendererList.ClearArray();
            rendererList.InsertArrayElementAtIndex(0);
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue = 0;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void AssignPipelineEverywhere(RenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;

            var qualityNames = QualitySettings.names;
            var previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(previousQuality, applyExpensiveChanges: true);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
