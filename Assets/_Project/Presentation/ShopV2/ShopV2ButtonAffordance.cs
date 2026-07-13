using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// Interaction feedback for a ShopV2 button: the BORDER lights on hover and flares on press,
    /// matching the border language the offer slots and fight cards already speak.
    ///
    /// Why this exists at all: a Unity Selectable's ColorTint can only tint ONE graphic
    /// (targetGraphic). The buttons want two channels — the fill warms (authored ColorBlock, no
    /// code) and the border lights (this). Everything this component uses is SERIALIZED and set
    /// in the scene; it holds no layout and invents no values.
    ///
    /// Ownership: where a presenter already owns the border's base colour (FightCard MarchButtons
    /// carry the tier accent), that presenter calls <see cref="SetPalette"/> and this component
    /// layers interaction on top. It never reads the border's current colour back — inheriting
    /// state from the art is the exact trap that made the mock's decoration permanent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopV2ButtonAffordance : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Graphic border;

        [Header("Border palette (authored)")]
        [SerializeField] private Color idle = new(0.17f, 0.14f, 0.10f, 1f);
        [SerializeField] private Color hover = new(0.90f, 0.78f, 0.50f, 1f);
        [SerializeField] private Color pressed = new(1f, 0.94f, 0.76f, 1f);
        [SerializeField] private Color disabled = new(0.12f, 0.11f, 0.09f, 1f);

        [Header("Feel")]
        [Tooltip("Hover eases in; press is instant so the click feels like it lands.")]
        [SerializeField] private float fadeSeconds = 0.08f;

        private Selectable _selectable;
        private bool _hovered;
        private bool _held;
        private bool _wasInteractable = true;

        private Color _from;
        private Color _to;
        private float _t = 1f;
        private float _duration;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _wasInteractable = _selectable == null || _selectable.interactable;
            SnapTo(Target());
        }

        private void OnDisable()
        {
            // A button hidden mid-hover never gets its exit event; drop the highlight so it does
            // not come back lit the next time the panel opens.
            _hovered = false;
            _held = false;
            SnapTo(Target());
        }

        /// <summary>
        /// For presenters that own the border's base colour (e.g. the fight-card tier accent).
        /// Hover/press are derived from it so a bronze NORMAL front stays bronze when lit.
        /// </summary>
        public void SetPalette(Color idleColor)
        {
            idle = idleColor;
            hover = Color.Lerp(idleColor, Color.white, 0.35f);
            pressed = Color.Lerp(idleColor, Color.white, 0.65f);
            disabled = idleColor * 0.45f;
            disabled.a = idleColor.a;
            SnapTo(Target());
        }

        private void Update()
        {
            // Interactable is driven by game state (Begin Combat gates on a chosen front), so it
            // can flip without any pointer event. Poll it — one bool compare, no allocation.
            bool interactable = _selectable == null || _selectable.interactable;
            if (interactable != _wasInteractable)
            {
                _wasInteractable = interactable;
                if (!interactable)
                {
                    _hovered = false;
                    _held = false;
                }

                Fade(Target());
            }

            if (_t >= 1f || border == null)
                return;

            _t = _duration <= 0f ? 1f : Mathf.Clamp01(_t + Time.unscaledDeltaTime / _duration);
            border.color = Color.Lerp(_from, _to, _t);
        }

        private Color Target()
        {
            if (_selectable != null && !_selectable.interactable)
                return disabled;
            if (_held)
                return pressed;
            return _hovered ? hover : idle;
        }

        private void Fade(Color to)
        {
            if (border == null)
                return;

            _from = border.color;
            _to = to;
            _t = 0f;
            _duration = fadeSeconds;
        }

        private void SnapTo(Color to)
        {
            if (border == null)
                return;

            _from = to;
            _to = to;
            _t = 1f;
            border.color = to;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Fade(Target());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _held = false;
            Fade(Target());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            // Instant, not eased: the press must feel like it lands the moment the mouse goes down.
            SnapTo(Target());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _held = false;
            Fade(Target());
        }
    }
}
