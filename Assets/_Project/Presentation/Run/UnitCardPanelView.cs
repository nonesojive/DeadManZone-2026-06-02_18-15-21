using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;
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

        public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
        {
            if (definition == null)
                return;

            if (cardView == null)
            {
                Debug.LogError("UnitCardPanelView is missing cardView reference.", this);
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
    }
}
