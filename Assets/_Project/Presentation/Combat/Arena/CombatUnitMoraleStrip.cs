using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Thin flat ground strip at the near (camera) edge of a unit's side ring showing its
    /// combat Morale (ADR-0005). Built lazily and only for units that can break
    /// (Definition.MaxMorale &gt; 0) — morale-immune units show nothing new. Deliberately
    /// NOT the side channel: desaturated bone/amber (side blue/red stay reserved for the
    /// rings and army bars), with a brief bone pulse when morale damage lands — the cheap
    /// stand-in for a floating number channel this arena doesn't have. Presentation only:
    /// fractions are pushed in from the arena presenter's replay bookkeeping.
    /// </summary>
    public sealed class CombatUnitMoraleStrip : MonoBehaviour
    {
        // Desaturated amber, nowhere near ring blue (0.30,0.42,0.60) or red (0.60,0.26,0.22).
        private static readonly Color FillAmber = new(0.62f, 0.54f, 0.38f, 1f);
        private static readonly Color BackDark = new(0.10f, 0.09f, 0.075f, 1f);

        private const float DrainPerSecond = 1.4f; // same drain feel as the HP ring
        private const float PulseSeconds = 0.25f;
        private const float StripWidth = 0.78f;
        private const float StripDepth = 0.09f;
        private const float GroundY = 0.035f;      // above the ring quad (0.02) — no z-fight
        private const float NearEdgeOffset = -0.58f; // toward the gameplay camera (-Z)

        private Transform _fillPivot;
        private Material _backMaterial;
        private Material _fillMaterial;
        private float _target = 1f;
        private float _displayed = 1f;
        private float _pulseUntil = float.NegativeInfinity;

        /// <summary>Build the strip flat on the ground in front of the side ring.
        /// <paramref name="widthScale"/> follows the ring scale (vehicles use 1.35).</summary>
        public static CombatUnitMoraleStrip Attach(Transform parent, float widthScale)
        {
            var go = new GameObject("MoraleStrip");
            go.transform.SetParent(parent, false);
            var strip = go.AddComponent<CombatUnitMoraleStrip>();
            strip.Build(Mathf.Max(1f, widthScale));
            return strip;
        }

        public void SetFraction(float fraction)
        {
            float clamped = Mathf.Clamp01(fraction);
            if (clamped < _target - 0.0001f)
                _pulseUntil = Time.time + PulseSeconds; // morale hit → bone flash on the fill
            _target = clamped;
        }

        private void Build(float widthScale)
        {
            float width = StripWidth * widthScale;
            float nearZ = NearEdgeOffset * widthScale;

            _backMaterial = CreateUnlitMaterial(BackDark);
            var back = CreateFlatQuad("Back", _backMaterial, transform);
            back.transform.localPosition = new Vector3(0f, GroundY, nearZ);
            back.transform.localScale = new Vector3(width, StripDepth, 1f);

            // Left-anchored fill: the pivot sits on the strip's left edge, the quad hangs
            // half a width to its right — scaling the pivot's X drains the bar rightward.
            var pivotGo = new GameObject("FillPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
            _fillPivot = pivotGo.transform;

            _fillMaterial = CreateUnlitMaterial(FillAmber);
            var fill = CreateFlatQuad("Fill", _fillMaterial, _fillPivot);
            fill.transform.localPosition = new Vector3(width * 0.5f, GroundY + 0.005f, nearZ);
            fill.transform.localScale = new Vector3(width - 0.02f, StripDepth - 0.02f, 1f);
        }

        private void Update()
        {
            bool pulsing = Time.time < _pulseUntil;
            if (pulsing && _fillMaterial != null)
            {
                // Pulse toward bone-white then settle back — reads as "nerve damage",
                // clearly not the white _HitFlash the HP channel uses on the model.
                float p = Mathf.Clamp01((_pulseUntil - Time.time) / PulseSeconds);
                ApplyFillColor(Color.Lerp(FillAmber, CombatGrimdarkSkin.Bone, p * 0.85f));
            }
            else if (_fillMaterial != null && Time.time - _pulseUntil < 0.5f)
            {
                ApplyFillColor(FillAmber);
            }

            if (Mathf.Approximately(_displayed, _target))
                return;

            _displayed = Mathf.MoveTowards(_displayed, _target, DrainPerSecond * Time.deltaTime);
            if (_fillPivot != null)
            {
                var scale = _fillPivot.localScale;
                scale.x = _displayed;
                _fillPivot.localScale = scale;
            }
        }

        private void OnDestroy()
        {
            if (_backMaterial != null)
                Destroy(_backMaterial);
            if (_fillMaterial != null)
                Destroy(_fillMaterial);
        }

        private void ApplyFillColor(Color color)
        {
            if (_fillMaterial.HasProperty("_BaseColor"))
                _fillMaterial.SetColor("_BaseColor", color);
            _fillMaterial.color = color;
        }

        private static GameObject CreateFlatQuad(string name, Material material, Transform parent)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat, facing up

            var quadRenderer = quad.GetComponent<MeshRenderer>();
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            if (material != null)
                quadRenderer.sharedMaterial = material;
            return quad;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            // Same shader ladder as the arena's other runtime unlit surfaces.
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = "MoraleStrip",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            material.color = color;
            return material;
        }
    }
}
