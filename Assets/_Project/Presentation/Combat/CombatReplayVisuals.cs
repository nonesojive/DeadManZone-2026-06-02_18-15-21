using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Presentation.Board;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Applies combat replay events to board visuals (movement, destruction).</summary>
    internal sealed class CombatReplayVisuals
    {
        private readonly Dictionary<string, GridCoord> _anchors = new();
        private readonly Dictionary<string, PieceDefinition> _definitions = new();
        private readonly Dictionary<string, PieceRotation> _rotations = new();

        public IReadOnlyDictionary<string, GridCoord> Anchors => _anchors;

        public void ResetFromBattlefield(BattlefieldState battlefield)
        {
            _anchors.Clear();
            _definitions.Clear();
            _rotations.Clear();

            if (battlefield == null)
                return;

            foreach (var cell in battlefield.Cells)
            {
                _anchors[cell.InstanceId] = cell.Position;
                _definitions[cell.InstanceId] = cell.Definition;
                _rotations[cell.InstanceId] = PieceRotation.R0;
            }
        }

        /// <summary>
        /// Restores piece positions from spawn, then applies combat log events.
        /// Skips <paramref name="excludePhase"/> so that phase can be replayed visually.
        /// </summary>
        public void RestoreFromBattlefieldAndEvents(
            BattlefieldState battlefield,
            IEnumerable<CombatEvent> events,
            CombatPhase? excludePhase = null)
        {
            ResetFromBattlefield(battlefield);
            if (events == null)
                return;

            foreach (var combatEvent in OrderEvents(events))
            {
                if (excludePhase.HasValue && combatEvent.Phase == excludePhase.Value)
                    continue;

                ApplyEventToState(combatEvent);
            }
        }

        public void SyncBoardView(BoardView boardView)
        {
            if (boardView == null)
                return;

            boardView.SyncCombatPiecePositions(_anchors, _definitions, _rotations);
        }

        public void ApplyEvent(CombatEvent combatEvent, BoardView boardView)
        {
            if (combatEvent == null || boardView == null)
                return;

            switch (combatEvent.ActionType)
            {
                case "move":
                    ApplyMove(combatEvent, boardView);
                    break;
                case "destroyed":
                    ApplyDestroy(combatEvent.ActorId, boardView);
                    break;
                case "fight_end":
                    break;
            }
        }

        private void ApplyMove(CombatEvent combatEvent, BoardView boardView)
        {
            if (!ApplyEventToState(combatEvent))
                return;

            if (!_definitions.TryGetValue(combatEvent.ActorId, out var definition))
                return;

            _rotations.TryGetValue(combatEvent.ActorId, out var rotation);
            boardView.RepositionPieceVisual(combatEvent.ActorId, definition, _anchors[combatEvent.ActorId], rotation);
        }

        private bool ApplyEventToState(CombatEvent combatEvent)
        {
            switch (combatEvent.ActionType)
            {
                case "move":
                    if (!_anchors.ContainsKey(combatEvent.ActorId))
                        return false;

                    if (!TryParseCoord(combatEvent.TargetId, out var destination))
                        return false;

                    _anchors[combatEvent.ActorId] = destination;
                    return true;
                case "destroyed":
                    ApplyDestroyState(combatEvent.ActorId);
                    return true;
                default:
                    return false;
            }
        }

        private void ApplyDestroyState(string instanceId)
        {
            _anchors.Remove(instanceId);
            _definitions.Remove(instanceId);
            _rotations.Remove(instanceId);
        }

        private static IEnumerable<CombatEvent> OrderEvents(IEnumerable<CombatEvent> events) =>
            events.OrderBy(e => (int)e.Phase).ThenBy(e => e.Tick).ThenBy(e => e.ActorId);

        private void ApplyDestroy(string instanceId, BoardView boardView)
        {
            ApplyDestroyState(instanceId);
            boardView.RemovePieceVisual(instanceId);
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
    }
}
