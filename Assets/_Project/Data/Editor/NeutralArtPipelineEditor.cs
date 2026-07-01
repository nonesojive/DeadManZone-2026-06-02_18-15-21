using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class NeutralArtPipelineEditor
    {
        private const string PiecesRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";
        private const int IconSize = 256;

        public static void CreateFolders()
        {
            EnsureFolder(PieceArtPaths.ArtRoot);
            EnsureFolder(PieceArtPaths.NeutralSource);
            EnsureFolder(PieceArtPaths.NeutralStyleSheet);
            EnsureFolder(PieceArtPaths.NeutralIcons);
            EnsureFolder(PieceArtPaths.NeutralCells);
            EnsureFolder(PieceArtPaths.SharedRoot);
            AssetDatabase.Refresh();
            Debug.Log("Neutral art folders are ready under Assets/_Project/Art/.");
        }

        internal static void GeneratePlaceholderIcons()
        {
            CreateFolders();
            var assigned = 0;

            for (var i = 0; i < PieceArtPaths.NeutralPieceIds.Length; i++)
            {
                var pieceId = PieceArtPaths.NeutralPieceIds[i];
                var path = PieceArtPaths.IconAssetPath(pieceId);
                WritePlaceholderIcon(path, pieceId, i);
                ConfigureSpriteImporter(path, IconSize);
                assigned += AssignIcon(pieceId, path) ? 1 : 0;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {PieceArtPaths.NeutralPieceIds.Length} placeholder icons; assigned {assigned} on piece assets.");
        }

        public static void AssignIconsFromRenders()
        {
            var assigned = 0;
            foreach (var pieceId in PieceArtPaths.NeutralPieceIds)
            {
                var path = PieceArtPaths.IconAssetPath(pieceId);
                if (!File.Exists(path))
                    continue;

                ConfigureSpriteImporter(path, IconSize);
                if (AssignIcon(pieceId, path))
                    assigned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Assigned {assigned} neutral shop icons from {PieceArtPaths.NeutralIcons}.");
        }

        public static void AssignCellSpritesFromRenders()
        {
            var assignedPieces = 0;
            foreach (var pieceId in PieceArtPaths.NeutralPieceIds)
            {
                var piece = LoadPiece(pieceId);
                if (piece == null)
                    continue;

                var entries = new System.Collections.Generic.List<PieceCellSprite>();
                foreach (var cell in piece.shapeCells)
                {
                    var path = PieceArtPaths.CellAssetPath(pieceId, $"{cell.x}_{cell.y}");
                    if (!File.Exists(path))
                        continue;

                    ConfigureSpriteImporter(path, 128);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                        continue;

                    entries.Add(new PieceCellSprite
                    {
                        localCell = cell,
                        sprite = sprite
                    });
                }

                if (entries.Count == 0)
                    continue;

                piece.cellSprites = entries.ToArray();
                EditorUtility.SetDirty(piece);
                assignedPieces++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Assigned cell sprites on {assignedPieces} neutral pieces from {PieceArtPaths.NeutralCells}.");
        }

        internal static void ValidateNeutralArt()
        {
            var report = string.Empty;
            foreach (var pieceId in PieceArtPaths.NeutralPieceIds)
            {
                var piece = LoadPiece(pieceId);
                var iconPath = PieceArtPaths.IconAssetPath(pieceId);
                var hasIcon = piece?.icon != null;
                var hasIconFile = File.Exists(iconPath);
                var cellCount = 0;
                var expectedCells = piece?.shapeCells?.Length ?? 0;

                if (piece?.cellSprites != null)
                {
                    foreach (var entry in piece.cellSprites)
                    {
                        if (entry.sprite != null)
                            cellCount++;
                    }
                }

                report += $"- {pieceId}: icon={(hasIcon ? "assigned" : "missing")}, file={(hasIconFile ? "yes" : "no")}, cells={cellCount}/{expectedCells}\n";
            }

            Debug.Log($"Neutral art validation:\n{report}");
        }

        private static void WritePlaceholderIcon(string assetPath, string pieceId, int paletteIndex)
        {
            var absolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);

            var baseColor = new Color(0.45f, 0.48f, 0.42f, 1f);
            var accent = Color.HSVToRGB((paletteIndex * 0.17f) % 1f, 0.25f, 0.55f);
            var border = new Color(0.12f, 0.13f, 0.11f, 1f);

            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var pixels = new Color[IconSize * IconSize];
            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    var edge = x < 8 || y < 8 || x >= IconSize - 8 || y >= IconSize - 8;
                    var stripe = (x + y) % 32 < 4;
                    pixels[y * IconSize + x] = edge ? border : stripe ? accent : baseColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath);
        }

        private static bool AssignIcon(string pieceId, string iconPath)
        {
            var piece = LoadPiece(pieceId);
            if (piece == null)
                return false;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null)
                return false;

            piece.icon = sprite;
            EditorUtility.SetDirty(piece);
            return true;
        }

        private static PieceDefinitionSO LoadPiece(string pieceId) =>
            AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PiecesRoot}/{pieceId}.asset");

        private static void ConfigureSpriteImporter(string assetPath, int maxSize)
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

        private static void EnsureFolder(string path)
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
    }
}
