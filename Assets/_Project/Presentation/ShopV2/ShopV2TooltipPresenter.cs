using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// The single tooltip panel. Binds authored children by name (`TooltipHost/Title`,
    /// `TooltipHost/Body`) and does nothing but fill and place them — the panel's look, size and
    /// sorting are authored in the scene. Attach to `ShopV2Canvas`.
    ///
    /// One shared panel rather than a tooltip per element: N panels means N things that can be
    /// left open, drift out of style, or fight for sorting order.
    /// </summary>
    public sealed class ShopV2TooltipPresenter : MonoBehaviour
    {
        public static ShopV2TooltipPresenter Instance { get; private set; }

        /// <summary>Sits below and right of the cursor, out from under it.</summary>
        private static readonly Vector2 PointerOffset = new(18f, -18f);

        /// <summary>Keeps the panel off the canvas edge.</summary>
        private const float EdgePadding = 12f;

        private RectTransform _canvasRect;
        private RectTransform _host;
        private TMP_Text _title;
        private TMP_Text _body;

        private void Awake()
        {
            Instance = this;
            _canvasRect = (RectTransform)transform;

            var host = transform.Find("TooltipHost");
            if (host == null)
            {
                Debug.LogWarning("ShopV2TooltipPresenter: TooltipHost not found.", this);
                return;
            }

            _host = (RectTransform)host;
            _title = host.Find("Title")?.GetComponent<TMP_Text>();
            _body = host.Find("Body")?.GetComponent<TMP_Text>();

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(string title, string body, Vector2 pointerScreenPosition)
        {
            if (_host == null || (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)))
                return;

            if (_title != null)
            {
                _title.text = title ?? string.Empty;
                _title.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
            }

            if (_body != null)
            {
                _body.text = body ?? string.Empty;
                _body.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
            }

            _host.gameObject.SetActive(true);

            // Force a layout pass before measuring — the panel auto-sizes to the body text, and
            // an unrebuilt rect would be measured at its PREVIOUS size and clamp against the wrong
            // edge (the tooltip would drift off-screen for longer bodies).
            LayoutRebuilder.ForceRebuildLayoutImmediate(_host);
            Place(pointerScreenPosition);
        }

        public void Hide()
        {
            if (_host != null)
                _host.gameObject.SetActive(false);
        }

        private void Place(Vector2 pointerScreenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, pointerScreenPosition, null, out var local);

            var canvasSize = _canvasRect.rect.size;
            var half = canvasSize * 0.5f;

            // ScreenPointToLocalPointInRectangle returns coordinates relative to the canvas CENTRE
            // (its pivot), but TooltipHost is anchored TOP-LEFT — so the pointer must be converted
            // into that space or the panel lands off-screen. Getting this wrong is silent: the
            // tooltip "works", it is just parked above the top edge.
            var anchor = new Vector2(local.x + half.x, local.y - half.y);

            float x = anchor.x + PointerOffset.x;
            float y = anchor.y + PointerOffset.y;

            // FLIP at the edges rather than clamp: a clamped tooltip ends up sitting under the
            // cursor, covering the very thing it is explaining.
            if (x + size(_host).x > canvasSize.x - EdgePadding)
                x = anchor.x - PointerOffset.x - size(_host).x;
            if (y - size(_host).y < -canvasSize.y + EdgePadding)
                y = anchor.y - PointerOffset.y + size(_host).y;

            _host.anchoredPosition = new Vector2(x, y);
        }

        private static Vector2 size(RectTransform t) => t.rect.size;
    }
}
