using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Run
{
    public sealed class PlacedPieceRecord
    {
        public string InstanceId { get; set; }
        public string PieceId { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int RotationDegrees { get; set; }

        /// <summary>2026-07-16 faction-roster-v1 §1.4: additive field, deserializes false on
        /// older saves (no mercenaries could exist before this wave). Mirrors PlacedPiece
        /// .IsMercenary — acquisition-based and permanent.</summary>
        public bool IsMercenary { get; set; }

        /// <summary>2026-07-17 Oathborn transport tentpole: additive field, deserializes null on
        /// older saves (no transports could exist before this wave). Mirrors PlacedPiece
        /// .CarrierInstanceId — null = not cargo. Restored in a second pass after every piece
        /// is placed (the carrier must already exist on the board for TryLoadCargo to accept
        /// it), see <see cref="BoardSnapshotMapper.ToBoard"/>.</summary>
        public string CarrierInstanceId { get; set; }

        /// <summary>PROVISIONAL 2026-07-19 owner spec (fight-option strength ratios):
        /// additive field, deserializes 1 on older saves (no scaled pieces could exist
        /// before this wave — Newtonsoft leaves the initializer untouched for a missing
        /// member). Mirrors PlacedPiece.StatScale; restored in a final pass after every
        /// piece — including reconstructed cargo — is on the board, see
        /// <see cref="BoardSnapshotMapper.ToBoard"/>.</summary>
        public float StatScale { get; set; } = 1f;
    }

    public sealed class BoardSnapshot
    {
        public string BoardKind { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int RearCols { get; set; }
        public int SupportCols { get; set; }

        /// <summary>Legacy row-based layout; used when <see cref="RearCols"/> is zero.</summary>
        public int RearRows { get; set; }
        public int SupportRows { get; set; }

        public List<GridCoordRecord> SpecialTiles { get; set; } = new();
        public List<GridCoordRecord> BlockedCells { get; set; } = new();
        public List<PlacedPieceRecord> Pieces { get; set; } = new();
    }

    public sealed class GridCoordRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public static class BoardSnapshotMapper
    {
        public static BoardSnapshot FromBoard(BoardState board) =>
            new BoardSnapshot
            {
                BoardKind = board.Layout.Kind.ToString(),
                Width = board.Layout.Width,
                Height = board.Layout.Height,
                SpecialTiles = board.Layout.SpecialTiles
                    .Select(t => new GridCoordRecord { X = t.X, Y = t.Y })
                    .ToList(),
                BlockedCells = board.Layout.BlockedCells
                    .Select(t => new GridCoordRecord { X = t.X, Y = t.Y })
                    .ToList(),
                Pieces = board.Pieces.Select(p => new PlacedPieceRecord
                {
                    InstanceId = p.InstanceId,
                    PieceId = p.Definition.Id,
                    AnchorX = p.Anchor.X,
                    AnchorY = p.Anchor.Y,
                    RotationDegrees = (int)p.Rotation,
                    IsMercenary = p.IsMercenary,
                    CarrierInstanceId = p.CarrierInstanceId,
                    StatScale = p.StatScale
                }).ToList()
            };

        [System.Obsolete("Use FromBoard(BoardState) for schema v8 boards.")]
        public static BoardSnapshot FromBoard(BoardState board, int rearCols, int supportCols)
        {
            var snapshot = FromBoard(board);
            snapshot.RearCols = rearCols;
            snapshot.SupportCols = supportCols;
            return snapshot;
        }

        public static BoardState ToBoard(BoardSnapshot snapshot, ContentRegistry registry) =>
            ToBoard(snapshot, registry, null, null);

        /// <summary>2026-07-17 round-2 playtest fix: <paramref name="reservesForEviction"/> and
        /// <paramref name="warnings"/> are additive — every existing call site (2-arg overload)
        /// is unaffected. Needed because TryLoadCargo now gates on the cargo hold's fixed 2x2
        /// footprint (BoardState.CargoGridWidth/Height): a save made before that rule existed
        /// can legally list more cargo than now fits (the owner's repro: 3 pieces / 5 cells into
        /// a 4-cell hold). Re-tagging never throws for a fit failure — the excess piece(s) are
        /// gracefully evicted to <paramref name="reservesForEviction"/> (or just left on the
        /// board, already legally placed there since Build, if reserves is null/full) and
        /// logged to <paramref name="warnings"/> — never dropped.</summary>
        public static BoardState ToBoard(
            BoardSnapshot snapshot,
            ContentRegistry registry,
            ReservesState reservesForEviction,
            List<string> warnings)
        {
            var layout = CreateLayout(snapshot);
            var board = new BoardState(layout);
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                var anchor = new GridCoord(record.AnchorX, record.AnchorY);

                // 2026-07-17 round-4 fix: a carried piece's persisted anchor is cosmetic (its
                // carrier's own anchor, see BoardState.TryEmbarkCargo) — it never claimed a real
                // board cell, so running it through the ordinary TryPlace here collided with the
                // transport sitting on that same cell ("Cell occupied", surfaced the moment any
                // loaded transport survived a save/reload). Stub it in instead; the second pass
                // below re-tags it via TryLoadCargo exactly like a fresh load would.
                if (!string.IsNullOrEmpty(record.CarrierInstanceId))
                {
                    board.RestoreCargoStub(
                        definition, record.InstanceId, record.CarrierInstanceId, anchor, rotation, record.IsMercenary);
                    continue;
                }

                var result = board.TryPlace(
                    definition,
                    anchor,
                    record.InstanceId,
                    rotation,
                    record.IsMercenary);
                if (!result.Success)
                    throw new System.InvalidOperationException(
                        $"Failed to restore '{record.PieceId}' at ({record.AnchorX},{record.AnchorY}): {result.Reason}");
            }

            // Second pass: re-apply cargo tags now every piece (including every transport)
            // is on the board — TryLoadCargo requires both instance ids to already exist.
            // Deterministic order (InstanceId) so which pieces "win" the hold on an
            // over-capacity legacy save is stable and testable.
            foreach (var record in snapshot.Pieces.OrderBy(p => p.InstanceId))
            {
                if (string.IsNullOrEmpty(record.CarrierInstanceId))
                    continue;

                var loadResult = board.TryLoadCargo(record.InstanceId, record.CarrierInstanceId);
                if (loadResult.Success)
                    continue;

                EvictCargoExcess(board, reservesForEviction, record, loadResult.Reason, warnings);
            }

            // Final pass: restore StatScale (fight-option ratio scaling). Runs AFTER the
            // cargo pass because TryLoadCargo reconstructs the PlacedPiece instances it
            // re-tags — a scale set in the first pass would be lost. <= 0 (corrupt) and 1
            // (the universal default) both mean "leave the freshly-placed default alone".
            var piecesById = board.Pieces.ToDictionary(p => p.InstanceId);
            foreach (var record in snapshot.Pieces)
            {
                if (record.StatScale <= 0f || record.StatScale == 1f)
                    continue;

                if (piecesById.TryGetValue(record.InstanceId, out var placed))
                    placed.StatScale = record.StatScale;
            }

            return board;
        }

        /// <summary>The piece is already legally placed on <paramref name="board"/> at its own
        /// Anchor (first pass) — it just fails to be TAGGED as cargo. Prefer moving it to
        /// reserves (bench it for the player to re-place deliberately); if reserves is
        /// unavailable or full, leave it on the board exactly where it already is. Either way
        /// it is never removed without a home.</summary>
        private static void EvictCargoExcess(
            BoardState board,
            ReservesState reservesForEviction,
            PlacedPieceRecord record,
            string reason,
            List<string> warnings)
        {
            string outcome = "left on the board";

            if (reservesForEviction != null
                && board.TryRemove(record.InstanceId, out var removed)
                && TryFindOpenReservesAnchor(reservesForEviction, removed.Definition, out var reservesAnchor))
            {
                var placed = reservesForEviction.TryPlace(
                    removed.Definition, reservesAnchor, removed.InstanceId, PieceRotation.R0, removed.IsMercenary);
                if (placed.Success)
                {
                    outcome = "evicted to reserves";
                }
                else
                {
                    // Shouldn't happen (we just found an open anchor) — put it back rather
                    // than lose it.
                    board.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                }
            }

            warnings?.Add(
                $"Cargo '{record.InstanceId}' no longer fits transport '{record.CarrierInstanceId}' ({reason}) — {outcome}.");
        }

        private static bool TryFindOpenReservesAnchor(ReservesState reserves, PieceDefinition definition, out GridCoord anchor)
        {
            for (int y = 0; y < ReservesState.Height; y++)
            {
                for (int x = 0; x < ReservesState.Width; x++)
                {
                    var candidate = new GridCoord(x, y);
                    if (reserves.CanPlace(definition, candidate))
                    {
                        anchor = candidate;
                        return true;
                    }
                }
            }

            anchor = default;
            return false;
        }

        private static BoardLayout CreateLayout(BoardSnapshot snapshot)
        {
            var specialTiles = snapshot.SpecialTiles
                .Select(t => new GridCoord(t.X, t.Y))
                .ToArray();
            var blockedCells = snapshot.BlockedCells
                .Select(t => new GridCoord(t.X, t.Y))
                .ToArray();

            if (!string.IsNullOrWhiteSpace(snapshot.BoardKind)
                && System.Enum.TryParse<BoardKind>(snapshot.BoardKind, out var kind))
            {
                return kind switch
                {
                    BoardKind.Combat => BoardLayout.CreateCombatBoard(
                        System.Math.Max(snapshot.Width, snapshot.Height),
                        specialTiles),
                    BoardKind.Hq => BoardLayout.CreateHqBoard(
                        snapshot.Width,
                        snapshot.Height,
                        blockedCells,
                        specialTiles),
                    _ => BoardLayout.CreateUnzoned(
                        snapshot.Width,
                        snapshot.Height,
                        kind,
                        blockedCells,
                        specialTiles)
                };
            }

            if (snapshot.RearCols > 0 || snapshot.SupportCols > 0)
            {
                return BoardLayout.CreateHorizontalZones(
                    snapshot.Width,
                    snapshot.Height,
                    snapshot.RearCols > 0 ? snapshot.RearCols : 3,
                    snapshot.SupportCols > 0 ? snapshot.SupportCols : 3,
                    specialTiles);
            }

            return BoardLayout.CreateStandard(
                snapshot.Width,
                snapshot.Height,
                snapshot.RearRows,
                snapshot.SupportRows,
                specialTiles);
        }

        private static PieceRotation RotationFromDegrees(int degrees) =>
            degrees switch
            {
                90 => PieceRotation.R90,
                180 => PieceRotation.R180,
                270 => PieceRotation.R270,
                _ => PieceRotation.R0
            };
    }
}
