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
        // Rout exit: slower, ease-in ramp — the runner stays solid for the first strides
        // and fades out softly, clearly gentler than the hard 0.8 s death dissolve.
        private const float RoutDissolveSeconds = 1.4f;
        private const float CorpseLingerSeconds = 0.35f;
        // Softened from 0.85/0.16 — full peak read as a pure-white ghost at gameplay distance.
        private const float HitFlashPeak = 0.45f;
        private const float HitFlashSeconds = 0.12f;
        private const float FallbackDieClipSeconds = 3f;
        // 2026-07-17 fluidity pass: ~0.2s for a 90 degree heading change (was 720 = 0.125s —
        // already eased via RotateTowards, not an instant snap, but faster than the owner's
        // ~0.2s target for a visible "turn" rather than a flick).
        private const float TurnDegreesPerSecond = 450f;
        private const float AttackLungeDistance = 0.22f;

        // Rate limit on the port-arms additive rotations: fast enough to track the
        // 720°/s facing turns and the 0.15 s aim blend, slow enough that a single-frame
        // animator extreme (walk-swing apex mid-turn) can't pop the carry in one frame.
        private const float PortArmsMaxDegreesPerSecond = 720f;

        // Ring-fill health drain speed (fill units/sec): fast enough that a hit reads as an
        // immediate response, slow enough to read as a drain rather than a pop.
        private const float RingDrainPerSecond = 1.4f;

        private static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int RingFillId = Shader.PropertyToID("_Fill");
        private static readonly int RingGutterId = Shader.PropertyToID("_Gutter");
        private static readonly int FactionTintId = Shader.PropertyToID("_FactionTint");
        private static readonly int FactionTintWeightId = Shader.PropertyToID("_FactionTintWeight");

        // Wave 3 placeholder-art pass: subtle per-faction rim accent (CombatRingFill's
        // _FactionTint/_FactionTintWeight) so same-side enemies from different factions are
        // tellable apart. Kept low so the side color (blue/red) stays dominant.
        private const float FactionTintWeight = 0.35f;

        // Phase 0 verdict 2: the morale gutter (achromatic rim flicker) reads fine even at
        // the louder 0.7/1.0 bands from the crowd captures, but the runtime driver stays on
        // the subtle end on purpose — cap intensity at 0.35 rather than escalating with morale.
        private const float MoraleGutterMaxIntensity = 0.35f;

        [Header("Rifle grip (hand-bone axes, world meters; shared default across archetypes)")]
        [SerializeField] private Vector3 rifleGripOffsetMeters = new(0f, 0.05f, 0.02f);
        [SerializeField] private Vector3 rifleGripLocalEuler = new(-90f, 0f, 0f);
        [Tooltip("Rifle size relative to its authored 0.73 m (for a 1.7 m figure). " +
                 "1.2 = stylized board-game upscale so the prop reads at gameplay distance.")]
        [SerializeField] private float rifleWorldScale = 1.2f;

        [Header("Port-arms rest carry (additive right-arm pose, character space; fades while aiming/dying)")]
        [SerializeField, Range(0f, 1f)] private float portArmsArmWeight = 0.65f;
        [SerializeField, Range(0f, 1f)] private float portArmsBarrelWeight = 0.95f;
        [Tooltip("Where the right (grip) hand carries to, in character space meters for a 1.7 m figure. " +
                 "Held slightly out from the chest so the rifle separates from the torso silhouette.")]
        [SerializeField] private Vector3 portArmsHandAnchorLocal = new(0.13f, 1.15f, 0.34f);
        [Tooltip("Barrel direction at rest, character space (up-left, slightly flatter than 45° " +
                 "so more barrel crosses the silhouette when viewed from behind/above).")]
        [SerializeField] private Vector3 portArmsBarrelDirLocal = new(-1f, 0.85f, 0.22f);
        [Tooltip("Rest-carry multiplier while the walk clip plays, so the additive doesn't fight arm swing.")]
        [SerializeField, Range(0f, 1f)] private float portArmsMovingMultiplier = 0.75f;

        [Header("Left hand on forestock (analytic two-bone IK, after aim + recoil)")]
        [SerializeField, Range(0f, 1f)] private float leftHandIkWeight = 1f;
        [Tooltip("Elbow pole hint, character space meters for a 1.7 m figure (down-left of the shoulder).")]
        [SerializeField] private Vector3 leftElbowHintLocal = new(-0.38f, 0.75f, 0.10f);
        [Tooltip("Direction the left fingers (+Y of the hand bone) wrap, in rifle-local space.")]
        [SerializeField] private Vector3 leftHandFingersDirRifleLocal = new(0.9f, 0.45f, 0f);
        [Tooltip("Wrist offset from ForestockPoint, rifle-local axes in world meters.")]
        [SerializeField] private Vector3 forestockGripOffsetMeters = new(0f, -0.02f, 0.03f);
        [Tooltip("Seconds over which the hands release the carry/IK as the die clip starts.")]
        [SerializeField] private float deathReleaseSeconds = 0.35f;
        [Tooltip("World-rotation low-pass time constant for the rifle during the die clip, " +
                 "so fast hand-bone whips don't flail the rigidly-parented prop.")]
        [SerializeField] private float rifleDeathRotationSmoothSeconds = 0.12f;

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
        private Renderer _ringRenderer;
        private MaterialPropertyBlock _ringMpb;
        private float _ringTargetFill = 1f;
        private float _ringDisplayedFill = 1f;
        private float _ringGutter;
        private Color _factionTint = Color.clear;
        private float _visualHeight = 1.7f;
        private float _dieClipSeconds = FallbackDieClipSeconds;
        private float _yawOffsetDegrees;
        private Quaternion _targetRotation = Quaternion.identity;
        private Vector3 _lastFacingDir = Vector3.forward;
        private bool _dying;
        private bool _routExiting;
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
        private Transform _leftUpperArmBone;
        private Transform _leftForearmBone;
        private Transform _leftHandBone;
        private Transform _rifle;
        private Transform _muzzlePoint;
        private Transform _forestockPoint;
        private Vector3 _rifleRestLocalPosition;
        private Quaternion _rifleRestLocalRotation;
        private Vector3 _aimTargetWorld;
        private float _aimStartTime = float.NegativeInfinity;
        private float _aimEndTime = float.NegativeInfinity;
        private float _recoilStartTime = float.NegativeInfinity;
        private float _deathStartTime = float.NegativeInfinity;
        private float _movingCarry01 = 1f;

        // Rate-clamped port-arms additives (identity = no offset), previous frame.
        private Quaternion _portArmsUpperAdditive = Quaternion.identity;
        private Quaternion _portArmsForearmAdditive = Quaternion.identity;
        private Quaternion _portArmsBarrelAdditive = Quaternion.identity;

        // Previous-frame rifle world rotation for the die-clip flail damping.
        private Quaternion _rifleLastWorldRotation = Quaternion.identity;
        private bool _hasRifleLastWorldRotation;

        public bool IsBuilt => _modelRoot != null;

        /// <inheritdoc/>
        public float VisualHeight => _visualHeight;

        /// <inheritdoc/>
        public float DeathSeconds => _dieClipSeconds + CorpseLingerSeconds + DissolveSeconds;

        /// <inheritdoc/>
        public bool BlocksLocomotion => _dying || Time.time < _locomotionLockUntil;

        /// <summary>The base ring IS the health display in 3D — no floating overhead bar.</summary>
        public bool DisplaysHealth => _ringRenderer != null;

        /// <inheritdoc/>
        public void SetHealthFraction(float fraction) =>
            _ringTargetFill = Mathf.Clamp01(fraction);

        /// <summary>Infantry run for their own board edge when broken.</summary>
        public bool FleesWhenBroken => true;

        /// <inheritdoc/>
        public void SetMoraleFraction(float fraction)
        {
            // 2026-07-17: the old floating morale strip (CombatUnitMoraleStrip) is gone —
            // the ring's _Gutter rim below is the only morale display now (still lazy: this
            // is only ever called for breakable units, so morale-immune units are untouched,
            // ADR-0005).
            // Ring's _Gutter: achromatic rim flicker, driven by MPB same as _Fill (no
            // per-unit material instances). Solid morale (1) = no gutter; broken (0) ramps
            // to the subtle 0.35 cap above.
            _ringGutter = (1f - Mathf.Clamp01(fraction)) * MoraleGutterMaxIntensity;
            ApplyRingFill();
        }

        /// <inheritdoc/>
        public void PlayRoutExit(Action onComplete)
        {
            if (_dying || _routExiting)
            {
                onComplete?.Invoke();
                return;
            }

            _routExiting = true;
            StopTransientRoutines();

            // Out of the fight: the status displays leave immediately (a fleeing unit
            // reading as a live bar is noise). NOT _dying — the walk cycle keeps playing
            // while the actor marches the runner off-field.
            if (_ring != null)
                _ring.SetActive(false);

            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(RoutExitRoutine(onComplete));
        }

        private IEnumerator RoutExitRoutine(Action onComplete)
        {
            for (float t = 0f; t < RoutDissolveSeconds; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / RoutDissolveSeconds));
                ApplyStatus(hitFlash: 0f, dissolve: p);
                yield return null;
            }

            ApplyStatus(hitFlash: 0f, dissolve: 1f);
            _deathRoutine = null;
            onComplete?.Invoke();
        }

        /// <summary>Instantiates the rigged model under this actor, applies the toon-ink
        /// material across its renderers, wires the Animator, and drops the side ring.</summary>
        public void Build(
            GameObject modelSource,
            RuntimeAnimatorController animatorController,
            Material unitMaterial,
            Material ringMaterial,
            GameObject riflePrefab,
            float targetHeight,
            float yawOffsetDegrees,
            Color factionTint = default)
        {
            Clear();

            if (modelSource == null)
                return;

            _factionTint = factionTint;
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
            _upperArmBone = FindUpperArm(rigRoot, "right");
            _spineBone = FindSpine(rigRoot);

            // Left support arm (all optional — a missing bone just skips the forestock IK).
            _leftHandBone = FindBone(rigRoot, "hand", "left");
            _leftForearmBone = FindBone(rigRoot, "forearm", "left");
            _leftUpperArmBone = FindUpperArm(rigRoot, "left");

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
            _forestockPoint = FindBone(rifle.transform, "forestockpoint", null);

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

        private static Transform FindUpperArm(Transform root, string sideToken)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                if (name.Contains(sideToken) && name.Contains("arm") &&
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

        /// <summary>True once the first FaceDirection after Configure has snapped the
        /// model. Spawn facing must be instant — easing from the fresh root's identity
        /// (+z) made whole armies swivel from north at fight start.</summary>
        private bool _hasFacing;

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

            if (!_hasFacing && _modelRoot != null)
            {
                _modelRoot.rotation = _targetRotation;
                _hasFacing = true;
            }
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
            _deathStartTime = Time.time; // carry + left-hand IK release over deathReleaseSeconds
            _aimEndTime = Mathf.Min(_aimEndTime, Time.time); // any live aim blends out now
            _locomotionLockUntil = float.MaxValue;
            _ringTargetFill = 0f; // ring drains empty as the unit falls
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
            _ringRenderer = null;
            _ringTargetFill = 1f;
            _ringDisplayedFill = 1f;
            _ringGutter = 0f;
            _factionTint = Color.clear;
            _animator = null;
            _renderers.Clear();
            _spineBone = null;
            _upperArmBone = null;
            _forearmBone = null;
            _handBone = null;
            _leftUpperArmBone = null;
            _leftForearmBone = null;
            _leftHandBone = null;
            _rifle = null;
            _muzzlePoint = null;
            _forestockPoint = null;
            _aimStartTime = float.NegativeInfinity;
            _aimEndTime = float.NegativeInfinity;
            _recoilStartTime = float.NegativeInfinity;
            _deathStartTime = float.NegativeInfinity;
            _movingCarry01 = 1f;
            _portArmsUpperAdditive = Quaternion.identity;
            _portArmsForearmAdditive = Quaternion.identity;
            _portArmsBarrelAdditive = Quaternion.identity;
            _rifleLastWorldRotation = Quaternion.identity;
            _hasRifleLastWorldRotation = false;
            _dying = false;
            _routExiting = false;
            _locomotionLockUntil = 0f;
            _targetRotation = Quaternion.identity;
            _lastFacingDir = Vector3.forward;
            _hasFacing = false; // pooled reuse: next spawn's first facing snaps again
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

        /// <summary>Additive pose stack, written after the Animator each frame:
        /// 1. port-arms rest carry (right arm brings the rifle across the chest, barrel
        ///    up-left; eases down while walking, yields 1:1 to the aim layer),
        /// 2. the existing aim layer (swing shoulder→hand toward the victim, spine twist,
        ///    level the barrel) + recoil kick synced to the muzzle flash,
        /// 3. left-hand two-bone IK onto the rifle's ForestockPoint — runs LAST so the
        ///    support hand tracks the rifle through aim blends and recoil.
        /// Everything scales by a death fade so the hands release naturally as the die
        /// clip starts, instead of freezing mid-carry.</summary>
        private void LateUpdate()
        {
            if (_handBone == null || _rifle == null)
                return;

            // The animator restores bones every frame but not our prop — re-seat the rifle
            // on its rest grip first so recoil offsets never accumulate frame to frame.
            _rifle.localPosition = _rifleRestLocalPosition;
            _rifle.localRotation = _rifleRestLocalRotation;

            float deathFade = _dying
                ? 1f - Mathf.Clamp01((Time.time - _deathStartTime) / Mathf.Max(0.01f, deathReleaseSeconds))
                : 1f;

            if (deathFade > 0f)
            {
                float aim01 = ComputeAim01() * deathFade;

                // Rest carry yields to the aim layer 1:1 (preserves the approved aim feel at
                // full weight) and eases down a notch while the walk clip swings the arms.
                bool moving = !_dying && _animator != null && _animator.GetBool(MovingParam);
                _movingCarry01 = Mathf.MoveTowards(
                    _movingCarry01, moving ? portArmsMovingMultiplier : 1f, Time.deltaTime * 4f);
                float rest01 = (1f - aim01) * deathFade;
                // The moving multiplier only eases the ARM swing (that's what fights the walk
                // clip); the barrel align stays full so the muzzle never droops mid-march.
                ApplyPortArmsPose(rest01 * _movingCarry01, rest01);

                if (aim01 > 0f)
                {
                    ApplyAimPose(aim01);
                    ApplyRecoil(aim01);
                }

                ApplyLeftHandIk(leftHandIkWeight * deathFade);
            }

            // Die-clip flail damping: once the hands have released, low-pass the rifle's
            // WORLD rotation so a fast hand-bone whip (some fall frames) can't sling the
            // rigidly-parented prop around. Converges to the animated pose as the body
            // settles, so the rifle still ends resting with the corpse and dissolves with it.
            if (_dying && _hasRifleLastWorldRotation)
            {
                float damp01 = 1f - deathFade; // ramps in over deathReleaseSeconds
                if (damp01 > 0f)
                {
                    float follow = 1f - Mathf.Exp(
                        -Time.deltaTime / Mathf.Max(0.01f, rifleDeathRotationSmoothSeconds));
                    _rifle.rotation = Quaternion.Slerp(
                        _rifleLastWorldRotation, _rifle.rotation, Mathf.Lerp(1f, follow, damp01));
                }
            }

            _rifleLastWorldRotation = _rifle.rotation;
            _hasRifleLastWorldRotation = true;
        }

        /// <summary>Character-space frame: unit yaw with the model's authored yaw offset
        /// removed, so +Z is where the unit visually faces.</summary>
        private Quaternion CharacterRotation() =>
            _modelRoot.rotation * Quaternion.Euler(0f, -_yawOffsetDegrees, 0f);

        /// <summary>Port-arms carry, same self-correcting math shape as the aim layer:
        /// swing the right shoulder→hand line toward a chest-front anchor, then rotate the
        /// hand so the barrel points up-left. World-space FromToRotations, so it lands the
        /// same pose on all four rigs regardless of their local bone axes.
        /// Each additive is rate-clamped against its previous frame (RotateTowards at
        /// PortArmsMaxDegreesPerSecond) — a single-frame animator extreme (walk-swing apex
        /// mid-turn) or a FromToRotation near-180° axis flip can no longer pop the carry
        /// in one frame; it spreads over a few frames instead.</summary>
        private void ApplyPortArmsPose(float w, float barrelW)
        {
            if ((w <= 0.001f && barrelW <= 0.001f) || _modelRoot == null)
                return;

            float maxDegrees = PortArmsMaxDegreesPerSecond * Time.deltaTime;
            Quaternion charRot = CharacterRotation();
            float figureScale = _visualHeight / 1.7f;
            Vector3 anchor = transform.position + charRot * (portArmsHandAnchorLocal * figureScale);

            if (_upperArmBone != null)
            {
                Vector3 armDir = _handBone.position - _upperArmBone.position;
                Vector3 toAnchor = anchor - _upperArmBone.position;
                if (armDir.sqrMagnitude > 0.0001f && toAnchor.sqrMagnitude > 0.0001f)
                {
                    var swing = Quaternion.FromToRotation(armDir.normalized, toAnchor.normalized);
                    var desired = Quaternion.Slerp(Quaternion.identity, swing, w * portArmsArmWeight);
                    _portArmsUpperAdditive =
                        Quaternion.RotateTowards(_portArmsUpperAdditive, desired, maxDegrees);
                    _upperArmBone.rotation = _portArmsUpperAdditive * _upperArmBone.rotation;
                }
            }

            if (_forearmBone != null)
            {
                Vector3 foreDir = _handBone.position - _forearmBone.position;
                Vector3 toAnchor = anchor - _forearmBone.position;
                if (foreDir.sqrMagnitude > 0.0001f && toAnchor.sqrMagnitude > 0.0001f)
                {
                    var raise = Quaternion.FromToRotation(foreDir.normalized, toAnchor.normalized);
                    var desired = Quaternion.Slerp(Quaternion.identity, raise, w * portArmsArmWeight * 0.6f);
                    _portArmsForearmAdditive =
                        Quaternion.RotateTowards(_portArmsForearmAdditive, desired, maxDegrees);
                    _forearmBone.rotation = _portArmsForearmAdditive * _forearmBone.rotation;
                }
            }

            // Barrel up-left across the chest, via the hand (rifle is its child).
            if (_muzzlePoint != null)
            {
                Vector3 barrelDir = _muzzlePoint.position - _rifle.position;
                Vector3 want = charRot * portArmsBarrelDirLocal.normalized;
                if (barrelDir.sqrMagnitude > 0.0001f)
                {
                    var level = Quaternion.FromToRotation(barrelDir.normalized, want);
                    var desired = Quaternion.Slerp(Quaternion.identity, level, barrelW * portArmsBarrelWeight);
                    _portArmsBarrelAdditive =
                        Quaternion.RotateTowards(_portArmsBarrelAdditive, desired, maxDegrees);
                    _handBone.rotation = _portArmsBarrelAdditive * _handBone.rotation;
                }
            }
        }

        /// <summary>Left palm onto the rifle's ForestockPoint via analytic two-bone IK
        /// (law-of-cosines bend + swing to target + pole-hint roll), then a minimal-twist
        /// wrist align so the fingers wrap across the stock. Runs after aim + recoil, so
        /// the support hand follows the rifle wherever the right hand takes it.</summary>
        private void ApplyLeftHandIk(float w)
        {
            if (w <= 0.001f || _forestockPoint == null ||
                _leftUpperArmBone == null || _leftForearmBone == null || _leftHandBone == null)
                return;

            Vector3 target = _forestockPoint.position + _rifle.rotation * forestockGripOffsetMeters;
            Vector3 hint = transform.position
                           + CharacterRotation() * (leftElbowHintLocal * (_visualHeight / 1.7f));
            SolveTwoBoneIk(_leftUpperArmBone, _leftForearmBone, _leftHandBone, target, hint, w);

            // Wrist: hand-bone +Y runs along the fingers on these rigs — wrap them across
            // the forestock. Single-axis FromToRotation = shortest arc, no added twist.
            Vector3 fingersNow = _leftHandBone.rotation * Vector3.up;
            Vector3 fingersWant = _rifle.rotation * leftHandFingersDirRifleLocal.normalized;
            var wrap = Quaternion.FromToRotation(fingersNow, fingersWant);
            _leftHandBone.rotation =
                Quaternion.Slerp(Quaternion.identity, wrap, w) * _leftHandBone.rotation;
        }

        /// <summary>Standard analytic two-bone IK (the two-joint solution popularized by
        /// Ryan Juckett / Daniel Holden): clamp the target into reach, set the elbow
        /// interior angle from the law of cosines (bend about the current bend-plane
        /// normal), swing the whole chain so the end lands on the target, then roll the
        /// chain about the shoulder→target axis so the elbow points at the hint. Weight
        /// blends the solved world rotations back toward the animated pose.</summary>
        private static void SolveTwoBoneIk(
            Transform upper, Transform lower, Transform end,
            Vector3 target, Vector3 hint, float weight)
        {
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float lab = Vector3.Distance(a, b);
            float lcb = Vector3.Distance(b, c);
            if (lab < 0.0001f || lcb < 0.0001f)
                return;
            float lat = Mathf.Clamp(Vector3.Distance(a, target), 0.001f, lab + lcb - 0.001f);

            // Bend axis: current bend plane; hint plane when the arm starts out straight.
            Vector3 axis = Vector3.Cross(c - a, b - a);
            if (axis.sqrMagnitude < 0.000001f)
                axis = Vector3.Cross(c - a, hint - a);
            if (axis.sqrMagnitude < 0.000001f)
                return;
            axis.Normalize();

            Quaternion upperPre = upper.rotation;
            Quaternion lowerPre = lower.rotation;

            float acAb0 = Vector3.Angle(c - a, b - a);
            float baBc0 = Vector3.Angle(a - b, c - b);
            float acAb1 = Mathf.Acos(Mathf.Clamp(
                (lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f)) * Mathf.Rad2Deg;
            float baBc1 = Mathf.Acos(Mathf.Clamp(
                (lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f)) * Mathf.Rad2Deg;

            upper.rotation = Quaternion.AngleAxis(acAb1 - acAb0, axis) * upper.rotation;
            lower.rotation = Quaternion.AngleAxis(baBc1 - baBc0, axis) * lower.rotation;

            // Swing the bent chain so the end effector lands on the target.
            Vector3 endDir = end.position - a;
            Vector3 targetDir = target - a;
            if (endDir.sqrMagnitude > 0.000001f && targetDir.sqrMagnitude > 0.000001f)
                upper.rotation =
                    Quaternion.FromToRotation(endDir.normalized, targetDir.normalized)
                    * upper.rotation;

            // Pole hint: roll about the shoulder→target axis so the elbow faces the hint.
            Vector3 n = (target - a).normalized;
            Vector3 elbowDir = Vector3.ProjectOnPlane(lower.position - a, n);
            Vector3 hintDir = Vector3.ProjectOnPlane(hint - a, n);
            if (elbowDir.sqrMagnitude > 0.000001f && hintDir.sqrMagnitude > 0.000001f)
                upper.rotation =
                    Quaternion.AngleAxis(Vector3.SignedAngle(elbowDir, hintDir, n), n)
                    * upper.rotation;

            if (weight < 1f)
            {
                // Blend solved world rotations back toward the animated pose (upper first —
                // lower's world rotation depends on it).
                Quaternion lowerSolved = lower.rotation;
                upper.rotation = Quaternion.Slerp(upperPre, upper.rotation, weight);
                lower.rotation = Quaternion.Slerp(lowerPre, lowerSolved, weight);
            }
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

        /// <summary>Base ring = health display: a flat quad running DMZ/CombatRingFill —
        /// the disc drains top-down (orb-style level cutoff) with HP while the outer rim
        /// stays side-colored. The quad's +V faces world +Z (the actor root never rotates;
        /// only ModelRoot3D does), and the gameplay camera looks from -Z, so the shader's
        /// high-V edge = the far edge = screen top.</summary>
        private void BuildSideRing(Material ringMaterial)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "SideRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat, facing up
            // ~0.9x CELL outer diameter (Phase 0 verdict 4) — see CombatArenaVisualPlacement.
            ring.transform.localScale = new Vector3(
                CombatArenaVisualPlacement.RingBaseLocalScale,
                CombatArenaVisualPlacement.RingBaseLocalScale, 1f);

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

        /// <summary>Ease the displayed fill toward the latest replayed HP so hits read as a
        /// short drain, not a pop. Keeps draining while dying (a kill empties the disc as
        /// the unit falls); the ring hides when the dissolve finishes.</summary>
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
            _ringMpb.SetColor(FactionTintId, _factionTint);
            _ringMpb.SetFloat(FactionTintWeightId, _factionTint.a > 0.001f ? FactionTintWeight : 0f);
            _ringRenderer.SetPropertyBlock(_ringMpb);
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
