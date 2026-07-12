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

        /// <summary>Scene-installed visual backend (the ToonInk3D arena installs it via
        /// <see cref="CombatUnitVisual3DInstaller"/>). Actors are pooled/runtime-created,
        /// so this is a static hook rather than a serialized field.</summary>
        public static Func<CombatUnitActor, PieceDefinitionSO, CombatSide, Camera, ICombatUnitVisual>
            VisualFactory;

        public string InstanceId { get; private set; }
        public string PieceId { get; private set; }
        public GridCoord Anchor => _anchor;
        public bool IsAlive { get; private set; } = true;
        /// <summary>Which army the unit fights for — a broken unit flees toward this side's board edge.</summary>
        public CombatSide Side { get; private set; } = CombatSide.Player;
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
            Side = combatSide;
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

                // The scene-installed 3D backend is the only visual pipeline. A missing
                // factory (or one that declines) is a wiring bug — log it and run the
                // sim presentation without a visual rather than throwing mid-fight.
                _visual = VisualFactory?.Invoke(this, pieceDefinition, combatSide, arenaCamera);
                if (_visual == null)
                {
                    Debug.LogError(
                        $"[CombatUnitActor] No visual for '{pieceDefinition.id}' — " +
                        "CombatUnitVisual3DInstaller did not install a VisualFactory " +
                        "(or it declined). The unit will be invisible.", this);
                }
            }

            SnapToAnchor(anchor);
            _smoothedSimWorld = _mapper.ToWorld(anchor);

            // Face the enemy line from the first frame. A fresh visual's target rotation
            // is identity (+z / screen-north), and nothing else faces a unit until it
            // moves or attacks — so idle armies stood staring north through the opening
            // tactical pause, and 0-speed units held north all fight (2026-07-12 playtest).
            _visual?.FaceDirection(combatSide == CombatSide.Player ? Vector3.right : Vector3.left);
        }

        /// <summary>Update the unit's HP display (0..1) — the visual's own presentation
        /// (3D base-ring fill).</summary>
        public void SetHealthFraction(float fraction)
        {
            if (!IsAlive)
                return;

            _visual?.SetHealthFraction(fraction);
        }

        /// <summary>Update the unit's Morale display (0..1). The presenter only calls this
        /// for units that can break, so morale-immune units never grow a strip.</summary>
        public void SetMoraleFraction(float fraction)
        {
            if (!IsAlive)
                return;

            _visual?.SetMoraleFraction(fraction);
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

        // Rout flee pacing: a panicked sprint over the calibrated march speed, with a few
        // solid strides before the soft dissolve starts, and a hard cap so a stalled exit
        // can never wedge the fight-end wait.
        private const float RoutFleeSpeedScale = 1.35f;
        private const float RoutFleeLeadSeconds = 0.45f;
        private const float RoutMaxSeconds = 8f;

        /// <summary>Rout presentation (ADR-0005): the unit ESCAPES — no die clip, no death
        /// VFX/audio. Infantry reuse the march machinery to run for their own board edge
        /// while the softer rout dissolve removes the visual; vehicles slump-abandon in
        /// place (<see cref="ICombatUnitVisual.FleesWhenBroken"/>).</summary>
        public void PlayRout(Vector3 fleeWorldTarget, Action onComplete)
        {
            if (!IsAlive)
            {
                onComplete?.Invoke();
                return;
            }

            IsAlive = false; // leaves the sim-anchor march loop; the flee routine drives movement
            ClearChaseTarget();

            if (_visual == null)
            {
                // No visual pipeline (wiring bug fallback): reuse the silent scale-out.
                StartCoroutine(DeathRoutine(onComplete));
                return;
            }

            if (_visual.FleesWhenBroken)
            {
                StartCoroutine(RoutFleeRoutine(fleeWorldTarget, onComplete));
                return;
            }

            _visual.SetWalking(false);
            _visual.PlayRoutExit(() =>
            {
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
        }

        private IEnumerator RoutFleeRoutine(Vector3 fleeWorldTarget, Action onComplete)
        {
            Vector3 fleeDir = fleeWorldTarget - transform.position;
            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.0001f)
                fleeDir = Vector3.left;

            _visual.FaceDirection(fleeDir);
            _visual.SetWalking(true);

            bool exitStarted = false;
            bool exitDone = false;
            float speed = _moveWorldSpeed * RoutFleeSpeedScale;

            for (float t = 0f; !exitDone && t < RoutMaxSeconds; t += Time.deltaTime)
            {
                if (!exitStarted && t >= RoutFleeLeadSeconds)
                {
                    exitStarted = true;
                    _visual.PlayRoutExit(() => exitDone = true);
                }

                // Same anti-lunge step cap as the live march — the runner sprints, never pops.
                float step = speed * Mathf.Min(Time.deltaTime, MaxMoveDeltaTime);
                transform.position = Vector3.MoveTowards(transform.position, fleeWorldTarget, step);
                _visual.UpdateSortAndBob(transform.position);
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
