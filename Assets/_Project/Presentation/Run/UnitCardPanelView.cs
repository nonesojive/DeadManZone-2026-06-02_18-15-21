using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Fixed center-column unit detail panel (hidden when idle).</summary>
    public sealed class UnitCardPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [FormerlySerializedAs("unitCard")]
        [SerializeField] private PieceCardView cardView;

        public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

        public PieceCardView CardView => cardView;

        /// <summary>Wires an existing scene/prefab instance. Never writes to UnitDetailCard.prefab.</summary>
        public void EnsureCardView()
        {
            var host = panelRoot != null ? panelRoot : transform;
            LegacyUnitCardCleanup.RemoveLegacyChildren(host);
            ResolveCardView(host);
        }

        public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
        {
            if (definition == null)
                return;

            var host = panelRoot != null ? panelRoot : transform;
            LegacyUnitCardCleanup.RemoveLegacyChildren(host);
            ResolveCardView(host);

            if (cardView == null)
            {
                Debug.LogWarning(
                    "UnitCardPanelView has no assigned UnitDetailCard. Place or link one under UnitCardPanel in the Run scene.",
                    this);
                return;
            }

            var model = PieceCardViewModelBuilder.Build(definition, context);
            string overflowTooltip = PieceCardOverflowTooltip.Build(definition, model);
            cardView.Bind(model, overflowTooltip);
            cardView.Show();

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            cardView?.Hide();
            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }

        private void ResolveCardView(Transform host)
        {
            if (cardView != null)
                return;

            cardView = host.GetComponentInChildren<PieceCardView>(true);
        }
    }
}
