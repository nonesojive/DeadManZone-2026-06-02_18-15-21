using System.Collections.Generic;
using DeadManZone.Core.Board;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class DragDropController : MonoBehaviour
    {
        public static DragDropController Instance { get; private set; }

        [SerializeField] private Canvas rootCanvas;

        private DragGhost _ghost;
        private DragPayload _activePayload;
        private Transform _returnParent;
        private int _returnSiblingIndex;

        /// <summary>Rect the ghost's localPosition is expressed in — always its actual parent.</summary>
        private RectTransform _ghostSpace;

        /// <summary>
        /// The canvas the ghost hangs off. ShopV2Canvas (order 10) outranks the legacy Canvas
        /// (order 0), and the boards now live on V2 — so a ghost left on the legacy canvas
        /// draws BEHIND the boards it is being dragged over. Follow the live shop surface.
        /// </summary>
        private Canvas GhostHost()
        {
            var v2 = ShopV2.ShopV2Surface.Canvas;
            return v2 != null ? v2 : rootCanvas;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (_activePayload == null)
                return;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R))
                ApplyRotation(PieceRotationUtil.RotateClockwise(_activePayload.Rotation));
            else if (Input.GetKeyDown(KeyCode.Q))
                ApplyRotation(PieceRotationUtil.RotateCounterClockwise(_activePayload.Rotation));
        }

        public void BeginDrag(
            DragPayload payload,
            Transform returnParent,
            PointerEventData eventData,
            float cellSize = 36f,
            float cellSpacing = 3f,
            bool pieceOnlyGhost = false)
        {
            _activePayload = payload;
            _returnParent = returnParent;
            _returnSiblingIndex = returnParent != null ? returnParent.GetSiblingIndex() : 0;

            if (_ghost != null)
                Destroy(_ghost.gameObject);

            var host = GhostHost();
            var layer = DragLayer.For(host);
            var canvasTransform = layer != null
                ? layer
                : (host != null ? host.transform : transform);

            _ghostSpace = canvasTransform as RectTransform;

            _ghost = DragGhost.Create(
                canvasTransform,
                payload.PieceId ?? payload.Offer?.PieceId ?? "piece",
                payload.Definition,
                payload.Rotation,
                cellSize,
                cellSpacing,
                pieceOnlyGhost);
            FollowPointer(eventData);
        }

        public void UpdateDrag(PointerEventData eventData) => FollowPointer(eventData);

        public void EndDrag(PointerEventData eventData)
        {
            if (_activePayload == null)
                return;

            var target = FindDropTarget(eventData);
            bool accepted = target != null && target.TryAccept(_activePayload);

            if (!accepted && _returnParent != null)
                _returnParent.SetSiblingIndex(_returnSiblingIndex);

            if (_ghost != null)
            {
                Destroy(_ghost.gameObject);
                _ghost = null;
            }

            _ghostSpace = null;
            _activePayload = null;
            _returnParent = null;
        }

        private void ApplyRotation(PieceRotation rotation)
        {
            if (_activePayload == null)
                return;

            _activePayload.Rotation = rotation;
            _ghost?.SetRotation(rotation);
        }

        private void FollowPointer(PointerEventData eventData)
        {
            // Must convert into the ghost's ACTUAL parent rect, not rootCanvas — the ghost now
            // lives on the DragLayer under the live shop surface, which may not be rootCanvas.
            if (_ghost == null || _ghostSpace == null)
                return;

            var rect = _ghost.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _ghostSpace,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            rect.localPosition = localPoint;
        }

        private static IDropTarget FindDropTarget(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var hit in results)
            {
                var target = hit.gameObject.GetComponentInParent<IDropTarget>();
                if (target != null)
                    return target;
            }

            return null;
        }
    }
}
