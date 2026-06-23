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
            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = Sprite.Create(
                    sheet.texture,
                    new Rect(rect.x + frameWidth * i, rect.y, frameWidth, rect.height),
                    sheet.pivot,
                    sheet.pixelsPerUnit);
            }

            return frames;
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
