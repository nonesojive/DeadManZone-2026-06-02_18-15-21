using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Shared crop/trim helpers for Grok isometric sheet imports.</summary>
    public static class GrokImageCropUtility
    {
        public static Texture2D LoadTexture(string assetPath)
        {
            var absolute = Path.GetFullPath(assetPath);
            if (!File.Exists(absolute))
                return null;

            var bytes = File.ReadAllBytes(absolute);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.DestroyImmediate(texture);
                return null;
            }

            return texture;
        }

        public static Rect HorizontalSlice(int width, int height, int index, int count)
        {
            var sliceWidth = width / (float)count;
            var leftInset = index == 0 ? 0.06f : 0.12f;
            var rightInset = index >= count - 1 ? 0.06f : 0.20f;
            var innerWidth = Mathf.Max(sliceWidth * (1f - leftInset - rightInset), sliceWidth * 0.45f);
            var innerX = index * sliceWidth + sliceWidth * leftInset;
            return new Rect(innerX, 0f, innerWidth, height);
        }

        public static Rect GridSlice(int width, int height, int columns, int rows, int index)
        {
            var col = index % columns;
            var row = index / columns;
            var cellW = width / (float)columns;
            var cellH = height / (float)rows;
            var insetX = 0.08f;
            var insetY = 0.08f;
            return new Rect(
                col * cellW + cellW * insetX,
                row * cellH + cellH * insetY,
                cellW * (1f - insetX * 2f),
                cellH * (1f - insetY * 2f));
        }

        public static Texture2D ExtractSlice(Texture2D source, Rect slice, bool removeBackground = true)
        {
            var x = Mathf.Clamp(Mathf.FloorToInt(slice.x), 0, source.width - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(slice.y), 0, source.height - 1);
            var w = Mathf.Clamp(Mathf.FloorToInt(slice.width), 1, source.width - x);
            var h = Mathf.Clamp(Mathf.FloorToInt(slice.height), 1, source.height - y);
            var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cropped.SetPixels(source.GetPixels(x, y, w, h));
            cropped.Apply();

            if (removeBackground)
                RemoveGrayBackground(cropped);

            var bounds = FindOpaqueBounds(cropped);
            var trimmed = Crop(cropped, bounds);
            Object.DestroyImmediate(cropped);
            return trimmed;
        }

        public static Texture2D FitToSquare(Texture2D source, int size, float fill)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color[size * size];
            for (var i = 0; i < clear.Length; i++)
                clear[i] = Color.clear;
            output.SetPixels(clear);

            var target = size * fill;
            var scale = target / Mathf.Max(source.width, source.height);
            var drawW = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var drawH = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var offsetX = (size - drawW) / 2;
            var offsetY = (size - drawH) / 2;

            var resized = ScaleTexture(source, drawW, drawH);
            output.SetPixels(offsetX, offsetY, drawW, drawH, resized.GetPixels());
            output.Apply();
            Object.DestroyImmediate(resized);
            return output;
        }

        public static Texture2D CropRegion(Texture2D source, RectInt bounds)
        {
            bounds.x = Mathf.Clamp(bounds.x, 0, source.width - 1);
            bounds.y = Mathf.Clamp(bounds.y, 0, source.height - 1);
            bounds.width = Mathf.Clamp(bounds.width, 1, source.width - bounds.x);
            bounds.height = Mathf.Clamp(bounds.height, 1, source.height - bounds.y);
            return Crop(source, bounds);
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

        public static void RemoveGrayBackground(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                if (IsBackground(pixels[i]))
                    pixels[i] = Color.clear;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private static bool IsBackground(Color color)
        {
            var gray = (color.r + color.g + color.b) / 3f;
            var spread = Mathf.Max(
                Mathf.Abs(color.r - color.g),
                Mathf.Max(Mathf.Abs(color.g - color.b), Mathf.Abs(color.r - color.b)));
            return color.a > 0.9f && spread < 0.06f && gray > 0.22f && gray < 0.58f;
        }

        public static RectInt FindOpaqueBounds(Texture2D texture)
        {
            var w = texture.width;
            var h = texture.height;
            var pixels = texture.GetPixels();
            var minX = w;
            var minY = h;
            var maxX = 0;
            var maxY = 0;

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
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

        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}
