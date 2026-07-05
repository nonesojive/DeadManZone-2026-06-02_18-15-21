using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Loads 2D combat VFX sprite strips from Resources and slices frames at runtime.</summary>
    public static class CombatArena2DVfxArt
    {
        private const string ResourcePath = "DeadManZone/CombatArena2DVfxArt";
        private const int RifleImpactFrameCount = 4;
        private const int ExplosionFrameCount = 4;
        private const int DeathPuffFrameCount = 4;

        private static CombatArena2DVfxArtSO _cached;
        private static Sprite[] _rifleImpact;
        private static Sprite[] _explosion;
        private static Sprite[] _deathPuff;

        public static CombatArena2DVfxArtSO Load()
        {
            if (_cached == null)
                _cached = Resources.Load<CombatArena2DVfxArtSO>(ResourcePath);
            return _cached;
        }

        public static Sprite[] RifleImpactFrames =>
            _rifleImpact ??= SliceStrip(Load()?.rifleImpactStrip, RifleImpactFrameCount);

        public static Sprite[] ExplosionFrames =>
            _explosion ??= SliceStrip(Load()?.explosionSmallStrip, ExplosionFrameCount);

        public static Sprite[] DeathPuffFrames =>
            _deathPuff ??= SliceStrip(Load()?.deathPuffStrip, DeathPuffFrameCount);

        internal static Sprite[] SliceStrip(Sprite sheet, int frameCount)
        {
            if (sheet == null || sheet.texture == null || frameCount < 1)
                return System.Array.Empty<Sprite>();

            var rect = sheet.rect;
            float frameWidth = rect.width / frameCount;
            var frames = new Sprite[frameCount];
            bool maskable = sheet.texture.isReadable;
            for (int i = 0; i < frameCount; i++)
            {
                var frameRect = new Rect(rect.x + frameWidth * i, rect.y, frameWidth, rect.height);
                frames[i] = maskable
                    ? CreateMaskedFrame(sheet.texture, frameRect, sheet.pixelsPerUnit)
                    : Sprite.Create(sheet.texture, frameRect, sheet.pivot, sheet.pixelsPerUnit);
            }

            return frames;
        }

        /// <summary>Copy a frame with a radial falloff baked in. The source strips carry
        /// haze to their square cell edges, which reads as a flashing box in the arena;
        /// fading RGB and alpha toward the frame border keeps only the round core.</summary>
        private static Sprite CreateMaskedFrame(Texture2D source, Rect rect, float pixelsPerUnit)
        {
            int w = Mathf.Max(1, (int)rect.width);
            int h = Mathf.Max(1, (int)rect.height);
            var pixels = source.GetPixels((int)rect.x, (int)rect.y, w, h);

            for (int y = 0; y < h; y++)
            {
                float ny = h > 1 ? (y / (float)(h - 1)) * 2f - 1f : 0f;
                for (int x = 0; x < w; x++)
                {
                    float nx = w > 1 ? (x / (float)(w - 1)) * 2f - 1f : 0f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    // Full strength inside the core, fading to zero before the corners.
                    float falloff = 1f - Mathf.SmoothStep(0.55f, 0.95f, d);
                    int index = y * w + x;
                    var c = pixels[index];
                    pixels[index] = new Color(c.r * falloff, c.g * falloff, c.b * falloff, c.a * falloff);
                }
            }

            var masked = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            masked.SetPixels(pixels);
            masked.Apply();
            return Sprite.Create(masked, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        internal static void ClearCacheForTests()
        {
            _cached = null;
            _rifleImpact = null;
            _explosion = null;
            _deathPuff = null;
        }
    }
}
