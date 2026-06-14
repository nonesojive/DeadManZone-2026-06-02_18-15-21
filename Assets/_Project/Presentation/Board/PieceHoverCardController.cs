using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    public sealed class PieceHoverCardController : MonoBehaviour
    {
        private static PieceHoverCard _sharedHoverCard;

        [SerializeField] private PieceHoverCard hoverCard;
        [SerializeField] private UnitCardPanelView fixedUnitCardPanel;
        [SerializeField] private BuildMessagesView messagesView;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Vector2 screenOffset = new(24f, -24f);

        private readonly List<string> _hiddenTagNames = new();

        private void Awake() => Hide();

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

            if (fixedUnitCardPanel != null)
            {
                fixedUnitCardPanel.Show(definition, context);
                ResolveMessagesView()?.SetFlavorFromPiece(definition);
                return;
            }

            var card = ResolveHoverCard();
            var canvas = ResolveCanvas();
            if (card == null)
                return;

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(definition, context);
            string overflowTooltip = BuildOverflowTooltip(definition, model);
            card.Bind(model, overflowTooltip);
            card.SetScreenPosition(canvas, screenPosition, screenOffset);
            card.Show();
        }

        public void Hide()
        {
            if (fixedUnitCardPanel != null)
                fixedUnitCardPanel.Hide();

            ResolveMessagesView()?.ClearFlavor();

            if (hoverCard != null)
                hoverCard.Hide();
        }

        public void SetFixedUnitCardPanel(UnitCardPanelView panel) => fixedUnitCardPanel = panel;

        public void SetMessagesView(BuildMessagesView messages) => messagesView = messages;

        private PieceHoverCard ResolveHoverCard()
        {
            if (hoverCard != null)
                return hoverCard;

            if (_sharedHoverCard != null)
            {
                hoverCard = _sharedHoverCard;
                return hoverCard;
            }

            hoverCard = GetComponentInChildren<PieceHoverCard>(true);
            if (hoverCard != null)
            {
                _sharedHoverCard = hoverCard;
                return hoverCard;
            }

            var canvas = ResolveCanvas();
            if (canvas == null)
                return null;

            var layer = GetOrCreateTooltipLayer(canvas);
            var cardGo = new GameObject("PieceHoverCard", typeof(RectTransform), typeof(PieceHoverCard));
            cardGo.transform.SetParent(layer, false);
            cardGo.SetActive(false);
            hoverCard = cardGo.GetComponent<PieceHoverCard>();
            _sharedHoverCard = hoverCard;
            return hoverCard;
        }

        private Canvas ResolveCanvas()
        {
            if (targetCanvas != null)
                return targetCanvas;

            var canvas = GetComponentInParent<Canvas>();
            var outermost = canvas;
            while (canvas != null)
            {
                outermost = canvas;
                var parent = canvas.transform.parent;
                canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            }

            if (outermost != null)
            {
                targetCanvas = outermost;
                return targetCanvas;
            }

            foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    targetCanvas = candidate;
                    return targetCanvas;
                }
            }

            targetCanvas = FindFirstObjectByType<Canvas>();
            return targetCanvas;
        }

        private static RectTransform GetOrCreateTooltipLayer(Canvas canvas)
        {
            const string layerName = "PieceHoverCardLayer";
            var existing = canvas.transform.Find(layerName);
            if (existing != null)
                return existing as RectTransform;

            var layerGo = new GameObject(layerName, typeof(RectTransform));
            layerGo.transform.SetParent(canvas.transform, false);
            var layerRect = layerGo.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerGo.transform.SetAsLastSibling();

            var canvasOverride = layerGo.AddComponent<Canvas>();
            canvasOverride.overrideSorting = true;
            canvasOverride.sortingOrder = 500;

            if (canvas.GetComponent<GraphicRaycaster>() != null)
                layerGo.AddComponent<GraphicRaycaster>();

            return layerRect;
        }

        private string BuildOverflowTooltip(PieceDefinition definition, PieceCardViewModel model)
        {
            if (definition == null || model == null || model.OverflowCount <= 0)
                return string.Empty;

            PieceTagQueries.PlayerVisibleTagsResult allVisible = PieceTagQueries.GetPlayerVisibleTags(
                definition,
                maxOptionalChips: int.MaxValue);

            int visibleCount = model.IdentityTags.Count + model.OptionalTags.Count;
            if (visibleCount >= allVisible.VisibleTags.Count)
                return string.Empty;

            _hiddenTagNames.Clear();
            for (int i = visibleCount; i < allVisible.VisibleTags.Count; i++)
            {
                var hiddenTag = allVisible.VisibleTags[i];
                if (hiddenTag != null && !string.IsNullOrWhiteSpace(hiddenTag.DisplayName))
                    _hiddenTagNames.Add(hiddenTag.DisplayName);
            }

            return _hiddenTagNames.Count == 0 ? string.Empty : string.Join(", ", _hiddenTagNames);
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
