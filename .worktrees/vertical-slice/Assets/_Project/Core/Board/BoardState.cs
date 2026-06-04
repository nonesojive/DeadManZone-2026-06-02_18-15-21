using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

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

        public PlacementResult TryPlace(PieceDefinition definition, GridCoord anchor, string instanceId = null)
        {
            instanceId ??= Guid.NewGuid().ToString("N");

            foreach (var cell in definition.Shape.GetCells(anchor))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= Layout.Width || cell.Y >= Layout.Height)
                    return new PlacementResult { Success = false, Reason = "Out of bounds" };

                if (_occupied.Contains(cell))
                    return new PlacementResult { Success = false, Reason = "Cell occupied" };

                if (!IsCategoryAllowed(definition.Category, Layout.GetZone(cell)))
                    return new PlacementResult { Success = false, Reason = "Invalid zone for category" };
            }

            foreach (var cell in definition.Shape.GetCells(anchor))
                _occupied.Add(cell);

            _pieces[instanceId] = new PlacedPiece
            {
                InstanceId = instanceId,
                Definition = definition,
                Anchor = anchor
            };

            return new PlacementResult { Success = true };
        }

        public bool IsOnSpecialTile(string instanceId)
        {
            if (!_pieces.TryGetValue(instanceId, out var piece))
                return false;

            return piece.Definition.Shape.GetCells(piece.Anchor)
                .Any(cell => Layout.IsSpecialTile(cell));
        }

        public IEnumerable<string> GetAdjacentInstanceIds(string instanceId) =>
            BoardAdjacency.GetTouchingPairs(_pieces.Values)
                .Where(pair => pair.A == instanceId || pair.B == instanceId)
                .Select(pair => pair.A == instanceId ? pair.B : pair.A);

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
