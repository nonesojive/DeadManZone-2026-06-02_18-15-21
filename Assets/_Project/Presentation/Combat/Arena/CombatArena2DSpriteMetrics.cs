using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal static class CombatArena2DSpriteMetrics
    {
        private const float AlphaThreshold01 = 10f / 255f;
        private static readonly Dictionary<int, Rect> VisibleBoundsCache = new();

        public static float VisibleHeightUnits(Sprite sprite)
        {
            if (sprite == null || sprite.pixelsPerUnit <= 0f)
                return 0f;

            return VisibleBoundsPixels(sprite).height / sprite.pixelsPerUnit;
        }

        public static float VisibleBottomUnits(Sprite sprite)
        {
            if (sprite == null || sprite.pixelsPerUnit <= 0f)
                return 0f;

            return VisibleBoundsPixels(sprite).yMin / sprite.pixelsPerUnit;
        }

        public static Rect VisibleBoundsPixels(Sprite sprite)
        {
            if (sprite == null)
                return Rect.zero;

            int key = sprite.GetInstanceID();
            if (VisibleBoundsCache.TryGetValue(key, out var cached))
                return cached;

            var bounds = DetectVisibleBoundsPixels(sprite);
            VisibleBoundsCache[key] = bounds;
            return bounds;
        }

        private static Rect DetectVisibleBoundsPixels(Sprite sprite)
        {
            var fallback = new Rect(0f, 0f, sprite.rect.width, sprite.rect.height);
            var texture = sprite.texture;
            if (texture == null || !texture.isReadable)
                return fallback;

            var rect = sprite.rect;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(rect.xMin), 0, texture.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(rect.yMin), 0, texture.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(rect.xMax), x0 + 1, texture.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(rect.yMax), y0 + 1, texture.height);

            int minX = x1;
            int minY = y1;
            int maxX = x0 - 1;
            int maxY = y0 - 1;

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
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
                return fallback;

            return new Rect(
                minX - rect.xMin,
                minY - rect.yMin,
                maxX - minX + 1,
                maxY - minY + 1);
        }
    }
}
