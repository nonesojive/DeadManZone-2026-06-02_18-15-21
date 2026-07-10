using System.Collections.Generic;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal enum CombatUnit2DAnimState
    {
        Idle,
        Walk,
        Run,
        Hurt,
        HitReact,
        Shoot,
        Die
    }

    /// <summary>Advances horizontal strip frames and resolves per-frame sprites for unit quads.</summary>
    internal sealed class CombatUnit2DStripPlayer
    {
        private const float FramePivotX = 0.5f;
        private const float FramePivotY = 0.05f;
        private const int FrameEdgeInsetPixels = 6;
        private const int FrameCropPaddingPixels = 4;
        private const float AlphaThreshold01 = 10f / 255f;

        private CombatUnit2DAnimationSetSO _set;
        private CombatUnit2DAnimState _state = CombatUnit2DAnimState.Idle;
        private float _time;
        private float _rate = 1f;
        private bool _locked;
        private static readonly Dictionary<int, RectInt> SharedCropCache = new();
        private static readonly Dictionary<int, Sprite[]> SharedFrameCache = new();
        private readonly Dictionary<CombatUnit2DAnimState, Sprite[]> _frameCache = new();

        public CombatUnit2DAnimState State => _state;
        public bool IsLocked => _locked;
        public float CurrentDurationSeconds => _rate > 0f
            ? ResolveStrip(_state).DurationSeconds / _rate
            : ResolveStrip(_state).DurationSeconds;
        internal float CurrentTimeSeconds => _time;

        public void Bind(CombatUnit2DAnimationSetSO set)
        {
            _set = set;
            _frameCache.Clear();
        }

        /// <summary>Play a state. When <paramref name="targetDurationSeconds"/> is positive,
        /// the strip is time-scaled so its full playback fits that window (attack/death
        /// strips are authored longer than the presentation beats they must match).</summary>
        public void Play(CombatUnit2DAnimState state, bool restart = true, float targetDurationSeconds = -1f)
        {
            if (_set == null)
                return;

            var strip = ResolveStrip(state);
            if (!strip.IsValid)
            {
                if (state != CombatUnit2DAnimState.Idle)
                    Play(CombatUnit2DAnimState.Idle, restart);
                return;
            }

            // Locomotion states (Idle/Walk/Run) share one continuous cycle: the sim's
            // bursty anchor makes movement pulse, toggling Walk<->Idle many times a
            // second. Resetting _time on each toggle restarted the cycle every few
            // frames, so the legs never advanced past frame ~0-3 and the unit "slid"
            // with a frozen pose. Preserve the phase across locomotion transitions;
            // only one-shots (Shoot/Hurt/HitReact/Die) or an explicit restart rewind.
            bool loopSwap = state != _state && IsLocomotion(state) && IsLocomotion(_state);
            if (restart || (state != _state && !loopSwap))
            {
                _time = 0f;
            }
            else if (loopSwap)
            {
                // Idle/Walk/Run cycle at very different rates (idle ~4s, walk ~1s):
                // carrying raw seconds across a swap lands on an unrelated pose. Carry
                // the cycle PHASE instead so the legs continue from the same beat.
                float oldDuration = ResolveStrip(_state).DurationSeconds;
                float newDuration = strip.DurationSeconds;
                if (oldDuration > 0f && newDuration > 0f)
                    _time = _time % oldDuration / oldDuration * newDuration;
            }

            float naturalDuration = strip.DurationSeconds;
            _rate = targetDurationSeconds > 0f && naturalDuration > 0f
                ? naturalDuration / targetDurationSeconds
                : 1f;

            _state = state;
            _locked = state is CombatUnit2DAnimState.Shoot or CombatUnit2DAnimState.Hurt
                or CombatUnit2DAnimState.HitReact or CombatUnit2DAnimState.Die;
        }

        public void Tick(float deltaTime)
        {
            if (_set == null)
                return;

            var strip = ResolveStrip(_state);
            if (!strip.IsValid || strip.framesPerSecond <= 0f)
                return;

            _time += deltaTime * _rate;
            float duration = strip.DurationSeconds;
            if (duration <= 0f)
                return;

            if (strip.loop)
            {
                while (_time >= duration)
                    _time -= duration;
                return;
            }

            if (_time >= duration)
            {
                _time = duration;
                if (_state == CombatUnit2DAnimState.Die)
                {
                    _locked = true;
                    return;
                }

                _locked = false;
                Play(CombatUnit2DAnimState.Idle, restart: true);
            }
        }

        /// <summary>Sprite for the current state/frame, or <paramref name="fallback"/> when unavailable.</summary>
        public Sprite ResolveSprite(Sprite fallback)
        {
            if (_set == null)
                return fallback;

            var strip = ResolveStrip(_state);
            var activeState = _state;
            if (!strip.IsValid)
            {
                strip = _set.idle;
                activeState = CombatUnit2DAnimState.Idle;
                if (!strip.IsValid)
                    return fallback;
            }

            var frames = GetFrames(activeState, strip);
            if (frames.Length == 0)
                return fallback;

            return frames[ResolveFrameIndex(strip, _time)] ?? fallback;
        }

        private static bool IsLocomotion(CombatUnit2DAnimState state) =>
            state is CombatUnit2DAnimState.Idle
                or CombatUnit2DAnimState.Walk
                or CombatUnit2DAnimState.Run;

        internal static int ResolveFrameIndex(CombatUnit2DStrip strip, float timeSeconds)
        {
            if (strip.frameCount < 1)
                return 0;

            float duration = strip.DurationSeconds;
            if (duration <= 0f)
                return 0;

            // Floor keeps every frame on screen for its full window; rounding halves
            // the first/last frames and skips ahead mid-strip.
            float t = strip.loop ? timeSeconds % duration : Mathf.Min(timeSeconds, duration);
            int index = Mathf.FloorToInt(t * strip.framesPerSecond);
            return Mathf.Clamp(index, 0, strip.frameCount - 1);
        }

        /// <summary>Slice a grid (or single-row) sheet into per-frame sprites with a shared
        /// bottom-center pivot. Frames are row-major from the top-left, matching the source layout.</summary>
        internal static Sprite[] SliceUnitStrip(CombatUnit2DStrip strip)
        {
            if (!strip.IsValid || strip.sheet.texture == null)
                return System.Array.Empty<Sprite>();

            var texture = strip.sheet.texture;
            int texW = texture.width;
            int texH = texture.height;
            if (texW < 1 || texH < 1)
                return System.Array.Empty<Sprite>();

            int columns = Mathf.Max(1, strip.ColumnsOrDefault);
            int rows = Mathf.Max(1, Mathf.CeilToInt(strip.frameCount / (float)columns));

            // Sprite rect can reflect source PNG size while the imported GPU texture is downscaled.
            var source = strip.sheet.textureRect;
            float scaleX = source.width > 0f ? texW / source.width : 1f;
            float scaleY = source.height > 0f ? texH / source.height : 1f;
            float originX = source.x * scaleX;
            float originY = source.y * scaleY;

            int cellW = Mathf.Max(1, Mathf.RoundToInt(texW / (float)columns));
            int cellH = Mathf.Max(1, Mathf.RoundToInt(texH / (float)rows));
            float cellWf = texW / (float)columns;
            float cellHf = texH / (float)rows;
            var crop = ResolveSharedCellCrop(texture, columns, rows, strip.frameCount, cellW, cellH);
            var pivot = new Vector2(FramePivotX, FramePivotY);
            float ppu = strip.sheet.pixelsPerUnit * scaleX;

            var frames = new Sprite[strip.frameCount];
            for (int i = 0; i < strip.frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                int cellX = Mathf.RoundToInt(originX + col * cellWf);
                int cellY = Mathf.RoundToInt(originY + texH - (row + 1) * cellHf);
                int x = Mathf.Clamp(cellX + crop.x, 0, texW - crop.width);
                int y = Mathf.Clamp(cellY + crop.y, 0, texH - crop.height);
                if (x + crop.width > texW || y + crop.height > texH)
                    continue;

                frames[i] = Sprite.Create(texture, new Rect(x, y, crop.width, crop.height), pivot, ppu);
            }

            return frames;
        }

        private static RectInt ResolveSharedCellCrop(
            Texture2D texture,
            int columns,
            int rows,
            int frameCount,
            int cellW,
            int cellH)
        {
            int key = CropCacheKey(texture, columns, rows, frameCount, cellW, cellH);
            if (SharedCropCache.TryGetValue(key, out var cached))
                return cached;

            var crop = DetectSharedContentCrop(texture, columns, rows, frameCount, cellW, cellH);
            SharedCropCache[key] = crop;
            return crop;
        }

        private static RectInt DetectSharedContentCrop(
            Texture2D texture,
            int columns,
            int rows,
            int frameCount,
            int cellW,
            int cellH)
        {
            int edgeInset = Mathf.Min(FrameEdgeInsetPixels, Mathf.Max(0, Mathf.Min(cellW, cellH) / 8));
            var fallback = new RectInt(
                edgeInset,
                edgeInset,
                Mathf.Max(1, cellW - edgeInset * 2),
                Mathf.Max(1, cellH - edgeInset * 2));

            if (texture == null || !texture.isReadable)
                return fallback;

            int minX = cellW;
            int minY = cellH;
            int maxX = -1;
            int maxY = -1;
            int safeMaxX = Mathf.Max(edgeInset, cellW - edgeInset);
            int safeMaxY = Mathf.Max(edgeInset, cellH - edgeInset);

            int count = Mathf.Min(frameCount, columns * rows);
            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                int cellX = col * cellW;
                int cellY = texture.height - (row + 1) * cellH;

                for (int y = edgeInset; y < safeMaxY; y++)
                {
                    for (int x = edgeInset; x < safeMaxX; x++)
                    {
                        if (texture.GetPixel(cellX + x, cellY + y).a <= AlphaThreshold01)
                            continue;

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            if (maxX < minX || maxY < minY)
                return fallback;

            minX = Mathf.Max(edgeInset, minX - FrameCropPaddingPixels);
            minY = Mathf.Max(edgeInset, minY - FrameCropPaddingPixels);
            maxX = Mathf.Min(cellW - edgeInset - 1, maxX + FrameCropPaddingPixels);
            maxY = Mathf.Min(cellH - edgeInset - 1, maxY + FrameCropPaddingPixels);

            return new RectInt(minX, minY, Mathf.Max(1, maxX - minX + 1), Mathf.Max(1, maxY - minY + 1));
        }

        private static int CropCacheKey(Texture2D texture, int columns, int rows, int frameCount, int cellW, int cellH)
        {
            unchecked
            {
                int key = texture != null ? texture.GetInstanceID() : 0;
                key = (key * 397) ^ columns;
                key = (key * 397) ^ rows;
                key = (key * 397) ^ frameCount;
                key = (key * 397) ^ cellW;
                key = (key * 397) ^ cellH;
                return key;
            }
        }

        private Sprite[] GetFrames(CombatUnit2DAnimState state, CombatUnit2DStrip strip)
        {
            if (_frameCache.TryGetValue(state, out var cached))
                return cached;

            var frames = ResolveSharedFrames(strip);
            _frameCache[state] = frames;
            return frames;
        }

        private static Sprite[] ResolveSharedFrames(CombatUnit2DStrip strip)
        {
            int key = FrameCacheKey(strip);
            if (SharedFrameCache.TryGetValue(key, out var cached))
                return cached;

            var frames = SliceUnitStrip(strip);
            SharedFrameCache[key] = frames;
            return frames;
        }

        private static int FrameCacheKey(CombatUnit2DStrip strip)
        {
            unchecked
            {
                int key = strip.sheet != null ? strip.sheet.GetInstanceID() : 0;
                var texture = strip.sheet != null ? strip.sheet.texture : null;
                key = (key * 397) ^ (texture != null ? texture.GetInstanceID() : 0);
                key = (key * 397) ^ strip.frameCount;
                key = (key * 397) ^ strip.ColumnsOrDefault;
                key = (key * 397) ^ (texture != null ? texture.width : 0);
                key = (key * 397) ^ (texture != null ? texture.height : 0);
                return key;
            }
        }

        private CombatUnit2DStrip ResolveStrip(CombatUnit2DAnimState state)
        {
            if (_set == null)
                return default;

            return state switch
            {
                CombatUnit2DAnimState.Idle => _set.idle,
                CombatUnit2DAnimState.Walk => _set.walk,
                CombatUnit2DAnimState.Run => _set.run,
                CombatUnit2DAnimState.Hurt => _set.hurt,
                CombatUnit2DAnimState.HitReact => _set.hitReact,
                CombatUnit2DAnimState.Shoot => _set.shoot,
                CombatUnit2DAnimState.Die => _set.die,
                _ => default
            };
        }
    }
}
