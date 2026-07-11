using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Placeholder 3D combat VFX behind the <see cref="ICombatArenaVfxPresenter"/> seam:
    /// brief additive muzzle flashes (camera-facing quad stretched toward the target + a
    /// point-light pop) on attack events, smaller pops on impacts/explosions. Everything is
    /// generated in code (radial texture, URP Unlit additive material, pooled quad+light
    /// rigs) — no asset dependencies, no per-shot allocation after pool warm-up. Damage
    /// numbers / death dust stay no-ops here (the unit shader hit-flash and dissolve own
    /// those channels until the real VFX saturation pass).
    /// </summary>
    public sealed class Combat3DVfxPresenter : MonoBehaviour, ICombatArenaVfxPresenter
    {
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private Color flashColor = new(1f, 0.85f, 0.55f);
        [SerializeField, Range(0.02f, 0.3f)] private float muzzleFlashSeconds = 0.07f;
        [SerializeField] private float muzzleFlashWorldSize = 0.55f;
        [SerializeField] private float flashLightIntensity = 2.6f;
        [SerializeField] private float flashLightRange = 4f;
        [SerializeField] private int initialPoolSize = 8;

        private sealed class FlashInstance
        {
            public GameObject Root;
            public Transform Transform;
            public Light Light;
            public Vector3 BaseScale;
            public float MaxLightIntensity;
            public float EndTime;
            public float Duration;
            public bool Active;
        }

        private readonly List<FlashInstance> _pool = new();
        private Material _material;
        private Texture2D _texture;
        private Transform _poolRoot;

        private void Awake()
        {
            _texture = CreateRadialTexture();
            _material = CreateAdditiveMaterial(_texture, flashColor);

            var rootGo = new GameObject("MuzzleFlashPool");
            rootGo.transform.SetParent(transform, false);
            _poolRoot = rootGo.transform;

            for (int i = 0; i < initialPoolSize; i++)
                _pool.Add(CreateFlash());
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
            if (_texture != null)
                Destroy(_texture);
        }

        public void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld) =>
            Spawn(muzzleWorld, targetWorld, sizeScale: 1f, muzzleFlashSeconds);

        public void PlayCannonMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld) =>
            Spawn(muzzleWorld, targetWorld, sizeScale: 1.9f, muzzleFlashSeconds * 1.6f);

        public void PlayImpact(Vector3 targetWorld, int damageAmount) =>
            Spawn(targetWorld + Vector3.up * 0.6f, aimTarget: null, sizeScale: 0.65f, muzzleFlashSeconds);

        public void PlayExplosion(Vector3 targetWorld, int damageAmount) =>
            Spawn(targetWorld + Vector3.up * 0.4f, aimTarget: null, sizeScale: 2.2f, muzzleFlashSeconds * 1.8f);

        // Unit-body feedback channels (hit flash, dissolve) live on the toon-ink shader;
        // gas/death/number pops wait for the real VFX pass.
        public void PlayDeath(Vector3 worldPosition) { }
        public void PlayDamage(Vector3 worldPosition, int amount) { }
        public void PlayEnvironmentalDamage(Vector3 targetWorld, int damageAmount) { }

        private void Spawn(Vector3 world, Vector3? aimTarget, float sizeScale, float duration)
        {
            var flash = Rent();
            if (flash == null)
                return;

            Vector3 cameraForward = ResolveCameraForward();

            Quaternion rotation;
            Vector3 scale;
            if (aimTarget.HasValue)
            {
                // Camera-facing quad, rolled so its long axis points at the target and
                // nudged toward it, reading as a directional muzzle flash.
                Vector3 toTarget = aimTarget.Value - world;
                Vector3 flat = Vector3.ProjectOnPlane(toTarget, cameraForward);
                rotation = flat.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, flat.normalized))
                    : Quaternion.LookRotation(cameraForward);
                scale = new Vector3(1.8f, 0.9f, 1f) * (muzzleFlashWorldSize * sizeScale);
                if (toTarget.sqrMagnitude > 0.0001f)
                    world += toTarget.normalized * 0.12f;
            }
            else
            {
                rotation = Quaternion.LookRotation(cameraForward)
                           * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                scale = Vector3.one * (muzzleFlashWorldSize * sizeScale);
            }

            flash.Transform.SetPositionAndRotation(world, rotation);
            flash.BaseScale = scale;
            flash.Transform.localScale = scale;
            flash.MaxLightIntensity = flashLightIntensity * Mathf.Clamp(sizeScale, 0.5f, 2.5f);
            flash.Light.intensity = flash.MaxLightIntensity;
            flash.Light.range = flashLightRange * Mathf.Clamp(sizeScale, 0.6f, 2f);
            flash.Duration = Mathf.Max(0.02f, duration);
            flash.EndTime = Time.time + flash.Duration;
            flash.Active = true;
            flash.Root.SetActive(true);
        }

        private void Update()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var flash = _pool[i];
                if (!flash.Active)
                    continue;

                float remaining01 = (flash.EndTime - Time.time) / flash.Duration;
                if (remaining01 <= 0f)
                {
                    flash.Active = false;
                    flash.Root.SetActive(false);
                    continue;
                }

                flash.Transform.localScale = flash.BaseScale * (0.65f + 0.35f * remaining01);
                flash.Light.intensity = flash.MaxLightIntensity * remaining01;
            }
        }

        private FlashInstance Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Active)
                    return _pool[i];
            }

            var grown = CreateFlash();
            _pool.Add(grown);
            return grown;
        }

        private FlashInstance CreateFlash()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "MuzzleFlash";
            go.transform.SetParent(_poolRoot, false);
            Destroy(go.GetComponent<Collider>());

            var quadRenderer = go.GetComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = _material;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            var lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(go.transform, false);
            var flashLight = lightGo.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = flashColor;
            flashLight.shadows = LightShadows.None;

            go.SetActive(false);
            return new FlashInstance
            {
                Root = go,
                Transform = go.transform,
                Light = flashLight
            };
        }

        private Vector3 ResolveCameraForward()
        {
            if (arenaCamera == null)
                arenaCamera = CombatArenaBootstrap.Instance?.ArenaCamera;

            return arenaCamera != null ? arenaCamera.transform.forward : Vector3.forward;
        }

        /// <summary>Soft radial falloff so the flash reads as a glow, not a hard-edged square.
        /// Falloff baked into RGB — with One/One additive blending, RGB is the contribution.</summary>
        private static Texture2D CreateRadialTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Combat3DFlashRadial",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float falloff = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 2.2f);
                    byte value = (byte)Mathf.RoundToInt(falloff * 255f);
                    pixels[y * size + x] = new Color32(value, value, value, value);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Material CreateAdditiveMaterial(Texture2D texture, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            var material = new Material(shader)
            {
                name = "Combat3DMuzzleFlashAdditive",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = texture
            };

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            // Over-unity color so HDR bloom in the grade picks the flash up (spec §6:
            // VFX own the saturation budget / brightest elements on screen).
            var hdrColor = color * 2f;
            hdrColor.a = 1f;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", hdrColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", hdrColor);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f); // transparent
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.One);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent + 100;
            return material;
        }
    }
}
