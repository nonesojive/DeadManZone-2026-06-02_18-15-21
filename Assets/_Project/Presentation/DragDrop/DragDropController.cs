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

        public void BeginDrag(DragPayload payload, Transform returnParent, PointerEventData eventData)
        {
            _activePayload = payload;
            _returnParent = returnParent;
            _returnSiblingIndex = returnParent != null ? returnParent.GetSiblingIndex() : 0;

            if (_ghost != null)
                Destroy(_ghost.gameObject);

            var canvasTransform = rootCanvas != null ? rootCanvas.transform : transform;
            _ghost = DragGhost.Create(
                canvasTransform,
                payload.PieceId ?? payload.Offer?.PieceId ?? "piece",
                payload.Definition,
                payload.Rotation);
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
            if (_ghost == null || rootCanvas == null)
                return;

            var rect = _ghost.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
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
