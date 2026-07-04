using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Brief decaying positional shake layered on the arena camera.
    /// Offsets are applied in LateUpdate and fully removed when idle, so the
    /// orthographic framer keeps owning the base camera pose.</summary>
    public sealed class CombatArenaCameraShake : MonoBehaviour
    {
        private const float Frequency = 34f;

        private Vector3 _appliedOffset;
        private float _amplitude;
        private float _timeLeft;
        private float _duration;
        private float _seedX;
        private float _seedY;

        public static void Kick(float amplitude, float duration)
        {
            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (camera == null)
                return;

            var shake = camera.GetComponent<CombatArenaCameraShake>();
            if (shake == null)
                shake = camera.gameObject.AddComponent<CombatArenaCameraShake>();

            shake.Play(amplitude, duration);
        }

        public void Play(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f)
                return;

            // Stronger of current/incoming wins; overlapping volleys don't stack to nausea.
            if (amplitude >= _amplitude || _timeLeft <= 0f)
            {
                _amplitude = amplitude;
                _duration = duration;
                _timeLeft = duration;
                _seedX = Random.value * 100f;
                _seedY = Random.value * 100f;
            }
        }

        private void LateUpdate()
        {
            // Remove last frame's offset first so the framer's pose stays authoritative.
            transform.position -= _appliedOffset;
            _appliedOffset = Vector3.zero;

            if (_timeLeft <= 0f)
                return;

            _timeLeft -= Time.deltaTime;
            float falloff = _duration > 0f ? Mathf.Clamp01(_timeLeft / _duration) : 0f;
            float strength = _amplitude * falloff * falloff;

            float x = (Mathf.PerlinNoise(_seedX, Time.time * Frequency) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seedY, Time.time * Frequency) - 0.5f) * 2f;

            _appliedOffset = transform.right * (x * strength) + transform.up * (y * strength);
            transform.position += _appliedOffset;

            if (_timeLeft <= 0f)
                _amplitude = 0f;
        }
    }
}
