using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.DragDrop
{
    /// <summary>2026-07-17 Oathborn transport tentpole (§2.5 Armored Ark): one cargo slot on a
    /// placed transport's on-board cargo panel. Accepts a shop offer, a reserves piece, or an
    /// already-placed board piece — routes to the matching BoardView composite, which is the
    /// only thing that knows how to turn "drag this in" into BoardState.TryLoadCargo. Dragging
    /// the rendered cargo icon back OUT (see TransportCargoPanelPresenter) is an ordinary
    /// BoardPieceDragSource onto a normal board tile, which un-embarks it as a side effect of
    /// BoardState.TryRelocate — no extra removal path needed here.</summary>
    public sealed class TransportCargoSlotDropTarget : MonoBehaviour, IDropTarget
    {
        private string _transportInstanceId;
        private BoardView _boardView;

        public void Configure(string transportInstanceId, BoardView boardView)
        {
            _transportInstanceId = transportInstanceId;
            _boardView = boardView;
        }

        public bool TryAccept(DragPayload payload)
        {
            if (_boardView == null || string.IsNullOrEmpty(_transportInstanceId) || payload == null)
                return false;

            bool accepted;
            string reason;
            switch (payload.SourceKind)
            {
                case DragSourceKind.ShopOffer:
                    accepted = _boardView.TryAcquireOfferToCargo(payload.OfferId, _transportInstanceId, out reason);
                    break;
                case DragSourceKind.ReservesPiece:
                    accepted = _boardView.TryLoadCargoFromReserves(payload.ReservesInstanceId, _transportInstanceId, out reason);
                    break;
                case DragSourceKind.BoardPiece:
                    accepted = _boardView.TryLoadCargoFromBoard(payload.BoardInstanceId, _transportInstanceId, out reason);
                    break;
                default:
                    accepted = false;
                    reason = null;
                    break;
            }

            // 2026-07-17 round-2 playtest fix: rejected-drop affordance — brief shake/flash
            // + the actual reason (e.g. "Cargo does not fit in transport hold"), not a silent
            // snap-back. Same GameObject as the panel (TransportCargoPanelPresenter.Configure
            // adds this drop target to itself), so no lookup needed.
            if (!accepted && !string.IsNullOrEmpty(reason))
                GetComponent<TransportCargoPanelPresenter>()?.FlashRejected(reason);

            return accepted;
        }
    }
}
