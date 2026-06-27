using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public readonly struct PlacementResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; }
    }

    public sealed class BoardState
    {
        private readonly Dictionary<string, PlacedPiece> _pieces = new();
        private readonly HashSet<GridCoord> _occupied = new();

        public BoardLayout Layout { get; }

        public BoardState(BoardLayout layout) => Layout = layout;

        public IReadOnlyCollection<PlacedPiece> Pieces => _pieces.Values;

        public bool CanPlace(PieceDefinition definition, GridCoord anchor, PieceRotation rotation = PieceRotation.R0)
        {
            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
            {
                if (!Layout.IsPlaceableCell(cell))
                    return false;

                if (_occupied.Contains(cell))
                    return false;

                if (!IsPlacementAllowedForDefinition(definition, cell))
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
                if (!Layout.IsPlaceableCell(cell))
                    return new PlacementResult
                    {
                        Success = false,
                        Reason = Layout.IsBlocked(cell) ? "Cell blocked" : "Out of bounds"
                    };

                if (_occupied.Contains(cell))
                    return new PlacementResult { Success = false, Reason = "Cell occupied" };

                if (!IsPlacementAllowedForDefinition(definition, cell))
                {
                    var boardReason = BoardPlacementRules.InvalidBoardReason(definition, Layout.Kind);
                    if (!string.IsNullOrEmpty(boardReason))
                        return new PlacementResult { Success = false, Reason = boardReason };

                    return new PlacementResult { Success = false, Reason = "Invalid zone for category" };
                }
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

        public bool IsOnSpecialTile(string instanceId)
        {
            if (!_pieces.TryGetValue(instanceId, out var piece))
                return false;

            return piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation)
                .Any(cell => Layout.IsSpecialTile(cell));
        }

        public IEnumerable<string> GetAdjacentInstanceIds(string instanceId) =>
            BoardAdjacency.GetTouchingPairs(_pieces.Values)
                .Where(pair => pair.A == instanceId || pair.B == instanceId)
                .Select(pair => pair.A == instanceId ? pair.B : pair.A);

        public bool TryRemove(string instanceId, out PlacedPiece removedPiece)
        {
            if (!_pieces.TryGetValue(instanceId, out removedPiece))
                return false;

            if (IsImmovableHq(removedPiece.Definition))
                return false;

            foreach (var cell in removedPiece.Definition.Shape.GetCells(removedPiece.Anchor, removedPiece.Rotation))
                _occupied.Remove(cell);

            _pieces.Remove(instanceId);
            return true;
        }

        public PlacementResult TryRelocate(string instanceId, GridCoord newAnchor, PieceRotation rotation)
        {
            if (!_pieces.TryGetValue(instanceId, out var piece))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            if (IsImmovableHq(piece.Definition))
                return new PlacementResult { Success = false, Reason = "HQ cannot be moved" };

            if (piece.Anchor.X == newAnchor.X && piece.Anchor.Y == newAnchor.Y && piece.Rotation == rotation)
                return new PlacementResult { Success = true };

            if (!TryRemove(instanceId, out var removed))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            if (!CanPlace(removed.Definition, newAnchor, rotation))
            {
                TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation);
                return new PlacementResult { Success = false, Reason = "Invalid placement" };
            }

            return TryPlace(removed.Definition, newAnchor, removed.InstanceId, rotation);
        }

        private static bool IsImmovableHq(PieceDefinition definition) =>
            PieceTagQueries.HasTag(definition, GameTagIds.Hq);

        private static bool IsPlacementAllowedForDefinition(
            PieceDefinition definition,
            GridCoord cell,
            BoardLayout layout)
        {
            if (definition == null)
                return false;

            if (!BoardPlacementRules.IsAllowedForBoard(definition, layout.Kind))
                return false;

            if (!layout.UsesZones)
                return true;

            return IsZoneAllowedForDefinition(definition, layout.GetZone(cell));
        }

        private bool IsPlacementAllowedForDefinition(PieceDefinition definition, GridCoord cell) =>
            IsPlacementAllowedForDefinition(definition, cell, Layout);

        private static bool IsZoneAllowedForDefinition(PieceDefinition definition, ZoneType zone)
        {
            if (definition == null)
                return false;

            if (!string.IsNullOrWhiteSpace(definition.Primary))
                return PrimaryZoneRules.IsZoneAllowed(definition.Primary, zone);

            return IsCategoryAllowed(definition.Category, zone);
        }

        private static bool IsCategoryAllowed(PieceCategory category, ZoneType zone) =>
            zone switch
            {
                ZoneType.Rear => category is PieceCategory.Building or PieceCategory.Hybrid,
                ZoneType.Front => category is PieceCategory.Unit or PieceCategory.Hybrid,
                ZoneType.Support => true,
                _ => false
            };
    }
}
