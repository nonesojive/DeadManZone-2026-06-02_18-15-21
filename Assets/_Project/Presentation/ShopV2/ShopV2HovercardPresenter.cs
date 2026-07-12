using DeadManZone.Core.Board;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Shows the unit/building hovercard for a hovered piece, sliding to the side
    /// opposite the pointer (same behavior contract as PieceHoverCardController). Attach to
    /// `ShopV2Canvas`; expects a `HovercardHost` child holding one instance of each card prefab.</summary>
    public sealed class ShopV2HovercardPresenter : MonoBehaviour
    {
        public static ShopV2HovercardPresenter Instance { get; private set; }

        private const float LeftX = 96f;
        private const float RightX = 1508f;
        private const float TopY = -140f;

        private RectTransform _host;
        private ShopV2HovercardView _unitCard;
        private ShopV2HovercardView _buildingCard;

        private void Awake()
        {
            Instance = this;
            var host = transform.Find("HovercardHost");
            if (host == null)
            {
                Debug.LogWarning("ShopV2HovercardPresenter: HovercardHost not found.", this);
                return;
            }

            _host = (RectTransform)host;
            _unitCard = EnsureView(host, "UnitHovercardV2");
            _buildingCard = EnsureView(host, "BuildingHovercardV2");
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private static ShopV2HovercardView EnsureView(Transform host, string childName)
        {
            var child = host.Find(childName);
            if (child == null)
                return null;
            var view = child.GetComponent<ShopV2HovercardView>();
            return view != null ? view : child.gameObject.AddComponent<ShopV2HovercardView>();
        }

        public void Show(PieceDefinition definition, Vector2 pointerScreenPosition)
        {
            if (definition == null || _host == null)
                return;

            // HQ pieces are Category.Building; board structures (e.g. MG nest) are Category.Unit
            // with a structure/building primary tag — both read better on the building card.
            bool isStructure = definition.Category == PieceCategory.Building
                || string.Equals(definition.Primary, "structure", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(definition.Primary, "building", System.StringComparison.OrdinalIgnoreCase);
            var view = isStructure ? _buildingCard : _unitCard;
            var other = view == _unitCard ? _buildingCard : _unitCard;
            if (view == null)
                return;

            if (other != null)
                other.gameObject.SetActive(false);

            view.gameObject.SetActive(true);
            view.Bind(definition);

            // Slide the card to the side of the screen opposite the pointer.
            bool pointerOnLeft = pointerScreenPosition.x < Screen.width * 0.5f;
            _host.anchoredPosition = new Vector2(pointerOnLeft ? RightX : LeftX, TopY);
            _host.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_host != null)
                _host.gameObject.SetActive(false);
        }
    }
}
