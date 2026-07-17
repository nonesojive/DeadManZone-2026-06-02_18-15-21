using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Run;

namespace DeadManZone.Game
{
    /// <summary>2026-07-17 round-3 playtest fix (Armored Ark cargo lives IN the transport, not
    /// on the board): loading cargo from the shop or reserves goes STRAIGHT into the transport's
    /// hold (BoardState.TryEmbarkCargo) — no more "place it on the main board, then tag it"
    /// composite. A piece is either on the board or in a hold, never a placeholder for both, so
    /// there's no main-board anchor to find and no TryFindOpenAnchor scan left to do. Every
    /// rollback undoes the one step taken (refund money / put the piece back) so a failed load
    /// never leaves a stray unpaid/untagged piece behind.</summary>
    public sealed partial class RunOrchestrator
    {
        public bool TryLoadCargoFromBoard(string cargoInstanceId, string transportInstanceId) =>
            TryLoadCargoFromBoard(cargoInstanceId, transportInstanceId, out _);

        /// <summary>2026-07-17 round-2 playtest fix: the reason out-param surfaces
        /// BoardState.TryLoadCargo's own rejection message (e.g. "Cargo does not fit in
        /// transport hold") all the way to the cargo panel's rejected-drop tooltip.</summary>
        public bool TryLoadCargoFromBoard(string cargoInstanceId, string transportInstanceId, out string reason)
        {
            reason = null;
            if (State.Phase != RunPhase.Build)
            {
                reason = "Not in Build phase";
                return false;
            }

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
            {
                reason = "Transport not found";
                return false;
            }

            // Cargo must already live on the SAME BoardState instance as its transport
            // (TryLoadCargo looks the id up in that one board's piece dictionary).
            if (board.Pieces.All(p => p.InstanceId != cargoInstanceId))
            {
                reason = "Piece not found on this board";
                return false;
            }

            var result = board.TryLoadCargo(cargoInstanceId, transportInstanceId);
            if (!result.Success)
            {
                reason = result.Reason;
                return false;
            }

            SaveBoardForPiece(transport.Definition, board);
            return true;
        }

        public bool TryLoadCargoFromReserves(string reservesInstanceId, string transportInstanceId) =>
            TryLoadCargoFromReserves(reservesInstanceId, transportInstanceId, out _);

        public bool TryLoadCargoFromReserves(string reservesInstanceId, string transportInstanceId, out string reason)
        {
            reason = null;
            if (State.Phase != RunPhase.Build)
            {
                reason = "Not in Build phase";
                return false;
            }

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
            {
                reason = "Transport not found";
                return false;
            }

            var reserves = GetReserves();
            if (!reserves.TryRemove(reservesInstanceId, out var removed))
            {
                reason = "Piece not found in reserves";
                return false;
            }

            var embark = board.TryEmbarkCargo(removed.Definition, transportInstanceId, removed.InstanceId, removed.IsMercenary);
            if (!embark.Success)
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                reason = embark.Reason;
                return false;
            }

            SaveReserves(reserves);
            SaveBoardForPiece(transport.Definition, board);
            return true;
        }

        public bool TryAcquireOfferToCargo(string offerId, string transportInstanceId) =>
            TryAcquireOfferToCargo(offerId, transportInstanceId, out _);

        public bool TryAcquireOfferToCargo(string offerId, string transportInstanceId, out string reason)
        {
            reason = null;
            if (State.Phase != RunPhase.Build)
            {
                reason = "Not in Build phase";
                return false;
            }

            if (!TryFindPlacedPiece(transportInstanceId, out var board, out var transport) || !transport.Definition.IsTransport)
            {
                reason = "Transport not found";
                return false;
            }

            var offer = FindOffer(offerId);
            if (offer == null || !CanAffordOffer(offerId))
            {
                reason = "Cannot afford offer";
                return false;
            }

            var piece = _registry.GetById(offer.PieceId);
            string instanceId = System.Guid.NewGuid().ToString("N");
            PayOffer(offer);

            var embark = board.TryEmbarkCargo(piece, transportInstanceId, instanceId, offer.IsMercenary);
            if (!embark.Success)
            {
                RefundOffer(offer);
                reason = embark.Reason;
                return false;
            }

            SaveBoardForPiece(transport.Definition, board);
            RemoveOffer(offerId);
            Persist();
            return true;
        }
    }
}
