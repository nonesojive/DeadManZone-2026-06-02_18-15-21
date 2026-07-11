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
        // Movement anti-lunge: cap the per-frame step to a 40fps-equivalent, and allow catch-up
        // at up to ~1.6x that so a lagging unit closes the gap over a few smooth frames, never a jump.
        private const float MaxMoveDeltaTime = 1f / 40f;
        private const float MaxCatchupStepScale = 1.6f;

        // SmoothDamp follow for the free-chase march: eases catch-ups so they glide, not jump.
        // Target = anchor biased slightly toward the goal (keeps marching through pacing gaps).
        // Low speed cap => any catch-up is a gentle glide, not a sprint.
        private const float ChaseSmoothTime = 0.14f;
        private const float ChaseMaxSpeedScale = 1.25f;
        private const float ChaseGoalBias = 0.3f;
        // Walk/idle animation switch: compare instantaneous speed (delta/dt), not the
        // raw per-frame delta — a fixed delta threshold flips with frame rate (at the
        // editor's ~500fps a marching unit moves ~0.003u/frame and read as "idle").
        private const float MovingSpeedFraction = 0.15f;
        private const float MovingSpeedFloorWorldPerSec = 0.05f;
        private Vector3 _chaseVelocity;

        private ICombatUnitVisual _visual;
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
        private CombatUnitHealthBar _healthBar;

        /// <summary>Scene-installed visual backend override (e.g. the ToonInk3D arena via
        /// <see cref="CombatUnitVisual3DInstaller"/>). Null — the default — keeps the 2D
        /// sprite pipeline byte-identical. Actors are pooled/runtime-created, so this is a
        /// static hook rather than a serialized field.</summary>
        public static Func<CombatUnitActor, PieceDefinitionSO, CombatSide, Camera, ICombatUnitVisual>
            VisualFactory;

        public string InstanceId { get; private set; }
        public string PieceId { get; private set; }
        public GridCoord Anchor => _anchor;
        public bool IsAlive { get; private set; } = true;
        public CombatAttackPresentationProfile AttackProfile => _attackProfile;

        /// <summary>Seconds the death presentation takes (through the visual seam);
        /// falls back to the actor's own scale-out routine duration.</summary>
        public float DeathSeconds => _visual?.DeathSeconds ?? FallbackDeathSeconds;

        private const float FallbackDeathSeconds = 0.35f;

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
                var arenaCamera = cameraTransform != null
                    ? cameraTransform.GetComponent<Camera>()
                    : null;

                // Scene-installed backend (3D toon-ink) wins; the 2D sprite pipeline
                // stays the default when no factory is installed (or it declines).
                _visual = VisualFactory?.Invoke(this, pieceDefinition, combatSide, arenaCamera);
                if (_visual == null)
                {
                    var visual2D = gameObject.AddComponent<CombatUnitVisual2D>();
                    int squadSize = Mathf.Clamp(pieceDefinition.manpowerCost, 1, 5);
                    visual2D.Build(pieceDefinition, combatSide, arenaCamera, squadSize);
                    _visual = visual2D;
                }
            }

            SnapToAnchor(anchor);
            _smoothedSimWorld = _mapper.ToWorld(anchor);

            _healthBar = CombatUnitHealthBar.Attach(
                this,
                combatSide,
                cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null,
                _visual != null ? _visual.VisualHeight : 1.8f);
        }

        /// <summary>Update the unit's HP bar (0..1); hidden at full health.</summary>
        public void SetHealthFraction(float fraction)
        {
            if (IsAlive)
                _healthBar?.SetFraction(fraction);
        }

        public void SetFrozen(bool frozen) => _frozen = frozen;

        public void SnapToAnchor(GridCoord anchor)
        {
            _anchor = anchor;
            transform.position = _mapper.ToWorld(anchor);
            _smoothedSimWorld = transform.position;
            _chaseVelocity = Vector3.zero;
            _visual?.SetWalking(false);
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
            if (!IsAlive || _frozen || _mapper == null || _visual == null)
                return;

            if (_visual.BlocksLocomotion)
            {
                _visual.SetWalking(false);
                _visual.UpdateSortAndBob(transform.position);
                return;
            }

            Vector3 current = transform.position;
            Vector3 simWorld = _mapper.ToWorld(_anchor);
            _smoothedSimWorld = Vector3.Lerp(
                _smoothedSimWorld,
                simWorld,
                Mathf.Clamp01(Time.deltaTime * 14f));

            // Cap the frame step so a hitch (long dt) or a catch-up from accumulated lag can't
            // teleport the unit forward. The visual march lags its sparse sim anchor; letting it
            // sprint the whole gap in one frame read as the "lunge 2 squares" glitch. Capping to a
            // brisk-walk maximum makes the catch-up a smooth few frames instead of a jump.
            float moveDt = Mathf.Min(Time.deltaTime, MaxMoveDeltaTime);
            float maxStep = _moveWorldSpeed * MaxMoveDeltaTime * MaxCatchupStepScale;

            if (_freeChaseEnabled && _chaseTargetWorld.HasValue)
            {
                // Track the (smoothed) sim anchor with a SmoothDamp, biased slightly toward the
                // chase goal so the unit keeps marching through the presentation's pacing gaps
                // instead of reaching a leash point and hard-stopping (the old stop → sprint-resume
                // read as a jump). SmoothDamp eases in/out and a LOW speed cap keeps any catch-up a
                // gentle glide — the visual may trail its bursty anchor slightly, but never lunges.
                Vector3 chaseWorld = _chaseTargetWorld.Value;
                Vector3 anchorFlat = new(_smoothedSimWorld.x, current.y, _smoothedSimWorld.z);
                Vector3 goalFlat = new(chaseWorld.x, current.y, chaseWorld.z);
                Vector3 targetFlat = Vector3.Lerp(anchorFlat, goalFlat, ChaseGoalBias);

                float maxSpeed = _moveWorldSpeed * ChaseMaxSpeedScale;
                Vector3 next = Vector3.SmoothDamp(
                    current, targetFlat, ref _chaseVelocity, ChaseSmoothTime, maxSpeed, moveDt);

                Vector3 moveDelta = next - current;
                moveDelta.y = 0f;
                float movingThreshold = Mathf.Max(
                    _moveWorldSpeed * MovingSpeedFraction,
                    MovingSpeedFloorWorldPerSec) * moveDt;
                bool moving = moveDelta.magnitude > movingThreshold;
                if (moving)
                    _visual.FaceDirection(moveDelta);

                _visual.SetWalking(moving);
                transform.position = new Vector3(next.x, current.y, next.z);
                _visual.UpdateSortAndBob(transform.position);
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
                _visual.SetWalking(withinMarchGrace);
            }
            else
            {
                _visual.SetWalking(true);
                Vector3 delta = flatTarget - current;
                delta.y = 0f;
                _visual.FaceDirection(delta);
                float step = Mathf.Min(_moveWorldSpeed * moveDt, maxStep);
                transform.position = Vector3.MoveTowards(current, flatTarget, step);
            }

            _visual.UpdateSortAndBob(transform.position);
        }

        public void PlayAttackToward(
            Vector3 targetWorld,
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle = null,
            Action onImpact = null)
        {
            if (_frozen || !IsAlive)
                return;

            _visual?.PlayAttack(profile, targetWorld, onMuzzle, onImpact);
        }

        public void PlayHurt()
        {
            if (_frozen || !IsAlive)
                return;

            _visual?.PlayHurt();
        }

        public void PlayDeath(Action onComplete)
        {
            IsAlive = false;
            _healthBar?.Hide();

            if (_visual != null)
            {
                _visual.PlayDeath(() =>
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
            _chaseVelocity = Vector3.zero;
            _moveWorldSpeed = 1f;
            _moveLerpFallbackSeconds = 0.4f;
            _marchGraceSeconds = 2.4f;
            _lastMoveCommandTime = -999f;
            _attackProfile = CombatAttackPresentationProfile.InfantryRifle;
            _healthBar?.Clear();
            ClearPresentation();
        }

        private IEnumerator DeathRoutine(Action onComplete)
        {
            float duration = FallbackDeathSeconds;
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
            if (_visual != null)
            {
                _visual.Clear();
                if (_visual is Component visualComponent)
                    Destroy(visualComponent);
                _visual = null;
            }
        }
    }
}
