using System.Collections;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaVfx : MonoBehaviour
    {
        private const string DefaultImpactPrefabPath =
            "Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_01.prefab";
        private const string DefaultDeathPrefabPath =
            "Assets/Synty/PolygonParticleFX/Prefabs/FX_Dust_Small_01.prefab";

        [SerializeField] private CombatArenaFreezeController freezeController;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField] private ParticleSystem deathPrefab;
        [SerializeField] private Color damageTextColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float damageTextScale = 0.35f;
        [SerializeField] private float damageTextRise = 0.85f;
        [SerializeField] private float damageTextLifetime = 0.8f;

        private void Awake()
        {
            if (freezeController == null)
                freezeController = GetComponent<CombatArenaFreezeController>();

            EnsureDefaultPrefabs();
        }

        private void EnsureDefaultPrefabs()
        {
            if (impactPrefab == null)
                impactPrefab = SyntyRuntimeAssetLoader.LoadParticleSystem(DefaultImpactPrefabPath);

            if (deathPrefab == null)
                deathPrefab = SyntyRuntimeAssetLoader.LoadParticleSystem(DefaultDeathPrefabPath);
        }

        public void Configure(CombatArenaFreezeController controller)
        {
            if (controller != null)
                freezeController = controller;
        }

        public void PlayDamage(Vector3 worldPosition, int amount)
        {
            SpawnBurst(impactPrefab, worldPosition);

            string label = amount > 0 ? $"-{amount}" : amount.ToString();
            SpawnFloatingText(worldPosition + Vector3.up * 1.1f, label);
        }

        public void PlayDeath(Vector3 worldPosition) => SpawnBurst(deathPrefab, worldPosition);

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

        private void SpawnFloatingText(Vector3 worldPosition, string text)
        {
            var cameraTransform = CombatArenaBootstrap.Instance?.ArenaCamera?.transform;
            var go = new GameObject("CombatDamageText");
            go.transform.SetParent(transform, true);
            go.transform.position = worldPosition;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = damageTextColor;
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

            while (elapsed < damageTextLifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / damageTextLifetime);

                textTransform.position = startPosition + Vector3.up * (damageTextRise * t);
                if (cameraTransform != null)
                    textTransform.rotation = cameraTransform.rotation;

                var color = startColor;
                color.a = 1f - t;
                text.color = color;
                yield return null;
            }

            if (text != null)
                Destroy(text.gameObject);
        }
    }
}
