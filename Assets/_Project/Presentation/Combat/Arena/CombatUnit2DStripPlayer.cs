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

        private CombatUnit2DAnimationSetSO _set;
        private CombatUnit2DAnimState _state = CombatUnit2DAnimState.Idle;
        private float _time;
        private bool _locked;
        private readonly Dictionary<CombatUnit2DAnimState, Sprite[]> _frameCache = new();

        public CombatUnit2DAnimState State => _state;
        public bool IsLocked => _locked;
        public float CurrentDurationSeconds => ResolveStrip(_state).DurationSeconds;

        public void Bind(CombatUnit2DAnimationSetSO set)
        {
            _set = set;
            _frameCache.Clear();
        }

        public void Play(CombatUnit2DAnimState state, bool restart = true)
        {
            if (_set == null)
                return;

            if (!ResolveStrip(state).IsValid)
            {
                if (state != CombatUnit2DAnimState.Idle)
                    Play(CombatUnit2DAnimState.Idle, restart);
                return;
            }

            if (restart || state != _state)
                _time = 0f;

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

            _time += deltaTime;
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

        internal static int ResolveFrameIndex(CombatUnit2DStrip strip, float timeSeconds)
        {
            if (strip.frameCount < 1)
                return 0;

            float duration = strip.DurationSeconds;
            if (duration <= 0f)
                return 0;

            float t = strip.loop ? timeSeconds % duration : Mathf.Min(timeSeconds, duration);
            int index = Mathf.FloorToInt(t * strip.framesPerSecond);
            return Mathf.Clamp(index, 0, strip.frameCount - 1);
        }

        /// <summary>Slice a horizontal strip into per-frame sprites with a shared bottom-center pivot.</summary>
        internal static Sprite[] SliceUnitStrip(CombatUnit2DStrip strip)
        {
            if (!strip.IsValid || strip.sheet.texture == null)
                return System.Array.Empty<Sprite>();

            var rect = strip.sheet.textureRect;
            float frameWidth = rect.width / strip.frameCount;
            var pivot = new Vector2(FramePivotX, FramePivotY);
            float ppu = strip.sheet.pixelsPerUnit;
            var frames = new Sprite[strip.frameCount];
            for (int i = 0; i < strip.frameCount; i++)
            {
                frames[i] = Sprite.Create(
                    strip.sheet.texture,
                    new Rect(rect.x + frameWidth * i, rect.y, frameWidth, rect.height),
                    pivot,
                    ppu);
            }

            return frames;
        }

        private Sprite[] GetFrames(CombatUnit2DAnimState state, CombatUnit2DStrip strip)
        {
            if (_frameCache.TryGetValue(state, out var cached))
                return cached;

            var frames = SliceUnitStrip(strip);
            _frameCache[state] = frames;
            return frames;
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
