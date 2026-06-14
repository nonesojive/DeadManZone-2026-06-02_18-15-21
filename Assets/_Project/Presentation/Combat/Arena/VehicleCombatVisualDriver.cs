using System;
using System.Collections;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class VehicleCombatVisualDriver : ICombatUnitVisualDriver
    {
        private Transform _modelRoot;
        private CombatArenaMuzzleAnchor _muzzleAnchor;
        private MonoBehaviour _coroutineHost;
        private Vector3 _restLocalPosition;
        private Coroutine _attackRoutine;
        private Coroutine _recoilRoutine;

        public void Configure(MonoBehaviour coroutineHost, Transform modelRoot)
        {
            _coroutineHost = coroutineHost;
            _modelRoot = modelRoot;
            _restLocalPosition = modelRoot != null ? modelRoot.localPosition : Vector3.zero;
            _muzzleAnchor = modelRoot != null ? modelRoot.GetComponentInChildren<CombatArenaMuzzleAnchor>() : null;
        }

        public void Bind(Animator animator, CombatArenaAnimationSetSO animationSet) { }

        public void SetWalking(bool walking) { }

        public void PlayAttack(
            CombatAttackPresentationProfile profile,
            Action onMuzzleFrame,
            Action onImpactFrame)
        {
            if (_attackRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_attackRoutine);

            if (_recoilRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_recoilRoutine);

            if (_coroutineHost != null)
            {
                _recoilRoutine = _coroutineHost.StartCoroutine(RecoilRoutine());
                _attackRoutine = _coroutineHost.StartCoroutine(
                    AttackTimingRoutine(profile, onMuzzleFrame, onImpactFrame));
            }
        }

        public void PlayDeath(Action onComplete) => onComplete?.Invoke();

        public Vector3 GetMuzzleWorldPosition()
        {
            if (_muzzleAnchor != null)
                return _muzzleAnchor.transform.position;

            if (_modelRoot == null)
                return Vector3.zero;

            return _modelRoot.position + _modelRoot.forward * 0.6f + Vector3.up * 0.5f;
        }

        public void Clear()
        {
            if (_recoilRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_recoilRoutine);

            if (_attackRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_attackRoutine);

            if (_modelRoot != null)
                _modelRoot.localPosition = _restLocalPosition;

            _modelRoot = null;
            _muzzleAnchor = null;
            _coroutineHost = null;
            _attackRoutine = null;
            _recoilRoutine = null;
        }

        private IEnumerator RecoilRoutine()
        {
            if (_modelRoot == null)
                yield break;

            const float duration = 0.12f;
            const float recoilDistance = 0.08f;
            Vector3 back = _restLocalPosition - Vector3.forward * recoilDistance;
            float half = duration * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                _modelRoot.localPosition = Vector3.Lerp(_restLocalPosition, back, t / half);
                yield return null;
            }

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                _modelRoot.localPosition = Vector3.Lerp(back, _restLocalPosition, t / half);
                yield return null;
            }

            _modelRoot.localPosition = _restLocalPosition;
            _recoilRoutine = null;
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

            _attackRoutine = null;
        }
    }
}
