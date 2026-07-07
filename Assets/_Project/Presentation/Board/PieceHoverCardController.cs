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
            PieceCardBuildContext context,
            Vector2? pointerScreenPosition = null)
        {
            _hoverLock.Enter(instanceId);
            Show(definition, pointerScreenPosition ?? Vector2.zero, context);

            // Slide the card to the side of the screen opposite the hovered piece so
            // the middle of the shop stays clear (piece on the left → card on the right).
            if (pointerScreenPosition.HasValue)
                ResolveUnitCardPanel()?.PositionOppositePointer(pointerScreenPosition.Value);
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
            PieceAbilityEngine.SynergyResult? synergy = null)
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

            unitCardPanel = FindFirstObjectByType<UnitCardPanelView>(FindObjectsInactive.Include);
            if (unitCardPanel == null)
                unitCardPanel = CreateFloatingCardPanel();
            return unitCardPanel;
        }

        /// <summary>The hover card is positioned dynamically opposite the pointer, so it needs
        /// no fixed home. If the authored panel was removed (e.g. the center column was deleted),
        /// build a floating one under the canvas so hover keeps working.</summary>
        private UnitCardPanelView CreateFloatingCardPanel()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return null;

            var go = new GameObject("UnitCardPanel", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(450f, 700f);

            // The building card is auto-provisioned by EnsureCardView; instantiate the unit card.
            var unitPrefab = CardPrefabRuntimeLoader.LoadPrefab(CardPrefabPaths.UnitDetailCard);
            if (unitPrefab != null)
            {
                var card = Instantiate(unitPrefab, go.transform);
                card.name = UnitCardPanelView.UnitDetailCardName;
                card.SetActive(false);
            }

            var view = go.AddComponent<UnitCardPanelView>();
            view.EnsureCardView();
            go.SetActive(false);
            return view;
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
