using System;
using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatUnitActor : MonoBehaviour
    {
        private const float DefaultModelHeight = 1.6f;

        private CombatBillboard _billboard;
        private CombatArenaUnitVisual _unitVisual;
        private CombatUnitVisual2D _visual2D;
        private bool _use2DVisual;
        private CombatGridMapper _mapper;
        private Transform _presentationRoot;
        private GridCoord _anchor;
        private Coroutine _lungeRoutine;
        private Coroutine _attackTimingRoutine;
        private float _moveWorldSpeed = 1f;
        private float _moveLerpFallbackSeconds = 0.4f;
        private float _marchGraceSeconds = 2.4f;
        private float _lastMoveCommandTime = -999f;
        private float _lungeSeconds = 0.15f;
        private float _lungeDistance = 0.35f;
        private float _bobTime;
        private bool _frozen;
        private bool _useModelVisual;
        private bool _useProceduralVisual;
        private bool _freeChaseEnabled;
        private float _chaseMaxLeadCells = 4f;
        private Vector3? _chaseTargetWorld;
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
            float moveWorldSpeed,
            float marchGraceSeconds,
            float lungeSeconds,
            float lungeDistance,
            CombatAttackPresentationProfile attackProfile,
            PieceDefinitionSO pieceDefinition = null,
            CombatSide combatSide = CombatSide.Player,
            bool useProceduralUnitVisuals = false,
            bool useFreeChaseMovement = false,
            float chaseMaxLeadCells = 4f,
            bool use2DUnitVisuals = false)
        {
            InstanceId = instanceId;
            PieceId = pieceId;
            _attackProfile = attackProfile;
            _mapper = mapper;
            _anchor = anchor;
            _moveLerpFallbackSeconds = moveLerpSeconds;
            _moveWorldSpeed = moveWorldSpeed > 0f
                ? moveWorldSpeed
                : ResolveFallbackMoveSpeed(moveLerpSeconds);
            _marchGraceSeconds = marchGraceSeconds > 0f ? marchGraceSeconds : 2.4f;
            _lastMoveCommandTime = Time.time;
            _lungeSeconds = lungeSeconds;
            _lungeDistance = lungeDistance;
            _freeChaseEnabled = useFreeChaseMovement;
            _chaseMaxLeadCells = chaseMaxLeadCells > 0f ? chaseMaxLeadCells : 4f;
            _chaseTargetWorld = null;
            IsAlive = true;
            _bobTime = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            gameObject.SetActive(true);

            ClearPresentation();
            EnsurePresentationRoot();

            _use2DVisual = use2DUnitVisuals;
            if (_use2DVisual && pieceDefinition != null)
            {
                _visual2D = gameObject.AddComponent<CombatUnitVisual2D>();
                int squadSize = Mathf.Clamp(pieceDefinition.manpowerCost, 1, 5);
                _visual2D.Build(pieceDefinition, combatSide, cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null, squadSize);
                SnapToAnchor(anchor);
                return;
            }

            _useModelVisual = arenaPrefab != null;
            _useProceduralVisual = false;

            if (_useModelVisual)
            {
                _unitVisual = _presentationRoot.gameObject.AddComponent<CombatArenaUnitVisual>();

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

            if (!_useModelVisual && useProceduralUnitVisuals && pieceDefinition != null)
            {
                TopTroopsSquadVisualFactory.BuildSquad(_presentationRoot, pieceDefinition, combatSide);
                _useProceduralVisual = _presentationRoot.childCount > 0;
            }

            if (!_useModelVisual && !_useProceduralVisual && icon != null)
            {
                _billboard = gameObject.AddComponent<CombatBillboard>();
                _billboard.Configure(cameraTransform, icon);
            }

            SnapToAnchor(anchor);
        }

        public void SetFrozen(bool frozen)
        {
            _frozen = frozen;
            if (frozen)
                _unitVisual?.SetWalking(false);
        }

        public void SnapToAnchor(GridCoord anchor)
        {
            _anchor = anchor;
            transform.position = _mapper.ToWorld(anchor);
            _unitVisual?.SetWalking(false);
        }

        public void MoveTo(GridCoord anchor)
        {
            if (!anchor.Equals(_anchor))
                _lastMoveCommandTime = Time.time;

            _anchor = anchor;
        }

        public void SetChaseTargetWorld(Vector3 worldTarget) =>
            _chaseTargetWorld = worldTarget;

        public void ClearChaseTarget() =>
            _chaseTargetWorld = null;

        private void Update()
        {
            if (!IsAlive || _frozen || _mapper == null)
                return;

            if (_use2DVisual && _visual2D != null)
            {
                Update2DMovementAndPresentation();
                return;
            }

            Vector3 current = transform.position;
            Vector3 simWorld = _mapper.ToWorld(_anchor);

            if (_freeChaseEnabled && _chaseTargetWorld.HasValue)
            {
                ApplyFreeChaseMovement(current, simWorld);
                return;
            }

            Vector3 target = simWorld;
            var flatTarget = new Vector3(target.x, current.y, target.z);
            var flatCurrent = new Vector3(current.x, 0f, current.z);
            float dist = Vector3.Distance(flatCurrent, new Vector3(flatTarget.x, 0f, flatTarget.z));
            bool atDestination = dist <= 0.02f;
            bool withinMarchGrace = Time.time - _lastMoveCommandTime < _marchGraceSeconds;

            if (atDestination)
            {
                if (current != flatTarget)
                    transform.position = flatTarget;

                _unitVisual?.SetWalking(withinMarchGrace);
                UpdateIdleBob();
                return;
            }

            _unitVisual?.SetWalking(true);
            Vector3 delta = flatTarget - current;
            delta.y = 0f;
            FaceMovementDirection(delta);

            float step = _moveWorldSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, flatTarget, step);

            UpdateIdleBob();
        }

        private void Update2DMovementAndPresentation()
        {
            Vector3 current = transform.position;
            Vector3 simWorld = _mapper.ToWorld(_anchor);

            if (_freeChaseEnabled && _chaseTargetWorld.HasValue)
            {
                Vector3 chaseWorld = _chaseTargetWorld.Value;
                if (!CombatArenaFreeChaseMovement.ShouldKeepMarching(current, chaseWorld))
                {
                    _visual2D.SetWalking(false);
                    _visual2D.UpdateSortAndBob(transform.position);
                    return;
                }

                Vector3 next = CombatArenaFreeChaseMovement.ComputeStep(
                    current, simWorld, chaseWorld, _moveWorldSpeed, Time.deltaTime,
                    _mapper.CellWidth, _chaseMaxLeadCells);
                next = CombatArenaFreeChaseMovement.ClampToSimLead(next, simWorld, _mapper.CellWidth, _chaseMaxLeadCells);

                Vector3 moveDelta = next - current;
                moveDelta.y = 0f;
                if (moveDelta.sqrMagnitude > 0.0001f)
                    _visual2D.FaceDirection(moveDelta);

                _visual2D.SetWalking(true);
                transform.position = new Vector3(next.x, current.y, next.z);
                _visual2D.UpdateSortAndBob(transform.position);
                return;
            }

            Vector3 target = simWorld;
            var flatTarget = new Vector3(target.x, current.y, target.z);
            var flatCurrent = new Vector3(current.x, 0f, current.z);
            float dist = Vector3.Distance(flatCurrent, new Vector3(flatTarget.x, 0f, flatTarget.z));
            bool atDestination = dist <= 0.02f;
            bool withinMarchGrace = Time.time - _lastMoveCommandTime < _marchGraceSeconds;

            if (atDestination)
            {
                if (current != flatTarget)
                    transform.position = flatTarget;
                _visual2D.SetWalking(withinMarchGrace);
            }
            else
            {
                _visual2D.SetWalking(true);
                Vector3 delta = flatTarget - current;
                delta.y = 0f;
                _visual2D.FaceDirection(delta);
                float step = _moveWorldSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(current, flatTarget, step);
            }

            _visual2D.UpdateSortAndBob(transform.position);
        }

        private void ApplyFreeChaseMovement(Vector3 current, Vector3 simWorld)
        {
            Vector3 chaseWorld = _chaseTargetWorld.Value;

            if (!CombatArenaFreeChaseMovement.ShouldKeepMarching(current, chaseWorld))
            {
                _unitVisual?.SetWalking(false);
                UpdateIdleBob();
                return;
            }

            Vector3 next = CombatArenaFreeChaseMovement.ComputeStep(
                current,
                simWorld,
                chaseWorld,
                _moveWorldSpeed,
                Time.deltaTime,
                _mapper.CellWidth,
                _chaseMaxLeadCells);

            next = CombatArenaFreeChaseMovement.ClampToSimLead(
                next,
                simWorld,
                _mapper.CellWidth,
                _chaseMaxLeadCells);

            Vector3 moveDelta = next - current;
            moveDelta.y = 0f;
            if (moveDelta.sqrMagnitude > 0.0001f)
                FaceMovementDirection(moveDelta);

            _unitVisual?.SetWalking(true);
            transform.position = new Vector3(next.x, current.y, next.z);
            UpdateIdleBob();
        }

        private void FaceMovementDirection(Vector3 delta)
        {
            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel && delta.sqrMagnitude > 0.0001f)
                _unitVisual.FaceWorldDirection(delta);
            else if (_useProceduralVisual && delta.sqrMagnitude > 0.0001f)
                _presentationRoot.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private void UpdateIdleBob()
        {
            if (_presentationRoot == null || !_useProceduralVisual && !_useModelVisual)
                return;

            _bobTime += Time.deltaTime * 6f;
            float bob = Mathf.Sin(_bobTime) * 0.02f;
            var local = _presentationRoot.localPosition;
            _presentationRoot.localPosition = new Vector3(
                Mathf.MoveTowards(local.x, 0f, Time.deltaTime),
                bob,
                Mathf.MoveTowards(local.z, 0f, Time.deltaTime * 3f));
        }

        public void PlayAttackToward(
            Vector3 targetWorld,
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle = null,
            Action onImpact = null)
        {
            if (_frozen || !IsAlive)
                return;

            if (_use2DVisual && _visual2D != null)
            {
                _visual2D.PlayAttack(profile, targetWorld, onMuzzle, onImpact);
                return;
            }

            if (profile.UseForwardStep)
            {
                if (_useProceduralVisual)
                    PlayProceduralAttackLunge();
                else
                {
                    if (_lungeRoutine != null)
                        StopCoroutine(_lungeRoutine);
                    _lungeRoutine = StartCoroutine(LungeRoutine(targetWorld));
                }
            }

            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel)
            {
                _unitVisual.PlayAttackToward(targetWorld, profile, onMuzzle, onImpact);
                return;
            }

            if (_useProceduralVisual && (onMuzzle != null || onImpact != null))
            {
                if (_attackTimingRoutine != null)
                    StopCoroutine(_attackTimingRoutine);
                _attackTimingRoutine = StartCoroutine(
                    ProceduralAttackTimingRoutine(profile, onMuzzle, onImpact));
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
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            if (_attackTimingRoutine != null)
                StopCoroutine(_attackTimingRoutine);
            _unitVisual?.SetWalking(false);

            if (_use2DVisual && _visual2D != null)
            {
                _visual2D.PlayDeath(() =>
                {
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
                return;
            }

            if (_useModelVisual && _unitVisual != null && _unitVisual.HasModel)
            {
                _unitVisual.PlayDeath(() =>
                {
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
                return;
            }

            if (_useProceduralVisual)
            {
                StartCoroutine(ProceduralDeathRoutine(onComplete));
                return;
            }

            StartCoroutine(DeathRoutine(onComplete));
        }

        private void EnsurePresentationRoot()
        {
            if (_presentationRoot != null)
                return;

            var existing = transform.Find("PresentationRoot");
            if (existing != null)
            {
                _presentationRoot = existing;
                return;
            }

            var rootGo = new GameObject("PresentationRoot");
            rootGo.transform.SetParent(transform, false);
            _presentationRoot = rootGo.transform;
        }

        private void PlayProceduralAttackLunge()
        {
            if (_presentationRoot == null)
                return;

            var local = _presentationRoot.localPosition;
            _presentationRoot.localPosition = new Vector3(local.x, local.y, 0.15f);
            _presentationRoot.localScale = Vector3.one;
        }

        private IEnumerator ProceduralAttackTimingRoutine(
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            Vector3 muzzle = transform.position + (_presentationRoot != null ? _presentationRoot.forward : transform.forward) * 0.35f + Vector3.up * 0.8f;
            onMuzzle?.Invoke(muzzle);

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            PlayProceduralHitFlash();
            onImpact?.Invoke();
            _attackTimingRoutine = null;
        }

        private void PlayProceduralHitFlash()
        {
            if (_presentationRoot == null)
                return;

            _presentationRoot.localScale = Vector3.one * 0.92f;
        }

        private IEnumerator ProceduralDeathRoutine(Action onComplete)
        {
            float duration = 0.5f;
            Transform target = _presentationRoot != null ? _presentationRoot : transform;
            Vector3 startScale = target.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                target.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                yield return null;
            }

            onComplete?.Invoke();
            gameObject.SetActive(false);
        }

        private static float ResolveFallbackMoveSpeed(float moveLerpSeconds)
        {
            if (moveLerpSeconds <= 0f)
                return 4f;

            return 1.8f / moveLerpSeconds;
        }

        public void ResetForPool()
        {
            InstanceId = null;
            PieceId = null;
            IsAlive = true;
            transform.localScale = Vector3.one;
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            if (_attackTimingRoutine != null)
                StopCoroutine(_attackTimingRoutine);
            _lungeRoutine = null;
            _attackTimingRoutine = null;
            _frozen = false;
            _useModelVisual = false;
            _useProceduralVisual = false;
            _use2DVisual = false;
            _freeChaseEnabled = false;
            _chaseTargetWorld = null;
            _presentationRoot = null;
            _moveWorldSpeed = 1f;
            _moveLerpFallbackSeconds = 0.4f;
            _marchGraceSeconds = 2.4f;
            _lastMoveCommandTime = -999f;
            _attackProfile = CombatAttackPresentationProfile.InfantryRifle;
            ClearPresentation();
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

        private void ClearPresentation()
        {
            if (_visual2D != null)
            {
                _visual2D.Clear();
                Destroy(_visual2D);
                _visual2D = null;
            }

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

            if (_presentationRoot != null)
            {
                for (int i = _presentationRoot.childCount - 1; i >= 0; i--)
                    Destroy(_presentationRoot.GetChild(i).gameObject);

                Destroy(_presentationRoot.gameObject);
                _presentationRoot = null;
            }

            var quad = transform.Find("BillboardQuad");
            if (quad != null)
                Destroy(quad.gameObject);
        }
    }
}
