#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Assigns the checked-in URP pipeline asset in Project Settings.
    /// </summary>
    public static class DeadManZoneUrpSetup
    {
        internal const string SettingsFolder = "Assets/_Project/Settings/Rendering";
        internal const string PipelineAssetPath = SettingsFolder + "/DeadManZone_URP.asset";
        internal const string PipelineAssetGuid = "ac1dd16d394f72644af562cda94736d0";

        /// <summary>
        /// Assigns the project URP asset when Graphics/Quality settings have no pipeline.
        /// Returns true when a pipeline is active after the call.
        /// </summary>
        public static bool TryAssignPipelineIfMissing()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
                return true;

            if (!TryResolvePipelineAsset(out var pipeline))
                return false;

            AssignPipelineEverywhere(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log($"DeadManZone: Auto-assigned URP via {PipelineAssetPath}");
            return true;
        }

        [MenuItem("DeadManZone/Rendering/Setup URP For Project")]
        public static void SetupUrpForProject()
        {
            EnsureFolder(SettingsFolder);

            if (!TryResolvePipelineAsset(out var pipeline))
            {
                EditorUtility.DisplayDialog(
                    "DeadManZone URP Setup",
                    BuildMissingPipelineMessage(),
                    "OK");
                return;
            }

            AssignPipelineEverywhere(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DeadManZone URP Setup",
                "URP pipeline asset assigned in Project Settings.\n\n" +
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
            var report = new StringBuilder();
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

        private static bool TryResolvePipelineAsset(out RenderPipelineAsset pipeline)
        {
#if UNITY_URP_PRESENT
            return DeadManZoneUrpAssetFactory.TryResolvePipelineAsset(out pipeline);
#else
            pipeline = null;
            return TryResolvePipelineAssetWithoutUrpReference(out pipeline);
#endif
        }

#if !UNITY_URP_PRESENT
        private static bool TryResolvePipelineAssetWithoutUrpReference(out RenderPipelineAsset pipeline)
        {
            pipeline = null;

            foreach (var path in GetCandidatePipelinePaths())
            {
                if (TryLoadPipelineAtPath(path, out pipeline))
                    return true;
            }

            var urpType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null)
            {
                var reflected = AssetDatabase.LoadAssetAtPath(PipelineAssetPath, urpType);
                if (TryCastPipeline(reflected, out pipeline))
                    return true;
            }

            return false;
        }
#endif

        private static string[] GetCandidatePipelinePaths()
        {
            var guidPath = AssetDatabase.GUIDToAssetPath(PipelineAssetGuid);
            return string.IsNullOrEmpty(guidPath) || guidPath == PipelineAssetPath
                ? new[] { PipelineAssetPath }
                : new[] { guidPath, PipelineAssetPath };
        }

        private static bool TryLoadPipelineAtPath(string path, out RenderPipelineAsset pipeline)
        {
            pipeline = null;
            if (string.IsNullOrEmpty(path))
                return false;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (TryCastPipeline(AssetDatabase.LoadMainAssetAtPath(path), out pipeline))
                return true;

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (TryCastPipeline(sub, out pipeline))
                    return true;
            }

            return false;
        }

        private static bool TryCastPipeline(UnityEngine.Object asset, out RenderPipelineAsset pipeline)
        {
            pipeline = asset as RenderPipelineAsset;
            return pipeline != null;
        }

        private static string BuildMissingPipelineMessage()
        {
            var message = new StringBuilder();
            message.AppendLine("Could not load render pipeline asset:");
            message.AppendLine(PipelineAssetPath);
            message.AppendLine();
            message.AppendLine($"On disk: {File.Exists(GetAbsolutePipelinePath())}");
            message.AppendLine($"GUID path: {AssetDatabase.GUIDToAssetPath(PipelineAssetGuid)}");

#if UNITY_URP_PRESENT
            var typed = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            message.AppendLine(typed != null
                ? $"Loaded type: {typed.GetType().FullName}"
                : "Loaded type: <null> (asset will be recreated on next successful URP import)");
#else
            var raw = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(PipelineAssetGuid))
                      ?? AssetDatabase.LoadMainAssetAtPath(PipelineAssetPath);
            message.AppendLine(raw != null
                ? $"Loaded type: {raw.GetType().FullName}"
                : "Loaded type: <null>");
#endif

            message.AppendLine();
            message.AppendLine("If the file exists, reimport it or reinstall URP via Package Manager.");
            message.AppendLine("Otherwise create Assets → Create → Rendering → URP Asset (with Forward Renderer),");
            message.AppendLine("save it to the path above, then run this menu again.");
            return message.ToString();
        }

        private static string GetAbsolutePipelinePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return string.IsNullOrEmpty(projectRoot)
                ? PipelineAssetPath
                : Path.Combine(projectRoot, PipelineAssetPath.Replace('/', Path.DirectorySeparatorChar));
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
