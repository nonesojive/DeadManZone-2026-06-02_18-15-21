using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Fixed center-column unit detail panel (hidden when idle).</summary>
    public sealed class UnitCardPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private PieceHoverCard unitCard;

        public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

        public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
        {
            if (definition == null || unitCard == null)
                return;

            unitCard.ConfigureEmbeddedLayout();
            var model = PieceCardViewModelBuilder.Build(definition, context);
            unitCard.Bind(model, string.Empty);
            unitCard.Show();

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            unitCard?.Hide();
            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }
    }
}
