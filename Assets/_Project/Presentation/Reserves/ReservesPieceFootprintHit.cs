using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Reserves
{
    /// <summary>
    /// Raycast target on reserves piece shape overlays for hover cards and drag.
    /// </summary>
    public sealed class ReservesPieceFootprintHit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string instanceId;
        [SerializeField] private GridCoord anchor;
        [SerializeField] private PieceRotation rotation;

        private PieceDefinition _definition;
        private PieceHoverCardController _hoverCardController;
        private BoardView _boardView;
        private Image _hitImage;
        private bool _isHovering;

        private void Awake() => EnsureHitTarget();

        public void Configure(
            string pieceInstanceId,
            PieceDefinition definition,
            GridCoord pieceAnchor,
            PieceRotation pieceRotation,
            PieceHoverCardController hoverController,
            BoardView boardView)
        {
            instanceId = pieceInstanceId;
            _definition = definition;
            anchor = pieceAnchor;
            rotation = pieceRotation;
            _hoverCardController = hoverController;
            _boardView = boardView;
            EnsureHitTarget();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_definition == null || string.IsNullOrEmpty(instanceId))
                return;

            _hoverCardController?.NotifyPieceHoverEnter(instanceId, _definition, BuildContext());
            _isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData) => ExitHover();

        public void OnBeginDrag(PointerEventData eventData)
        {
            ExitHover();
            _hoverCardController?.Hide();

            if (string.IsNullOrEmpty(instanceId) || DragDropController.Instance == null)
                return;

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.ReservesPiece,
                ReservesInstanceId = instanceId,
                PieceId = _definition.Id,
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

        private void OnDisable()
        {
            ExitHover();
            _hoverCardController?.Hide();
        }

        private PieceCardBuildContext BuildContext()
        {
            if (_boardView == null)
                return new PieceCardBuildContext { InstanceId = instanceId };

            return new PieceCardBuildContext
            {
                Board = _boardView.GetBoardState(),
                InstanceId = instanceId
            };
        }

        private void ExitHover()
        {
            if (!_isHovering)
                return;

            _isHovering = false;
            _hoverCardController?.NotifyPieceHoverExit(instanceId);
        }

        private void EnsureHitTarget()
        {
            if (_hitImage == null)
                _hitImage = GetComponent<Image>();
            if (_hitImage == null)
                _hitImage = gameObject.AddComponent<Image>();

            _hitImage.sprite = UiWhiteSprite.Get();
            _hitImage.type = Image.Type.Simple;
            _hitImage.color = new Color(1f, 1f, 1f, 0f);
            _hitImage.raycastTarget = true;
        }
    }
}
