using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;

namespace DeadManZone.Game
{
    /// <summary>2026-07-17 Oathborn transport tentpole (§2.5 Armored Ark): Build-phase cargo
    /// loading. BoardState.TryLoadCargo only tags an ALREADY-PLACED piece (it doesn't place
    /// one) — so "buy/move a unit into the Ark's cargo slots" is a two-step composite here,
    /// not a new Core rule: place the piece exactly the way the existing shop/reserves flows
    /// already do (an anchor found via the same BoardState.CanPlace every drag-drop placement
    /// already calls), then tag it with the existing TryLoadCargo. Every rollback undoes both
    /// steps so a failed load never leaves a stray unpaid/untagged piece behind.</summary>
    public sealed partial class RunOrchestrator
    {
        public bool TryLoadCargoFromBoard(string cargoInstanceId, string transportInstanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
                return false;

            // Cargo must already live on the SAME BoardState instance as its transport
            // (TryLoadCargo looks the id up in that one board's piece dictionary).
            if (board.Pieces.All(p => p.InstanceId != cargoInstanceId))
                return false;

            var result = board.TryLoadCargo(cargoInstanceId, transportInstanceId);
            if (!result.Success)
                return false;

            SaveBoardForPiece(transport.Definition, board);
            return true;
        }

        public bool TryLoadCargoFromReserves(string reservesInstanceId, string transportInstanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
                return false;

            var reserves = GetReserves();
            if (!reserves.TryRemove(reservesInstanceId, out var removed))
                return false;

            if (!TryFindOpenAnchor(board, removed.Definition, out var anchor))
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                return false;
            }

            var place = board.TryPlace(removed.Definition, anchor, removed.InstanceId, PieceRotation.R0, removed.IsMercenary);
            if (!place.Success)
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                return false;
            }

            var load = board.TryLoadCargo(removed.InstanceId, transportInstanceId);
            if (!load.Success)
            {
                board.TryRemove(removed.InstanceId, out _);
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                return false;
            }

            SaveReserves(reserves);
            SaveBoardForPiece(transport.Definition, board);
            return true;
        }

        public bool TryAcquireOfferToCargo(string offerId, string transportInstanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
                return false;

            var offer = FindOffer(offerId);
            if (offer == null || !CanAffordOffer(offerId))
                return false;

            var piece = _registry.GetById(offer.PieceId);
            if (!TryFindOpenAnchor(board, piece, out var anchor))
                return false;

            string instanceId = System.Guid.NewGuid().ToString("N");
            PayOffer(offer);

            var place = board.TryPlace(piece, anchor, instanceId, PieceRotation.R0, offer.IsMercenary);
            if (!place.Success)
            {
                RefundOffer(offer);
                return false;
            }

            var load = board.TryLoadCargo(instanceId, transportInstanceId);
            if (!load.Success)
            {
                board.TryRemove(instanceId, out _);
                RefundOffer(offer);
                return false;
            }

            SaveBoardForPiece(transport.Definition, board);
            RemoveOffer(offerId);
            Persist();
            return true;
        }

        /// <summary>First cell (row-major) the piece can legally occupy on <paramref name="board"/>
        /// right now — the same CanPlace rule every drag-drop placement already calls, just
        /// scanned instead of player-chosen (there's no meaningful "which empty cell" choice
        /// for a piece that's about to disappear into cargo anyway).</summary>
        private static bool TryFindOpenAnchor(BoardState board, PieceDefinition piece, out GridCoord anchor)
        {
            for (int y = 0; y < board.Layout.Height; y++)
            {
                for (int x = 0; x < board.Layout.Width; x++)
                {
                    var candidate = new GridCoord(x, y);
                    if (board.CanPlace(piece, candidate))
                    {
                        anchor = candidate;
                        return true;
                    }
                }
            }

            anchor = default;
            return false;
        }
    }
}
