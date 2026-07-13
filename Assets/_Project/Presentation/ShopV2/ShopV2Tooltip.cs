using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// Makes one element explain itself on hover, and lights it while it does.
    ///
    /// The highlight and the tooltip are deliberately the SAME component: the highlight is the
    /// affordance FOR the tooltip. Shipping a hover glow on something that then explains nothing
    /// teaches the player to stop hovering.
    ///
    /// Content is either AUTHORED (title/body typed in the scene — resources, the Smelter, reroll
    /// rules) or pushed by a presenter via <see cref="SetContent"/> for anything live (a critical
    /// mass rule's progress, a battle condition's effect). Nothing here reads game state itself.
    ///
    /// Requires the element's own Graphic to have raycastTarget = true — authored, per the
    /// "mock decoration is not state" rule.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopV2Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content (authored, or pushed by a presenter)")]
        [SerializeField] private string title;
        [TextArea(2, 5)]
        [SerializeField] private string body;

        [Header("Hover highlight (optional)")]
        [Tooltip("Usually the element's Border. Leave empty for elements that should not light.")]
        [SerializeField] private Graphic highlight;
        [SerializeField] private Color idle = new(0.17f, 0.14f, 0.10f, 1f);
        [SerializeField] private Color hover = new(0.90f, 0.78f, 0.50f, 1f);
        [SerializeField] private float fadeSeconds = 0.08f;

        private Color _from;
        private Color _to;
        private float _t = 1f;
        private bool _hovered;

        /// <summary>Live content from a presenter. Empty title AND body = no tooltip on hover.</summary>
        public void SetContent(string newTitle, string newBody)
        {
            title = newTitle;
            body = newBody;

            // Re-show if the content changed while the pointer is already sitting on it.
            if (_hovered)
                ShopV2TooltipPresenter.Instance?.Show(title, body, CurrentPointer());
        }

        /// <summary>For presenters that own this element's idle colour (e.g. tier accents).</summary>
        public void SetIdle(Color idleColor)
        {
            idle = idleColor;
            if (!_hovered)
                Snap(idle);
        }

        private void Awake() => Snap(idle);

        private void OnDisable()
        {
            // Hidden mid-hover, the exit event never arrives — so the tooltip would stay on screen
            // pointing at nothing, and the element would come back already lit.
            if (_hovered)
                ShopV2TooltipPresenter.Instance?.Hide();

            _hovered = false;
            Snap(idle);
        }

        private void Update()
        {
            if (_t >= 1f || highlight == null)
                return;

            _t = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(_t + Time.unscaledDeltaTime / fadeSeconds);
            highlight.color = Color.Lerp(_from, _to, _t);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Fade(hover);
            ShopV2TooltipPresenter.Instance?.Show(title, body, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Fade(idle);
            ShopV2TooltipPresenter.Instance?.Hide();
        }

        private static Vector2 CurrentPointer() =>
            Input.mousePosition;

        private void Fade(Color to)
        {
            if (highlight == null)
                return;

            _from = highlight.color;
            _to = to;
            _t = 0f;
        }

        private void Snap(Color to)
        {
            if (highlight == null)
                return;

            _from = to;
            _to = to;
            _t = 1f;
            highlight.color = to;
        }
    }
}
