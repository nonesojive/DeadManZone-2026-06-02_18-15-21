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
        // Softened from 0.85/0.16 — full peak read as a pure-white ghost at gameplay distance.
        private const float HitFlashPeak = 0.45f;
        private const float HitFlashSeconds = 0.12f;
        private const float FallbackDieClipSeconds = 3f;
        private const float TurnDegreesPerSecond = 720f;
        private const float AttackLungeDistance = 0.22f;

        private static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");

        [Header("Rifle grip (hand-bone axes, world meters; shared default across archetypes)")]
        [SerializeField] private Vector3 rifleGripOffsetMeters = new(0f, 0.05f, 0.02f);
        [SerializeField] private Vector3 rifleGripLocalEuler = new(-90f, 0f, 0f);
        [Tooltip("Rifle size relative to its authored 0.73 m (for a 1.7 m figure).")]
        [SerializeField] private float rifleWorldScale = 1f;

        [Header("Aim & recoil (code-driven, layered after the Animator in LateUpdate)")]
        [SerializeField, Range(0f, 1f)] private float aimWeight = 0.65f;
        [SerializeField, Range(0f, 1f)] private float spineTwistWeight = 0.25f;
        [SerializeField] private float aimBlendInSeconds = 0.15f;
        [SerializeField] private float aimBlendOutSeconds = 0.20f;
        [SerializeField] private float recoilDistanceMeters = 0.05f;
        [SerializeField] private float recoilOutSeconds = 0.06f;
        [SerializeField] private float recoilSettleSeconds = 0.15f;

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

        // Rifle + shoot-pose state (all optional — units without a right hand go rifle-less).
        private Transform _spineBone;
        private Transform _upperArmBone;
        private Transform _forearmBone;
        private Transform _handBone;
        private Transform _rifle;
        private Transform _muzzlePoint;
        private Vector3 _rifleRestLocalPosition;
        private Quaternion _rifleRestLocalRotation;
        private Vector3 _aimTargetWorld;
        private float _aimStartTime = float.NegativeInfinity;
        private float _aimEndTime = float.NegativeInfinity;
        private float _recoilStartTime = float.NegativeInfinity;

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
            GameObject riflePrefab,
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

            // After the unit-material pass so the rifle keeps its own gunmetal/wood
            // ToonInk materials (hull outline ON — clean primitive normals).
            AttachRifle(instance.transform, riflePrefab);

            _mpb ??= new MaterialPropertyBlock();
            ApplyStatus(hitFlash: 0f, dissolve: 0f);

            BuildSideRing(ringMaterial);
        }

        /// <summary>Parents the rifle prop to the rig's right hand so it inherits animation
        /// and falls/dissolves with the body. Bone lookup is by name (case-insensitive);
        /// a rig without a right hand logs an error and stays rifle-less.</summary>
        private void AttachRifle(Transform rigRoot, GameObject riflePrefab)
        {
            if (riflePrefab == null)
                return;

            _handBone = FindBone(rigRoot, "hand", "right");
            if (_handBone == null)
            {
                Debug.LogError(
                    $"[Combat3D] No right-hand bone found under '{rigRoot.name}' " +
                    "(searched for names containing 'right'+'hand') — unit goes rifle-less.",
                    this);
                return;
            }

            _forearmBone = FindBone(rigRoot, "forearm", "right");
            _upperArmBone = FindRightUpperArm(rigRoot);
            _spineBone = FindSpine(rigRoot);

            var rifle = Instantiate(riflePrefab, _handBone);
            rifle.name = "Rifle";
            // Meshy armatures carry ~0.01 bone scale — normalize so the rifle keeps its
            // authored world size (scaled with the figure) and the grip offset stays in meters.
            float boneScale = Mathf.Max(0.0001f, _handBone.lossyScale.x);
            float figureScale = _visualHeight / 1.7f; // prop authored for a 1.7 m figure
            _rifleRestLocalPosition = rifleGripOffsetMeters / boneScale;
            _rifleRestLocalRotation = Quaternion.Euler(rifleGripLocalEuler);
            rifle.transform.localPosition = _rifleRestLocalPosition;
            rifle.transform.localRotation = _rifleRestLocalRotation;
            rifle.transform.localScale = Vector3.one * (rifleWorldScale * figureScale / boneScale);
            _rifle = rifle.transform;
            _muzzlePoint = FindBone(rifle.transform, "muzzlepoint", null);

            // Fold the rifle into the status channels so hit flashes and the death
            // dissolve cover the whole figure (rifle materials are ToonInk too).
            _renderers.AddRange(rifle.GetComponentsInChildren<Renderer>(true));
        }

        private static Transform FindBone(Transform root, string token, string sideToken)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                if (name.Contains(token) && (sideToken == null || name.Contains(sideToken)))
                    return bone;
            }

            return null;
        }

        private static Transform FindRightUpperArm(Transform root)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                if (name.Contains("right") && name.Contains("arm") &&
                    !name.Contains("forearm") && !name.Contains("lowerarm") &&
                    !name.Contains("shoulder") && !name.Contains("hand"))
                    return bone;
            }

            return null;
        }

        /// <summary>Prefer the chest bone (exact 'Spine', parent of the shoulders on the
        /// Meshy rigs) over lower Spine01/Spine02 so the twist stays in the upper body.</summary>
        private static Transform FindSpine(Transform root)
        {
            Transform fallback = null;
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                if (name == "spine")
                    return bone;
                if (fallback == null && name.Contains("spine"))
                    fallback = bone;
            }

            return fallback;
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

            // Arm the LateUpdate aim layer: chest-height point on the victim, blended
            // in over aimBlendInSeconds, held for the window, released after recovery.
            _aimTargetWorld = targetWorld + Vector3.up * (_visualHeight * 0.55f);
            _aimStartTime = Time.time;
            _aimEndTime = Time.time + window;

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
            _spineBone = null;
            _upperArmBone = null;
            _forearmBone = null;
            _handBone = null;
            _rifle = null;
            _muzzlePoint = null;
            _aimStartTime = float.NegativeInfinity;
            _aimEndTime = float.NegativeInfinity;
            _recoilStartTime = float.NegativeInfinity;
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

            Vector3 muzzle;
            if (_muzzlePoint != null)
            {
                // Real rifle tip — the LateUpdate aim pose has already leveled it.
                muzzle = _muzzlePoint.position;
            }
            else
            {
                float shoulder = Mathf.Max(0.8f, _visualHeight * 0.72f);
                muzzle = transform.position
                         + Vector3.up * shoulder
                         + _lastFacingDir * (_visualHeight * 0.25f);
            }

            _recoilStartTime = Time.time; // recoil kick syncs to the muzzle-flash moment
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

        /// <summary>Additive shoot pose, written after the Animator each frame: swing the
        /// right arm up so the rifle levels at the victim (60-70% aim, no full IK), a
        /// slight chest twist toward the target, and a short recoil kick on the rifle +
        /// forearm synced to the muzzle flash. Weight eases 0→1→0 so it never pops
        /// against idle/walk.</summary>
        private void LateUpdate()
        {
            if (_dying || _handBone == null || _rifle == null)
                return;

            // The animator restores bones every frame but not our prop — re-seat the rifle
            // on its rest grip first so recoil offsets never accumulate frame to frame.
            _rifle.localPosition = _rifleRestLocalPosition;
            _rifle.localRotation = _rifleRestLocalRotation;

            float aim01 = ComputeAim01();
            if (aim01 <= 0f)
                return;

            ApplyAimPose(aim01);
            ApplyRecoil(aim01);
        }

        private float ComputeAim01()
        {
            float blendIn = Mathf.Clamp01(
                (Time.time - _aimStartTime) / Mathf.Max(0.01f, aimBlendInSeconds));
            float blendOut = Mathf.Clamp01(
                1f - (Time.time - _aimEndTime) / Mathf.Max(0.01f, aimBlendOutSeconds));
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));
        }

        private void ApplyAimPose(float aim01)
        {
            Vector3 aimOrigin = _upperArmBone != null ? _upperArmBone.position : _handBone.position;
            Vector3 aimDir = _aimTargetWorld - aimOrigin;
            if (aimDir.sqrMagnitude < 0.0001f)
                return;
            aimDir.Normalize();

            float armWeight = aim01 * aimWeight;

            // Slight chest twist toward the target (facing usually covers most of it).
            if (_spineBone != null)
            {
                Vector3 flat = aimDir;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.0001f)
                {
                    float yawDelta = Vector3.SignedAngle(_lastFacingDir, flat.normalized, Vector3.up);
                    _spineBone.rotation =
                        Quaternion.AngleAxis(yawDelta * spineTwistWeight * aim01, Vector3.up)
                        * _spineBone.rotation;
                }
            }

            // Raise the arm: swing the shoulder→hand line toward the aim direction.
            if (_upperArmBone != null)
            {
                Vector3 armDir = _handBone.position - _upperArmBone.position;
                if (armDir.sqrMagnitude > 0.0001f)
                {
                    var swing = Quaternion.FromToRotation(armDir.normalized, aimDir);
                    _upperArmBone.rotation =
                        Quaternion.Slerp(Quaternion.identity, swing, armWeight)
                        * _upperArmBone.rotation;
                }
            }

            // Straighten the forearm a touch more along the aim line.
            if (_forearmBone != null)
            {
                Vector3 foreDir = _handBone.position - _forearmBone.position;
                if (foreDir.sqrMagnitude > 0.0001f)
                {
                    var straighten = Quaternion.FromToRotation(foreDir.normalized, aimDir);
                    _forearmBone.rotation =
                        Quaternion.Slerp(Quaternion.identity, straighten, armWeight * 0.5f)
                        * _forearmBone.rotation;
                }
            }

            // Level the rifle at the victim by rotating the hand (rifle is its child).
            if (_muzzlePoint != null)
            {
                Vector3 barrelDir = _muzzlePoint.position - _rifle.position;
                if (barrelDir.sqrMagnitude > 0.0001f)
                {
                    var level = Quaternion.FromToRotation(barrelDir.normalized, aimDir);
                    _handBone.rotation =
                        Quaternion.Slerp(Quaternion.identity, level, armWeight)
                        * _handBone.rotation;
                }
            }
        }

        private void ApplyRecoil(float aim01)
        {
            float t = Time.time - _recoilStartTime;
            if (t < 0f || t >= recoilOutSeconds + recoilSettleSeconds)
                return;

            // Fast kick out, slower settle back.
            float envelope = t < recoilOutSeconds
                ? Mathf.Sin(Mathf.Clamp01(t / Mathf.Max(0.01f, recoilOutSeconds)) * Mathf.PI * 0.5f)
                : 1f - Mathf.Clamp01((t - recoilOutSeconds) / Mathf.Max(0.01f, recoilSettleSeconds));

            Vector3 backDir = _aimTargetWorld - _rifle.position;
            backDir.y = 0f;
            if (backDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 kick = -backDir.normalized * (recoilDistanceMeters * envelope * aim01);
            if (_forearmBone != null)
                _forearmBone.position += kick * 0.6f; // carries hand + rifle back together
            _rifle.position += kick * 0.4f;           // extra bite on the gun itself
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
