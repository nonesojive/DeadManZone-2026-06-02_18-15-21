using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private CombatArenaVfx vfx;
        [SerializeField] private CombatArenaAudioPresenter audio;

        private readonly Dictionary<string, CombatUnitActor> _actors = new();
        private readonly CombatReplayState _replayState = new();
        private readonly Dictionary<string, PieceDefinitionSO> _piecesById = new();

        private CombatUnitActorPool _pool;
        private CombatGridMapper _mapper;
        private BattlefieldState _battlefield;
        private CombatArenaChaseController _chaseController;
        private ContentRegistry _registry;
        private Transform _arenaCameraTransform;
        private CombatArenaBuildingSpawner _buildingSpawner = new();

        public bool HasBuildingVisualForTests(string instanceId) => _buildingSpawner.HasVisual(instanceId);

        public bool IsPresentationFrozen { get; private set; }

        public void SetPresentationFrozen(bool frozen) => IsPresentationFrozen = frozen;

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
                combatDirector.EventReplayed += OnEventReplayed;
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed -= OnEventReplayed;
        }

        public IEnumerable<CombatUnitActor> GetActiveActors() => _actors.Values;

        public void Configure(CombatDirector director, CombatArenaVfx arenaVfx, CombatArenaAudioPresenter arenaAudio = null)
        {
            if (director != null)
            {
                if (isActiveAndEnabled && combatDirector != null)
                    combatDirector.EventReplayed -= OnEventReplayed;

                combatDirector = director;

                if (isActiveAndEnabled)
                    combatDirector.EventReplayed += OnEventReplayed;
            }

            if (arenaVfx != null)
                vfx = arenaVfx;

            if (arenaAudio != null)
                audio = arenaAudio;
        }

        /// <summary>Called when the additive arena scene unloads so pooled actors are not reused.</summary>
        public void OnArenaUnloaded()
        {
            _actors.Clear();
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

            _mapper = new CombatGridMapper(battlefield.Layout, config.cellWidth, config.cellDepth);
            _battlefield = battlefield;
            bootstrap.FrameBattlefield(battlefield.Layout);
            _arenaCameraTransform = bootstrap.ArenaCamera != null ? bootstrap.ArenaCamera.transform : null;

            EnsureChaseController();
            _chaseController.Configure(this, _replayState, _mapper, _battlefield, config);

            Transform poolRoot = bootstrap.UnitsRoot != null ? bootstrap.UnitsRoot : transform;
            ResetArenaActors(poolRoot);
            _replayState.ResetFromBattlefield(battlefield);

            Transform buildingsRoot = bootstrap.BuildingsRoot != null ? bootstrap.BuildingsRoot : poolRoot;
            _buildingSpawner.SpawnAll(battlefield, _mapper, buildingsRoot, GetPiece, config);

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                var actor = _pool.Rent();
                var source = GetPiece(cell.Definition.Id);
                float moveSpeed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(source, config);
                actor.Initialize(
                    cell.InstanceId,
                    cell.Definition.Id,
                    source != null ? source.icon : null,
                    CombatArenaPrefabResolver.ResolveUnitPrefab(source, config),
                    CombatArenaPrefabResolver.ResolveUnitScale(source, config),
                    CombatArenaPrefabResolver.ResolveUnitHeight(source, config),
                    _arenaCameraTransform,
                    _mapper,
                    cell.Position,
                    config.moveLerpSeconds,
                    moveSpeed,
                    config.moveMarchGraceSeconds,
                    config.attackLungeSeconds,
                    config.attackLungeDistance,
                    CombatAttackProfileResolver.Resolve(source),
                    source,
                    cell.Side,
                    config.useProceduralUnitVisuals,
                    config.useTopTroopsFreeChaseMovement,
                    config.topTroopsChaseMaxLeadCells);

                _actors[cell.InstanceId] = actor;
                actor.SetFrozen(IsPresentationFrozen);
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
        }

        private void SyncActorsToAnchors(BattlefieldState battlefield)
        {
            if (_pool == null || _mapper == null || battlefield == null)
                return;

            var bootstrap = CombatArenaBootstrap.Instance;
            var config = bootstrap?.Config;
            if (config == null)
                return;

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

                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                if (!_replayState.TryGetAnchor(cell.InstanceId, out var anchor) || _actors.ContainsKey(cell.InstanceId))
                    continue;

                var actor = _pool.Rent();
                var source = GetPiece(cell.Definition.Id);
                float moveSpeed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(source, config);
                actor.Initialize(
                    cell.InstanceId,
                    cell.Definition.Id,
                    source != null ? source.icon : null,
                    CombatArenaPrefabResolver.ResolveUnitPrefab(source, config),
                    CombatArenaPrefabResolver.ResolveUnitScale(source, config),
                    CombatArenaPrefabResolver.ResolveUnitHeight(source, config),
                    _arenaCameraTransform,
                    _mapper,
                    anchor,
                    config.moveLerpSeconds,
                    moveSpeed,
                    config.moveMarchGraceSeconds,
                    config.attackLungeSeconds,
                    config.attackLungeDistance,
                    CombatAttackProfileResolver.Resolve(source),
                    source,
                    cell.Side,
                    config.useProceduralUnitVisuals,
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
            ApplyEventVisual(combatEvent);
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
            }
        }

        private void PlayDamageEvent(CombatEvent combatEvent)
        {
            if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
                return;

            if (!_actors.TryGetValue(combatEvent.ActorId, out var attacker))
            {
                vfx?.PlayDamage(targetWorld, combatEvent.Value);
                return;
            }

            var piece = ResolvePieceForActor(attacker);
            var profile = CombatAttackProfileResolver.Resolve(piece);

            attacker.PlayAttackToward(
                targetWorld,
                profile,
                muzzleWorld => PlayAttackMuzzleVfx(profile, muzzleWorld, targetWorld),
                () => PlayAttackImpactVfx(profile, targetWorld, combatEvent.Value));
        }

        private void PlayMissEvent(CombatEvent combatEvent)
        {
            if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
                return;

            if (!_actors.TryGetValue(combatEvent.ActorId, out var attacker))
                return;

            var piece = ResolvePieceForActor(attacker);
            var profile = CombatAttackProfileResolver.Resolve(piece);

            attacker.PlayAttackToward(
                targetWorld,
                profile,
                muzzleWorld => PlayAttackMuzzleVfx(profile, muzzleWorld, targetWorld),
                onImpact: null);
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
                    vfx?.PlayCannonMuzzleAndTracer(muzzleWorld, targetWorld);
                    break;
                default:
                    audio?.PlayRifleShot(muzzleWorld);
                    vfx?.PlayRifleMuzzleAndTracer(muzzleWorld, targetWorld);
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
                    vfx?.PlayExplosion(targetWorld, damageAmount);
                    break;
                default:
                    audio?.PlayImpact(targetWorld);
                    vfx?.PlayImpact(targetWorld, damageAmount);
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
            _actors.Remove(combatEvent.ActorId);
            dead.PlayDeath(() => _pool.Release(dead));
            audio?.PlayDeath(deathWorld);
            vfx?.PlayDeath(deathWorld);
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

            if (vfx == null)
                vfx = GetComponent<CombatArenaVfx>();

            if (audio == null)
                audio = GetComponent<CombatArenaAudioPresenter>();
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
