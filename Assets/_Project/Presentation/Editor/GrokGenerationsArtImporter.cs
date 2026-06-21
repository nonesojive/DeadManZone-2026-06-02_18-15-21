using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Configures GrokGenerations PNGs as UI sprites (Sprite 2D, uncompressed, 100 PPU).
    /// </summary>
    public static class GrokGenerationsArtImporter
    {
        private const string Root = "Assets/_Project/Art/UI/GrokGenerations";

        [MenuItem("DeadManZone/Art/Import GrokGenerations Sprites")]
        public static void ImportGrokGenerationsSprites()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                Debug.LogError("GrokGenerations folder missing: " + Root);
                return;
            }

            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                ConfigureSprite(path);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"GrokGenerations: configured {count} PNG(s) as UI sprites under {Root}.");
        }

        private static void ConfigureSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        [MenuItem("DeadManZone/Art/Sync Grok Source Into GrokGenerations")]
        public static void SyncFromGrokSourceFolders()
        {
            CopyPngTree("Assets/grok tiles/groktileset5", Root + "/Tileset5");
            CopyPngTree("Assets/grok tiles/grokbuttons", Root + "/Buttons");
            AssetDatabase.Refresh();
            ImportGrokGenerationsSprites();
        }

        private static void CopyPngTree(string sourceRoot, string destRoot)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return;

            var absSource = Path.Combine(projectRoot, sourceRoot.Replace('/', Path.DirectorySeparatorChar));
            var absDest = Path.Combine(projectRoot, destRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absSource))
            {
                Debug.LogWarning("Grok source missing: " + sourceRoot);
                return;
            }

            EnsureAssetFolder(destRoot);
            var copied = 0;
            foreach (var file in Directory.EnumerateFiles(absSource, "*.png", SearchOption.AllDirectories))
            {
                var rel = file.Substring(absSource.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var target = Path.Combine(absDest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? absDest);
                File.Copy(file, target, overwrite: true);
                copied++;
            }

            Debug.Log($"Synced {copied} PNG(s) from {sourceRoot} -> {destRoot}.");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureAssetFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
