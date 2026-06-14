using System;
using System.Collections;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatUnitActor : MonoBehaviour
    {
        private const float DefaultModelHeight = 1.6f;

        private CombatBillboard _billboard;
        private CombatArenaUnitVisual _unitVisual;
        private CombatGridMapper _mapper;
        private GridCoord _anchor;
        private Coroutine _moveRoutine;
        private Coroutine _lungeRoutine;
        private Coroutine _attackTimingRoutine;
        private float _moveLerpSeconds = 0.4f;
        private float _lungeSeconds = 0.15f;
        private float _lungeDistance = 0.35f;
        private bool _frozen;
        private bool _useModelVisual;
        private CombatAttackPresentationProfile _attackProfile;

        public string InstanceId { get; private set; }
        public string PieceId { get; private set; }
        public GridCoord Anchor => _anchor;
        public bool IsAlive { get; private set; } = true;
        public CombatAttackPresentationProfile AttackProfile => _attackProfile;

        public void Initialize(
            string instanceId,
            string pieceId,
            Sprite icon,
            GameObject arenaPrefab,
            float arenaModelScale,
            float arenaModelHeight,
            Transform cameraTransform,
            CombatGridMapper mapper,
            GridCoord anchor,
            float moveLerpSeconds,
            float lungeSeconds,
            float lungeDistance,
            CombatAttackPresentationProfile attackProfile)
        {
            InstanceId = instanceId;
            PieceId = pieceId;
            _attackProfile = attackProfile;
            _mapper = mapper;
            _anchor = anchor;
            _moveLerpSeconds = moveLerpSeconds;
            _lungeSeconds = lungeSeconds;
            _lungeDistance = lungeDistance;
            IsAlive = true;
            gameObject.SetActive(true);

            ClearPresentation();

            _useModelVisual = arenaPrefab != null;
            if (_useModelVisual)
            {
                _unitVisual = GetComponent<CombatArenaUnitVisual>();
                if (_unitVisual == null)
                    _unitVisual = gameObject.AddComponent<CombatArenaUnitVisual>();

                float height = arenaModelHeight > 0f ? arenaModelHeight : DefaultModelHeight;
                _unitVisual.Build(arenaPrefab, height, arenaModelScale);

                if (!_unitVisual.HasModel)
                {
                    _useModelVisual = false;
                    _unitVisual.Clear();
                    Destroy(_unitVisual);
                    _unitVisual = null;
                }
            }

            if (!_useModelVisual && icon != null)
            {
                _billboard = GetComponent<CombatBillboard>();
                if (_billboard == null)
                    _billboard = gameObject.AddComponent<CombatBillboard>();
                _billboard.Configure(cameraTransform, icon);
            }

            SnapToAnchor(anchor);
        }

        public void SetFrozen(bool frozen) => _frozen = frozen;

        public void SnapToAnchor(GridCoord anchor)
        {
            _anchor = anchor;
            transform.position = _mapper.ToWorld(anchor);
            _unitVisual?.SetWalking(false);
        }

        public void MoveTo(GridCoord anchor)
        {
            _anchor = anchor;
            if (_frozen)
            {
                SnapToAnchor(anchor);
                return;
            }

            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(_mapper.ToWorld(anchor)));
        }

        public void PlayAttackToward(
            Vector3 targetWorld,
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle = null,
            Action onImpact = null)
        {
            if (_frozen || !IsAlive)
                return;

            if (profile.UseForwardStep)
            {
                if (_lungeRoutine != null)
                    StopCoroutine(_lungeRoutine);
                _lungeRoutine = StartCoroutine(LungeRoutine(targetWorld));
            }

            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel)
            {
                _unitVisual.PlayAttackToward(targetWorld, profile, onMuzzle, onImpact);
                return;
            }

            if (onMuzzle == null && onImpact == null)
                return;

            if (_attackTimingRoutine != null)
                StopCoroutine(_attackTimingRoutine);
            _attackTimingRoutine = StartCoroutine(
                BillboardAttackTimingRoutine(profile, onMuzzle, onImpact));
        }

        public void PlayDeath(Action onComplete)
        {
            IsAlive = false;
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            if (_attackTimingRoutine != null)
                StopCoroutine(_attackTimingRoutine);
            _unitVisual?.SetWalking(false);

            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel)
            {
                _unitVisual.PlayDeath(() =>
                {
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
                return;
            }

            StartCoroutine(DeathRoutine(onComplete));
        }

        private IEnumerator MoveRoutine(Vector3 target)
        {
            Vector3 start = transform.position;
            _unitVisual?.SetWalking(true);

            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel)
                _unitVisual.FaceWorldDirection(target - start);

            float elapsed = 0f;
            while (elapsed < _moveLerpSeconds)
            {
                if (_frozen)
                    yield return null;
                else
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _moveLerpSeconds);
                    transform.position = Vector3.Lerp(start, target, t);
                    yield return null;
                }
            }

            transform.position = target;
            _unitVisual?.SetWalking(false);
            _moveRoutine = null;
        }

        private IEnumerator LungeRoutine(Vector3 targetWorld)
        {
            Vector3 start = transform.position;
            Vector3 flatTarget = new Vector3(targetWorld.x, start.y, targetWorld.z);
            Vector3 dir = (flatTarget - start).normalized;
            Vector3 lungePoint = start + dir * _lungeDistance;
            float half = _lungeSeconds * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                if (!_frozen)
                    transform.position = Vector3.Lerp(start, lungePoint, t / half);
                yield return null;
            }

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                if (!_frozen)
                    transform.position = Vector3.Lerp(lungePoint, start, t / half);
                yield return null;
            }

            transform.position = start;
            _lungeRoutine = null;
        }

        private IEnumerator BillboardAttackTimingRoutine(
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            onMuzzle?.Invoke(transform.position + transform.forward * 0.35f + Vector3.up * 1.1f);

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            onImpact?.Invoke();
            _attackTimingRoutine = null;
        }

        private IEnumerator DeathRoutine(Action onComplete)
        {
            float duration = 0.35f;
            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                yield return null;
            }

            onComplete?.Invoke();
            gameObject.SetActive(false);
        }

        public void ResetForPool()
        {
            InstanceId = null;
            PieceId = null;
            IsAlive = true;
            transform.localScale = Vector3.one;
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            if (_attackTimingRoutine != null)
                StopCoroutine(_attackTimingRoutine);
            _moveRoutine = null;
            _lungeRoutine = null;
            _attackTimingRoutine = null;
            _frozen = false;
            _useModelVisual = false;
            _attackProfile = CombatAttackPresentationProfile.InfantryRifle;
            ClearPresentation();
        }

        private void ClearPresentation()
        {
            if (_unitVisual != null)
            {
                _unitVisual.Clear();
                Destroy(_unitVisual);
                _unitVisual = null;
            }

            if (_billboard != null)
            {
                Destroy(_billboard);
                _billboard = null;
            }

            var quad = transform.Find("BillboardQuad");
            if (quad != null)
                Destroy(quad.gameObject);
        }
    }
}
