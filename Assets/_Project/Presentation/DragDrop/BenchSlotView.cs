using DeadManZone.Core.Board;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class BenchSlotView : MonoBehaviour, IDropTarget, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;
        [SerializeField] private Transform chipRoot;

        private string _pieceId;
        private PieceDefinition _definition;

        public void Bind(int index, string pieceId, PieceDefinition definition)
        {
            slotIndex = index;
            _pieceId = pieceId;
            _definition = definition;

            ClearChip();
            var theme = UiThemeProvider.Current;

            if (background != null)
            {
                background.color = string.IsNullOrEmpty(pieceId)
                    ? theme.cardColor
                    : Color.Lerp(theme.cardColor, theme.GetCategoryTint(definition?.Category ?? PieceCategory.Unit), 0.35f);
            }

            if (label != null)
            {
                label.text = string.IsNullOrEmpty(pieceId) ? $"Bench {index + 1}" : string.Empty;
                label.gameObject.SetActive(string.IsNullOrEmpty(pieceId));
            }

            if (!string.IsNullOrEmpty(pieceId) && definition != null)
            {
                var root = chipRoot != null ? chipRoot : transform;
                PieceChipView.Create(root, definition, PieceVisualLookup.GetSource(pieceId));
            }
        }

        public bool TryAccept(DragPayload payload)
        {
            if (RunManager.Instance == null || payload == null)
                return false;

            if (!string.IsNullOrEmpty(_pieceId))
                return false;

            if (payload.SourceKind == DragSourceKind.ShopOffer)
                return RunManager.Instance.TryAcquireOfferToBench(payload.OfferId);

            if (payload.SourceKind == DragSourceKind.BoardPiece)
                return RunManager.Instance.TryMoveBoardToBench(payload.BoardInstanceId, slotIndex);

            return false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_pieceId) || _definition == null || DragDropController.Instance == null)
                return;

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.BenchPiece,
                PieceId = _pieceId,
                BenchIndex = slotIndex,
                Definition = _definition
            };
            DragDropController.Instance.BeginDrag(payload, transform, eventData);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) =>
            DragDropController.Instance?.EndDrag(eventData);

        private void ClearChip()
        {
            var root = chipRoot != null ? chipRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.GetComponent<PieceChipView>() != null)
                    Destroy(child.gameObject);
            }
        }
    }
}
