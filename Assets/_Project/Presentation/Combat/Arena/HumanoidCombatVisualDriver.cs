using System;
using System.Collections;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class HumanoidCombatVisualDriver : ICombatUnitVisualDriver
    {
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int CurrentGait = Animator.StringToHash("CurrentGait");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int ShootTrigger = Animator.StringToHash("Shoot");
        private static readonly int GrenadeThrowTrigger = Animator.StringToHash("GrenadeThrow");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        private const float WalkSpeed = 1.5f;
        private const int WalkGait = 1;

        private Animator _animator;
        private Transform _modelRoot;
        private CombatArenaMuzzleAnchor _muzzleAnchor;
        private CombatArenaAnimationSetSO _animationSet;
        private MonoBehaviour _coroutineHost;
        private Coroutine _attackRoutine;

        public void Configure(MonoBehaviour coroutineHost, Transform modelRoot)
        {
            _coroutineHost = coroutineHost;
            _modelRoot = modelRoot;
            _muzzleAnchor = modelRoot != null ? modelRoot.GetComponentInChildren<CombatArenaMuzzleAnchor>() : null;
        }

        public void Bind(Animator animator, CombatArenaAnimationSetSO animationSet)
        {
            _animator = animator;
            _animationSet = animationSet;
            if (_animator == null)
                return;

            _animator.applyRootMotion = false;
            _animator.SetBool(IsGrounded, true);
            _animator.SetFloat(MoveSpeed, 0f);
            _animator.SetBool(IsWalking, false);
            _animator.SetInteger(CurrentGait, 0);
        }

        public void SetWalking(bool walking)
        {
            if (_animator == null)
                return;

            _animator.SetBool(IsWalking, walking);
            _animator.SetFloat(MoveSpeed, walking ? WalkSpeed : 0f);
            _animator.SetInteger(CurrentGait, walking ? WalkGait : 0);
        }

        public void PlayAttack(
            CombatAttackPresentationProfile profile,
            Action onMuzzleFrame,
            Action onImpactFrame)
        {
            if (_attackRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_attackRoutine);

            if (_animator != null)
            {
                int trigger = profile.Kind == CombatAttackPresentationKind.InfantryGrenade
                    ? GrenadeThrowTrigger
                    : ShootTrigger;
                _animator.ResetTrigger(DeathTrigger);
                _animator.SetTrigger(trigger);
            }

            if (_coroutineHost != null)
                _attackRoutine = _coroutineHost.StartCoroutine(
                    AttackTimingRoutine(profile, onMuzzleFrame, onImpactFrame));
        }

        public void PlayDeath(Action onComplete)
        {
            AnimationClip clip = PickDeathClip();
            if (_animator != null)
            {
                _animator.ResetTrigger(ShootTrigger);
                _animator.ResetTrigger(GrenadeThrowTrigger);
                _animator.SetTrigger(DeathTrigger);
                float duration = clip != null ? clip.length : 0.6f;
                if (_coroutineHost != null)
                    _coroutineHost.StartCoroutine(WaitThenComplete(duration, onComplete));
                return;
            }

            onComplete?.Invoke();
        }

        public Vector3 GetMuzzleWorldPosition()
        {
            if (_muzzleAnchor != null)
                return _muzzleAnchor.transform.position;

            if (_modelRoot == null)
                return Vector3.zero;

            return _modelRoot.position + _modelRoot.forward * 0.4f + Vector3.up * 1.2f;
        }

        public void Clear()
        {
            if (_attackRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_attackRoutine);

            _animator = null;
            _animationSet = null;
            _modelRoot = null;
            _muzzleAnchor = null;
            _coroutineHost = null;
            _attackRoutine = null;
        }

        private AnimationClip PickDeathClip()
        {
            if (_animationSet == null)
                return null;

            int pick = UnityEngine.Random.Range(0, 3);
            return pick switch
            {
                0 => _animationSet.death01 ?? _animationSet.sidekickDeathForward,
                1 => _animationSet.death02 ?? _animationSet.sidekickDeathForward,
                _ => _animationSet.death03 ?? _animationSet.sidekickDeathForward
            };
        }

        private IEnumerator AttackTimingRoutine(
            CombatAttackPresentationProfile profile,
            Action onMuzzleFrame,
            Action onImpactFrame)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            onMuzzleFrame?.Invoke();

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            onImpactFrame?.Invoke();

            float remaining = profile.TotalDurationSeconds - profile.ImpactDelaySeconds;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);

            SetWalking(false);
            _attackRoutine = null;
        }

        private static IEnumerator WaitThenComplete(float seconds, Action onComplete)
        {
            yield return new WaitForSeconds(Mathf.Max(seconds, 0.1f));
            onComplete?.Invoke();
        }
    }
}
