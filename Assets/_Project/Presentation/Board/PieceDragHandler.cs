using System;
using DeadManZone.Core.Board;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Board
{
    /// <summary>MVP drag source that announces which piece is being dragged.</summary>
    public sealed class PieceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string pieceId;
        private PieceDefinition _definition;

        public event Action<PieceDefinition> DragStarted;
        public event Action DragEnded;

        public void SetPiece(PieceDefinition definition)
        {
            _definition = definition;
            pieceId = definition?.Id ?? string.Empty;
        }

        public string PieceId => pieceId;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_definition != null)
                DragStarted?.Invoke(_definition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Visual drag proxy is wired during Task 17 scene integration.
        }

        public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke();
    }
}
