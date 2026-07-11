using System.Collections.Generic;
using DeadManZone.Core.Combat;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Scripted punch-in camera beats per the combat-arena spec (§1/§6): on a kill the camera
    /// dollies briefly toward the kill position, holds, then eases back to the home framing.
    /// Two tiers only — tier 1 = single kill (modest punch), tier 2 = multi-kill or the
    /// fight-ending kill (deeper punch, slower return). Discipline per spec: max one active
    /// punch, new tier-1 triggers inside the cooldown are skipped (spam degrades the currency),
    /// the camera never rotates or leaves the battlefield framing (pure dolly, SmoothDamp —
    /// retargets glide, never snap). Optional kill hit-stop (a tiny time-scale dip) rides the
    /// same beat. Consumes only replayed <see cref="CombatEvent"/>s ("destroyed"/"fight_end") —
    /// no game rules, no sim access.
    /// </summary>
    public sealed class CombatArenaPunchInCamera : MonoBehaviour
    {
        [SerializeField] private CombatDirector director;
        [SerializeField] private CombatArenaPresenter presenter;
        [SerializeField] private Camera arenaCamera;

        [Header("Punch-in")]
        [SerializeField] private bool enablePunchIns = true;
        [SerializeField, Tooltip("Tier-1 kill: fraction of camera→kill distance to dolly in.")]
        private float tier1PunchFraction = 0.14f;
        [SerializeField, Tooltip("Tier-2 (multi-kill / fight-ending kill): deeper dolly fraction.")]
        private float tier2PunchFraction = 0.22f;
        [SerializeField] private float maxPunchDistance = 3.5f;
        [SerializeField] private float tier1HoldSeconds = 0.4f;
        [SerializeField] private float tier2HoldSeconds = 0.7f;
        [SerializeField, Tooltip("SmoothDamp time easing into the punched pose.")]
        private float punchInSmoothTime = 0.14f;
        [SerializeField] private float tier1ReturnSmoothTime = 0.35f;
        [SerializeField, Tooltip("Tier 2 lingers on the way home.")]
        private float tier2ReturnSmoothTime = 0.6f;
        [SerializeField, Tooltip("Spec §1: skip new punch-ins fired within this window.")]
        private float punchCooldownSeconds = 2f;
        [SerializeField, Tooltip("A second kill within this window upgrades the active punch to tier 2.")]
        private float multiKillWindowSeconds = 0.9f;

        [Header("Kill hit-stop")]
        [SerializeField, Tooltip("Tiny time-scale dip on kill beats. Off = no Time.timeScale writes.")]
        private bool enableHitStop = true;
        [SerializeField, Range(0.02f, 0.12f)] private float hitStopSeconds = 0.05f;
        [SerializeField, Range(0f, 0.5f)] private float hitStopTimeScale = 0.12f;

        // Aim slightly above the feet so the dolly ray points at the unit, not the dirt.
        private const float AimHeightWorld = 0.9f;
        // The fight-ending kill gets its beat even if the per-kill cooldown swallowed it.
        private const float FightEndGraceSeconds = 2f;

        // Victim positions are cached from earlier events in the same fight: by the time
        // "destroyed" replays, the presenter has already pulled the actor from its live set.
        private readonly Dictionary<string, Vector3> _lastKnownActorPositions = new();

        private Vector3 _homePosition;
        private Vector3 _punchTargetOffset;
        private Vector3 _dollyVelocity;
        private float _punchEndTime;
        private int _activeTier;
        private bool _punching;
        private float _lastPunchStartTime = -999f;
        private float _lastKillTime = -999f;
        private Vector3 _lastKillWorld;
        private bool _hasLastKillWorld;

        private bool _hitStopActive;
        private float _hitStopEndUnscaledTime;
        private float _storedTimeScale = 1f;

        private void OnEnable()
        {
            if (director == null)
                director = GetComponent<CombatDirector>();
            if (presenter == null)
                presenter = GetComponent<CombatArenaPresenter>();

            if (director != null)
                director.EventReplayed += OnEventReplayed;
        }

        private void OnDisable()
        {
            if (director != null)
                director.EventReplayed -= OnEventReplayed;

            RestoreTimeScale();
            _lastKnownActorPositions.Clear();
            _punching = false;
            _activeTier = 0;
            _dollyVelocity = Vector3.zero;
        }

        private void OnEventReplayed(CombatEvent combatEvent)
        {
            if (!CombatArenaSession.IsActive || combatEvent == null)
                return;

            CacheActorPositions();

            switch (combatEvent.ActionType)
            {
                case "destroyed":
                    HandleKill(combatEvent);
                    break;
                case "fight_end":
                    HandleFightEnd();
                    break;
            }
        }

        /// <summary>"destroyed" carries the victim in ActorId (same read as ArmyHealthReplayTracker).</summary>
        private void HandleKill(CombatEvent combatEvent)
        {
            if (string.IsNullOrEmpty(combatEvent.ActorId) ||
                !_lastKnownActorPositions.TryGetValue(combatEvent.ActorId, out var killWorld))
                return;

            TriggerHitStop();

            float now = Time.time;
            bool withinMultiKillWindow = now - _lastKillTime <= multiKillWindowSeconds;
            _lastKillTime = now;
            _lastKillWorld = killWorld;
            _hasLastKillWorld = true;

            if (!enablePunchIns)
                return;

            if (_punching && withinMultiKillWindow)
            {
                UpgradeToTier2(killWorld);
                return;
            }

            // Spec discipline: one punch at a time; skip triggers inside the cooldown.
            if (_punching || now - _lastPunchStartTime < punchCooldownSeconds)
                return;

            StartPunch(killWorld, tier: 1);
        }

        private void HandleFightEnd()
        {
            if (!enablePunchIns || !_hasLastKillWorld)
                return;

            if (_punching)
            {
                UpgradeToTier2(_lastKillWorld);
                return;
            }

            // The final kill is the one beat that matters — grant it even if the
            // per-kill cooldown swallowed its own tier-1 punch.
            if (Time.time - _lastKillTime <= FightEndGraceSeconds)
                StartPunch(_lastKillWorld, tier: 2);
        }

        private void StartPunch(Vector3 killWorld, int tier)
        {
            if (arenaCamera == null)
                arenaCamera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (arenaCamera == null)
                return;

            // Home = wherever the framing camera currently rests (LateUpdate keeps it fresh
            // while idle, so external reframing is absorbed rather than fought).
            if (!_punching)
                _homePosition = arenaCamera.transform.position;

            var offset = ComputePunchOffset(killWorld, tier);
            if (offset == Vector3.zero)
                return;

            _punchTargetOffset = offset;
            _activeTier = tier;
            _punching = true;
            _lastPunchStartTime = Time.time;
            _punchEndTime = Time.time + punchInSmoothTime +
                            (tier >= 2 ? tier2HoldSeconds : tier1HoldSeconds);
        }

        private void UpgradeToTier2(Vector3 killWorld)
        {
            var offset = ComputePunchOffset(killWorld, tier: 2);
            if (offset != Vector3.zero)
                _punchTargetOffset = offset;

            _activeTier = 2;
            _punchEndTime = Time.time + punchInSmoothTime + tier2HoldSeconds;
        }

        private Vector3 ComputePunchOffset(Vector3 killWorld, int tier)
        {
            Vector3 aim = killWorld + Vector3.up * AimHeightWorld;
            Vector3 toKill = aim - _homePosition;
            float distance = toKill.magnitude;
            if (distance < 1f)
                return Vector3.zero;

            float fraction = tier >= 2 ? tier2PunchFraction : tier1PunchFraction;
            float punchDistance = Mathf.Min(distance * fraction, maxPunchDistance);
            return toKill / distance * punchDistance;
        }

        private void LateUpdate()
        {
            UpdateHitStop();

            if (arenaCamera == null)
                arenaCamera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (arenaCamera == null)
                return;

            var cameraTransform = arenaCamera.transform;
            if (!_punching)
            {
                // Idle: track the resting pose so any external framing change becomes home.
                _homePosition = cameraTransform.position;
                return;
            }

            bool holding = enablePunchIns && Time.time < _punchEndTime;
            Vector3 desired = holding ? _homePosition + _punchTargetOffset : _homePosition;
            float smoothTime = holding
                ? punchInSmoothTime
                : (_activeTier >= 2 ? tier2ReturnSmoothTime : tier1ReturnSmoothTime);

            cameraTransform.position = Vector3.SmoothDamp(
                cameraTransform.position, desired, ref _dollyVelocity, Mathf.Max(0.01f, smoothTime));

            if (!holding &&
                (cameraTransform.position - _homePosition).sqrMagnitude < 0.0004f &&
                _dollyVelocity.sqrMagnitude < 0.01f)
            {
                cameraTransform.position = _homePosition;
                _dollyVelocity = Vector3.zero;
                _punching = false;
                _activeTier = 0;
            }
        }

        private void CacheActorPositions()
        {
            if (presenter == null)
                return;

            foreach (var actor in presenter.GetActiveActors())
            {
                if (actor != null && !string.IsNullOrEmpty(actor.InstanceId))
                    _lastKnownActorPositions[actor.InstanceId] = actor.transform.position;
            }
        }

        private void TriggerHitStop()
        {
            if (!enableHitStop || hitStopSeconds <= 0f)
                return;

            if (!_hitStopActive)
            {
                _storedTimeScale = Time.timeScale;
                Time.timeScale = _storedTimeScale * hitStopTimeScale;
                _hitStopActive = true;
            }

            // Overlapping kills extend the dip instead of stacking/restacking scales.
            _hitStopEndUnscaledTime = Time.unscaledTime + hitStopSeconds;
        }

        private void UpdateHitStop()
        {
            if (_hitStopActive && Time.unscaledTime >= _hitStopEndUnscaledTime)
                RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (!_hitStopActive)
                return;

            Time.timeScale = _storedTimeScale;
            _hitStopActive = false;
        }
    }
}
