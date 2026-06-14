#if UNITY_EDITOR && UNITY_URP_PRESENT
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Loads or recreates the checked-in URP pipeline + renderer assets.
    /// </summary>
    internal static class DeadManZoneUrpAssetFactory
    {
        internal const string SettingsFolder = "Assets/_Project/Settings/Rendering";
        internal const string PipelineAssetPath = SettingsFolder + "/DeadManZone_URP.asset";
        internal const string RendererAssetPath = SettingsFolder + "/DeadManZone_ForwardRenderer.asset";
        internal const string PipelineAssetGuid = "ac1dd16d394f72644af562cda94736d0";

        internal static bool TryResolvePipelineAsset(out RenderPipelineAsset pipeline)
        {
            pipeline = null;

            if (TryLoadExistingPipeline(out var urp))
            {
                pipeline = urp;
                return true;
            }

            if (TryRecreatePipelineAssets(out urp))
            {
                pipeline = urp;
                return true;
            }

            return false;
        }

        private static bool TryLoadExistingPipeline(out UniversalRenderPipelineAsset pipeline)
        {
            pipeline = null;

            foreach (var path in GetPipelineCandidatePaths())
            {
                ForceReimport(path);
                pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (pipeline != null)
                    return true;

                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub is UniversalRenderPipelineAsset typed)
                    {
                        pipeline = typed;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryRecreatePipelineAssets(out UniversalRenderPipelineAsset pipeline)
        {
            pipeline = null;
            EnsureFolder(SettingsFolder);

            var renderer = LoadOrCreateRenderer();
            if (renderer == null)
            {
                Debug.LogError("DeadManZone: Failed to create URP forward renderer data.");
                return false;
            }

            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = "DeadManZone_URP";
            ReplaceAssetFileKeepingMeta(PipelineAssetPath, pipeline);

            pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                Debug.LogError("DeadManZone: Recreated URP pipeline asset but AssetDatabase still returns null.");
                return false;
            }

            Debug.LogWarning(
                "DeadManZone: Recreated URP pipeline asset at " + PipelineAssetPath +
                ". Project Settings references were preserved via the existing .meta GUID.");
            return true;
        }

        private static UniversalRendererData LoadOrCreateRenderer()
        {
            ForceReimport(RendererAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer != null)
                return renderer;

            var templatePipeline = UniversalRenderPipelineAsset.Create();
            var templateRenderer = templatePipeline.rendererDataList[0] as UniversalRendererData;
            var created = templateRenderer != null
                ? Object.Instantiate(templateRenderer)
                : ScriptableObject.CreateInstance<UniversalRendererData>();
            created.name = "DeadManZone_ForwardRenderer";
            Object.DestroyImmediate(templatePipeline);

            ReplaceAssetFileKeepingMeta(RendererAssetPath, created);
            return AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
        }

        private static void ReplaceAssetFileKeepingMeta(string assetPath, Object asset)
        {
            var absolute = GetAbsolutePath(assetPath);
            if (!string.IsNullOrEmpty(absolute) && File.Exists(absolute))
            {
                File.Delete(absolute);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            ForceReimport(assetPath);
        }

        private static string[] GetPipelineCandidatePaths()
        {
            var guidPath = AssetDatabase.GUIDToAssetPath(PipelineAssetGuid);
            return string.IsNullOrEmpty(guidPath) || guidPath == PipelineAssetPath
                ? new[] { PipelineAssetPath }
                : new[] { guidPath, PipelineAssetPath };
        }

        private static void ForceReimport(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
        }

        private static string GetAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return string.IsNullOrEmpty(projectRoot)
                ? null
                : Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
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
