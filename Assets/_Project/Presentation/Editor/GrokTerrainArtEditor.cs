using DeadManZone.Data;
using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class GrokTerrainArtEditor
    {
        private const string GrokRoot = "Assets/Grok Images";
        private const string BattlefieldBackdropPath = "Assets/_Project/Art/Board/trench_battlefield_backdrop.png";
        private const string TerrainArtPath = "Assets/_Project/Data/Resources/DeadManZone/BoardTerrainArt.asset";
        private static readonly string[] UiThemePaths =
        {
            "Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset",
            "Assets/_Project/Data/Visual/Presets/BunkerSurvivalUiTheme.asset"
        };

        [MenuItem(DeadManZoneEditorMenus.Art + "Import Grok Terrain And Shop Background")]
        public static void ImportGrokTerrainAndShopBackground()
        {
            ConfigureTerrainTexture(BattlefieldBackdropPath, 2048);
            ConfigureTerrainTexture($"{GrokRoot}/Bunkerwall2.jpg", 2048);
            ConfigureTerrainTexture($"{GrokRoot}/ReartileA.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/ReartileB.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/SupporttileA1.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/SupporttileA2.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/SupporttileB1.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/SupporttileB2.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/FronttileA1.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/FronttileA4.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/FronttileC1.jpg", 1024);
            ConfigureTerrainTexture($"{GrokRoot}/FronttileC2.jpg", 1024);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var terrainArt = LoadOrCreateTerrainArt();
            terrainArt.battlefieldBackdrop = AssetDatabase.LoadAssetAtPath<Sprite>(BattlefieldBackdropPath);
            terrainArt.cellSprite = LoadSprite("SupporttileB1.jpg");
            terrainArt.rearTiles = LoadSprites("ReartileA.jpg", "ReartileB.jpg");
            terrainArt.supportTiles = LoadSprites(
                "SupporttileA1.jpg",
                "SupporttileA2.jpg",
                "SupporttileB1.jpg",
                "SupporttileB2.jpg");
            terrainArt.frontTiles = LoadSprites(
                "FronttileA1.jpg",
                "FronttileA4.jpg",
                "FronttileC1.jpg",
                "FronttileC2.jpg");

            var shopBackground = LoadSprite("Bunkerwall2.jpg");
            var themesUpdated = 0;
            foreach (var themePath in UiThemePaths)
            {
                var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(themePath);
                if (theme == null)
                    continue;

                theme.shopBackgroundSprite = shopBackground;
                theme.shopBackgroundScrimColor = new Color(0.04f, 0.05f, 0.07f, 0.38f);
                theme.shopLaneTintScaleWithBackground = 0.45f;
                theme.terrainZoneTintStrength = 0.12f;
                theme.boardCellZoneOverlayAlpha = 0.1f;
                theme.boardGridLineColor = new Color(1f, 1f, 1f, 0.14f);
                theme.boardZoneDividerColor = new Color(1f, 1f, 1f, 0.28f);
                EditorUtility.SetDirty(theme);
                themesUpdated++;
            }

            EditorUtility.SetDirty(terrainArt);
            AssetDatabase.SaveAssets();
            BoardTerrainArtProvider.InvalidateCache();
            UiThemeProvider.InvalidateCache();

            Debug.Log(
                "Battlefield backdrop="
                + (terrainArt.battlefieldBackdrop != null ? "trench_battlefield_backdrop" : "missing")
                + ". Legacy tiles Rear="
                + terrainArt.rearTiles.Length
                + " Support="
                + terrainArt.supportTiles.Length
                + " Front="
                + terrainArt.frontTiles.Length
                + ". Shop background="
                + (shopBackground != null ? "Bunkerwall2" : "missing")
                + ". Themes updated="
                + themesUpdated
                + ". Enter Play mode.");
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

        private static Sprite LoadSprite(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{GrokRoot}/{fileName}");

        private static Sprite[] LoadSprites(params string[] fileNames)
        {
            var sprites = new Sprite[fileNames.Length];
            var count = 0;
            for (var i = 0; i < fileNames.Length; i++)
            {
                var sprite = LoadSprite(fileNames[i]);
                if (sprite == null)
                    continue;

                sprites[count++] = sprite;
            }

            if (count == sprites.Length)
                return sprites;

            var trimmed = new Sprite[count];
            for (var i = 0; i < count; i++)
                trimmed[i] = sprites[i];
            return trimmed;
        }

        private static void ConfigureTerrainTexture(string assetPath, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing texture: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
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

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
