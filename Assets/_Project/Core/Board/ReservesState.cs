using System;
using System.Collections.Generic;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class ReservesState
    {
        public const int Width = 8;
        public const int Height = 2;

        private readonly Dictionary<string, PlacedPiece> _pieces = new();
        private readonly HashSet<GridCoord> _occupied = new();

        public IReadOnlyCollection<PlacedPiece> Pieces => _pieces.Values;

        public bool CanPlace(PieceDefinition definition, GridCoord anchor, PieceRotation rotation = PieceRotation.R0)
        {
            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height)
                    return false;

                if (_occupied.Contains(cell))
                    return false;
            }

            return true;
        }

        public PlacementResult TryPlace(
            PieceDefinition definition,
            GridCoord anchor,
            string instanceId = null,
            PieceRotation rotation = PieceRotation.R0)
        {
            instanceId ??= Guid.NewGuid().ToString("N");

            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height)
                    return new PlacementResult { Success = false, Reason = "Out of bounds" };

                if (_occupied.Contains(cell))
                    return new PlacementResult { Success = false, Reason = "Cell occupied" };
            }

            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
                _occupied.Add(cell);

            _pieces[instanceId] = new PlacedPiece
            {
                InstanceId = instanceId,
                Definition = definition,
                Anchor = anchor,
                Rotation = rotation
            };

            return new PlacementResult { Success = true };
        }

        public bool TryRemove(string instanceId, out PlacedPiece removedPiece)
        {
            if (!_pieces.TryGetValue(instanceId, out removedPiece))
                return false;

            foreach (var cell in removedPiece.Definition.Shape.GetCells(removedPiece.Anchor, removedPiece.Rotation))
                _occupied.Remove(cell);

            _pieces.Remove(instanceId);
            return true;
        }

        public PlacementResult TryRelocate(string instanceId, GridCoord newAnchor)
        {
            if (!_pieces.TryGetValue(instanceId, out var piece))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            if (piece.Anchor.X == newAnchor.X && piece.Anchor.Y == newAnchor.Y)
                return new PlacementResult { Success = true };

            if (!TryRemove(instanceId, out var removed))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            if (!CanPlace(removed.Definition, newAnchor, removed.Rotation))
            {
                TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation);
                return new PlacementResult { Success = false, Reason = "Invalid placement" };
            }

            return TryPlace(removed.Definition, newAnchor, removed.InstanceId, removed.Rotation);
        }
    }
}
