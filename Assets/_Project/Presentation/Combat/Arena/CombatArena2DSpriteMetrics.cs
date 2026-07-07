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

            // Sprite sheets here are up to 3584² — a per-pixel GetPixel scan is millions of
            // slow native calls, and it ran the first time every unit was built (cached after),
            // which is exactly why the FIRST combat of a session hitched and later ones didn't.
            // For large sheets, sample on an adaptive grid (~64 taps per axis) — ample precision
            // for figure height / shadow bounds while capping the cost. Small sprites scan
            // exactly (step 1) so tight-bounds precision is preserved where it's cheap.
            const int MaxTapsPerAxis = 64;
            const int ExactScanMaxSpan = 512;
            int span = Mathf.Max(x1 - x0, y1 - y0);
            int step = span > ExactScanMaxSpan ? Mathf.Max(1, span / MaxTapsPerAxis) : 1;

            int minX = x1;
            int minY = y1;
            int maxX = x0 - 1;
            int maxY = y0 - 1;

            for (int y = y0; y < y1; y += step)
            {
                for (int x = x0; x < x1; x += step)
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

            // When downsampling, the grid may miss the true edge by up to one step; pad so the
            // figure is never clipped, then clamp back into the sprite rect. At step 1 the scan
            // is exact, so no padding (it would inflate the bounds).
            if (step > 1)
            {
                minX = Mathf.Max(x0, minX - step);
                minY = Mathf.Max(y0, minY - step);
                maxX = Mathf.Min(x1 - 1, maxX + step);
                maxY = Mathf.Min(y1 - 1, maxY + step);
            }

            return new Rect(
                minX - rect.xMin,
                minY - rect.yMin,
                maxX - minX + 1,
                maxY - minY + 1);
        }
    }
}
