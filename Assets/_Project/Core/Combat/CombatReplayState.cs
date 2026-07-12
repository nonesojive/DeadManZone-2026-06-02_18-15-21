using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Tracks unit grid anchors while combat events are replayed for presentation.
    /// Shared by the 3D arena and any other replay consumers.
    /// </summary>
    public sealed class CombatReplayState
    {
        private readonly Dictionary<string, GridCoord> _anchors = new();

        public IReadOnlyDictionary<string, GridCoord> Anchors => _anchors;

        public void ResetFromBattlefield(BattlefieldState battlefield)
        {
            _anchors.Clear();
            if (battlefield == null)
                return;

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                _anchors[cell.InstanceId] = cell.Position;
            }
        }

        public void RestoreFromBattlefieldAndEvents(
            BattlefieldState battlefield,
            IEnumerable<CombatEvent> events,
            int? excludeSegment = null)
        {
            ResetFromBattlefield(battlefield);
            if (events == null)
                return;

            foreach (var combatEvent in OrderEvents(events))
            {
                if (excludeSegment.HasValue && combatEvent.Segment == excludeSegment.Value)
                    continue;

                ApplyEvent(combatEvent);
            }
        }

        public bool TryGetAnchor(string instanceId, out GridCoord anchor) =>
            _anchors.TryGetValue(instanceId, out anchor);

        public bool ApplyEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return false;

            switch (combatEvent.ActionType)
            {
                case "move":
                    if (!_anchors.ContainsKey(combatEvent.ActorId))
                        return false;

                    if (!TryParseCoord(combatEvent.TargetId, out var destination))
                        return false;

                    _anchors[combatEvent.ActorId] = destination;
                    return true;
                // "rout" carries the victim in ActorId like "destroyed": the unit left the
                // field, so it must not re-anchor on save/resume (ADR-0005).
                case "destroyed":
                case "rout":
                    _anchors.Remove(combatEvent.ActorId);
                    return true;
                default:
                    return false;
            }
        }

        private static IEnumerable<CombatEvent> OrderEvents(IEnumerable<CombatEvent> events) =>
            events.OrderBy(e => e.Segment).ThenBy(e => e.Tick).ThenBy(e => e.ActorId);

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
