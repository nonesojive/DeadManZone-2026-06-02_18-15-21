using UnityEngine;

namespace DeadManZone.Presentation.DragDrop
{
    /// <summary>
    /// Top-most layer the drag ghost lives on, so the ghost is never sorted underneath the
    /// thing you are dragging it over.
    ///
    /// Why nested under the host canvas rather than a standalone overlay canvas: the ghost's
    /// cells are sized in the same reference-resolution pixels as the board cells it has to
    /// line up with, and the two shop canvases use DIFFERENT CanvasScaler match values
    /// (legacy Canvas match=0, ShopV2Canvas match=0.5). A standalone canvas would need its
    /// own scaler and would drift out of step with whichever surface is live. Nesting
    /// inherits the host's scaler for free; overrideSorting still lifts it above every other
    /// overlay canvas in the scene.
    /// </summary>
    public static class DragLayer
    {
        /// <summary>Above the run meta strip (300) and the combat layers (400-500).</summary>
        public const int SortingOrder = 900;

        private static RectTransform _layer;

        /// <summary>The drag layer under <paramref name="host"/>, created on first use.</summary>
        public static RectTransform For(Canvas host)
        {
            if (host == null)
                return null;

            // Re-home if the shop surface changed (V2 <-> legacy) since the last drag.
            if (_layer != null && _layer.parent == host.transform)
                return _layer;

            if (_layer != null)
                Object.Destroy(_layer.gameObject);

            var go = new GameObject("DragLayer", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(host.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            // Deliberately NO GraphicRaycaster: the ghost sits under the cursor for the whole
            // drag, and DragDropController.FindDropTarget raycasts to locate the drop zone
            // beneath it. A raycaster here would let the ghost eat its own drop.

            _layer = rect;
            return _layer;
        }
    }
}
