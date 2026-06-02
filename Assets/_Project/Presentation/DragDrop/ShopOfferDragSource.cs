using DeadManZone.Core.Shop;
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

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.ShopOffer,
                OfferId = _offer.OfferId,
                PieceId = _offer.PieceId,
                Offer = _offer
            };
            DragDropController.Instance.BeginDrag(payload, transform, eventData);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) =>
            DragDropController.Instance?.EndDrag(eventData);
    }
}
