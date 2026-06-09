using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Crops SuperGrok Batch 2 roster/vehicle sheets into neutral shop icons.
    /// Source: Assets/Grok Images/Isometric Batch 2/
    /// </summary>
    public static class GrokBatch2IconImporter
    {
        private const string Batch2Folder = "Assets/Grok Images/Isometric Batch 2";
        private const string RosterFile = "grok-image-2eb75a93-e52d-4847-ae43-03394588e5fd.jpg";
        private const string VehicleFile = "grok-image-129be410-7172-41f3-950b-9e1a668f383c.jpg";
        private const int OutputSize = 256;
        private const float FrameFill = 0.78f;

        private static readonly string[] RosterPieceIds =
        {
            "conscript_rifleman",
            "grenade_thrower",
            "field_medic",
            "armored_transport",
            "mobile_cannon"
        };

        [MenuItem("DeadManZone/Art/Import Grok Batch 2 Icons")]
        public static void ImportBatch2Icons()
        {
            NeutralArtPipelineEditor.CreateFolders();

            var rosterPath = $"{Batch2Folder}/{RosterFile}";
            var vehiclePath = $"{Batch2Folder}/{VehicleFile}";

            if (!File.Exists(rosterPath))
            {
                Debug.LogError($"Missing roster image: {rosterPath}");
                return;
            }

            var roster = LoadTexture(rosterPath);
            if (roster == null)
                return;

            Texture2D vehicle = File.Exists(vehiclePath) ? LoadTexture(vehiclePath) : null;
            var imported = 0;

            for (var i = 0; i < RosterPieceIds.Length; i++)
            {
                var pieceId = RosterPieceIds[i];
                Texture2D source;
                Rect slice;
                var disposeSource = false;

                if (pieceId == "armored_transport" && vehicle != null)
                {
                    source = vehicle;
                    slice = HorizontalSlice(source.width, source.height, 0, 2);
                }
                else if (pieceId == "mobile_cannon" && vehicle != null)
                {
                    source = vehicle;
                    slice = HorizontalSlice(source.width, source.height, 1, 2);
                }
                else
                {
                    source = roster;
                    slice = HorizontalSlice(roster.width, roster.height, i, RosterPieceIds.Length);
                }

                var icon = BuildIcon(source, slice);
                WritePng(PieceArtPaths.IconAssetPath(pieceId), icon);
                Object.DestroyImmediate(icon);
                imported++;
            }

            Object.DestroyImmediate(roster);
            if (vehicle != null)
                Object.DestroyImmediate(vehicle);

            NeutralArtPipelineEditor.AssignIconsFromRenders();
            NeutralArtPipelineEditor.AssignCellSpritesFromRenders();
            AssetDatabase.Refresh();
            Debug.Log($"Imported {imported} Grok Batch 2 icons into {PieceArtPaths.NeutralIcons}.");
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            var bytes = File.ReadAllBytes(assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.DestroyImmediate(texture);
                Debug.LogError($"Failed to load image: {assetPath}");
                return null;
            }

            return texture;
        }

        private static Rect HorizontalSlice(int width, int height, int index, int count)
        {
            var sliceWidth = width / (float)count;
            var leftInset = index == 0 ? 0.06f : 0.12f;
            var rightInset = index >= count - 1 ? 0.06f : 0.20f;
            var innerWidth = Mathf.Max(sliceWidth * (1f - leftInset - rightInset), sliceWidth * 0.45f);
            var innerX = index * sliceWidth + sliceWidth * leftInset;
            return new Rect(innerX, 0f, innerWidth, height);
        }

        private static Texture2D BuildIcon(Texture2D source, Rect slice)
        {
            var x = Mathf.FloorToInt(slice.x);
            var y = Mathf.FloorToInt(slice.y);
            var w = Mathf.FloorToInt(slice.width);
            var h = Mathf.FloorToInt(slice.height);
            var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);

            cropped.SetPixels(source.GetPixels(x, y, w, h));
            cropped.Apply();
            RemoveGrayBackground(cropped);

            var bounds = FindOpaqueBounds(cropped);
            var trimmed = Crop(cropped, bounds);
            Object.DestroyImmediate(cropped);

            var fitted = FitToSquare(trimmed, OutputSize, FrameFill);
            Object.DestroyImmediate(trimmed);
            return fitted;
        }

        private static void RemoveGrayBackground(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                if (IsBackground(pixels[i]))
                    pixels[i] = new Color(0f, 0f, 0f, 0f);
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

        private static RectInt FindOpaqueBounds(Texture2D texture)
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

        private static Texture2D FitToSquare(Texture2D source, int size, float fill)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color[size * size];
            for (var i = 0; i < clear.Length; i++)
                clear[i] = new Color(0f, 0f, 0f, 0f);
            output.SetPixels(clear);

            var target = size * fill;
            var scale = target / Mathf.Max(source.width, source.height);
            var drawW = Mathf.RoundToInt(source.width * scale);
            var drawH = Mathf.RoundToInt(source.height * scale);
            var offsetX = (size - drawW) / 2;
            var offsetY = (size - drawH) / 2;

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
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private static void WritePng(string assetPath, Texture2D texture)
        {
            var absolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}
