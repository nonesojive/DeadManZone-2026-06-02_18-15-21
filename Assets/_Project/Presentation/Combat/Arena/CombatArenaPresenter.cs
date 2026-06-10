using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
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

        private readonly Dictionary<string, CombatUnitActor> _actors = new();
        private readonly Dictionary<string, GridCoord> _anchors = new();
        private readonly Dictionary<string, PieceDefinitionSO> _piecesById = new();

        private CombatUnitActorPool _pool;
        private CombatGridMapper _mapper;
        private ContentRegistry _registry;
        private Transform _arenaCameraTransform;

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponent<CombatDirector>();

            var database = ContentDatabase.Load();
            _registry = ContentRegistryProvider.Build(database);
            BuildPieceLookup(database);
        }

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed += OnEventReplayed;

            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced += OnCombatAdvanced;

            InitializeReplayState();
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed -= OnEventReplayed;

            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced -= OnCombatAdvanced;
        }

        public IEnumerable<CombatUnitActor> GetActiveActors() => _actors.Values;

        public void InitializeArena(BattlefieldState battlefield)
        {
            if (battlefield == null)
                return;

            var bootstrap = CombatArenaBootstrap.Instance;
            if (bootstrap == null)
                return;

            var config = bootstrap.Config;
            if (config == null)
                return;

            _mapper = new CombatGridMapper(battlefield.Layout, config.cellWidth, config.cellDepth);
            _arenaCameraTransform = bootstrap.ArenaCamera != null ? bootstrap.ArenaCamera.transform : null;

            Transform poolRoot = bootstrap.UnitsRoot != null ? bootstrap.UnitsRoot : transform;
            _pool ??= new CombatUnitActorPool(poolRoot);

            ClearActors();

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                var actor = _pool.Rent();
                var source = GetPiece(cell.Definition.Id);
                actor.Initialize(
                    cell.InstanceId,
                    source != null ? source.icon : null,
                    _arenaCameraTransform,
                    _mapper,
                    cell.Position,
                    config.moveLerpSeconds,
                    config.attackLungeSeconds,
                    config.attackLungeDistance);

                _actors[cell.InstanceId] = actor;
                _anchors[cell.InstanceId] = cell.Position;
            }
        }

        public void RestoreState(
            BattlefieldState battlefield,
            IEnumerable<CombatEvent> events,
            CombatPhase? excludePhase = null)
        {
            InitializeArena(battlefield);
            if (events == null)
                return;

            foreach (var combatEvent in OrderEvents(events))
            {
                if (excludePhase.HasValue && combatEvent.Phase == excludePhase.Value)
                    continue;

                ApplyEventStateOnly(combatEvent);
            }

            var removedIds = new List<string>();
            foreach (var pair in _actors)
            {
                if (!_anchors.ContainsKey(pair.Key))
                    removedIds.Add(pair.Key);
            }

            foreach (string instanceId in removedIds)
            {
                if (_actors.TryGetValue(instanceId, out var actor))
                    _pool.Release(actor);
                _actors.Remove(instanceId);
            }

            foreach (var pair in _anchors)
            {
                if (_actors.TryGetValue(pair.Key, out var actor))
                    actor.SnapToAnchor(pair.Value);
            }
        }

        private void OnCombatAdvanced(CombatAdvanceResult result)
        {
            if (!CombatPresentationMode.ArenaActive || result == null)
                return;

            RestoreStateBeforeSegment();
        }

        private void InitializeReplayState()
        {
            if (!CombatPresentationMode.ArenaActive)
                return;

            RestoreStateBeforeSegment();
        }

        private void RestoreStateBeforeSegment()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun || _registry == null)
                return;

            var state = RunManager.Instance.State;
            if (state?.Phase != RunPhase.Combat || state.Combat?.EnemyBoard == null)
                return;

            var playerBoard = RunManager.Instance.Orchestrator.GetPlayerBoard();
            var enemyBoard = BoardSnapshotMapper.ToBoard(state.Combat.EnemyBoard, _registry);
            var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            var excludePhase = state.Combat.AwaitingCommand ? state.Combat.CompletedPhase : (CombatPhase?)null;

            RestoreState(
                battlefield,
                ConvertSavedEvents(state.Combat.EventLog),
                excludePhase);
        }

        private void OnEventReplayed(CombatEvent combatEvent)
        {
            if (!CombatPresentationMode.ArenaActive || combatEvent == null)
                return;

            ApplyEventStateOnly(combatEvent);
            ApplyEventVisual(combatEvent);
        }

        private void ApplyEventStateOnly(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return;

            switch (combatEvent.ActionType)
            {
                case "move":
                    if (!_anchors.ContainsKey(combatEvent.ActorId))
                        return;

                    if (TryParseCoord(combatEvent.TargetId, out var destination))
                        _anchors[combatEvent.ActorId] = destination;
                    break;
                case "destroyed":
                    _anchors.Remove(combatEvent.ActorId);
                    break;
            }
        }

        private void ApplyEventVisual(CombatEvent combatEvent)
        {
            switch (combatEvent.ActionType)
            {
                case "move":
                    if (_actors.TryGetValue(combatEvent.ActorId, out var mover) &&
                        _anchors.TryGetValue(combatEvent.ActorId, out var destination))
                    {
                        mover.MoveTo(destination);
                    }

                    break;
                case "damage":
                case "gas_damage":
                    PlayDamageEvent(combatEvent);
                    break;
                case "destroyed":
                    PlayDestroyedEvent(combatEvent);
                    break;
            }
        }

        private void PlayDamageEvent(CombatEvent combatEvent)
        {
            if (TryGetDamageTargetPosition(combatEvent, out var targetWorld))
            {
                if (_actors.TryGetValue(combatEvent.ActorId, out var attacker))
                    attacker.PlayAttackToward(targetWorld);

                vfx?.PlayDamage(targetWorld, combatEvent.Value);
            }
        }

        private void PlayDestroyedEvent(CombatEvent combatEvent)
        {
            if (!_actors.TryGetValue(combatEvent.ActorId, out var dead))
                return;

            Vector3 deathWorld = dead.transform.position;
            _actors.Remove(combatEvent.ActorId);
            dead.PlayDeath(() => _pool.Release(dead));
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

            if (_anchors.TryGetValue(combatEvent.TargetId, out var targetAnchor))
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

        private static IEnumerable<CombatEvent> ConvertSavedEvents(IReadOnlyList<CombatEventRecord> records)
        {
            if (records == null)
                yield break;

            foreach (var record in records)
            {
                yield return new CombatEvent
                {
                    Phase = record.Phase,
                    Tick = record.Tick,
                    ActorId = record.ActorId,
                    ActionType = record.ActionType,
                    TargetId = record.TargetId,
                    Value = record.Value
                };
            }
        }

        private static IEnumerable<CombatEvent> OrderEvents(IEnumerable<CombatEvent> events) =>
            events.OrderBy(e => (int)e.Phase).ThenBy(e => e.Tick).ThenBy(e => e.ActorId);

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

        private void ClearActors()
        {
            _pool?.ReleaseAll(_actors.Values);
            _actors.Clear();
            _anchors.Clear();
        }
    }
}
