using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private CombatArenaAudioPresenter audio;

        private ICombatArenaVfxPresenter _activeVfx;

        private readonly Dictionary<string, CombatUnitActor> _actors = new();
        private readonly CombatReplayState _replayState = new();
        private readonly Dictionary<string, PieceDefinitionSO> _piecesById = new();
        private readonly ArmyHealthReplayTracker _unitHealth = new();

        // 2026-07-17 Oathborn transport tentpole (§2.5 Armored Ark): every cell, keyed by
        // instance id, so an embarked cargo piece's Definition/Side can be looked up to spawn
        // its actor later (on "transport_unload"/"transport_spill") even though it never got
        // one at InitializeArena time.
        private readonly Dictionary<string, BattlefieldCell> _cellsById = new();

        // Cargo instance ids that have ALREADY disembarked as of the events replayed into this
        // presentation (used only by the save/resume path, SyncActorsToAnchors, to tell
        // "still embarked — stay hidden" apart from "already unloaded before the save").
        private readonly HashSet<string> _disembarkedCargoIds = new();

        // Replay-side morale bookkeeping (mirrors _unitHealth): only units that can break
        // (Definition.MaxMorale > 0) are registered, so morale-immune units never get a
        // strip. "morale_damage" drains it, "rout" zeroes it — no game rules, just replay.
        private sealed class UnitMorale
        {
            public int Max;
            public int Current;
        }

        private readonly Dictionary<string, UnitMorale> _unitMorale = new();

        private CombatUnitActorPool _pool;
        private CombatGridMapper _mapper;
        private BattlefieldState _battlefield;
        private CombatArenaChaseController _chaseController;
        private ContentRegistry _registry;
        private Transform _arenaCameraTransform;
        private CombatArenaBuildingSpawner _buildingSpawner = new();

        public bool HasBuildingVisualForTests(string instanceId) => _buildingSpawner.HasVisual(instanceId);

        public bool IsPresentationFrozen { get; private set; }

        /// <summary>True while a unit is still playing its death presentation (die strip / shrink).</summary>
        public bool HasPendingDeathPresentations => _pendingDeathPresentations > 0;

        private int _pendingDeathPresentations;

        public void SetPresentationFrozen(bool frozen) => IsPresentationFrozen = frozen;

        public IEnumerator WaitForPendingDeathPresentations()
        {
            while (_pendingDeathPresentations > 0)
                yield return null;
        }

        private void Awake()
        {
            EnsureReferences();

            var database = ContentDatabase.Load();
            _registry = ContentRegistryProvider.Build(database);
            BuildPieceLookup(database);
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (combatDirector != null)
            {
                combatDirector.EventReplayed += OnEventReplayed;
                combatDirector.SegmentPlaybackFinished += OnSegmentPlaybackFinished;
            }
        }

        private void OnDisable()
        {
            if (combatDirector != null)
            {
                combatDirector.EventReplayed -= OnEventReplayed;
                combatDirector.SegmentPlaybackFinished -= OnSegmentPlaybackFinished;
            }
        }

        public IEnumerable<CombatUnitActor> GetActiveActors() => _actors.Values;

        public void Configure(CombatDirector director, CombatArenaAudioPresenter arenaAudio = null)
        {
            if (director != null)
            {
                if (isActiveAndEnabled && combatDirector != null)
                    combatDirector.EventReplayed -= OnEventReplayed;

                combatDirector = director;

                if (isActiveAndEnabled)
                {
                    combatDirector.EventReplayed += OnEventReplayed;
                    combatDirector.SegmentPlaybackFinished += OnSegmentPlaybackFinished;
                }
            }

            if (arenaAudio != null)
                audio = arenaAudio;
        }

        public void SnapAllActorsToReplayAnchors()
        {
            foreach (var pair in _replayState.Anchors)
            {
                if (!_actors.TryGetValue(pair.Key, out var actor))
                    continue;

                actor.ClearChaseTarget();
                actor.SnapToAnchor(pair.Value);
            }
        }

        private void OnSegmentPlaybackFinished()
        {
            // Top Troops chase moves actors ahead of grid anchors; snapping on pause looks like a rewind.
            if (UsesFreeChaseMovement())
                return;

            SnapAllActorsToReplayAnchors();
        }

        private bool UsesFreeChaseMovement()
        {
            var config = CombatArenaBootstrap.Instance?.Config;
            return config != null && config.useTopTroopsFreeChaseMovement;
        }

        /// <summary>Called when the additive arena scene unloads so pooled actors are not reused.</summary>
        public void OnArenaUnloaded()
        {
            _actors.Clear();
            _unitHealth.Clear();
            _unitMorale.Clear();
            _cellsById.Clear();
            _disembarkedCargoIds.Clear();
            _pendingDeathPresentations = 0;
            _replayState.ResetFromBattlefield(null);
            _battlefield = null;
            _chaseController?.Clear();
            _pool?.Clear();
            _pool = null;
            _mapper = null;
            _arenaCameraTransform = null;
            _buildingSpawner.Clear();
        }

        public void InitializeArena(BattlefieldState battlefield)
        {
            EnsureReferences();

            if (battlefield == null)
                return;

            var bootstrap = CombatArenaBootstrap.Instance;
            if (bootstrap == null)
                return;

            var config = bootstrap.Config;
            if (config == null)
                return;

            // The arena scene carries its own audio presenter (3D SFX set, positional
            // one-shots) on the bootstrap rig; prefer it over the flow-side default.
            var sceneAudio = bootstrap.GetComponent<CombatArenaAudioPresenter>();
            if (sceneAudio != null)
                audio = sceneAudio;

            _activeVfx = ResolveVfxPresenter();

            _mapper = new CombatGridMapper(battlefield.Layout, config.cellWidth, config.cellDepth);
            _battlefield = battlefield;
            bootstrap.FrameBattlefield3D(battlefield, _mapper);
            _arenaCameraTransform = bootstrap.ArenaCamera != null ? bootstrap.ArenaCamera.transform : null;

            EnsureChaseController();
            _chaseController.Configure(this, _replayState, _mapper, _battlefield, config, combatDirector);

            Transform poolRoot = bootstrap.UnitsRoot != null ? bootstrap.UnitsRoot : transform;
            ResetArenaActors(poolRoot);
            _replayState.ResetFromBattlefield(battlefield);

            Transform buildingsRoot = bootstrap.BuildingsRoot != null ? bootstrap.BuildingsRoot : poolRoot;
            _buildingSpawner.SpawnAll(battlefield, _mapper, buildingsRoot, GetPiece, config);

            _unitHealth.Clear();
            _unitMorale.Clear();
            _cellsById.Clear();
            _disembarkedCargoIds.Clear();
            // Per-fight army-size durability scale — recomputed from the battlefield, which
            // yields the identical value TickCombatRun spawned with (same two boards).
            float durabilityScale = CombatPacingConfig.DurabilityScaleFor(battlefield);
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                _cellsById[cell.InstanceId] = cell;

                if (!PieceCombatRules.ParticipatesInCombat(cell.Definition))
                    continue;

                // Sim HP is durability-scaled at spawn; register the same scaled max so
                // per-unit HP fractions match what the replayed damage events actually drain.
                _unitHealth.RegisterUnit(cell.InstanceId, cell.Side, CombatPacingConfig.ScaleUnitMaxHp(cell.Definition.MaxHp, durabilityScale));
                if (cell.Definition.MaxMorale > 0)
                {
                    _unitMorale[cell.InstanceId] = new UnitMorale
                    {
                        Max = cell.Definition.MaxMorale,
                        Current = cell.Definition.MaxMorale
                    };
                }

                // §2.5 Armored Ark: a piece build-tagged as this transport's cargo rides
                // embarked from the very first tick — no actor (visible or otherwise) until
                // "transport_unload"/"transport_spill" replays (SpawnEmbarkedActor).
                if (!string.IsNullOrEmpty(cell.CarrierInstanceId))
                    continue;

                var actor = _pool.Rent();
                var source = GetPiece(cell.Definition.Id);
                float moveSpeed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(source, config);
                actor.Initialize(
                    cell.InstanceId,
                    cell.Definition.Id,
                    _arenaCameraTransform,
                    _mapper,
                    cell.Position,
                    config.moveLerpSeconds,
                    moveSpeed,
                    config.moveMarchGraceSeconds,
                    CombatAttackProfileResolver.Resolve(source),
                    source,
                    cell.Side,
                    config.useTopTroopsFreeChaseMovement,
                    config.topTroopsChaseMaxLeadCells);

                _actors[cell.InstanceId] = actor;
                actor.SetFrozen(IsPresentationFrozen);
                if (_unitMorale.ContainsKey(cell.InstanceId))
                    actor.SetMoraleFraction(1f); // lights the ring gutter only for breakable units
            }
        }

        public void RestoreState(
            BattlefieldState battlefield,
            IEnumerable<CombatEvent> events,
            int? excludeSegment = null)
        {
            InitializeArena(battlefield);
            ApplySavedAnchors(battlefield, events, excludeSegment);
            SyncActorsToAnchors(battlefield);
        }

        private void ApplySavedAnchors(
            BattlefieldState battlefield,
            IEnumerable<CombatEvent> events,
            int? excludeSegment)
        {
            _replayState.RestoreFromBattlefieldAndEvents(battlefield, events, excludeSegment);

            // Replay HP the same way anchors are replayed so bars survive save/resume.
            if (events == null)
                return;

            foreach (var combatEvent in events)
            {
                if (combatEvent == null)
                    continue;
                if (excludeSegment.HasValue && combatEvent.Segment == excludeSegment.Value)
                    continue;
                _unitHealth.ApplyEvent(combatEvent);
                ApplyMoraleEvent(combatEvent);

                // §2.5 Armored Ark: a cargo id only earns a visible actor in SyncActorsToAnchors
                // if it already disembarked before this save point — otherwise it's still
                // riding and must stay hidden exactly like a fresh fight's embarked cargo.
                if (combatEvent.ActionType is "transport_unload" or "transport_spill" &&
                    !string.IsNullOrEmpty(combatEvent.TargetId))
                    _disembarkedCargoIds.Add(combatEvent.TargetId);
            }
        }

        private void SyncActorsToAnchors(BattlefieldState battlefield)
        {
            if (_pool == null || _mapper == null || battlefield == null)
                return;

            var bootstrap = CombatArenaBootstrap.Instance;
            var config = bootstrap?.Config;
            if (config == null)
                return;

            _activeVfx = ResolveVfxPresenter();

            Transform poolRoot = bootstrap.UnitsRoot != null ? bootstrap.UnitsRoot : transform;
            DestroyOrphanActors(poolRoot);

            var removedIds = new List<string>();
            foreach (var pair in _actors)
            {
                if (!_replayState.TryGetAnchor(pair.Key, out _))
                    removedIds.Add(pair.Key);
            }

            foreach (string instanceId in removedIds)
            {
                if (_actors.TryGetValue(instanceId, out var actor))
                    _pool.Release(actor);
                _actors.Remove(instanceId);
            }

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                if (!PieceCombatRules.ParticipatesInCombat(cell.Definition))
                    continue;

                // §2.5 Armored Ark: still-embarked cargo (never disembarked before this save)
                // stays hidden, same as a fresh fight's InitializeArena.
                bool stillEmbarked = !string.IsNullOrEmpty(cell.CarrierInstanceId)
                    && !_disembarkedCargoIds.Contains(cell.InstanceId);
                if (stillEmbarked)
                    continue;

                if (!_replayState.TryGetAnchor(cell.InstanceId, out var anchor) || _actors.ContainsKey(cell.InstanceId))
                    continue;

                var actor = _pool.Rent();
                var source = GetPiece(cell.Definition.Id);
                float moveSpeed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(source, config);
                actor.Initialize(
                    cell.InstanceId,
                    cell.Definition.Id,
                    _arenaCameraTransform,
                    _mapper,
                    anchor,
                    config.moveLerpSeconds,
                    moveSpeed,
                    config.moveMarchGraceSeconds,
                    CombatAttackProfileResolver.Resolve(source),
                    source,
                    cell.Side,
                    config.useTopTroopsFreeChaseMovement,
                    config.topTroopsChaseMaxLeadCells);

                _actors[cell.InstanceId] = actor;
                actor.SetFrozen(IsPresentationFrozen);
            }

            foreach (var pair in _replayState.Anchors)
            {
                if (_actors.TryGetValue(pair.Key, out var actor))
                    actor.SnapToAnchor(pair.Value);
            }

            foreach (var pair in _actors)
            {
                if (_unitHealth.TryGetUnitFraction(pair.Key, out float fraction))
                    pair.Value.SetHealthFraction(fraction);
                if (TryGetMoraleFraction(pair.Key, out float morale))
                    pair.Value.SetMoraleFraction(morale);
            }
        }

        private void DestroyOrphanActors(Transform poolRoot)
        {
            if (poolRoot == null)
                return;

            for (int i = poolRoot.childCount - 1; i >= 0; i--)
            {
                var child = poolRoot.GetChild(i);
                var actor = child.GetComponent<CombatUnitActor>();
                if (actor != null && !_actors.ContainsValue(actor))
                    Destroy(child.gameObject);
            }
        }

        private void OnEventReplayed(CombatEvent combatEvent)
        {
            if (!CombatArenaSession.IsActive || combatEvent == null)
                return;

            _replayState.ApplyEvent(combatEvent);
            _unitHealth.ApplyEvent(combatEvent);
            ApplyMoraleEvent(combatEvent);
            ApplyEventVisual(combatEvent);
        }

        private void ApplyMoraleEvent(CombatEvent combatEvent)
        {
            switch (combatEvent.ActionType)
            {
                // "morale_damage": ActorId = source, TargetId = victim, Value = amount.
                case "morale_damage":
                    if (_unitMorale.TryGetValue(combatEvent.TargetId ?? string.Empty, out var shocked))
                        shocked.Current = System.Math.Max(0, shocked.Current - combatEvent.Value);
                    break;
                // "rout" carries the victim in ActorId (same read as ArmyHealthReplayTracker).
                case "rout":
                    if (_unitMorale.TryGetValue(combatEvent.ActorId ?? string.Empty, out var broken))
                        broken.Current = 0;
                    break;
            }
        }

        private bool TryGetMoraleFraction(string instanceId, out float fraction)
        {
            fraction = 0f;
            if (string.IsNullOrEmpty(instanceId) || !_unitMorale.TryGetValue(instanceId, out var morale))
                return false;

            fraction = morale.Max <= 0 ? 0f : (float)morale.Current / morale.Max;
            return true;
        }

        private void ApplyEventVisual(CombatEvent combatEvent)
        {
            switch (combatEvent.ActionType)
            {
                case "move":
                    if (_actors.TryGetValue(combatEvent.ActorId, out var mover) &&
                        _replayState.TryGetAnchor(combatEvent.ActorId, out var destination))
                    {
                        mover.MoveTo(destination);
                    }

                    break;
                case "damage":
                case "graze":
                case "gas_damage":
                    PlayDamageEvent(combatEvent);
                    break;
                case "miss":
                    PlayMissEvent(combatEvent);
                    break;
                case "destroyed":
                    PlayDestroyedEvent(combatEvent);
                    break;
                case "morale_damage":
                    // No floating-number channel exists in this arena (PlayDamage is a
                    // no-op in the 3D backend) — the strip's own drain + bone pulse is
                    // the feedback, so just push the new fraction.
                    UpdateMoraleBar(combatEvent.TargetId);
                    break;
                case "rout":
                    PlayRoutEvent(combatEvent);
                    break;
                // §2.5 Armored Ark: the good outcome — cargo appears on arrival, no shock.
                case "transport_unload":
                    SpawnEmbarkedActor(combatEvent.ActorId, combatEvent.TargetId, punchIn: false);
                    break;
                // §2.5 Armored Ark: the transport died in transit — cargo spills at the wreck.
                // The morale-shock hit itself replays as an ordinary "morale_damage" event
                // (ApplyMoraleEvent/UpdateMoraleBar) right after this one in the same log.
                case "transport_spill":
                    SpawnEmbarkedActor(combatEvent.ActorId, combatEvent.TargetId, punchIn: true);
                    break;
            }
        }

        /// <summary>§2.5 Armored Ark: cargo gets no actor until this fires — spawn one now at
        /// the transport's current position (never the cargo's own stale build-time anchor;
        /// the transport is the one guaranteed to already be a live, correctly-tracked actor).</summary>
        private void SpawnEmbarkedActor(string transportInstanceId, string cargoInstanceId, bool punchIn)
        {
            if (string.IsNullOrEmpty(cargoInstanceId) || _actors.ContainsKey(cargoInstanceId))
                return;

            if (!_cellsById.TryGetValue(cargoInstanceId, out var cell) || cell?.Definition == null)
                return;

            var bootstrap = CombatArenaBootstrap.Instance;
            var config = bootstrap?.Config;
            if (_pool == null || _mapper == null || config == null)
                return;

            if (!_replayState.TryGetAnchor(transportInstanceId, out var anchor))
                anchor = cell.Position; // last-ditch fallback; should always resolve above

            var actor = _pool.Rent();
            var source = GetPiece(cell.Definition.Id);
            float moveSpeed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(source, config);
            actor.Initialize(
                cargoInstanceId,
                cell.Definition.Id,
                _arenaCameraTransform,
                _mapper,
                anchor,
                config.moveLerpSeconds,
                moveSpeed,
                config.moveMarchGraceSeconds,
                CombatAttackProfileResolver.Resolve(source),
                source,
                cell.Side,
                config.useTopTroopsFreeChaseMovement,
                config.topTroopsChaseMaxLeadCells);

            _actors[cargoInstanceId] = actor;
            actor.SetFrozen(IsPresentationFrozen);
            if (_unitHealth.TryGetUnitFraction(cargoInstanceId, out float healthFraction))
                actor.SetHealthFraction(healthFraction);
            if (TryGetMoraleFraction(cargoInstanceId, out float moraleFraction))
                actor.SetMoraleFraction(moraleFraction);

            // Spill's "wreck" shock: reuse the existing hurt flinch rather than invent a new
            // VFX vocabulary entry — the morale bar drain (from the paired morale_damage event)
            // carries the actual shock read.
            if (punchIn)
                actor.PlayHurt();
        }

        // Events within one sim tick replay on the same frame; a small random offset
        // keeps volleys from firing in metronome sync across the whole army.
        private const float MaxAttackStaggerSeconds = 0.15f;

        private void PlayDamageEvent(CombatEvent combatEvent)
        {
            if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
                return;

            // Gas has no shooter; a rifle tracer here reads as a phantom attacker.
            if (combatEvent.ActionType == "gas_damage")
            {
                _activeVfx?.PlayEnvironmentalDamage(targetWorld, combatEvent.Value);
                if (_actors.TryGetValue(combatEvent.TargetId, out var gassed))
                    gassed.PlayHurt();
                UpdateHealthBar(combatEvent.TargetId);
                return;
            }

            if (!_actors.ContainsKey(combatEvent.ActorId))
            {
                _activeVfx?.PlayDamage(targetWorld, combatEvent.Value);
                if (_actors.TryGetValue(combatEvent.TargetId, out var victim))
                    victim.PlayHurt();
                UpdateHealthBar(combatEvent.TargetId);
                return;
            }

            StartCoroutine(PlayStaggeredAttack(combatEvent, targetWorld, withImpact: true));
        }

        private void PlayMissEvent(CombatEvent combatEvent)
        {
            if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
                return;

            if (!_actors.ContainsKey(combatEvent.ActorId))
                return;

            StartCoroutine(PlayStaggeredAttack(combatEvent, targetWorld, withImpact: false));
        }

        private IEnumerator PlayStaggeredAttack(
            CombatEvent combatEvent,
            Vector3 targetWorld,
            bool withImpact)
        {
            float stagger = Random.Range(0f, MaxAttackStaggerSeconds);
            if (stagger > 0f)
                yield return new WaitForSeconds(stagger);

            // Re-resolve after the delay: the actor may have died or been pooled meanwhile.
            if (!_actors.TryGetValue(combatEvent.ActorId, out var attacker))
                yield break;

            var piece = ResolvePieceForActor(attacker);
            var profile = CombatAttackProfileResolver.Resolve(piece);

            attacker.PlayAttackToward(
                targetWorld,
                profile,
                muzzleWorld => PlayAttackMuzzleVfx(profile, muzzleWorld, targetWorld),
                withImpact
                    ? () =>
                    {
                        PlayAttackImpactVfx(profile, targetWorld, combatEvent.Value);
                        if (_actors.TryGetValue(combatEvent.TargetId, out var victim))
                            victim.PlayHurt();
                        UpdateHealthBar(combatEvent.TargetId);
                    }
                    : (System.Action)null);
        }

        /// <summary>Push the replay tracker's HP fraction to the victim's bar.</summary>
        private void UpdateHealthBar(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            if (_actors.TryGetValue(instanceId, out var actor)
                && _unitHealth.TryGetUnitFraction(instanceId, out float fraction))
            {
                actor.SetHealthFraction(fraction);
            }
        }

        /// <summary>Push the replayed morale fraction to the victim's strip (breakable units only).</summary>
        private void UpdateMoraleBar(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            if (_actors.TryGetValue(instanceId, out var actor)
                && TryGetMoraleFraction(instanceId, out float fraction))
            {
                actor.SetMoraleFraction(fraction);
            }
        }

        /// <summary>Rout replay (ADR-0005): the unit escapes the field. Deliberately NOT
        /// the death presentation — no death VFX, no death audio, no punch-in; the actor
        /// reuses the march machinery to run for its own edge (vehicles slump in place)
        /// and the softer rout dissolve removes the visual.</summary>
        private void PlayRoutEvent(CombatEvent combatEvent)
        {
            if (!_actors.TryGetValue(combatEvent.ActorId, out var broken))
                return;

            _actors.Remove(combatEvent.ActorId);
            _pendingDeathPresentations++; // fight-end flow waits for the flee like a death
            broken.PlayRout(ComputeFleeWorldTarget(broken), () =>
            {
                _pendingDeathPresentations--;
                _pool.Release(broken);
            });
        }

        /// <summary>The broken unit's OWN board edge, a couple of cells past it: player
        /// columns map to low grid X (world -X), the enemy's to high X (world +X), so
        /// player units flee -X and enemy units +X, each keeping its current lane.</summary>
        private Vector3 ComputeFleeWorldTarget(CombatUnitActor broken)
        {
            Vector3 position = broken.transform.position;
            if (_mapper == null || _battlefield?.Layout == null)
                return position + (broken.Side == CombatSide.Player ? Vector3.left : Vector3.right) * 6f;

            int edgeX = broken.Side == CombatSide.Player ? -2 : _battlefield.Layout.TotalWidth + 1;
            Vector3 edge = _mapper.ToWorld(new GridCoord(edgeX, 0));
            return new Vector3(edge.x, position.y, position.z);
        }

        private void PlayAttackMuzzleVfx(
            CombatAttackPresentationProfile profile,
            Vector3 muzzleWorld,
            Vector3 targetWorld)
        {
            switch (profile.Kind)
            {
                case CombatAttackPresentationKind.InfantryGrenade:
                    break;
                case CombatAttackPresentationKind.VehicleCannon:
                case CombatAttackPresentationKind.BuildingArtillery:
                    audio?.PlayCannonShot(muzzleWorld);
                    _activeVfx?.PlayCannonMuzzleAndTracer(muzzleWorld, targetWorld);
                    break;
                default:
                    audio?.PlayRifleShot(muzzleWorld);
                    _activeVfx?.PlayRifleMuzzleAndTracer(muzzleWorld, targetWorld);
                    break;
            }
        }

        private void PlayAttackImpactVfx(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            int damageAmount)
        {
            switch (profile.Kind)
            {
                case CombatAttackPresentationKind.InfantryGrenade:
                case CombatAttackPresentationKind.VehicleCannon:
                case CombatAttackPresentationKind.BuildingArtillery:
                    audio?.PlayExplosion(targetWorld);
                    _activeVfx?.PlayExplosion(targetWorld, damageAmount);
                    break;
                default:
                    audio?.PlayImpact(targetWorld);
                    _activeVfx?.PlayImpact(targetWorld, damageAmount);
                    break;
            }
        }

        private PieceDefinitionSO ResolvePieceForActor(CombatUnitActor actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.PieceId))
                return null;

            return GetPiece(actor.PieceId);
        }

        private void PlayDestroyedEvent(CombatEvent combatEvent)
        {
            if (!_actors.TryGetValue(combatEvent.ActorId, out var dead))
                return;

            Vector3 deathWorld = dead.transform.position;
            float deathSeconds = dead.DeathSeconds;
            _actors.Remove(combatEvent.ActorId);
            _pendingDeathPresentations++;
            dead.PlayDeath(() =>
            {
                _pendingDeathPresentations--;
                _pool.Release(dead);
            });
            // Dust/audio land when the fall finishes, right as the death presentation completes.
            StartCoroutine(PlayDeathVfxAfterDelay(deathWorld, deathSeconds));
        }

        private System.Collections.IEnumerator PlayDeathVfxAfterDelay(Vector3 worldPosition, float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSeconds(delaySeconds);

            audio?.PlayDeath(worldPosition);
            _activeVfx?.PlayDeath(worldPosition);
        }

        private bool TryGetDamageTargetPosition(CombatEvent combatEvent, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (_mapper == null || combatEvent == null)
                return false;

            if (_actors.TryGetValue(combatEvent.TargetId, out var targetActor))
            {
                worldPosition = targetActor.transform.position;
                return true;
            }

            if (_replayState.TryGetAnchor(combatEvent.TargetId, out var targetAnchor))
            {
                worldPosition = _mapper.ToWorld(targetAnchor);
                return true;
            }

            if (TryParseCoord(combatEvent.TargetId, out var coord))
            {
                worldPosition = _mapper.ToWorld(coord);
                return true;
            }

            return false;
        }

        private PieceDefinitionSO GetPiece(string pieceId)
        {
            string resolvedId = pieceId;
            if (_registry != null && _registry.TryGetById(pieceId, out var registryPiece))
                resolvedId = registryPiece.Id;

            if (!string.IsNullOrEmpty(resolvedId) && _piecesById.TryGetValue(resolvedId, out var source))
                return source;

            return null;
        }

        private void BuildPieceLookup(ContentDatabase database)
        {
            _piecesById.Clear();
            if (database?.Pieces == null)
                return;

            foreach (var piece in database.Pieces)
            {
                if (piece == null || string.IsNullOrEmpty(piece.id))
                    continue;

                _piecesById[piece.id] = piece;
            }
        }

        private void ResetArenaActors(Transform poolRoot)
        {
            _pendingDeathPresentations = 0;
            foreach (var actor in _actors.Values)
            {
                if (actor != null)
                    Destroy(actor.gameObject);
            }

            _actors.Clear();
            _replayState.ResetFromBattlefield(null);
            _pool?.Clear();
            _buildingSpawner.Clear();

            if (poolRoot != null)
            {
                for (int i = poolRoot.childCount - 1; i >= 0; i--)
                    Destroy(poolRoot.GetChild(i).gameObject);
            }

            _pool = new CombatUnitActorPool(poolRoot);
        }

        private static bool TryParseCoord(string value, out GridCoord coord)
        {
            coord = default;
            if (string.IsNullOrEmpty(value))
                return false;

            var parts = value.Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y))
                return false;

            coord = new GridCoord(x, y);
            return true;
        }

        private void EnsureReferences()
        {
            if (combatDirector == null)
                combatDirector = GetComponent<CombatDirector>();

            if (audio == null)
                audio = GetComponent<CombatArenaAudioPresenter>();

            _activeVfx = ResolveVfxPresenter();
        }

        private ICombatArenaVfxPresenter ResolveVfxPresenter()
        {
            // The active VFX backend lives on the arena scene's bootstrap rig.
            var sceneVfx = CombatArenaBootstrap.Instance != null
                ? CombatArenaBootstrap.Instance.GetComponent<ICombatArenaVfxPresenter>()
                : null;
            if (sceneVfx != null)
                return sceneVfx;

            // Embedded 3D scenes (demo) put the backend on the presenter's own rig.
            return GetComponent<ICombatArenaVfxPresenter>();
        }

        private void EnsureChaseController()
        {
            if (_chaseController != null)
                return;

            _chaseController = GetComponent<CombatArenaChaseController>();
            if (_chaseController == null)
                _chaseController = gameObject.AddComponent<CombatArenaChaseController>();
        }
    }
}
