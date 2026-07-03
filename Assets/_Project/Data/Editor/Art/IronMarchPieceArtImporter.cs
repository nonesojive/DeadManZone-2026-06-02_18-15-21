using System.Collections.Generic;
using System.IO;
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Crops IronMarch piece icons and per-cell board sprites from themed sheets.</summary>
    public static class IronMarchPieceArtImporter
    {
        private const int IconSize = 256;
        private const int CellSize = 128;

        [MenuItem(DeadManZoneEditorMenus.Art + "Import IronMarch Piece Icons")]
        public static void ImportFromMenu() => ImportAll();

        public static int ImportAll()
        {
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.Icons);
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.Cells);

            var importedIcons = 0;
            var sheetCache = new Dictionary<string, Texture2D>();

            foreach (var pieceId in IronMarchArtPaths.PieceIds)
            {
                if (!IronMarchPieceArtMap.TryGetIconCell(pieceId, out var iconCell))
                    continue;

                if (!TryCropCell(iconCell, sheetCache, out var iconCrop))
                    continue;

                var iconPath = IronMarchArtPaths.IconAssetPath(pieceId);
                var icon = SpriteSheetCropUtility.BuildShopIcon(iconCrop, IconSize);
                SpriteSheetCropUtility.WritePng(iconPath, icon);
                Object.DestroyImmediate(icon);
                SpriteSheetCropUtility.ConfigureSpriteImporter(iconPath, IconSize);
                importedIcons++;
            }

            foreach (var sheet in sheetCache.Values)
                Object.DestroyImmediate(sheet);

            sheetCache.Clear();
            AssignPieceArt();
            return importedIcons;
        }

        public static void AssignPieceArt()
        {
            var assignedIcons = 0;
            var assignedCells = 0;

            foreach (var pieceId in IronMarchArtPaths.PieceIds)
            {
                var piece = LoadPiece(pieceId);
                if (piece == null)
                    continue;

                var iconPath = IronMarchArtPaths.IconAssetPath(pieceId);
                if (File.Exists(iconPath))
                {
                    SpriteSheetCropUtility.ConfigureSpriteImporter(iconPath, IconSize);
                    var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    if (icon != null)
                    {
                        piece.icon = icon;
                        assignedIcons++;
                    }
                }

                var entries = new List<PieceCellSprite>();
                foreach (var cell in piece.shapeCells)
                {
                    if (!IronMarchPieceArtMap.TryGetCellSprite(pieceId, cell, out var cellRef))
                        continue;

                    if (!TryLoadSheetCell(cellRef, out var cellTexture))
                        continue;

                    var cellPath = IronMarchArtPaths.CellAssetPath(pieceId, $"{cell.x}_{cell.y}");
                    SpriteSheetCropUtility.WritePng(cellPath, cellTexture);
                    Object.DestroyImmediate(cellTexture);
                    SpriteSheetCropUtility.ConfigureSpriteImporter(cellPath, CellSize);

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cellPath);
                    if (sprite == null)
                        continue;

                    entries.Add(new PieceCellSprite { localCell = cell, sprite = sprite });
                }

                if (entries.Count > 0)
                {
                    piece.cellSprites = entries.ToArray();
                    assignedCells++;
                }

                var combatPath = IronMarchPieceArtMap.TryGetCombatSpritePath(pieceId);
                if (!string.IsNullOrEmpty(combatPath) && File.Exists(combatPath))
                {
                    SpriteSheetCropUtility.ConfigureSpriteImporter(combatPath, 256);
                    piece.combatArenaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(combatPath);
                }

                EditorUtility.SetDirty(piece);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"IronMarch piece art assigned: icons={assignedIcons}, cellSprites on {assignedCells} pieces.");
        }

        private static bool TryCropCell(
            IronMarchSheetCell cell,
            Dictionary<string, Texture2D> cache,
            out Texture2D crop)
        {
            crop = null;
            if (!cache.TryGetValue(cell.SheetPath, out var sheet))
            {
                sheet = SpriteSheetCropUtility.LoadReadableTexture(cell.SheetPath);
                if (sheet == null)
                {
                    Debug.LogWarning("Missing themed sheet: " + cell.SheetPath);
                    return false;
                }

                cache[cell.SheetPath] = sheet;
            }

            crop = SpriteSheetCropUtility.CropGridCell(
                sheet,
                cell.Col,
                cell.Row,
                cell.GridCols,
                cell.GridRows);
            return crop != null;
        }

        private static bool TryLoadSheetCell(IronMarchSheetCell cell, out Texture2D crop)
        {
            crop = null;
            var sheet = SpriteSheetCropUtility.LoadReadableTexture(cell.SheetPath);
            if (sheet == null)
                return false;

            crop = SpriteSheetCropUtility.CropGridCell(
                sheet,
                cell.Col,
                cell.Row,
                cell.GridCols,
                cell.GridRows);
            Object.DestroyImmediate(sheet);
            return crop != null;
        }

        private static PieceDefinitionSO LoadPiece(string pieceId) =>
            AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{IronMarchArtPaths.PiecesRoot}/{pieceId}.asset");
    }
}
