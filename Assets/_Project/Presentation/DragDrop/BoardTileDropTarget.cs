using DeadManZone.Core.Common;
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

            switch (payload.SourceKind)
            {
                case DragSourceKind.ShopOffer:
                    return RunManager.Instance.TryAcquireOfferToBoard(payload.OfferId, anchor);
                case DragSourceKind.BenchPiece:
                    return _boardView.TryPlaceFromBench(payload.BenchIndex, anchor);
                case DragSourceKind.BoardPiece:
                    return _boardView.TryMovePlacedPiece(payload.BoardInstanceId, anchor);
                default:
                    return false;
            }
        }
    }
}
