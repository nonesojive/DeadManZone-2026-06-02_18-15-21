using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Crops grid cells or pixel rects from themed sprite sheets into PNG sprites.</summary>
    public static class SpriteSheetCropUtility
    {
        public static Texture2D LoadReadableTexture(string assetPath)
        {
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (source == null)
                return null;

            var renderTarget = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            renderTarget.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, renderTarget);

            var previous = RenderTexture.active;
            RenderTexture.active = renderTarget;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            readable.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            return readable;
        }

        public static Texture2D CropGridCell(
            Texture2D sheet,
            int col,
            int row,
            int gridCols,
            int gridRows)
        {
            int cellW = sheet.width / gridCols;
            int cellH = sheet.height / gridRows;
            int x = col * cellW;
            int y = sheet.height - (row + 1) * cellH;
            return CropPixels(sheet, x, y, cellW, cellH);
        }

        public static Texture2D CropPixels(Texture2D sheet, int x, int y, int width, int height)
        {
            var cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(sheet.GetPixels(x, y, width, height));
            cropped.Apply();
            return cropped;
        }

        public static Texture2D BuildShopIcon(Texture2D cell, int outputSize = 256, float frameFill = 0.78f)
        {
            var bounds = FindOpaqueBounds(cell);
            var trimmed = Crop(cell, bounds);
            Object.DestroyImmediate(cell);
            var fitted = FitToSquare(trimmed, outputSize, frameFill);
            Object.DestroyImmediate(trimmed);
            return fitted;
        }

        public static void WritePng(string assetPath, Texture2D texture)
        {
            var absolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
        }

        public static void ConfigureSpriteImporter(string assetPath, int maxSize, int pixelsPerUnit = 100)
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
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.SaveAndReimport();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static RectInt FindOpaqueBounds(Texture2D texture)
        {
            int w = texture.width;
            int h = texture.height;
            var pixels = texture.GetPixels();
            int minX = w;
            int minY = h;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a < 0.1f)
                        continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX <= minX || maxY <= minY)
                return new RectInt(0, 0, w, h);

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Texture2D Crop(Texture2D source, RectInt bounds)
        {
            var cropped = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            cropped.SetPixels(source.GetPixels(bounds.x, bounds.y, bounds.width, bounds.height));
            cropped.Apply();
            return cropped;
        }

        private static Texture2D FitToSquare(Texture2D source, int size, float fill)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color[size * size];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = new Color(0f, 0f, 0f, 0f);
            output.SetPixels(clear);

            float target = size * fill;
            float scale = target / Mathf.Max(source.width, source.height);
            int drawW = Mathf.RoundToInt(source.width * scale);
            int drawH = Mathf.RoundToInt(source.height * scale);
            int offsetX = (size - drawW) / 2;
            int offsetY = (size - drawH) / 2;

            var resized = ScaleTexture(source, drawW, drawH);
            output.SetPixels(offsetX, offsetY, drawW, drawH, resized.GetPixels());
            output.Apply();
            Object.DestroyImmediate(resized);
            return output;
        }

        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}
