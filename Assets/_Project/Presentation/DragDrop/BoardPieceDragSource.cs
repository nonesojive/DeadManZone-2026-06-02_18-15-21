using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class BoardPieceDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string pieceId;
        [SerializeField] private GridCoord anchor;
        [SerializeField] private PieceRotation rotation;
        private PieceDefinition _definition;
        private PieceHoverCardController _hoverCardController;

        public void Configure(
            string pieceInstanceId,
            string pieceDefinitionId,
            GridCoord pieceAnchor,
            PieceDefinition definition,
            PieceRotation pieceRotation,
            PieceHoverCardController hoverCardController = null)
        {
            instanceId = pieceInstanceId;
            pieceId = pieceDefinitionId;
            anchor = pieceAnchor;
            _definition = definition;
            rotation = pieceRotation;
            _hoverCardController = hoverCardController;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _hoverCardController?.Hide();

            if (string.IsNullOrEmpty(instanceId) || DragDropController.Instance == null)
                return;

            if (PieceTagQueries.HasTag(_definition, GameTagIds.Hq))
                return;

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.BoardPiece,
                BoardInstanceId = instanceId,
                PieceId = pieceId,
                BoardAnchor = anchor,
                Definition = _definition,
                Rotation = rotation
            };
            DragDropController.Instance.BeginDrag(payload, transform, eventData);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) =>
            DragDropController.Instance?.EndDrag(eventData);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_definition == null)
                return;

            _hoverCardController?.Show(_definition, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) =>
            _hoverCardController?.Hide();

        private void OnDisable() => _hoverCardController?.Hide();
    }
}
