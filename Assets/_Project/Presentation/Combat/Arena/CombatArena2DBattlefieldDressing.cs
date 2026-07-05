using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Scatters trench props (sandbags, wire, barrels) in the out-of-play
    /// bands above and below the battlefield so the frame reads as a war zone
    /// instead of empty dirt. Deterministic per layout size; play lanes stay clean.</summary>
    public static class CombatArena2DBattlefieldDressing
    {
        private const string ResourcePath = "DeadManZone/CombatArena2DDressingArt";
        private const int PropRenderQueue = 2455;
        private const int DecalRenderQueue = 2452; // above cells (2450), under props/units
        private const float AlphaThreshold01 = 10f / 255f;

        private static CombatArena2DDressingArtSO _cached;
        private static readonly Dictionary<int, Sprite> SpriteCache = new();

        private enum Sheet
        {
            Sandbags,
            Wire,
            Ruins
        }

        /// <summary>Generous source rects (texture-space, bottom-left origin) around
        /// isolated props; content crop trims the transparent padding at slice time.</summary>
        private readonly struct PropDef
        {
            public readonly Sheet Sheet;
            public readonly RectInt SourceRect;
            public readonly float WorldWidth;

            public PropDef(Sheet sheet, RectInt sourceRect, float worldWidth)
            {
                Sheet = sheet;
                SourceRect = sourceRect;
                WorldWidth = worldWidth;
            }
        }

        // Rects derived from a connected-component scan of the 1024px sheets
        // (texture-space, bottom-left origin), then confirmed visually in play —
        // each bounds one COMPLETE prop. Neighboring regions on these sheets hold
        // indoor/industrial tiles (vents, hatches) that read wrong on the field.
        private static readonly PropDef[] Props =
        {
            // "WW1 trench/1 (1).png" — whole sandbag wall sections.
            new(Sheet.Sandbags, new RectInt(0, 62, 130, 65), 1.6f),
            new(Sheet.Sandbags, new RectInt(0, 125, 130, 65), 1.6f),
            // "WW1 trench/3 (1).png" — complete barbed-wire fence runs.
            new(Sheet.Wire, new RectInt(3, 0, 187, 62), 2.3f),
            new(Sheet.Wire, new RectInt(195, 0, 187, 62), 2.3f),
            new(Sheet.Wire, new RectInt(387, 0, 123, 62), 1.6f),
            // "WW1 ruins/4.png" — rubble pile and wire/sandbag emplacements.
            new(Sheet.Ruins, new RectInt(0, 645, 128, 122), 1.3f),
            new(Sheet.Ruins, new RectInt(640, 638, 128, 125), 1.7f),
            new(Sheet.Ruins, new RectInt(768, 639, 128, 124), 1.7f),
            new(Sheet.Ruins, new RectInt(900, 640, 122, 126), 1.6f),
        };

        // Flat marks scattered INSIDE the field: shell craters that scar the ground
        // without cluttering unit readability (they render under everything mobile).
        private static readonly PropDef[] FieldDecals =
        {
            new(Sheet.Ruins, new RectInt(0, 520, 128, 118), 1.5f),
            new(Sheet.Ruins, new RectInt(127, 521, 128, 106), 1.3f),
        };

        public static void Build(
            Transform parent,
            BattlefieldLayout layout,
            CombatGridMapper mapper,
            CombatArenaConfigSO config)
        {
            if (parent == null || layout == null || mapper == null || config == null)
                return;

            var art = Load();
            if (art == null)
                return;

            var rootGo = new GameObject("BattlefieldDressing");
            rootGo.transform.SetParent(parent, false);

            // Deterministic scatter: same layout -> same battlefield.
            var random = new System.Random(layout.TotalWidth * 31 + layout.Height * 7);

            float fieldHalfWidth = layout.TotalWidth * config.cellWidth * 0.5f;
            float fieldHalfDepth = layout.Height * config.cellDepth * 0.5f;

            // Far band (behind the horizon row) and near apron (below the field),
            // hugging the trench lines so props stay clear of the HUD and screen edge.
            ScatterBand(rootGo.transform, random, art,
                minX: -fieldHalfWidth, maxX: fieldHalfWidth,
                minZ: fieldHalfDepth + 0.4f, maxZ: fieldHalfDepth + 1.7f,
                count: 6);
            ScatterBand(rootGo.transform, random, art,
                minX: -fieldHalfWidth, maxX: fieldHalfWidth,
                minZ: -fieldHalfDepth - 2.1f, maxZ: -fieldHalfDepth - 0.6f,
                count: 5);

            // Shell craters scar the field itself; flat and under everything mobile.
            float decalStep = fieldHalfWidth * 2f / 5;
            for (int i = 0; i < 5; i++)
            {
                var def = FieldDecals[random.Next(FieldDecals.Length)];
                var sprite = ResolveSprite(art, def);
                if (sprite == null)
                    continue;

                float x = -fieldHalfWidth + decalStep * i
                    + (float)random.NextDouble() * decalStep * 0.7f + decalStep * 0.15f;
                float z = Mathf.Lerp(-fieldHalfDepth + 1f, fieldHalfDepth - 1f, (float)random.NextDouble());
                // Above the cell plane (0.06) or the cells' depth writes clip the decal.
                CreateFlatProp(rootGo.transform, sprite, new Vector3(x, 0.07f, z), def.WorldWidth,
                    flipX: random.Next(2) == 0, renderQueue: DecalRenderQueue);
            }
        }

        private static void ScatterBand(
            Transform parent,
            System.Random random,
            CombatArena2DDressingArtSO art,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            int count)
        {
            // Stratified X so props spread across the width instead of clumping.
            float step = (maxX - minX) / count;
            for (int i = 0; i < count; i++)
            {
                var def = Props[random.Next(Props.Length)];
                var sprite = ResolveSprite(art, def);
                if (sprite == null)
                    continue;

                float x = minX + step * i + (float)random.NextDouble() * step * 0.8f + step * 0.1f;
                float z = Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());
                CreateFlatProp(parent, sprite, new Vector3(x, 0.05f, z), def.WorldWidth,
                    flipX: random.Next(2) == 0, renderQueue: PropRenderQueue);
            }
        }

        private static void CreateFlatProp(
            Transform parent,
            Sprite sprite,
            Vector3 position,
            float worldWidth,
            bool flipX,
            int renderQueue)
        {
            float aspect = sprite.rect.width > 0f ? sprite.rect.height / sprite.rect.width : 1f;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DressingProp";
            quad.transform.SetParent(parent, false);
            quad.transform.position = position;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(
                flipX ? -worldWidth : worldWidth,
                worldWidth * aspect,
                1f);

            var meshFilter = quad.GetComponent<MeshFilter>();
            CombatArena2DSpriteMesh.Apply(meshFilter, sprite);

            var renderer = quad.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateSprite(
                sprite, Color.white, renderQueue, softAlpha: false, ignoreDepth: false);
            if (material != null)
                renderer.sharedMaterial = material;

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        private static Sprite ResolveSprite(CombatArena2DDressingArtSO art, PropDef def)
        {
            var texture = def.Sheet switch
            {
                Sheet.Sandbags => art.sandbagSheet,
                Sheet.Wire => art.wireSheet,
                Sheet.Ruins => art.ruinsSheet,
                _ => null
            };
            if (texture == null)
                return null;

            int key = CacheKey(texture, def.SourceRect);
            if (SpriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            // Every rect in the table now bounds exactly ONE connected component
            // (scan-verified), so the largest component IS the whole prop — and any
            // neighbor-tile pixels bleeding into the rect get discarded instead of
            // stretching the sprite (seen as a clipped edge on one sandbag wall).
            var rect = CropToLargestComponent(texture, def.SourceRect);
            var sprite = Sprite.Create(
                texture,
                new Rect(rect.x, rect.y, rect.width, rect.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"DressingProp_{rect.x}_{rect.y}";
            SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>Bounds of the largest connected opaque blob in the rect. Generous
        /// hand-picked rects inevitably clip slivers of neighboring tiles; those are
        /// disconnected from the main prop, so keeping only the biggest component
        /// discards them.</summary>
        internal static RectInt CropToLargestComponent(Texture2D texture, RectInt rect)
        {
            var clamped = ClampRect(rect, texture.width, texture.height);
            if (!texture.isReadable)
                return clamped;

            int w = clamped.width;
            int h = clamped.height;
            var opaque = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    opaque[y * w + x] = texture.GetPixel(clamped.x + x, clamped.y + y).a > AlphaThreshold01;

            var visited = new bool[w * h];
            var stack = new Stack<int>();
            RectInt best = default;
            int bestSize = 0;

            for (int start = 0; start < opaque.Length; start++)
            {
                if (!opaque[start] || visited[start])
                    continue;

                int size = 0;
                int minX = w, minY = h, maxX = -1, maxY = -1;
                stack.Push(start);
                visited[start] = true;
                while (stack.Count > 0)
                {
                    int index = stack.Pop();
                    int px = index % w;
                    int py = index / w;
                    size++;
                    minX = Mathf.Min(minX, px);
                    minY = Mathf.Min(minY, py);
                    maxX = Mathf.Max(maxX, px);
                    maxY = Mathf.Max(maxY, py);

                    Visit(px - 1, py);
                    Visit(px + 1, py);
                    Visit(px, py - 1);
                    Visit(px, py + 1);
                }

                if (size > bestSize)
                {
                    bestSize = size;
                    best = new RectInt(clamped.x + minX, clamped.y + minY, maxX - minX + 1, maxY - minY + 1);
                }

                void Visit(int vx, int vy)
                {
                    if (vx < 0 || vy < 0 || vx >= w || vy >= h)
                        return;
                    int vi = vy * w + vx;
                    if (!opaque[vi] || visited[vi])
                        return;
                    visited[vi] = true;
                    stack.Push(vi);
                }
            }

            return bestSize > 0 ? best : clamped;
        }

        /// <summary>Trim a generous source rect to its opaque content so sloppy
        /// hand-picked rects still yield consistently sized props.</summary>
        internal static RectInt CropToContent(Texture2D texture, RectInt rect)
        {
            var clamped = ClampRect(rect, texture.width, texture.height);
            if (!texture.isReadable)
                return clamped;

            int minX = clamped.xMax;
            int minY = clamped.yMax;
            int maxX = clamped.xMin - 1;
            int maxY = clamped.yMin - 1;

            for (int y = clamped.yMin; y < clamped.yMax; y++)
            {
                for (int x = clamped.xMin; x < clamped.xMax; x++)
                {
                    if (texture.GetPixel(x, y).a <= AlphaThreshold01)
                        continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
                return clamped;

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static RectInt ClampRect(RectInt rect, int width, int height)
        {
            int x = Mathf.Clamp(rect.x, 0, Mathf.Max(0, width - 1));
            int y = Mathf.Clamp(rect.y, 0, Mathf.Max(0, height - 1));
            int w = Mathf.Clamp(rect.width, 1, width - x);
            int h = Mathf.Clamp(rect.height, 1, height - y);
            return new RectInt(x, y, w, h);
        }

        private static int CacheKey(Texture2D texture, RectInt rect)
        {
            unchecked
            {
                int key = texture.GetInstanceID();
                key = (key * 397) ^ rect.x;
                key = (key * 397) ^ rect.y;
                key = (key * 397) ^ rect.width;
                key = (key * 397) ^ rect.height;
                return key;
            }
        }

        private static CombatArena2DDressingArtSO Load()
        {
            if (_cached == null)
                _cached = Resources.Load<CombatArena2DDressingArtSO>(ResourcePath);
            return _cached;
        }
    }
}
