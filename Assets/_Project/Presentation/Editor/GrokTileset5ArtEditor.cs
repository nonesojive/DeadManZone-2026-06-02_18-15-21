using System.Collections.Generic;
using System.Linq;
using DeadManZone.Data;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Imports groktileset5 PNGs as board cell sprites (Rear / Support / Front zone pools)
    /// and vent grates for the reserves strip.
    /// </summary>
    public static class GrokTileset5ArtEditor
    {
        private const string TilesetRoot = "Assets/grok tiles/groktileset5";
        private const string TerrainArtPath = "Assets/_Project/Data/Resources/DeadManZone/BoardTerrainArt.asset";
        private const int SpritePpu = 100;

        // ponytail: fixed list from alpha scan — hollow center + >45% transparent.
        private static readonly int[] ExcludedHollowFrames = { 4, 5, 6, 18, 27 };

        // ponytail: slotted vent tiles identified by dark center fill; moved to reserves.
        private static readonly int[] GrateTiles = { 8, 11, 13, 20, 22 };

        [MenuItem("DeadManZone/Art/Import Grok Tileset5 Board Tiles")]
        public static void ImportGrokTileset5BoardTiles()
        {
            ConfigureTileTextures();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var terrainArt = LoadOrCreateTerrainArt();
            terrainArt.battlefieldBackdrop = null;
            terrainArt.cellSprite = null;
            terrainArt.rearTiles = LoadZoneTiles(1, 9);
            terrainArt.supportTiles = LoadZoneTiles(10, 18);
            terrainArt.frontTiles = LoadZoneTiles(19, 27);
            terrainArt.neutralTiles = System.Array.Empty<Sprite>();
            terrainArt.reserveSlotTiles = LoadNamedTiles(GrateTiles);

            ApplyThemeTuning(terrainArt.reserveSlotTiles);

            EditorUtility.SetDirty(terrainArt);
            AssetDatabase.SaveAssets();
            BoardTerrainArtProvider.InvalidateCache();
            UiThemeProvider.InvalidateCache();

            Debug.Log(
                "Grok tileset5 board tiles imported. "
                + "Rear=" + terrainArt.rearTiles.Length
                + " Support=" + terrainArt.supportTiles.Length
                + " Front=" + terrainArt.frontTiles.Length
                + " Reserves=" + terrainArt.reserveSlotTiles.Length
                + ". Excluded hollow frames: "
                + string.Join(", ", ExcludedHollowFrames.Select(i => "tile" + i))
                + ". Grates on reserves: "
                + string.Join(", ", GrateTiles.Select(i => "tile" + i))
                + ". Enter Play mode on Run scene.");
        }

        private static void ConfigureTileTextures()
        {
            for (var i = 1; i <= 27; i++)
            {
                var path = $"{TilesetRoot}/tile{i}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("Missing tile texture: " + path);
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;
                importer.spritePixelsPerUnit = SpritePpu;
                importer.SaveAndReimport();
            }
        }

        private static Sprite[] LoadZoneTiles(int start, int end)
        {
            var sprites = new List<Sprite>();
            for (var i = start; i <= end; i++)
            {
                if (IsExcludedFromBoard(i))
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilesetRoot}/tile{i}.png");
                if (sprite == null)
                    Debug.LogWarning("Missing sprite: tile" + i);
                else
                    sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        private static Sprite[] LoadNamedTiles(IReadOnlyList<int> tileNumbers)
        {
            var sprites = new List<Sprite>();
            foreach (var i in tileNumbers)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilesetRoot}/tile{i}.png");
                if (sprite == null)
                    Debug.LogWarning("Missing sprite: tile" + i);
                else
                    sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        private static bool IsExcludedFromBoard(int tileNumber) =>
            ExcludedHollowFrames.Contains(tileNumber) || GrateTiles.Contains(tileNumber);

        private static void ApplyThemeTuning(Sprite[] reserveGrates)
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

                // Let Grok art read naturally; zone columns still tint slightly.
                theme.terrainZoneTintStrength = 0.08f;
                theme.boardCellZoneOverlayAlpha = 0f;
                theme.boardGridLineColor = new Color(1f, 1f, 1f, 0.12f);
                theme.boardZoneDividerColor = new Color(1f, 1f, 1f, 0.22f);

                // Label strip / legacy reserves prefab fallback.
                if (reserveGrates != null && reserveGrates.Length > 0)
                    theme.storageSlotEmptySprite = reserveGrates[0];

                EditorUtility.SetDirty(theme);
            }
        }

        private static BoardTerrainArtSO LoadOrCreateTerrainArt()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BoardTerrainArtSO>(TerrainArtPath);
            if (asset != null)
                return asset;

            EnsureFolder("Assets/_Project/Data/Resources/DeadManZone");
            asset = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            AssetDatabase.CreateAsset(asset, TerrainArtPath);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
