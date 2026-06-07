using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Shop;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class ShopOfferDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ShopOffer _offer;

        public void SetOffer(ShopOffer offer) => _offer = offer;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_offer == null || DragDropController.Instance == null)
                return;

            var view = GetComponentInParent<ShopOfferView>();
            view?.SetPreviewVisible(false);

            var registry = ContentDatabase.Load()?.BuildRegistry();
            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.ShopOffer,
                OfferId = _offer.OfferId,
                PieceId = _offer.PieceId,
                Offer = _offer,
                Definition = registry?.GetById(_offer.PieceId),
                Rotation = PieceRotation.R0
            };

            ResolveBoardCellMetrics(out float cellSize, out float spacing);
            DragDropController.Instance.BeginDrag(
                payload,
                transform,
                eventData,
                cellSize,
                spacing,
                pieceOnlyGhost: true);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            DragDropController.Instance?.EndDrag(eventData);
            GetComponentInParent<ShopOfferView>()?.SetPreviewVisible(true);
        }

        private static void ResolveBoardCellMetrics(out float cellSize, out float spacing)
        {
            var board = Object.FindFirstObjectByType<BoardView>();
            if (board != null)
            {
                var resolved = ShopLayoutMetrics.Resolve(
                    board.CellSize.x,
                    new Vector2(board.CellSpacing.x, board.CellSpacing.y));
                cellSize = resolved.cellSize;
                spacing = resolved.spacing;
                return;
            }

            var fallback = ShopLayoutMetrics.Resolve(48f, new Vector2(3f, 3f));
            cellSize = fallback.cellSize;
            spacing = fallback.spacing;
        }
    }
}
