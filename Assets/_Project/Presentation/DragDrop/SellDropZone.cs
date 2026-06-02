using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class SellDropZone : MonoBehaviour, IDropTarget, IDropHandler
    {
        [SerializeField] private Image highlight;

        public bool TryAccept(DragPayload payload)
        {
            if (RunManager.Instance == null || payload == null)
                return false;

            switch (payload.SourceKind)
            {
                case DragSourceKind.BenchPiece:
                    return RunManager.Instance.TrySellFromBench(payload.BenchIndex);
                case DragSourceKind.BoardPiece:
                    return RunManager.Instance.TrySellPlacedPiece(payload.BoardInstanceId);
                default:
                    return false;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handled via DragDropController.EndDrag raycast.
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlight != null)
                highlight.color = highlighted
                    ? new Color(0.55f, 0.2f, 0.2f, 0.85f)
                    : new Color(0.28f, 0.12f, 0.12f, 0.75f);
        }
    }
}
