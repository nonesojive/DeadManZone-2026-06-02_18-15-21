using System.Collections;
using System.Collections.Generic;
using DeadManZone.Data;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>2D arena VFX: arced tracers, sprite impacts, floating damage text.</summary>
    public sealed class CombatArena2DVfx : MonoBehaviour, ICombatArenaVfxPresenter
    {
        [SerializeField] private CombatArenaFreezeController freezeController;
        [SerializeField] private Color damageTextColor = new(1f, 0.42f, 0.28f, 1f);
        [SerializeField] private Color damageTextOutlineColor = new(0.08f, 0.04f, 0.02f, 0.95f);
        [SerializeField] private float damageTextOutlineWidth = 0.22f;
        [SerializeField] private float damageTextScale = 0.42f;
        [SerializeField] private float damageTextRise = 1.05f;
        [SerializeField] private float damageTextLifetime = 0.95f;

        private CombatArenaConfigSO _config;
        private readonly List<LineRenderer> _activeTracers = new();

        private void Awake()
        {
            if (freezeController == null)
                freezeController = GetComponent<CombatArenaFreezeController>();

            _config = CombatArenaBootstrap.Instance?.Config
                ?? Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
        }

        public void Configure(CombatArenaFreezeController controller)
        {
            if (controller != null)
                freezeController = controller;
        }

        public void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld)
        {
            SpawnMuzzleFlash(muzzleWorld, targetWorld, 0.34f);
            StartCoroutine(BulletTracerRoutine(muzzleWorld, targetWorld));
        }

        public void PlayCannonMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld)
        {
            SpawnMuzzleFlash(muzzleWorld, targetWorld, 0.6f);
            CombatArenaCameraShake.Kick(0.05f, 0.12f);
            StartCoroutine(ArcTracerRoutine(muzzleWorld, targetWorld, 0.35f, 0.2f));
        }

        /// <summary>Gas/environmental damage: hurt feedback and text only — no weapon cues.</summary>
        public void PlayEnvironmentalDamage(Vector3 targetWorld, int damageAmount)
        {
            PlayStrip(
                CombatArena2DVfxArt.RifleImpactFrames,
                targetWorld + Vector3.up * 0.35f,
                0.8f,
                0.3f);
            SpawnFloatingText(targetWorld + Vector3.up * 0.8f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
        }

        public void PlayImpact(Vector3 targetWorld, int damageAmount)
        {
            PlayStrip(
                CombatArena2DVfxArt.RifleImpactFrames,
                targetWorld + Vector3.up * 0.15f,
                1f,
                0.32f);
            SpawnFloatingText(targetWorld + Vector3.up * 0.8f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
        }

        public void PlayExplosion(Vector3 targetWorld, int damageAmount)
        {
            CombatArenaCameraShake.Kick(0.11f, 0.22f);
            PlayStrip(
                CombatArena2DVfxArt.ExplosionFrames,
                targetWorld + Vector3.up * 0.2f,
                1.15f,
                0.55f);
            SpawnFloatingText(targetWorld + Vector3.up * 0.9f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
        }

        public void PlayDeath(Vector3 worldPosition) =>
            PlayStrip(
                CombatArena2DVfxArt.DeathPuffFrames,
                worldPosition + Vector3.up * 0.12f,
                0.85f,
                0.45f);

        public void PlayDamage(Vector3 worldPosition, int amount)
        {
            PlayRifleMuzzleAndTracer(worldPosition, worldPosition);
            PlayImpact(worldPosition, amount);
        }

        /// <summary>A short bright streak that shoots straight from muzzle to target —
        /// reads as a bullet tracer, unlike the old lobbed arc. The head races along the
        /// firing line with a fixed-length tail trailing behind it.</summary>
        private IEnumerator BulletTracerRoutine(Vector3 from, Vector3 to)
        {
            var go = new GameObject("BulletTracer");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.numCapVertices = 2;
            line.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            line.startColor = new Color(1f, 0.96f, 0.7f, 1f);   // hot head
            line.endColor = new Color(1f, 0.7f, 0.25f, 0.25f);  // faded tail
            _activeTracers.Add(line);

            Vector3 dir = (to - from);
            float dist = dir.magnitude;
            dir = dist > 0.0001f ? dir / dist : Vector3.right;
            float tailLen = Mathf.Min(0.9f, dist * 0.5f);
            const float speed = 42f; // world units/sec — fast, bullet-like
            float travelled = 0f;

            while (travelled < dist)
            {
                travelled += speed * Time.deltaTime;
                Vector3 head = from + dir * Mathf.Min(travelled, dist);
                Vector3 tail = from + dir * Mathf.Max(0f, Mathf.Min(travelled, dist) - tailLen);
                line.SetPosition(0, tail);
                line.SetPosition(1, head);
                yield return null;
            }

            _activeTracers.Remove(line);
            Destroy(go);
        }

        private float ArcHeight =>
            _config != null && _config.projectileArcHeight > 0f ? _config.projectileArcHeight : 0.6f;

        private IEnumerator ArcTracerRoutine(Vector3 from, Vector3 to, float duration, float width)
        {
            var go = new GameObject("ArcTracer");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 12;
            line.startWidth = width;
            line.endWidth = width * 0.6f;
            line.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            line.startColor = new Color(1f, 0.92f, 0.55f, 0.95f);
            line.endColor = new Color(1f, 0.55f, 0.2f, 0.85f);
            _activeTracers.Add(line);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float headT = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = headT * (i / (float)(line.positionCount - 1));
                    line.SetPosition(i, CombatArena2DProjectileArc.Sample(from, to, t, ArcHeight));
                }

                yield return null;
            }

            _activeTracers.Remove(line);
            Destroy(go);
        }

        private void PlayStrip(Sprite[] frames, Vector3 worldPosition, float scale, float durationSeconds)
        {
            if (frames == null || frames.Length == 0)
            {
                SpawnImpactFlash(worldPosition, scale * 0.35f);
                return;
            }

            int sortOrder = CombatArena2DSortOrder.FromWorldZ(worldPosition.z) + 50;
            CombatArena2DVfxSpriteAnim.Play(this, frames, worldPosition, scale, durationSeconds, sortOrder);
        }

        /// <summary>Short two-stage flash at the barrel: bright core that pops then fades,
        /// stretched slightly toward the target so the shot direction reads. Mesh quad +
        /// additive material — SpriteRenderers do not draw with this renderer setup.</summary>
        private void SpawnMuzzleFlash(Vector3 muzzleWorld, Vector3 targetWorld, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "MuzzleFlash";
            go.transform.SetParent(transform, true);
            go.transform.position = muzzleWorld;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateSpriteAdditive(
                CombatArena2DPlaceholderSprites.WhitePixel,
                CombatArena2DSortOrder.RenderQueueFromWorldZ(muzzleWorld.z, 60));
            if (material != null)
            {
                CombatArenaMaterialUtility.ApplyColor(material, new Color(1f, 0.82f, 0.42f, 1f));
                renderer.sharedMaterial = material;
            }

            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (camera != null)
                go.transform.rotation = camera.transform.rotation;

            Vector3 toTarget = targetWorld - muzzleWorld;
            toTarget.y = 0f;
            float side = toTarget.x >= 0f ? 1f : -1f;
            go.transform.localScale = new Vector3(scale * 1.6f * side, scale * 0.55f, 1f);

            StartCoroutine(MuzzleFlashRoutine(go.transform, material));
        }

        private IEnumerator MuzzleFlashRoutine(Transform flash, Material material)
        {
            const float lifetime = 0.09f;
            Vector3 startScale = flash != null ? flash.localScale : Vector3.one;
            float elapsed = 0f;
            while (elapsed < lifetime && flash != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                flash.localScale = startScale * (1f + 0.6f * t);
                if (material != null)
                {
                    // Additive: fading toward black fades the glow out.
                    float fade = 1f - t * t;
                    CombatArenaMaterialUtility.ApplyColor(
                        material, new Color(1f * fade, 0.82f * fade, 0.42f * fade, 1f));
                }
                yield return null;
            }

            if (flash != null)
                Destroy(flash.gameObject);
        }

        private void SpawnImpactFlash(Vector3 worldPosition, float scale)
        {
            var go = new GameObject("ImpactFlash");
            go.transform.SetParent(transform, true);
            go.transform.position = worldPosition + Vector3.up * 0.15f;
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CombatArena2DPlaceholderSprites.WhitePixel;
            sr.color = new Color(1f, 0.75f, 0.35f, 0.9f);
            sr.sortingOrder = CombatArena2DSortOrder.FromWorldZ(worldPosition.z) + 50;
            Destroy(go, 0.2f);
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

            while (elapsed < damageTextLifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / damageTextLifetime);
                textTransform.position = startPosition + Vector3.up * (damageTextRise * t);
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
