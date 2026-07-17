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
            PieceRotation rotation = PieceRotation.R0,
            bool isMercenary = false)
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
                Rotation = rotation,
                IsMercenary = isMercenary
            };

            return new PlacementResult { Success = true };
        }

        /// <summary>2026-07-17 round-2 playtest fix: the Ark's cargo hold is a real 2x2 mini
        /// board — pieces occupy their actual footprints inside it, not just a piece-count
        /// slot. Fixed size (no per-piece configurability, no rotation): "a tiny 2x2 board".</summary>
        public const int CargoGridWidth = 2;
        public const int CargoGridHeight = 2;

        /// <summary>2026-07-15 faction-roster-v1 §2.5 transport tentpole: load an already-placed
        /// piece as cargo into an already-placed transport during Build. Mostly a data tag
        /// (PlacedPiece.CarrierInstanceId) — the cargo piece's own board cell is untouched —
        /// but the load itself is gated by whether the piece's footprint actually FITS into the
        /// transport's fixed 2x2 cargo hold alongside whatever's already loaded (round-2
        /// playtest fix: the old rule only counted loaded PIECES, so e.g. a 2-cell piece +
        /// a 3-cell piece — 5 cells — both fit into what must be a 4-cell hold).</summary>
        public PlacementResult TryLoadCargo(string cargoInstanceId, string transportInstanceId)
        {
            if (!_pieces.TryGetValue(cargoInstanceId, out var cargo))
                return new PlacementResult { Success = false, Reason = "Cargo piece not found" };

            if (!_pieces.TryGetValue(transportInstanceId, out var transport))
                return new PlacementResult { Success = false, Reason = "Transport piece not found" };

            if (!transport.Definition.IsTransport)
                return new PlacementResult { Success = false, Reason = "Source is not a transport" };

            if (cargo.Definition.IsTransport)
                return new PlacementResult { Success = false, Reason = "A transport cannot be cargo" };

            if (cargoInstanceId == transportInstanceId)
                return new PlacementResult { Success = false, Reason = "Cannot load a transport into itself" };

            var occupiedCargoCells = ComputeOccupiedCargoCells(transportInstanceId);

            if (!TryFindCargoAnchor(cargo.Definition, occupiedCargoCells, out var anchor))
                return new PlacementResult { Success = false, Reason = "Cargo does not fit in transport hold" };

            // 2026-07-17 round-3 playtest fix: "a piece is EITHER on the main board OR in a
            // transport's cargo hold, never both" — rounds 1-2 only tagged the piece, leaving
            // its main-board cells occupied so it rendered/blocked space in both places at
            // once. Vacate them now that the fit check above has passed. Guarded on
            // CarrierInstanceId so re-tagging an already-carried piece (shouldn't happen, but
            // cheap to make idempotent) never double-frees someone else's cells.
            if (cargo.CarrierInstanceId == null)
            {
                foreach (var cell in cargo.Definition.Shape.GetCells(cargo.Anchor, cargo.Rotation))
                    _occupied.Remove(cell);
            }

            _pieces[cargoInstanceId] = new PlacedPiece
            {
                InstanceId = cargo.InstanceId,
                Definition = cargo.Definition,
                Anchor = cargo.Anchor,
                Rotation = cargo.Rotation,
                CarrierInstanceId = transportInstanceId,
                CargoAnchor = anchor,
                IsMercenary = cargo.IsMercenary
            };

            return new PlacementResult { Success = true };
        }

        /// <summary>2026-07-17 round-3 playtest fix: embark a piece that ISN'T on this board at
        /// all directly into <paramref name="transportInstanceId"/>'s hold — no main-board cell
        /// is ever claimed (the shop/reserves "place on the board, then tag it" composite is
        /// gone; a piece is either on the board or in the hold, never a placeholder for both).
        /// Same fit-check as <see cref="TryLoadCargo"/>, just without an already-placed source
        /// piece to read from.</summary>
        public PlacementResult TryEmbarkCargo(
            PieceDefinition definition,
            string transportInstanceId,
            string instanceId = null,
            bool isMercenary = false)
        {
            instanceId ??= Guid.NewGuid().ToString("N");

            if (definition == null)
                return new PlacementResult { Success = false, Reason = "No piece to embark" };

            if (!_pieces.TryGetValue(transportInstanceId, out var transport))
                return new PlacementResult { Success = false, Reason = "Transport piece not found" };

            if (!transport.Definition.IsTransport)
                return new PlacementResult { Success = false, Reason = "Source is not a transport" };

            if (definition.IsTransport)
                return new PlacementResult { Success = false, Reason = "A transport cannot be cargo" };

            var occupiedCargoCells = ComputeOccupiedCargoCells(transportInstanceId);
            if (!TryFindCargoAnchor(definition, occupiedCargoCells, out var anchor))
                return new PlacementResult { Success = false, Reason = "Cargo does not fit in transport hold" };

            _pieces[instanceId] = new PlacedPiece
            {
                InstanceId = instanceId,
                Definition = definition,
                // Cosmetic only — this piece never occupies a main-board cell, so any value
                // works; the transport's own anchor keeps TickCombatRun's pre-re-anchor spawn
                // step (SpawnCombatants) sane without meaning anything spatially.
                Anchor = transport.Anchor,
                Rotation = PieceRotation.R0,
                CarrierInstanceId = transportInstanceId,
                CargoAnchor = anchor,
                IsMercenary = isMercenary
            };

            return new PlacementResult { Success = true };
        }

        /// <summary>Every piece currently tagged as <paramref name="transportInstanceId"/>'s
        /// cargo (empty list if none/unknown).</summary>
        public IReadOnlyList<PlacedPiece> GetCargo(string transportInstanceId) =>
            _pieces.Values.Where(p => p.CarrierInstanceId == transportInstanceId).ToList();

        private HashSet<GridCoord> ComputeOccupiedCargoCells(string transportInstanceId)
        {
            var occupiedCargoCells = new HashSet<GridCoord>();
            foreach (var loaded in _pieces.Values)
            {
                if (loaded.CarrierInstanceId != transportInstanceId || loaded.CargoAnchor is not { } loadedAnchor)
                    continue;

                foreach (var cell in loaded.Definition.Shape.GetCells(loadedAnchor, PieceRotation.R0))
                    occupiedCargoCells.Add(cell);
            }

            return occupiedCargoCells;
        }

        /// <summary>2026-07-17 round-3 playtest fix: selling (removing) a transport can't just
        /// orphan its cargo's CarrierInstanceId tags — every loaded piece evicts to
        /// <paramref name="reservesForEviction"/> first. Plans every eviction anchor before
        /// mutating anything (ReservesState.CanPlace doesn't mutate), so a single piece with
        /// nowhere to go aborts the WHOLE sell — nothing is removed, nothing is destroyed.</summary>
        public bool TryRemoveTransportEvictingCargo(
            string transportInstanceId,
            ReservesState reservesForEviction,
            out PlacedPiece removedTransport)
        {
            removedTransport = null;
            if (!_pieces.TryGetValue(transportInstanceId, out var transport) || !transport.Definition.IsTransport)
                return false;

            var cargo = GetCargo(transportInstanceId);
            if (cargo.Count > 0 && reservesForEviction == null)
                return false;

            var plannedAnchors = new Dictionary<string, GridCoord>();
            var claimed = new HashSet<GridCoord>();
            foreach (var piece in cargo)
            {
                if (!TryFindOpenReservesAnchor(reservesForEviction, piece.Definition, claimed, out var anchor))
                    return false;

                plannedAnchors[piece.InstanceId] = anchor;
                foreach (var cell in piece.Definition.Shape.GetCells(anchor, PieceRotation.R0))
                    claimed.Add(cell);
            }

            foreach (var piece in cargo)
            {
                TryRemove(piece.InstanceId, out _);
                reservesForEviction.TryPlace(
                    piece.Definition, plannedAnchors[piece.InstanceId], piece.InstanceId, PieceRotation.R0, piece.IsMercenary);
            }

            return TryRemove(transportInstanceId, out removedTransport);
        }

        private static bool TryFindOpenReservesAnchor(
            ReservesState reserves, PieceDefinition definition, HashSet<GridCoord> claimed, out GridCoord anchor)
        {
            for (int y = 0; y < ReservesState.Height; y++)
            {
                for (int x = 0; x < ReservesState.Width; x++)
                {
                    var candidate = new GridCoord(x, y);
                    if (!reserves.CanPlace(definition, candidate))
                        continue;

                    if (definition.Shape.GetCells(candidate, PieceRotation.R0).Any(claimed.Contains))
                        continue;

                    anchor = candidate;
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        /// <summary>First (row-major) anchor in the fixed cargo grid where every cell of
        /// <paramref name="definition"/>'s own shape (no rotation — the hold isn't oriented)
        /// lands in-bounds and free. Mirrors CanPlace's per-cell checks at 2x2 scale.</summary>
        private static bool TryFindCargoAnchor(
            PieceDefinition definition, HashSet<GridCoord> occupiedCargoCells, out GridCoord anchor)
        {
            for (int y = 0; y < CargoGridHeight; y++)
            {
                for (int x = 0; x < CargoGridWidth; x++)
                {
                    var candidate = new GridCoord(x, y);
                    if (FitsCargoGrid(definition, candidate, occupiedCargoCells))
                    {
                        anchor = candidate;
                        return true;
                    }
                }
            }

            anchor = default;
            return false;
        }

        private static bool FitsCargoGrid(
            PieceDefinition definition, GridCoord anchor, HashSet<GridCoord> occupiedCargoCells)
        {
            foreach (var cell in definition.Shape.GetCells(anchor, PieceRotation.R0))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= CargoGridWidth || cell.Y >= CargoGridHeight)
                    return false;

                if (occupiedCargoCells.Contains(cell))
                    return false;
            }

            return true;
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

            // 2026-07-17 round-3 playtest fix: a carried piece already vacated its main-board
            // cells on load (TryLoadCargo/TryEmbarkCargo) — its stale Anchor may now coincide
            // with whatever legitimately occupies that cell today. Only vacate for a piece
            // that's actually still holding real board cells.
            if (removedPiece.CarrierInstanceId == null)
            {
                foreach (var cell in removedPiece.Definition.Shape.GetCells(removedPiece.Anchor, removedPiece.Rotation))
                    _occupied.Remove(cell);
            }

            _pieces.Remove(instanceId);
            return true;
        }

        /// <summary>Relocating a normal piece moves it on the main board as before. Relocating a
        /// CARRIED piece is round-3's "unload-to-cell": it un-tags (TryPlace never carries
        /// CarrierInstanceId forward) and re-validates at <paramref name="newAnchor"/> with the
        /// exact same rules as any other placement — a piece is either on the board or in a
        /// hold, never both, so "relocate" is how cargo gets back onto the board at all.</summary>
        public PlacementResult TryRelocate(string instanceId, GridCoord newAnchor, PieceRotation rotation)
        {
            if (!_pieces.TryGetValue(instanceId, out var piece))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            bool wasCarried = piece.CarrierInstanceId != null;

            if (!wasCarried && piece.Anchor.X == newAnchor.X && piece.Anchor.Y == newAnchor.Y && piece.Rotation == rotation)
                return new PlacementResult { Success = true };

            if (!TryRemove(instanceId, out var removed))
                return new PlacementResult { Success = false, Reason = "Piece not found" };

            if (!CanPlace(removed.Definition, newAnchor, rotation))
            {
                // Rollback: a normal piece goes back onto the board where it was; a carried
                // piece goes back into its hold exactly as it was (re-insert verbatim — no
                // main-board cells to reclaim, nothing else to recompute).
                if (wasCarried)
                    _pieces[instanceId] = removed;
                else
                    TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);

                return new PlacementResult { Success = false, Reason = "Invalid placement" };
            }

            return TryPlace(removed.Definition, newAnchor, removed.InstanceId, rotation, removed.IsMercenary);
        }

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
