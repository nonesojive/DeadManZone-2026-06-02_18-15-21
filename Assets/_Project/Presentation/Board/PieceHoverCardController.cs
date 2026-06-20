using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using UnityEngine;

namespace DeadManZone.Presentation.Board
{
    /// <summary>Shows the fixed center-column unit card panel on piece hover.</summary>
    public sealed class PieceHoverCardController : MonoBehaviour
    {
        [SerializeField] private UnitCardPanelView unitCardPanel;
        [SerializeField] private BuildMessagesView messagesView;

        private readonly PieceHoverLock _hoverLock = new();

        private void Awake()
        {
            LegacyUnitCardCleanup.RemoveFloatingHoverLayers();
            Hide();
        }

        public void NotifyPieceHoverEnter(
            string instanceId,
            PieceDefinition definition,
            PieceCardBuildContext context)
        {
            _hoverLock.Enter(instanceId);
            Show(definition, Vector2.zero, context);
        }

        public void NotifyPieceHoverExit(string instanceId)
        {
            _hoverLock.Exit(instanceId);
            if (!_hoverLock.HasActiveHover)
                Hide();
        }

        public void Show(
            PieceDefinition definition,
            Vector2 screenPosition,
            SynergyEngine.SynergyResult? synergy = null)
        {
            Show(definition, screenPosition, synergy.HasValue
                ? new PieceCardBuildContext { Synergy = synergy }
                : null);
        }

        public void Show(
            PieceDefinition definition,
            Vector2 screenPosition,
            PieceCardBuildContext context)
        {
            if (definition == null)
                return;

            var panel = ResolveUnitCardPanel();
            if (panel == null)
                return;

            panel.Show(definition, context);
            ResolveMessagesView()?.SetFlavorFromPiece(definition);
        }

        public void Hide()
        {
            ResolveUnitCardPanel()?.Hide();
            ResolveMessagesView()?.ClearFlavor();
        }

        public void SetFixedUnitCardPanel(UnitCardPanelView panel) => unitCardPanel = panel;

        public void SetMessagesView(BuildMessagesView messages) => messagesView = messages;

        private UnitCardPanelView ResolveUnitCardPanel()
        {
            if (unitCardPanel != null)
                return unitCardPanel;

            unitCardPanel = FindFirstObjectByType<UnitCardPanelView>();
            return unitCardPanel;
        }

        private BuildMessagesView ResolveMessagesView()
        {
            if (messagesView != null)
                return messagesView;

            messagesView = FindFirstObjectByType<BuildMessagesView>();
            return messagesView;
        }
    }
}
