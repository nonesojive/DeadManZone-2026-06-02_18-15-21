using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Rigged 3D toon-ink unit presentation behind <see cref="ICombatUnitVisual"/>.
    /// Drives the generated Animator (Moving bool / Die trigger), yaw facing, a placeholder
    /// punch-forward attack pose (no shoot clip exists yet — muzzle/impact timing hooks only),
    /// _HitFlash pulses and the _DissolveAmount death ramp via MaterialPropertyBlock on the
    /// DMZ/ToonInk shader, plus the side-channel base ring (blue player / red enemy).
    /// No game rules live here — everything is driven by the actor through the seam.
    /// </summary>
    public sealed class CombatUnitVisual3D : MonoBehaviour, ICombatUnitVisual
    {
        private const string MovingParam = "Moving";
        private const string DieParam = "Die";

        private const float DissolveSeconds = 0.8f;
        private const float CorpseLingerSeconds = 0.35f;
        private const float HitFlashPeak = 0.85f;
        private const float HitFlashSeconds = 0.16f;
        private const float FallbackDieClipSeconds = 3f;
        private const float TurnDegreesPerSecond = 720f;
        private const float AttackLungeDistance = 0.22f;

        private static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");

        private Transform _modelRoot;
        private Animator _animator;
        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _mpb;
        private GameObject _ring;
        private float _visualHeight = 1.7f;
        private float _dieClipSeconds = FallbackDieClipSeconds;
        private float _yawOffsetDegrees;
        private Quaternion _targetRotation = Quaternion.identity;
        private Vector3 _lastFacingDir = Vector3.forward;
        private bool _dying;
        private float _locomotionLockUntil;
        private Coroutine _attackRoutine;
        private Coroutine _lungeRoutine;
        private Coroutine _hurtRoutine;
        private Coroutine _deathRoutine;

        public bool IsBuilt => _modelRoot != null;

        /// <inheritdoc/>
        public float VisualHeight => _visualHeight;

        /// <inheritdoc/>
        public float DeathSeconds => _dieClipSeconds + CorpseLingerSeconds + DissolveSeconds;

        /// <inheritdoc/>
        public bool BlocksLocomotion => _dying || Time.time < _locomotionLockUntil;

        /// <summary>Instantiates the rigged model under this actor, applies the toon-ink
        /// material across its renderers, wires the Animator, and drops the side ring.</summary>
        public void Build(
            GameObject modelSource,
            RuntimeAnimatorController animatorController,
            Material unitMaterial,
            Material ringMaterial,
            float targetHeight,
            float yawOffsetDegrees)
        {
            Clear();

            if (modelSource == null)
                return;

            _yawOffsetDegrees = yawOffsetDegrees;
            _visualHeight = Mathf.Max(0.5f, targetHeight);

            var rootGo = new GameObject("ModelRoot3D");
            rootGo.transform.SetParent(transform, false);
            _modelRoot = rootGo.transform;
            _targetRotation = _modelRoot.rotation * Quaternion.Euler(0f, _yawOffsetDegrees, 0f);

            var instance = Instantiate(modelSource, _modelRoot);
            instance.name = "UnitModel";
            // Reuse the arena's uniform-scale/ground-align helper (feet on y=0).
            CombatArenaVisualPlacement.PlaceOnGround(
                instance.transform, transform.position, _visualHeight, 1f);

            _animator = instance.GetComponentInChildren<Animator>();
            if (_animator == null)
                _animator = instance.AddComponent<Animator>();
            _animator.applyRootMotion = false;
            if (animatorController != null)
                _animator.runtimeAnimatorController = animatorController;
            _dieClipSeconds = ResolveDieClipSeconds(animatorController);

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

            _mpb ??= new MaterialPropertyBlock();
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            BuildSideRing(ringMaterial);
        }

        /// <inheritdoc/>
        public void SetWalking(bool walking)
        {
            if (_dying || _animator == null)
                return;

            _animator.SetBool(MovingParam, walking);
        }

        /// <inheritdoc/>
        public void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            _lastFacingDir = worldDirection.normalized;
            _targetRotation =
                Quaternion.LookRotation(_lastFacingDir, Vector3.up)
                * Quaternion.Euler(0f, _yawOffsetDegrees, 0f);
        }

        /// <inheritdoc/>
        public void UpdateSortAndBob(Vector3 worldPosition)
        {
            // No render-queue sorting or sprite bob in 3D — just ease the yaw toward
            // the latest facing so direction flips read as a turn, not a snap.
            if (_modelRoot == null || _dying)
                return;

            _modelRoot.rotation = Quaternion.RotateTowards(
                _modelRoot.rotation, _targetRotation, TurnDegreesPerSecond * Time.deltaTime);
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

            // Square up to the target before firing; shooting backwards reads as a glitch.
            FaceDirection(targetWorld - transform.position);

            float window = Mathf.Max(0.05f, profile.TotalDurationSeconds);
            _locomotionLockUntil = Time.time + window;

            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            _lungeRoutine = StartCoroutine(AttackLungeRoutine(window));

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
            StopTransientRoutines();

            _dying = true;
            _locomotionLockUntil = float.MaxValue;
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            if (_animator != null)
            {
                _animator.SetBool(MovingParam, false);
                _animator.SetTrigger(DieParam);
            }

            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(DeathRoutine(onComplete));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            StopTransientRoutines();
            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = null;

            if (_modelRoot != null)
                Destroy(_modelRoot.gameObject);
            if (_ring != null)
                Destroy(_ring);

            _modelRoot = null;
            _ring = null;
            _animator = null;
            _renderers.Clear();
            _dying = false;
            _locomotionLockUntil = 0f;
            _targetRotation = Quaternion.identity;
            _lastFacingDir = Vector3.forward;
            _dieClipSeconds = FallbackDieClipSeconds;
        }

        private void StopTransientRoutines()
        {
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _attackRoutine = null;
            _lungeRoutine = null;
            _hurtRoutine = null;

            if (_modelRoot != null)
                _modelRoot.localPosition = Vector3.zero;
        }

        private IEnumerator AttackTimingRoutine(
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            float shoulder = Mathf.Max(0.8f, _visualHeight * 0.72f);
            Vector3 muzzle = transform.position
                             + Vector3.up * shoulder
                             + _lastFacingDir * (_visualHeight * 0.25f);
            onMuzzle?.Invoke(muzzle);

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            onImpact?.Invoke();
            _attackRoutine = null;
        }

        /// <summary>Placeholder attack pose: a small punch-forward/settle-back on the model
        /// root, timed to the attack window. Swapped for a real shoot clip when one exists.</summary>
        private IEnumerator AttackLungeRoutine(float windowSeconds)
        {
            float outSeconds = Mathf.Min(0.12f, windowSeconds * 0.3f);
            float backSeconds = Mathf.Max(0.05f, windowSeconds - outSeconds);
            Vector3 lunge = _lastFacingDir * AttackLungeDistance;

            for (float t = 0f; t < outSeconds && _modelRoot != null; t += Time.deltaTime)
            {
                _modelRoot.localPosition = transform.InverseTransformDirection(lunge)
                                           * Mathf.Sin(Mathf.Clamp01(t / outSeconds) * Mathf.PI * 0.5f);
                yield return null;
            }

            for (float t = 0f; t < backSeconds && _modelRoot != null; t += Time.deltaTime)
            {
                float p = 1f - Mathf.Clamp01(t / backSeconds);
                _modelRoot.localPosition = transform.InverseTransformDirection(lunge) * p;
                yield return null;
            }

            if (_modelRoot != null)
                _modelRoot.localPosition = Vector3.zero;
            _lungeRoutine = null;
        }

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

        private IEnumerator DeathRoutine(Action onComplete)
        {
            // Let the Die clip play out, hold the corpse a beat, then dissolve away.
            yield return new WaitForSeconds(_dieClipSeconds + CorpseLingerSeconds);

            for (float t = 0f; t < DissolveSeconds; t += Time.deltaTime)
            {
                ApplyStatus(hitFlash: 0f, dissolve: Mathf.Clamp01(t / DissolveSeconds));
                yield return null;
            }

            ApplyStatus(hitFlash: 0f, dissolve: 1f);
            if (_ring != null)
                _ring.SetActive(false);

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

        private void BuildSideRing(Material ringMaterial)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SideRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            // Flattened disc under the feet, matching the spike's ring proportions.
            ring.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);

            var ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            if (ringMaterial != null)
                ringRenderer.sharedMaterial = ringMaterial;

            _ring = ring;
        }

        private static float ResolveDieClipSeconds(RuntimeAnimatorController controller)
        {
            if (controller == null)
                return FallbackDieClipSeconds;

            foreach (var clip in controller.animationClips)
            {
                if (clip == null)
                    continue;

                string clipName = clip.name.ToLowerInvariant();
                if (clipName.Contains("die") || clipName.Contains("dead"))
                    return Mathf.Max(0.3f, clip.length);
            }

            return FallbackDieClipSeconds;
        }
    }
}
