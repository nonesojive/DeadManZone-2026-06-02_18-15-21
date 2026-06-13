using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class SandboxArtSpriteImporter
    {
        internal const int DefaultIconSize = 256;

        internal static void ConfigureSpriteImporter(string assetPath, int maxSize = DefaultIconSize)
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
            importer.maxTextureSize = maxSize;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }

        internal static void WritePlaceholderIcon(string assetPath, string pieceId, int paletteIndex)
        {
            var absolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);

            var baseColor = new Color(0.45f, 0.48f, 0.42f, 1f);
            var accent = Color.HSVToRGB((paletteIndex * 0.17f) % 1f, 0.25f, 0.55f);
            var border = new Color(0.12f, 0.13f, 0.11f, 1f);

            var texture = new Texture2D(DefaultIconSize, DefaultIconSize, TextureFormat.RGBA32, false);
            var pixels = new Color[DefaultIconSize * DefaultIconSize];
            for (var y = 0; y < DefaultIconSize; y++)
            {
                for (var x = 0; x < DefaultIconSize; x++)
                {
                    var edge = x < 8 || y < 8 || x >= DefaultIconSize - 8 || y >= DefaultIconSize - 8;
                    var stripe = (x + y) % 32 < 4;
                    pixels[y * DefaultIconSize + x] = edge ? border : stripe ? accent : baseColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath);
            ConfigureSpriteImporter(assetPath);
        }
    }
}
