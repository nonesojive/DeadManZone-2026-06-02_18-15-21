using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class BoardPieceDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string pieceId;
        [SerializeField] private GridCoord anchor;
        private PieceDefinition _definition;

        public void Configure(string pieceInstanceId, string pieceDefinitionId, GridCoord pieceAnchor, PieceDefinition definition)
        {
            instanceId = pieceInstanceId;
            pieceId = pieceDefinitionId;
            anchor = pieceAnchor;
            _definition = definition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(instanceId) || DragDropController.Instance == null)
                return;

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.BoardPiece,
                BoardInstanceId = instanceId,
                PieceId = pieceId,
                BoardAnchor = anchor,
                Definition = _definition
            };
            DragDropController.Instance.BeginDrag(payload, transform, eventData);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) =>
            DragDropController.Instance?.EndDrag(eventData);
    }
}
