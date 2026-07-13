using System.Collections.Generic;
using DeadManZone.Core.Board;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// Shows the unit/building hovercard for a hovered piece. Attach to `ShopV2Canvas`; expects a
    /// `HovercardHost` child holding one instance of each card prefab.
    ///
    /// The card lives in a FIXED slot: the right column, in the gap between Fight Orders and
    /// Begin Combat. It used to slide to the side opposite the pointer, which parked it top-right
    /// straight over War Footing and Fight Orders. A fixed slot is both what the owner asked for
    /// and simpler — that gap is dead space, so the card never occludes anything, and there is no
    /// pointer-side case left to get wrong.
    ///
    /// The host's slot (position, pivot, scale) is AUTHORED on `HovercardHost` in the scene. This
    /// presenter only decides WHICH card to show and binds it — it never moves or resizes anything.
    /// </summary>
    public sealed class ShopV2HovercardPresenter : MonoBehaviour
    {
        public static ShopV2HovercardPresenter Instance { get; private set; }

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

            // Position, pivot and scale are AUTHORED on HovercardHost in the scene — not set here.
            _host = (RectTransform)host;

            var missing = new List<string>();
            _unitCard = FindView(host, "UnitHovercardV2", missing);
            _buildingCard = FindView(host, "BuildingHovercardV2", missing);

            if (missing.Count > 0)
                Debug.LogWarning(
                    $"ShopV2HovercardPresenter: missing children: {string.Join(", ", missing)}", this);

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Binds an authored card. ShopV2HovercardView lives on the hovercard PREFABS, so it is
        /// never added here — a presenter that silently AddComponent's its own view papers over a
        /// broken prefab link and turns an authoring mistake into a bug you only find at runtime.
        /// A missing card is reported, not repaired.
        /// </summary>
        private static ShopV2HovercardView FindView(Transform host, string childName, List<string> missing)
        {
            var child = host.Find(childName);
            if (child == null)
            {
                missing.Add(childName);
                return null;
            }

            var view = child.GetComponent<ShopV2HovercardView>();
            if (view == null)
                missing.Add($"{childName} (ShopV2HovercardView)");

            return view;
        }

        public void Show(PieceDefinition definition, Vector2 pointerScreenPosition) =>
            Show(definition, pointerScreenPosition, null);

        /// <summary>
        /// <paramref name="context"/> is null for shop offers (no board yet) and non-null for
        /// placed pieces, where it carries the adjacency synergy the card must reflect.
        /// </summary>
        public void Show(
            PieceDefinition definition,
            Vector2 pointerScreenPosition,
            Core.Tags.PieceCardBuildContext context)
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
            view.Bind(definition, context);

            // Fixed slot — no pointer-side flip. The gap it sits in is dead space, so there is
            // nothing for the card to occlude and nothing to slide away from.
            _host.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_host != null)
                _host.gameObject.SetActive(false);
        }
    }
}
