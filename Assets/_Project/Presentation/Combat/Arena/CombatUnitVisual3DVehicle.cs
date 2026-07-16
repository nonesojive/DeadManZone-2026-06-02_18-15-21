using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Non-humanoid 3D unit presentation (tanks / transports / emplacements) behind
    /// <see cref="ICombatUnitVisual"/> — the owner-specced vehicle treatment: a single
    /// static Meshy mesh (no rig, no clips), code-driven motion instead of animation.
    /// Movement = engine-rumble bob + slight pitch rock while marching; attack = recoil
    /// kick with the muzzle at the mesh's bounds-front weapon line (no rifle prop, no
    /// aim/IK layers); death = collapse/settle + the shared _DissolveAmount ramp; hit
    /// feedback = the shared _HitFlash pulse; health = the same side-ring orb drain as
    /// the humanoid visual. No game rules live here.
    /// </summary>
    public sealed class CombatUnitVisual3DVehicle : MonoBehaviour, ICombatUnitVisual
    {
        private const float HitFlashPeak = 0.45f;
        private const float HitFlashSeconds = 0.12f;
        private const float DissolveSeconds = 0.8f;
        private const float WreckLingerSeconds = 0.6f;
        private const float CollapseSeconds = 0.9f;
        // Rout (crew abandons the vehicle, ADR-0005): the death collapse adapted — a
        // shallower settle, a short beat, then a slower, softer dissolve. No wreck read.
        private const float AbandonSinkFraction = 0.10f; // vs the 0.22 death sink
        private const float AbandonLingerSeconds = 0.25f;
        private const float RoutDissolveSeconds = 1.2f;
        private const float TurnDegreesPerSecond = 240f; // vehicles turn slower than infantry
        private const float RecoilDistanceMeters = 0.09f;
        private const float RecoilOutSeconds = 0.05f;
        private const float RecoilSettleSeconds = 0.25f;
        private const float RumbleAmplitudeMeters = 0.03f;
        private const float RumbleHertz = 5.5f;
        private const float RockDegrees = 1.4f;
        private const float RingDrainPerSecond = 1.4f;
        private const float RingScale = 1.35f; // wider disc under a wider silhouette

        private static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int RingFillId = Shader.PropertyToID("_Fill");
        private static readonly int RingGutterId = Shader.PropertyToID("_Gutter");

        // Phase 0 verdict 2 — same subtle cap as the humanoid visual (CombatUnitVisual3D).
        private const float MoraleGutterMaxIntensity = 0.35f;

        private Transform _modelRoot;
        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _mpb;
        private GameObject _ring;
        private Renderer _ringRenderer;
        private MaterialPropertyBlock _ringMpb;
        private CombatUnitMoraleStrip _moraleStrip;
        private float _ringTargetFill = 1f;
        private float _ringDisplayedFill = 1f;
        private float _ringGutter;

        private float _visualHeight = 2.2f;
        private float _yawOffsetDegrees;
        private Bounds _localBounds; // model-root space, measured once at build
        private Quaternion _targetRotation = Quaternion.identity;
        private Quaternion _currentYaw = Quaternion.identity; // eased; rock composes on top
        private Vector3 _lastFacingDir = Vector3.forward;
        private bool _walking;
        private bool _dying;
        private float _locomotionLockUntil;
        private float _rumblePhase;
        private float _recoilStartTime = float.NegativeInfinity;
        private Coroutine _attackRoutine;
        private Coroutine _hurtRoutine;
        private Coroutine _deathRoutine;

        public bool IsBuilt => _modelRoot != null;

        /// <inheritdoc/>
        public float VisualHeight => _visualHeight;

        /// <inheritdoc/>
        public float DeathSeconds => CollapseSeconds + WreckLingerSeconds + DissolveSeconds;

        /// <inheritdoc/>
        public bool BlocksLocomotion => _dying || Time.time < _locomotionLockUntil;

        /// <inheritdoc/>
        public bool DisplaysHealth => _ringRenderer != null;

        /// <inheritdoc/>
        public void SetHealthFraction(float fraction) =>
            _ringTargetFill = Mathf.Clamp01(fraction);

        /// <summary>Vehicles do NOT run when broken — the crew abandons them in place.</summary>
        public bool FleesWhenBroken => false;

        /// <inheritdoc/>
        public void SetMoraleFraction(float fraction)
        {
            // Lazily mirrors the side-ring machinery; only breakable units get this call.
            if (_moraleStrip == null && _modelRoot != null)
                _moraleStrip = CombatUnitMoraleStrip.Attach(transform, RingScale);

            _moraleStrip?.SetFraction(fraction);

            // Ring's _Gutter: achromatic rim flicker, driven by MPB same as _Fill (no
            // per-unit material instances) — mirrors CombatUnitVisual3D's humanoid path.
            _ringGutter = (1f - Mathf.Clamp01(fraction)) * MoraleGutterMaxIntensity;
            ApplyRingFill();
        }

        /// <inheritdoc/>
        public void PlayRoutExit(Action onComplete)
        {
            if (_dying)
            {
                onComplete?.Invoke();
                return;
            }

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = null;
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _hurtRoutine = null;

            // Reuse the death-collapse state gates (no locomotion, no new one-shots),
            // but the exit itself is the softer slump-abandon, not the wreck.
            _dying = true;
            _walking = false;
            _locomotionLockUntil = float.MaxValue;
            if (_ring != null)
                _ring.SetActive(false);
            if (_moraleStrip != null)
                _moraleStrip.gameObject.SetActive(false);
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(RoutExitRoutine(onComplete));
        }

        /// <summary>Slump-abandon: a shallow settle (crew bails, suspension sighs), a short
        /// beat, then a slower soft dissolve — no list-to-one-side wreck read, no VFX hooks.</summary>
        private IEnumerator RoutExitRoutine(Action onComplete)
        {
            float sink = _visualHeight * AbandonSinkFraction;
            var startPos = _modelRoot != null ? _modelRoot.localPosition : Vector3.zero;

            float settleSeconds = CollapseSeconds * 0.6f;
            for (float t = 0f; t < settleSeconds && _modelRoot != null; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / settleSeconds);
                _modelRoot.localPosition = startPos + Vector3.down * (sink * p);
                yield return null;
            }

            yield return new WaitForSeconds(AbandonLingerSeconds);

            for (float t = 0f; t < RoutDissolveSeconds; t += Time.deltaTime)
            {
                ApplyStatus(hitFlash: 0f, dissolve: Mathf.Clamp01(t / RoutDissolveSeconds));
                yield return null;
            }

            ApplyStatus(hitFlash: 0f, dissolve: 1f);
            _deathRoutine = null;
            onComplete?.Invoke();
        }

        /// <summary>Instantiates the static mesh under this actor at the archetype's target
        /// height (vehicles are NOT 1.7 m infantry — pass their authored silhouette height),
        /// applies the toon-ink side material, and drops the side ring.</summary>
        public void Build(
            GameObject modelSource,
            Material unitMaterial,
            Material ringMaterial,
            float targetHeight,
            float yawOffsetDegrees)
        {
            Clear();

            if (modelSource == null)
                return;

            _visualHeight = Mathf.Max(0.5f, targetHeight);

            var rootGo = new GameObject("VehicleModelRoot3D");
            rootGo.transform.SetParent(transform, false);
            _modelRoot = rootGo.transform;
            _yawOffsetDegrees = yawOffsetDegrees;
            _targetRotation = _modelRoot.rotation * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
            _currentYaw = _targetRotation;

            var instance = Instantiate(modelSource, _modelRoot);
            instance.name = "VehicleModel";
            CombatArenaVisualPlacement.PlaceOnGround(
                instance.transform, transform.position, _visualHeight, 1f);

            _renderers.Clear();
            instance.GetComponentsInChildren(true, _renderers);
            if (unitMaterial != null)
            {
                foreach (var unitRenderer in _renderers)
                {
                    var materials = unitRenderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = unitMaterial;
                    unitRenderer.sharedMaterials = materials;
                }
            }

            _localBounds = MeasureLocalBounds();

            _mpb ??= new MaterialPropertyBlock();
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            BuildSideRing(ringMaterial);
        }

        /// <inheritdoc/>
        public void SetWalking(bool walking)
        {
            if (_dying)
                return;

            _walking = walking;
        }

        /// <summary>Spawn facing snaps (see CombatUnitVisual3D._hasFacing — armies were
        /// swiveling from screen-north at fight start).</summary>
        private bool _hasFacing;

        /// <inheritdoc/>
        public void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            _lastFacingDir = worldDirection.normalized;
            _targetRotation = Quaternion.LookRotation(_lastFacingDir, Vector3.up)
                              * Quaternion.Euler(0f, _yawOffsetDegrees, 0f);

            if (!_hasFacing)
            {
                _currentYaw = _targetRotation;
                _hasFacing = true;
            }
        }

        /// <inheritdoc/>
        public void UpdateSortAndBob(Vector3 worldPosition)
        {
            if (_modelRoot == null || _dying)
                return;

            _currentYaw = Quaternion.RotateTowards(
                _currentYaw, _targetRotation, TurnDegreesPerSecond * Time.deltaTime);
        }

        /// <inheritdoc/>
        public void PlayAttack(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (_dying)
                return;

            FaceDirection(targetWorld - transform.position);
            _locomotionLockUntil = Time.time + Mathf.Max(0.05f, profile.TotalDurationSeconds);

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackTimingRoutine(profile, onMuzzle, onImpact));
        }

        /// <inheritdoc/>
        public void PlayHurt()
        {
            if (_dying || _renderers.Count == 0)
                return;

            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _hurtRoutine = StartCoroutine(HurtFlashRoutine());
        }

        /// <inheritdoc/>
        public void PlayDeath(Action onComplete)
        {
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = null;
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _hurtRoutine = null;

            _dying = true;
            _walking = false;
            _locomotionLockUntil = float.MaxValue;
            _ringTargetFill = 0f;
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(DeathRoutine(onComplete));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _attackRoutine = null;
            _hurtRoutine = null;
            _deathRoutine = null;

            if (_modelRoot != null)
                Destroy(_modelRoot.gameObject);
            if (_ring != null)
                Destroy(_ring);
            if (_moraleStrip != null)
                Destroy(_moraleStrip.gameObject);

            _modelRoot = null;
            _ring = null;
            _ringRenderer = null;
            _moraleStrip = null;
            _ringTargetFill = 1f;
            _ringDisplayedFill = 1f;
            _ringGutter = 0f;
            _renderers.Clear();
            _walking = false;
            _dying = false;
            _locomotionLockUntil = 0f;
            _recoilStartTime = float.NegativeInfinity;
            _hasFacing = false; // pooled reuse: next spawn's first facing snaps again
        }

        // ------------------------------------------------------------- motion & attack

        /// <summary>Engine rumble while marching + recoil kick, applied as model-root local
        /// offsets each frame (recomputed from zero, so nothing accumulates).</summary>
        private void LateUpdate()
        {
            if (_modelRoot == null || _dying)
                return;

            Vector3 offset = Vector3.zero;
            Quaternion rock = Quaternion.identity;

            if (_walking)
            {
                _rumblePhase += Time.deltaTime * RumbleHertz * Mathf.PI * 2f;
                offset.y = Mathf.Abs(Mathf.Sin(_rumblePhase)) * RumbleAmplitudeMeters;
                rock = Quaternion.Euler(Mathf.Sin(_rumblePhase * 0.5f) * RockDegrees, 0f, 0f);
            }

            float sinceRecoil = Time.time - _recoilStartTime;
            if (sinceRecoil >= 0f && sinceRecoil < RecoilOutSeconds + RecoilSettleSeconds)
            {
                float kick01 = sinceRecoil < RecoilOutSeconds
                    ? sinceRecoil / RecoilOutSeconds
                    : 1f - Mathf.Clamp01((sinceRecoil - RecoilOutSeconds) / RecoilSettleSeconds);
                offset += transform.InverseTransformDirection(-_lastFacingDir)
                          * (RecoilDistanceMeters * kick01);
            }

            // Absolute per-frame pose recomputed from eased yaw + phase — nothing accumulates.
            // (The actor root never rotates, so local rotation IS the world facing.)
            _modelRoot.localPosition = offset;
            _modelRoot.localRotation = _currentYaw * rock;
        }

        private IEnumerator AttackTimingRoutine(
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            _recoilStartTime = Time.time; // kick syncs to the flash
            onMuzzle?.Invoke(ComputeMuzzleWorld());

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            onImpact?.Invoke();
            _attackRoutine = null;
        }

        /// <summary>Built-in weapon line: the front-center of the measured hull at ~70%
        /// height, pushed along the current facing — no per-archetype muzzle authoring,
        /// correct for side cannons and roof guns alike at gameplay distance.</summary>
        private Vector3 ComputeMuzzleWorld()
        {
            if (_modelRoot == null)
                return transform.position + Vector3.up * (_visualHeight * 0.7f);

            float frontExtent = Mathf.Max(_localBounds.extents.x, _localBounds.extents.z);
            return transform.position
                   + Vector3.up * (_visualHeight * 0.7f)
                   + _lastFacingDir * frontExtent;
        }

        private Bounds MeasureLocalBounds()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            bool first = true;
            foreach (var meshRenderer in _renderers)
            {
                if (meshRenderer == null)
                    continue;

                var local = _modelRoot.InverseTransformPoint(meshRenderer.bounds.center);
                var size = _modelRoot.InverseTransformVector(meshRenderer.bounds.size);
                var b = new Bounds(local, new Vector3(
                    Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
                if (first)
                {
                    bounds = b;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(b);
                }
            }

            return bounds;
        }

        // ---------------------------------------------------------------- status & death

        private IEnumerator HurtFlashRoutine()
        {
            for (float t = 0f; t < HitFlashSeconds; t += Time.deltaTime)
            {
                float flash = HitFlashPeak * (1f - Mathf.Clamp01(t / HitFlashSeconds));
                ApplyStatus(flash, dissolve: 0f);
                yield return null;
            }

            ApplyStatus(hitFlash: 0f, dissolve: 0f);
            _hurtRoutine = null;
        }

        /// <summary>Vehicle death: no die clip — the wreck settles into the ground with a
        /// small list to one side, lingers a beat, then dissolves like every other unit.</summary>
        private IEnumerator DeathRoutine(Action onComplete)
        {
            float sink = _visualHeight * 0.22f;
            float listDegrees = 4f;
            var startPos = _modelRoot != null ? _modelRoot.localPosition : Vector3.zero;
            var startRot = _modelRoot != null ? _modelRoot.localRotation : Quaternion.identity;

            for (float t = 0f; t < CollapseSeconds && _modelRoot != null; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / CollapseSeconds);
                _modelRoot.localPosition = startPos + Vector3.down * (sink * p);
                _modelRoot.localRotation = startRot * Quaternion.Euler(0f, 0f, listDegrees * p);
                yield return null;
            }

            yield return new WaitForSeconds(WreckLingerSeconds);

            for (float t = 0f; t < DissolveSeconds; t += Time.deltaTime)
            {
                ApplyStatus(hitFlash: 0f, dissolve: Mathf.Clamp01(t / DissolveSeconds));
                yield return null;
            }

            ApplyStatus(hitFlash: 0f, dissolve: 1f);
            if (_ring != null)
                _ring.SetActive(false);
            if (_moraleStrip != null)
                _moraleStrip.gameObject.SetActive(false);

            _deathRoutine = null;
            onComplete?.Invoke();
        }

        private void ApplyStatus(float hitFlash, float dissolve)
        {
            if (_mpb == null || _renderers.Count == 0)
                return;

            foreach (var statusRenderer in _renderers)
            {
                if (statusRenderer == null)
                    continue;

                statusRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(HitFlashId, hitFlash);
                _mpb.SetFloat(DissolveId, dissolve);
                statusRenderer.SetPropertyBlock(_mpb);
            }
        }

        // ------------------------------------------------------------------ health ring

        /// <summary>Same orb-drain side ring as the humanoid visual, scaled up for the
        /// wider silhouette (see CombatUnitVisual3D.BuildSideRing for the V-axis invariant).</summary>
        private void BuildSideRing(Material ringMaterial)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "SideRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // ~0.9x CELL base outer diameter (Phase 0 verdict 4), widened by RingScale for
            // the bigger vehicle silhouette — see CombatArenaVisualPlacement.
            float ringLocalScale = CombatArenaVisualPlacement.RingBaseLocalScale * RingScale;
            ring.transform.localScale = new Vector3(ringLocalScale, ringLocalScale, 1f);

            var ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            if (ringMaterial != null)
                ringRenderer.sharedMaterial = ringMaterial;

            _ring = ring;
            _ringRenderer = ringRenderer;
            _ringMpb ??= new MaterialPropertyBlock();
            _ringTargetFill = 1f;
            _ringDisplayedFill = 1f;
            _ringGutter = 0f;
            ApplyRingFill();
        }

        private void Update()
        {
            if (_ringRenderer == null ||
                Mathf.Approximately(_ringDisplayedFill, _ringTargetFill))
                return;

            _ringDisplayedFill = Mathf.MoveTowards(
                _ringDisplayedFill, _ringTargetFill, RingDrainPerSecond * Time.deltaTime);
            ApplyRingFill();
        }

        private void ApplyRingFill()
        {
            if (_ringRenderer == null || _ringMpb == null)
                return;

            _ringRenderer.GetPropertyBlock(_ringMpb);
            _ringMpb.SetFloat(RingFillId, _ringDisplayedFill);
            _ringMpb.SetFloat(RingGutterId, _ringGutter);
            _ringRenderer.SetPropertyBlock(_ringMpb);
        }
    }
}
