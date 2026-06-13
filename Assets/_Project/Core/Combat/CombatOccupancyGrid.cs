using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public sealed class CombatOccupancyGrid
    {
        private readonly Dictionary<GridCoord, string> _occupancy = new();

        public bool CanPlace(
            string instanceId,
            GridCoord anchor,
            IReadOnlyList<GridCoord> offsets,
            BattlefieldLayout layout)
        {
            foreach (var cell in CombatFootprint.ComputeOccupiedCells(anchor, offsets))
            {
                if (!IsInBounds(cell, layout))
                    return false;

                if (_occupancy.TryGetValue(cell, out var occupant) && occupant != instanceId)
                    return false;
            }

            return true;
        }

        public void Place(string instanceId, GridCoord anchor, IReadOnlyList<GridCoord> offsets)
        {
            foreach (var cell in CombatFootprint.ComputeOccupiedCells(anchor, offsets))
                _occupancy[cell] = instanceId;
        }

        public void Remove(string instanceId)
        {
            var cellsToClear = new List<GridCoord>();
            foreach (var entry in _occupancy)
            {
                if (entry.Value == instanceId)
                    cellsToClear.Add(entry.Key);
            }

            foreach (var cell in cellsToClear)
                _occupancy.Remove(cell);
        }

        public void Move(string instanceId, GridCoord newAnchor, IReadOnlyList<GridCoord> offsets)
        {
            Remove(instanceId);
            Place(instanceId, newAnchor, offsets);
        }

        public bool IsOccupied(GridCoord cell) => _occupancy.ContainsKey(cell);

        public IReadOnlyDictionary<GridCoord, string> Snapshot() =>
            new Dictionary<GridCoord, string>(_occupancy);

        private static bool IsInBounds(GridCoord cell, BattlefieldLayout layout) =>
            cell.X >= 0 && cell.X < layout.TotalWidth && cell.Y >= 0 && cell.Y < layout.Height;
    }
}
