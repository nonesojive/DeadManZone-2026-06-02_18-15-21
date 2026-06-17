using System.Collections;
using DeadManZone.Data;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaVfx : MonoBehaviour
    {
        [SerializeField] private CombatArenaFreezeController freezeController;
        [SerializeField] private CombatArenaVfxSetSO vfxSet;
        [SerializeField] private Color damageTextColor = new(1f, 0.42f, 0.28f, 1f);
        [SerializeField] private Color damageTextOutlineColor = new(0.08f, 0.04f, 0.02f, 0.95f);
        [SerializeField] private float damageTextOutlineWidth = 0.22f;
        [SerializeField] private float damageTextScale = 0.42f;
        [SerializeField] private float damageTextRise = 1.05f;
        [SerializeField] private float damageTextLifetime = 0.95f;
        [SerializeField] private float damageTextPopScale = 1.18f;

        private void Awake()
        {
            if (freezeController == null)
                freezeController = GetComponent<CombatArenaFreezeController>();

            if (vfxSet == null)
                vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
        }

        public void Configure(CombatArenaFreezeController controller)
        {
            if (controller != null)
                freezeController = controller;
        }

        public void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld)
        {
            SpawnBurst(vfxSet?.rifleMuzzle, muzzleWorld);
            SpawnBurst(vfxSet?.rifleMuzzleSmoke, muzzleWorld);
            SpawnTracer(muzzleWorld, targetWorld);
        }

        public void PlayImpact(Vector3 targetWorld, int damageAmount)
        {
            SpawnBurst(vfxSet?.rifleImpact, targetWorld);
            SpawnFloatingText(
                targetWorld + Vector3.up * 1.1f,
                damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
        }

        public void PlayExplosion(Vector3 targetWorld, int damageAmount)
        {
            SpawnBurst(vfxSet?.explosionSmall, targetWorld);
            SpawnFloatingText(
                targetWorld + Vector3.up * 1.1f,
                damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
        }

        public void PlayCannonMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld)
        {
            SpawnBurst(vfxSet?.cannonShot, muzzleWorld);
            SpawnTracer(muzzleWorld, targetWorld);
        }

        public void PlayDeath(Vector3 worldPosition)
        {
            SpawnBurst(vfxSet?.deathBurst, worldPosition);
            SpawnBurst(vfxSet?.deathSmoke, worldPosition);
        }

        /// <summary>
        /// Legacy single-call damage VFX. Prefer timed PlayRifleMuzzleAndTracer + PlayImpact.
        /// </summary>
        public void PlayDamage(Vector3 worldPosition, int amount)
        {
            PlayRifleMuzzleAndTracer(worldPosition, worldPosition);
            PlayImpact(worldPosition, amount);
        }

        private void SpawnBurst(ParticleSystem prefab, Vector3 worldPosition)
        {
            if (prefab == null)
                return;

            var particle = Instantiate(prefab, worldPosition, Quaternion.identity, transform);
            particle.Play();
            freezeController?.TrackParticle(particle);

            float lifetime = particle.main.duration + particle.main.startLifetime.constantMax + 0.1f;
            Destroy(particle.gameObject, Mathf.Max(lifetime, 0.5f));
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (vfxSet?.bulletTracer == null)
                return;

            Vector3 direction = to - from;
            if (direction.sqrMagnitude < 0.001f)
                return;

            var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var particle = Instantiate(vfxSet.bulletTracer, from, rotation, transform);
            particle.transform.localScale = Vector3.one * direction.magnitude;
            particle.Play();
            freezeController?.TrackParticle(particle);

            float lifetime = particle.main.duration + particle.main.startLifetime.constantMax + 0.1f;
            Destroy(particle.gameObject, Mathf.Max(lifetime, 0.5f));
        }

        private void SpawnFloatingText(Vector3 worldPosition, string text)
        {
            var cameraTransform = CombatArenaBootstrap.Instance?.ArenaCamera?.transform;
            var go = new GameObject("CombatDamageText");
            go.transform.SetParent(transform, true);
            go.transform.position = worldPosition;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = damageTextColor;
            tmp.outlineColor = damageTextOutlineColor;
            tmp.outlineWidth = damageTextOutlineWidth;
            tmp.transform.localScale = Vector3.one * damageTextScale;

            StartCoroutine(AnimateFloatingText(tmp, worldPosition, cameraTransform));
        }

        private IEnumerator AnimateFloatingText(TextMeshPro text, Vector3 startPosition, Transform cameraTransform)
        {
            if (text == null)
                yield break;

            float elapsed = 0f;
            Color startColor = text.color;
            Transform textTransform = text.transform;
            float baseScale = damageTextScale;
            float popDuration = Mathf.Min(0.18f, damageTextLifetime * 0.25f);

            while (elapsed < damageTextLifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / damageTextLifetime);
                float popT = popDuration > 0f ? Mathf.Clamp01(elapsed / popDuration) : 1f;
                float scalePulse = Mathf.Lerp(damageTextPopScale, 1f, popT);

                textTransform.position = startPosition + Vector3.up * (damageTextRise * t);
                textTransform.localScale = Vector3.one * (baseScale * scalePulse);
                if (cameraTransform != null)
                    textTransform.rotation = cameraTransform.rotation;

                var color = startColor;
                color.a = 1f - Mathf.SmoothStep(0.35f, 1f, t);
                text.color = color;
                yield return null;
            }

            if (text != null)
                Destroy(text.gameObject);
        }
    }
}
