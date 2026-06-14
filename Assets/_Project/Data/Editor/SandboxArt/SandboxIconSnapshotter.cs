#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SandboxIconSnapshotter
    {
        private const int IconSize = 256;
        private const float CameraElevation = 35f;
        private const float CameraAzimuth = 225f;

        [MenuItem("DeadManZone/Art/Snapshot Missing Icons From Prefabs")]
        public static void SnapshotMissingIconsFromPrefabs() => SnapshotAllIconsFromPrefabs(forceResnapshot: false);

        public static void SnapshotAllIconsFromPrefabs(bool forceResnapshot)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("SandboxArtCatalog missing. Run Create Synty Sandbox Art Catalog first.");
                return;
            }

            EnsureFolder(SandboxArtPaths.SandboxIconsFolder);

            var created = 0;
            var failed = 0;
            foreach (var entry in catalog.entries)
            {
                if (!entry.snapshotIconFromPrefab)
                    continue;

                if (string.IsNullOrEmpty(entry.iconAssetPath) || string.IsNullOrEmpty(entry.combatArenaPrefabPath))
                    continue;

                if (!forceResnapshot && SandboxArtAssetPaths.FileExists(entry.iconAssetPath))
                    continue;

                if (TrySnapshotPrefab(entry.combatArenaPrefabPath, entry.iconAssetPath))
                    created++;
                else
                    failed++;
            }

            AssetDatabase.Refresh();
            Debug.Log(failed == 0
                ? $"Snapshot icons created: {created}."
                : $"Snapshot icons created: {created}, failed: {failed}. Check Console for warnings.");
        }

        private static bool TrySnapshotPrefab(string prefabPath, string outputPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Snapshot skipped — prefab missing: {prefabPath}");
                return false;
            }

            var absolute = SandboxArtAssetPaths.ToAbsolute(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);

            var instance = Object.Instantiate(prefab);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            try
            {
                var bounds = CalculateBounds(instance);
                var texture = RenderBounds(bounds, instance, prefabPath);
                if (texture == null)
                {
                    Debug.LogWarning($"Snapshot render failed: {prefabPath}");
                    return false;
                }

                File.WriteAllBytes(absolute, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(outputPath);
                SandboxArtSpriteImporter.ConfigureSpriteImporter(outputPath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Texture2D RenderBounds(Bounds bounds, GameObject subject, string prefabPath)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                preview.camera.orthographic = true;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                preview.camera.transform.rotation = Quaternion.Euler(CameraElevation, CameraAzimuth, 0f);

                if (preview.lights != null && preview.lights.Length > 0)
                {
                    preview.lights[0].intensity = 1.35f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                }

                preview.ambientColor = new Color(0.25f, 0.25f, 0.28f, 1f);
                preview.AddSingleGO(subject);

                var center = bounds.center;
                var extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                extent = Mathf.Max(extent, 0.5f);

                preview.camera.transform.position = center - preview.camera.transform.forward * (extent * 3f);
                preview.camera.orthographicSize = extent * 1.15f;
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = extent * 20f;

                preview.BeginPreview(new Rect(0f, 0f, IconSize, IconSize), GUIStyle.none);
                preview.camera.Render();
                var rendered = preview.EndPreview() as Texture2D;
                if (rendered == null)
                {
                    Debug.LogWarning($"EndPreview returned null for {prefabPath}");
                    return null;
                }

                return CopyToIconSize(rendered);
            }
            finally
            {
                preview.Cleanup();
            }
        }

        private static Texture2D CopyToIconSize(Texture2D source)
        {
            if (source.width == IconSize && source.height == IconSize)
            {
                var copy = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                copy.SetPixels(source.GetPixels());
                copy.Apply();
                return copy;
            }

            var rt = RenderTexture.GetTemporary(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var result = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
