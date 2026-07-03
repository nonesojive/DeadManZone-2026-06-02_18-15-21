using System.Collections.Generic;
using DeadManZone.Data;
using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>Imports themed WW1/trench/bunker tiles into BoardTerrainArt board-kind pools.</summary>
    public static class ThemedBoardTerrainEditor
    {
        private const string TerrainArtPath = "Assets/_Project/Data/Resources/DeadManZone/BoardTerrainArt.asset";
        private const string TrenchSheet = "Assets/_Project/Art/Tilesets/WW1 trench/1 (1).png";
        private const string BunkerSheet = "Assets/_Project/Art/Tilesets/Nuclear Shelter/1.png";
        private const int TileGrid = 16;
        private const int TilePpu = 48;

        // ponytail: hand-picked 48px cells from trench/bunker sheets; upgrade via ThemedTilesetSlicer UI later.
        private static readonly (int col, int row)[] CombatCells =
        {
            (4, 7), (5, 7), (6, 7), (7, 7), (8, 7), (9, 7),
            (4, 8), (5, 8), (6, 8), (7, 8), (8, 8), (9, 8)
        };

        private static readonly (int col, int row)[] FrontCells =
        {
            (0, 11), (1, 11), (2, 11), (3, 11),
            (0, 12), (1, 12), (2, 12), (3, 12)
        };

        private static readonly (int col, int row)[] HqCells =
        {
            (5, 9), (6, 9), (7, 9), (8, 9),
            (5, 10), (6, 10), (7, 10), (8, 10)
        };

        private static readonly (int col, int row)[] ReserveCells =
        {
            (9, 14), (10, 14), (11, 14), (9, 15), (10, 15)
        };

        [MenuItem(DeadManZoneEditorMenus.Art + "Import Themed Board Tiles")]
        public static void ImportThemedBoardTiles()
        {
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.CombatTiles);
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.FrontTiles);
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.HqTiles);
            SpriteSheetCropUtility.EnsureFolder(IronMarchArtPaths.ReserveTiles);

            var combatSprites = ExportCells(TrenchSheet, CombatCells, IronMarchArtPaths.CombatTiles, "combat");
            var frontSprites = ExportCells(TrenchSheet, FrontCells, IronMarchArtPaths.FrontTiles, "front");
            var hqSprites = ExportCells(BunkerSheet, HqCells, IronMarchArtPaths.HqTiles, "hq");
            var reserveSprites = ExportCells(TrenchSheet, ReserveCells, IronMarchArtPaths.ReserveTiles, "reserve");

            var terrainArt = LoadOrCreateTerrainArt();
            terrainArt.battlefieldBackdrop = null;
            terrainArt.cellSprite = null;
            terrainArt.combatBoardTiles = combatSprites;
            terrainArt.combatFrontColumnTiles = frontSprites;
            terrainArt.hqBoardTiles = hqSprites;
            terrainArt.reserveSlotTiles = reserveSprites;
            terrainArt.rearTiles = System.Array.Empty<Sprite>();
            terrainArt.supportTiles = System.Array.Empty<Sprite>();
            terrainArt.frontTiles = System.Array.Empty<Sprite>();
            terrainArt.neutralTiles = System.Array.Empty<Sprite>();

            ApplyThemeTuning(reserveSprites);
            EditorUtility.SetDirty(terrainArt);
            AssetDatabase.SaveAssets();
            BoardTerrainArtProvider.InvalidateCache();
            UiThemeProvider.InvalidateCache();

            Debug.Log(
                "Themed board tiles imported. Combat="
                + combatSprites.Length
                + " Front="
                + frontSprites.Length
                + " HQ="
                + hqSprites.Length
                + " Reserves="
                + reserveSprites.Length
                + ". Enter Play mode on Run scene.");
        }

        private static Sprite[] ExportCells(
            string sheetPath,
            IReadOnlyList<(int col, int row)> cells,
            string outputFolder,
            string prefix)
        {
            var sheet = SpriteSheetCropUtility.LoadReadableTexture(sheetPath);
            if (sheet == null)
            {
                Debug.LogError("Missing board tile sheet: " + sheetPath);
                return System.Array.Empty<Sprite>();
            }

            var sprites = new List<Sprite>();
            for (int i = 0; i < cells.Count; i++)
            {
                var (col, row) = cells[i];
                var crop = SpriteSheetCropUtility.CropGridCell(sheet, col, row, TileGrid, TileGrid);
                var assetPath = $"{outputFolder}/{prefix}_{i:00}.png";
                SpriteSheetCropUtility.WritePng(assetPath, crop);
                Object.DestroyImmediate(crop);
                SpriteSheetCropUtility.ConfigureSpriteImporter(assetPath, 256, TilePpu);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            Object.DestroyImmediate(sheet);
            return sprites.ToArray();
        }

        private static void ApplyThemeTuning(Sprite[] reserveTiles)
        {
            var themePaths = new[]
            {
                "Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset",
                "Assets/_Project/Data/Visual/Presets/BunkerSurvivalUiTheme.asset",
                "Assets/_Project/Data/Visual/Presets/GrittyPostApocalypticUiTheme.asset",
            };

            foreach (var path in themePaths)
            {
                var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(path);
                if (theme == null)
                    continue;

                theme.terrainZoneTintStrength = 0.06f;
                theme.boardCellZoneOverlayAlpha = 0f;
                theme.boardGridLineColor = new Color(1f, 1f, 1f, 0.12f);
                theme.boardZoneDividerColor = new Color(1f, 1f, 1f, 0.18f);

                if (reserveTiles != null && reserveTiles.Length > 0)
                    theme.storageSlotEmptySprite = reserveTiles[0];

                EditorUtility.SetDirty(theme);
            }
        }

        private static BoardTerrainArtSO LoadOrCreateTerrainArt()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BoardTerrainArtSO>(TerrainArtPath);
            if (asset != null)
                return asset;

            SpriteSheetCropUtility.EnsureFolder("Assets/_Project/Data/Resources/DeadManZone");
            asset = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            AssetDatabase.CreateAsset(asset, TerrainArtPath);
            return asset;
        }
    }
}
