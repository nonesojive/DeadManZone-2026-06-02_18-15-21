using DeadManZone.Game;
using DeadManZone.Presentation.Reserves;
using UnityEngine;

namespace DeadManZone.Presentation.DragDrop
{
    [RequireComponent(typeof(ReservesTileView))]
    public sealed class ReservesTileDropTarget : MonoBehaviour, IDropTarget
    {
        private ReservesTileView _tile;
        private ReservesView _reservesView;

        private void Awake()
        {
            _tile = GetComponent<ReservesTileView>();
            _reservesView = GetComponentInParent<ReservesView>();
        }

        public bool TryAccept(DragPayload payload)
        {
            if (_tile == null || _reservesView == null || RunManager.Instance == null || payload == null)
                return false;

            var anchor = _tile.Coord;
            var rotation = payload.Rotation;

            switch (payload.SourceKind)
            {
                case DragSourceKind.ShopOffer:
                    return RunManager.Instance.TryAcquireOfferToReserves(payload.OfferId, anchor, rotation);
                case DragSourceKind.BoardPiece:
                    return RunManager.Instance.TryMoveBoardToReserves(
                        payload.BoardInstanceId,
                        anchor,
                        rotation);
                case DragSourceKind.ReservesPiece:
                    return _reservesView.TryRelocatePiece(payload.ReservesInstanceId, anchor, rotation);
                default:
                    return false;
            }
        }
    }
}
