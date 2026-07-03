using System;
using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Drives a single 2D combat unit's movement and presentation in the arena.</summary>
    public sealed class CombatUnitActor : MonoBehaviour
    {
        private CombatUnitVisual2D _visual2D;
        private CombatGridMapper _mapper;
        private GridCoord _anchor;
        private float _moveWorldSpeed = 1f;
        private float _moveLerpFallbackSeconds = 0.4f;
        private float _marchGraceSeconds = 2.4f;
        private float _lastMoveCommandTime = -999f;
        private bool _frozen;
        private bool _freeChaseEnabled;
        private float _chaseMaxLeadCells = 4f;
        private Vector3? _chaseTargetWorld;
        private Vector3 _smoothedSimWorld;
        private CombatAttackPresentationProfile _attackProfile;

        public string InstanceId { get; private set; }
        public string PieceId { get; private set; }
        public GridCoord Anchor => _anchor;
        public bool IsAlive { get; private set; } = true;
        public CombatAttackPresentationProfile AttackProfile => _attackProfile;

        public void Initialize(
            string instanceId,
            string pieceId,
            Transform cameraTransform,
            CombatGridMapper mapper,
            GridCoord anchor,
            float moveLerpSeconds,
            float moveWorldSpeed,
            float marchGraceSeconds,
            CombatAttackPresentationProfile attackProfile,
            PieceDefinitionSO pieceDefinition = null,
            CombatSide combatSide = CombatSide.Player,
            bool useFreeChaseMovement = false,
            float chaseMaxLeadCells = 4f)
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
            _freeChaseEnabled = useFreeChaseMovement;
            _chaseMaxLeadCells = chaseMaxLeadCells > 0f ? chaseMaxLeadCells : 4f;
            _chaseTargetWorld = null;
            IsAlive = true;
            gameObject.SetActive(true);

            ClearPresentation();

            if (pieceDefinition != null)
            {
                _visual2D = gameObject.AddComponent<CombatUnitVisual2D>();
                int squadSize = Mathf.Clamp(pieceDefinition.manpowerCost, 1, 5);
                _visual2D.Build(
                    pieceDefinition,
                    combatSide,
                    cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null,
                    squadSize);
            }

            SnapToAnchor(anchor);
            _smoothedSimWorld = _mapper.ToWorld(anchor);
        }

        public void SetFrozen(bool frozen) => _frozen = frozen;

        public void SnapToAnchor(GridCoord anchor)
        {
            _anchor = anchor;
            transform.position = _mapper.ToWorld(anchor);
            _smoothedSimWorld = transform.position;
            _visual2D?.SetWalking(false);
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
            if (!IsAlive || _frozen || _mapper == null || _visual2D == null)
                return;

            if (_visual2D.BlocksLocomotion)
            {
                _visual2D.SetWalking(false);
                _visual2D.UpdateSortAndBob(transform.position);
                return;
            }

            Vector3 current = transform.position;
            Vector3 simWorld = _mapper.ToWorld(_anchor);
            _smoothedSimWorld = Vector3.Lerp(
                _smoothedSimWorld,
                simWorld,
                Mathf.Clamp01(Time.deltaTime * 14f));

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
                    current, _smoothedSimWorld, chaseWorld, _moveWorldSpeed, Time.deltaTime,
                    _mapper.CellWidth, _chaseMaxLeadCells);
                next = CombatArenaFreeChaseMovement.ClampToSimLead(
                    next, _smoothedSimWorld, _mapper.CellWidth, _chaseMaxLeadCells);

                Vector3 moveDelta = next - current;
                moveDelta.y = 0f;
                if (moveDelta.sqrMagnitude > 0.0001f)
                    _visual2D.FaceDirection(moveDelta);

                _visual2D.SetWalking(true);
                transform.position = new Vector3(next.x, current.y, next.z);
                _visual2D.UpdateSortAndBob(transform.position);
                return;
            }

            var flatTarget = new Vector3(_smoothedSimWorld.x, current.y, _smoothedSimWorld.z);
            var flatCurrent = new Vector3(current.x, 0f, current.z);
            float dist = Vector3.Distance(flatCurrent, new Vector3(flatTarget.x, 0f, flatTarget.z));
            bool atDestination = dist <= 0.04f;
            bool withinMarchGrace = Time.time - _lastMoveCommandTime < _marchGraceSeconds;

            if (atDestination)
            {
                transform.position = Vector3.Lerp(current, flatTarget, Mathf.Clamp01(Time.deltaTime * 18f));
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

        public void PlayAttackToward(
            Vector3 targetWorld,
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle = null,
            Action onImpact = null)
        {
            if (_frozen || !IsAlive)
                return;

            _visual2D?.PlayAttack(profile, targetWorld, onMuzzle, onImpact);
        }

        public void PlayHurt()
        {
            if (_frozen || !IsAlive)
                return;

            _visual2D?.PlayHurt();
        }

        public void PlayDeath(Action onComplete)
        {
            IsAlive = false;

            if (_visual2D != null)
            {
                _visual2D.PlayDeath(() =>
                {
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
                return;
            }

            StartCoroutine(DeathRoutine(onComplete));
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
            _frozen = false;
            _freeChaseEnabled = false;
            _chaseTargetWorld = null;
            _smoothedSimWorld = Vector3.zero;
            _moveWorldSpeed = 1f;
            _moveLerpFallbackSeconds = 0.4f;
            _marchGraceSeconds = 2.4f;
            _lastMoveCommandTime = -999f;
            _attackProfile = CombatAttackPresentationProfile.InfantryRifle;
            ClearPresentation();
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
        }
    }
}
