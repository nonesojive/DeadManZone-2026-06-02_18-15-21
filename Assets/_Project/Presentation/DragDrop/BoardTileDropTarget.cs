using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.DragDrop
{
    [RequireComponent(typeof(BoardTileView))]
    public sealed class BoardTileDropTarget : MonoBehaviour, IDropTarget
    {
        private BoardTileView _tile;
        private BoardView _boardView;

        private void Awake()
        {
            _tile = GetComponent<BoardTileView>();
            _boardView = GetComponentInParent<BoardView>();
        }

        public bool TryAccept(DragPayload payload)
        {
            if (_tile == null || _boardView == null || RunManager.Instance == null || payload == null)
                return false;

            var anchor = _tile.Coord;
            var rotation = payload.Rotation;

            switch (payload.SourceKind)
            {
                case DragSourceKind.ShopOffer:
                    return _boardView.TryAcquireOfferToBoard(payload.OfferId, anchor, rotation);
                case DragSourceKind.ReservesPiece:
                    return _boardView.TryPlaceFromReserves(payload.ReservesInstanceId, anchor, rotation);
                case DragSourceKind.BoardPiece:
                    return _boardView.TryMovePlacedPiece(payload.BoardInstanceId, anchor, rotation);
                default:
                    return false;
            }
        }
    }
}
