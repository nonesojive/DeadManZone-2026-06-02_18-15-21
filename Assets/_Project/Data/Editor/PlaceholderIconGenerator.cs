#if UNITY_EDITOR
using System.IO;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 3 temp-art pass: procedural placeholder shop icon for every PieceDefinitionSO that
    /// doesn't already have one — faction-color disc background + a simple combatRole glyph +
    /// a rarity frame. Deliberately crude (readability at 64px matters more than beauty); solid
    /// art replaces these later. Pieces that already carry an authored icon (the neutral +
    /// IronMarch roster) are left untouched.
    /// </summary>
    public static class PlaceholderIconGenerator
    {
        private const string PiecesFolder = "Assets/_Project/Data/Resources/DeadManZone/Pieces";
        private const string IconsFolder = "Assets/_Project/Data/Resources/DeadManZone/Icons/Placeholder";
        private const int Size = 128;
        private const int SaveBatchSize = 20;

        [MenuItem("DeadManZone/Content/Generate Placeholder Shop Icons")]
        public static void Generate()
        {
            EnsureFolder(IconsFolder);

            var guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PiecesFolder });
            int generated = 0, skipped = 0, sinceSave = 0;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(assetPath);
                if (piece == null)
                    continue;

                if (piece.icon != null)
                {
                    skipped++;
                    continue;
                }

                var texture = BuildIconTexture(piece);
                var pngPath = $"{IconsFolder}/{piece.id}_icon.png";
                File.WriteAllBytes(Path.GetFullPath(pngPath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                // AssetDatabase lesson (see DemoContentGenerator.LoadOrCreate): force the
                // import to complete, then reload canonical instances before assigning —
                // a plain LoadAssetAtPath right after a fresh write can hand back a ghost.
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                ApplySpriteImportSettings(pngPath);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                var canonicalPiece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(assetPath);
                if (sprite == null || canonicalPiece == null)
                {
                    Debug.LogError($"[PlaceholderIcons] Failed to import/link icon for '{piece.id}' ({pngPath}).");
                    continue;
                }

                canonicalPiece.icon = sprite;
                EditorUtility.SetDirty(canonicalPiece);
                generated++;
                sinceSave++;

                if (sinceSave >= SaveBatchSize)
                {
                    AssetDatabase.SaveAssets();
                    sinceSave = 0;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderIcons] Generated {generated} placeholder icons, skipped {skipped} " +
                      "(already had an icon).");
        }

        private static void ApplySpriteImportSettings(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        // -------------------------------------------------------------- texture generation

        private static Texture2D BuildIconTexture(PieceDefinitionSO piece)
        {
            var px = new Color[Size * Size];
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            float discRadius = Size * 0.40f;

            Color bg = FactionColor(piece.factionId);
            FillDisc(px, center, discRadius, bg);

            Color glyph = GlyphColor(bg);
            DrawRoleGlyph(px, center, discRadius, piece.combatRole, glyph);

            DrawRarityFrame(px, center, discRadius, piece.rarity);

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Color FactionColor(string factionId)
        {
            // Mirrors PieceArtResolver.DefaultFactionTokenBackground's palette (kept in sync
            // by eye, not by shared code — Data.Editor has no business depending on
            // Presentation types for a one-off color table).
            return factionId switch
            {
                FactionIds.IronmarchUnion => new Color(0.30f, 0.38f, 0.50f),
                FactionIds.DustScourge => new Color(0.55f, 0.44f, 0.30f),
                FactionIds.CartelOfEchoes => new Color(0.42f, 0.34f, 0.55f),
                FactionIds.CrimsonAssembly => new Color(0.58f, 0.24f, 0.22f),
                FactionIds.AshenCovenant => new Color(0.36f, 0.36f, 0.38f),
                FactionIds.OathbornAccord => new Color(0.55f, 0.46f, 0.22f),
                FactionIds.ParadoxEngine => new Color(0.24f, 0.42f, 0.48f),
                FactionIds.BlightbornPact => new Color(0.30f, 0.46f, 0.24f),
                _ => new Color(0.40f, 0.41f, 0.44f) // neutral
            };
        }

        private static Color GlyphColor(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.5f ? new Color(0.08f, 0.08f, 0.09f) : new Color(0.93f, 0.92f, 0.86f);
        }

        private static void DrawRoleGlyph(Color[] px, Vector2 c, float r, string role, Color col)
        {
            switch (role)
            {
                case "assault": // chevron
                    FillThickLine(px, c + new Vector2(-r * 0.42f, r * 0.18f), c + new Vector2(0f, -r * 0.32f), r * 0.16f, col);
                    FillThickLine(px, c + new Vector2(r * 0.42f, r * 0.18f), c + new Vector2(0f, -r * 0.32f), r * 0.16f, col);
                    break;

                case "defender": // shield outline
                    var shield = new[]
                    {
                        c + new Vector2(-r * 0.38f, -r * 0.32f),
                        c + new Vector2(r * 0.38f, -r * 0.32f),
                        c + new Vector2(r * 0.38f, r * 0.08f),
                        c + new Vector2(0f, r * 0.42f),
                        c + new Vector2(-r * 0.38f, r * 0.08f),
                    };
                    for (int i = 0; i < shield.Length; i++)
                        FillThickLine(px, shield[i], shield[(i + 1) % shield.Length], r * 0.09f, col);
                    break;

                case "sniper": // diamond + center dot
                    var diamond = new[]
                    {
                        c + new Vector2(0f, -r * 0.42f), c + new Vector2(r * 0.42f, 0f),
                        c + new Vector2(0f, r * 0.42f), c + new Vector2(-r * 0.42f, 0f),
                    };
                    for (int i = 0; i < diamond.Length; i++)
                        FillThickLine(px, diamond[i], diamond[(i + 1) % diamond.Length], r * 0.08f, col);
                    FillDisc(px, c, r * 0.09f, col);
                    break;

                case "support": // cross
                    FillThickLine(px, c + new Vector2(-r * 0.4f, 0f), c + new Vector2(r * 0.4f, 0f), r * 0.16f, col);
                    FillThickLine(px, c + new Vector2(0f, -r * 0.4f), c + new Vector2(0f, r * 0.4f), r * 0.16f, col);
                    break;

                case "command": // rank bars
                    FillThickLine(px, c + new Vector2(-r * 0.35f, -r * 0.2f), c + new Vector2(r * 0.35f, -r * 0.2f), r * 0.09f, col);
                    FillThickLine(px, c + new Vector2(-r * 0.35f, 0f), c + new Vector2(r * 0.35f, 0f), r * 0.09f, col);
                    FillThickLine(px, c + new Vector2(-r * 0.35f, r * 0.2f), c + new Vector2(r * 0.35f, r * 0.2f), r * 0.09f, col);
                    break;

                case "artillery": // arc + shell
                    DrawArc(px, c + new Vector2(0f, r * 0.02f), r * 0.5f, 205f, 335f, r * 0.09f, col);
                    FillDisc(px, c + new Vector2(0f, r * 0.30f), r * 0.13f, col);
                    break;

                case "gas": // circle cluster
                    FillRing(px, c + new Vector2(-r * 0.18f, -r * 0.12f), r * 0.13f, r * 0.19f, col);
                    FillRing(px, c + new Vector2(r * 0.20f, -r * 0.05f), r * 0.10f, r * 0.15f, col);
                    FillRing(px, c + new Vector2(0f, r * 0.24f), r * 0.15f, r * 0.21f, col);
                    break;

                case "tank": // rectangle hull + barrel
                    FillRotatedRect(px, c, r * 0.4f, r * 0.22f, 0f, col);
                    FillRotatedRect(px, c + new Vector2(r * 0.35f, -r * 0.05f), r * 0.28f, r * 0.06f, 0f, col);
                    break;

                case "utility": // gear
                    FillRing(px, c, r * 0.24f, r * 0.34f, col);
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = i * 60f * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        FillRotatedRect(px, c + dir * (r * 0.34f), r * 0.07f, r * 0.07f, i * 60f, col);
                    }
                    break;

                case "transport": // box + wheels
                default:
                    FillRotatedRect(px, c + new Vector2(0f, -r * 0.05f), r * 0.36f, r * 0.22f, 0f, col);
                    FillDisc(px, c + new Vector2(-r * 0.22f, r * 0.28f), r * 0.10f, col);
                    FillDisc(px, c + new Vector2(r * 0.22f, r * 0.28f), r * 0.10f, col);
                    break;
            }
        }

        private static void DrawRarityFrame(Color[] px, Vector2 c, float discRadius, Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon:
                    FillRing(px, c, discRadius + 2f, discRadius + 6f, new Color(0.78f, 0.80f, 0.84f));
                    break;
                case Rarity.Rare:
                    var gold = new Color(0.85f, 0.68f, 0.28f);
                    FillRing(px, c, discRadius + 2f, discRadius + 5f, gold);
                    FillRing(px, c, discRadius + 8f, discRadius + 11f, gold);
                    break;
            }
        }

        // ------------------------------------------------------------------- pixel primitives

        private static void FillDisc(Color[] px, Vector2 center, float radius, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(center.y + radius));
            float r2 = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    if (dx * dx + dy * dy <= r2)
                        px[y * Size + x] = col;
                }
            }
        }

        private static void FillRing(Color[] px, Vector2 center, float rInner, float rOuter, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - rOuter));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(center.x + rOuter));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - rOuter));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(center.y + rOuter));
            float rIn2 = rInner * rInner;
            float rOut2 = rOuter * rOuter;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= rIn2 && d2 <= rOut2)
                        px[y * Size + x] = col;
                }
            }
        }

        /// <summary>Axis-aligned-in-local-space rotated rectangle fill, used both directly
        /// (tank hull, gear teeth) and as the building block for thick "lines" (chevrons,
        /// cross, shield/diamond outlines) via <see cref="FillThickLine"/>.</summary>
        private static void FillRotatedRect(Color[] px, Vector2 center, float halfW, float halfH, float angleDeg, Color col)
        {
            float rad = -angleDeg * Mathf.Deg2Rad; // screen Y grows downward; flip for intuitive angles
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float extent = Mathf.Max(halfW, halfH) * 1.5f + 1f;

            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - extent));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(center.x + extent));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - extent));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(center.y + extent));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    float lx = dx * cos - dy * sin;
                    float ly = dx * sin + dy * cos;
                    if (Mathf.Abs(lx) <= halfW && Mathf.Abs(ly) <= halfH)
                        px[y * Size + x] = col;
                }
            }
        }

        private static void FillThickLine(Color[] px, Vector2 a, Vector2 b, float thickness, Color col)
        {
            Vector2 mid = (a + b) * 0.5f;
            float length = Vector2.Distance(a, b);
            float angleDeg = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            FillRotatedRect(px, mid, length * 0.5f + thickness * 0.4f, thickness * 0.5f, angleDeg, col);
        }

        /// <summary>Cheap arc approximation: a row of filled dots stepped along the arc path.
        /// Good enough at placeholder fidelity; a real fill algorithm would be overkill here.</summary>
        private static void DrawArc(Color[] px, Vector2 center, float radius, float fromDeg, float toDeg, float thickness, Color col)
        {
            const int steps = 24;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float ang = Mathf.Lerp(fromDeg, toDeg, t) * Mathf.Deg2Rad;
                var point = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                FillDisc(px, point, thickness, col);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
