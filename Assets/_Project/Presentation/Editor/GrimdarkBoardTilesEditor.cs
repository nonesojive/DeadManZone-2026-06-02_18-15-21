using System.Collections.Generic;
using DeadManZone.Data;
using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Bakes grimdark board tiles procedurally (2026-07-12 playtest: the old trench/bunker
    /// sheet tiles read as a different game next to the M6 kit). Same deterministic
    /// hash-noise approach as the arena ground albedo — no external sheets. Palettes sit
    /// a step lighter than the arena ground so pieces stay readable on the build board:
    /// combat = churned mud with duckboard slats (ties the board to the Trenchline arena),
    /// front column = mud with a sandbag-edge hint, HQ = dark poured concrete, reserves =
    /// riveted steel plate. Rerunnable; overwrites in place.
    /// </summary>
    public static class GrimdarkBoardTilesEditor
    {
        private const string TerrainArtPath = "Assets/_Project/Data/Resources/DeadManZone/BoardTerrainArt.asset";
        private const string OutFolder = "Assets/_Project/Art/Tilesets/GrimdarkBaked";
        private const int TileSize = 96;
        private const int TilePpu = 48;
        private const int Seed = 20260712;

        [MenuItem(DeadManZoneEditorMenus.Art + "Bake Grimdark Board Tiles")]
        public static void Bake()
        {
            SpriteSheetCropUtility.EnsureFolder(OutFolder);

            var combat = BakeSet("combat", 8, BakeCombatTile);
            var front = BakeSet("front", 4, BakeFrontTile);
            var hq = BakeSet("hq", 6, BakeHqTile);
            var reserve = BakeSet("reserve", 3, BakeReserveTile);

            var terrainArt = AssetDatabase.LoadAssetAtPath<BoardTerrainArtSO>(TerrainArtPath);
            if (terrainArt == null)
            {
                Debug.LogError("[GrimdarkTiles] No BoardTerrainArt at " + TerrainArtPath);
                return;
            }

            terrainArt.battlefieldBackdrop = null;
            terrainArt.cellSprite = null;
            terrainArt.combatBoardTiles = combat;
            terrainArt.combatFrontColumnTiles = front;
            terrainArt.hqBoardTiles = hq;
            terrainArt.reserveSlotTiles = reserve;
            terrainArt.rearTiles = System.Array.Empty<Sprite>();
            terrainArt.supportTiles = System.Array.Empty<Sprite>();
            terrainArt.frontTiles = System.Array.Empty<Sprite>();
            terrainArt.neutralTiles = System.Array.Empty<Sprite>();

            ApplyThemeTuning(reserve);
            EditorUtility.SetDirty(terrainArt);
            AssetDatabase.SaveAssets();
            BoardTerrainArtProvider.InvalidateCache();
            UiThemeProvider.InvalidateCache();

            Debug.Log($"[GrimdarkTiles] Baked combat={combat.Length} front={front.Length} " +
                      $"hq={hq.Length} reserves={reserve.Length}. Enter Play mode on Run scene.");
        }

        // ------------------------------------------------------------------ tile bakes

        private delegate Color PixelBaker(float u, float v, int variant);

        private static Sprite[] BakeSet(string prefix, int variants, PixelBaker baker)
        {
            var sprites = new List<Sprite>();
            for (int i = 0; i < variants; i++)
            {
                var tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                var pixels = new Color[TileSize * TileSize];
                for (int y = 0; y < TileSize; y++)
                for (int x = 0; x < TileSize; x++)
                    pixels[y * TileSize + x] = baker((x + 0.5f) / TileSize, (y + 0.5f) / TileSize, i);
                tex.SetPixels(pixels);
                tex.Apply();

                string path = $"{OutFolder}/{prefix}_{i:00}.png";
                SpriteSheetCropUtility.WritePng(path, tex);
                Object.DestroyImmediate(tex);
                SpriteSheetCropUtility.ConfigureSpriteImporter(path, 128, TilePpu);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        /// <summary>Combat board: churned mud, faint duckboard slats running horizontal —
        /// the arena's mud palette lifted ~1.6x so unit sprites keep their contrast.</summary>
        private static Color BakeCombatTile(float u, float v, int variant)
        {
            var mud = new Color(0.170f, 0.145f, 0.112f);
            var dry = new Color(0.235f, 0.205f, 0.150f);

            float n = Noise(u * 5f, v * 5f, variant * 7) * 0.6f
                    + Noise(u * 13f, v * 13f, variant * 7 + 3) * 0.4f;
            var c = Color.Lerp(mud, dry, n);

            // Duckboard slats: three horizontal boards with dark seams, mud showing at edges.
            float slat = Mathf.Repeat(v * 3f, 1f);
            float seam = Mathf.SmoothStep(0f, 1f, Mathf.Min(slat, 1f - slat) * 10f);
            float plankTone = 0.85f + Noise(Mathf.Floor(v * 3f) * 3.7f, variant * 1.3f, 5) * 0.3f;
            var board = new Color(0.205f, 0.165f, 0.115f) * plankTone;
            float boardMask = Mathf.Clamp01(Noise(u * 2.2f, Mathf.Floor(v * 3f) * 0.9f, variant * 7 + 11) * 1.6f - 0.35f);
            c = Color.Lerp(c, board * seam + board * 0.45f * (1f - seam), boardMask * 0.75f);

            // Ink-dark cell edge so the grid reads without bright lines.
            float edge = EdgeDarken(u, v);
            return new Color(c.r * edge, c.g * edge, c.b * edge, 1f);
        }

        /// <summary>Front column: the same mud, no boards, a sandbag-row hint along the
        /// leading edge (right side — the column faces the enemy).</summary>
        private static Color BakeFrontTile(float u, float v, int variant)
        {
            var mud = new Color(0.160f, 0.135f, 0.105f);
            var dry = new Color(0.225f, 0.195f, 0.140f);
            float n = Noise(u * 6f, v * 6f, variant * 13) * 0.65f
                    + Noise(u * 15f, v * 15f, variant * 13 + 2) * 0.35f;
            var c = Color.Lerp(mud, dry, n);

            if (u > 0.78f)
            {
                float bag = Mathf.Repeat(v * 4f, 1f);
                float bagSeam = Mathf.SmoothStep(0f, 1f, Mathf.Min(bag, 1f - bag) * 8f);
                var burlap = new Color(0.220f, 0.196f, 0.150f) * (0.8f + 0.4f * bagSeam);
                c = Color.Lerp(c, burlap, (u - 0.78f) / 0.22f * 0.85f);
            }

            float edge = EdgeDarken(u, v);
            return new Color(c.r * edge, c.g * edge, c.b * edge, 1f);
        }

        /// <summary>HQ board: dark poured concrete, hairline cracks, cool grey.</summary>
        private static Color BakeHqTile(float u, float v, int variant)
        {
            var slabDark = new Color(0.135f, 0.135f, 0.142f);
            var slabLight = new Color(0.190f, 0.190f, 0.198f);
            float n = Noise(u * 4f, v * 4f, variant * 17) * 0.55f
                    + Noise(u * 11f, v * 11f, variant * 17 + 4) * 0.45f;
            var c = Color.Lerp(slabDark, slabLight, n);

            // Hairline crack: one wandering dark line per variant, subtle.
            float crackPath = Noise(v * 3.2f, variant * 2.1f, 23) * 0.8f + 0.1f;
            if (Mathf.Abs(u - crackPath) < 0.012f)
                c *= 0.72f;

            float edge = EdgeDarken(u, v);
            return new Color(c.r * edge, c.g * edge, c.b * edge, 1f);
        }

        /// <summary>Reserves: dark steel plate, rivet dots in the corners.</summary>
        private static Color BakeReserveTile(float u, float v, int variant)
        {
            var steelDark = new Color(0.110f, 0.115f, 0.125f);
            var steelLight = new Color(0.160f, 0.165f, 0.175f);
            float n = Noise(u * 3f, v * 9f, variant * 29) * 0.5f
                    + Noise(u * 9f, v * 3f, variant * 29 + 6) * 0.5f;
            var c = Color.Lerp(steelDark, steelLight, n);

            foreach (var rv in RivetCenters)
            {
                float d = Vector2.Distance(new Vector2(u, v), rv);
                if (d < 0.045f)
                    c *= d < 0.03f ? 1.35f : 0.75f; // bright head, dark ring
            }

            float edge = EdgeDarken(u, v);
            return new Color(c.r * edge, c.g * edge, c.b * edge, 1f);
        }

        private static readonly Vector2[] RivetCenters =
        {
            new(0.12f, 0.12f), new(0.88f, 0.12f), new(0.12f, 0.88f), new(0.88f, 0.88f),
        };

        // ------------------------------------------------------------------ helpers

        /// <summary>Gentle vignette to ink-dark cell borders — the grid reads from tile
        /// edges, not from bright overlay lines (kept at low alpha in theme tuning).</summary>
        private static float EdgeDarken(float u, float v)
        {
            float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            return Mathf.Lerp(0.62f, 1f, Mathf.SmoothStep(0f, 1f, d * 14f));
        }

        private static void ApplyThemeTuning(Sprite[] reserveTiles)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(
                "Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset");
            if (theme == null)
                return;

            theme.terrainZoneTintStrength = 0.05f;
            theme.boardCellZoneOverlayAlpha = 0f;
            // Bone-toned hairlines at whisper alpha — edges live in the tiles themselves.
            theme.boardGridLineColor = new Color(0.87f, 0.83f, 0.74f, 0.07f);
            theme.boardZoneDividerColor = new Color(0.87f, 0.83f, 0.74f, 0.14f);
            if (reserveTiles != null && reserveTiles.Length > 0)
                theme.storageSlotEmptySprite = reserveTiles[0];
            EditorUtility.SetDirty(theme);
        }

        // Deterministic hash-lattice value noise (same family as CombatEnvironmentBuilder).
        private static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(Seed * 92821);
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        private static float Noise(float x, float y, int salt)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash01(x0 + salt * 101, y0), b = Hash01(x0 + 1 + salt * 101, y0);
            float c = Hash01(x0 + salt * 101, y0 + 1), d = Hash01(x0 + 1 + salt * 101, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }
    }
}
