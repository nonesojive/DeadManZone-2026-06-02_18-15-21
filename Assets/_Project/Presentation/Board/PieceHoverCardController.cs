using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Presentation.Board
{
    public sealed class PieceHoverCardController : MonoBehaviour
    {
        [SerializeField] private PieceHoverCard hoverCard;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Vector2 screenOffset = new(24f, -24f);

        private readonly List<string> _hiddenTagNames = new();

        private void Awake()
        {
            ResolveCanvas();
            ResolveHoverCard();
            Hide();
        }

        public void Show(PieceDefinition definition, Vector2 screenPosition)
        {
            if (definition == null)
                return;

            var card = ResolveHoverCard();
            var canvas = ResolveCanvas();
            if (card == null)
                return;

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(definition);
            string overflowTooltip = BuildOverflowTooltip(definition, model);
            card.Bind(model, overflowTooltip);
            card.SetScreenPosition(canvas, screenPosition, screenOffset);
            card.Show();
        }

        public void Hide()
        {
            if (hoverCard != null)
                hoverCard.Hide();
        }

        private PieceHoverCard ResolveHoverCard()
        {
            if (hoverCard != null)
                return hoverCard;

            hoverCard = GetComponentInChildren<PieceHoverCard>(true);
            if (hoverCard != null)
                return hoverCard;

            var canvas = ResolveCanvas();
            if (canvas == null)
                return null;

            var cardGo = new GameObject("PieceHoverCard", typeof(RectTransform), typeof(PieceHoverCard));
            cardGo.transform.SetParent(canvas.transform, false);
            hoverCard = cardGo.GetComponent<PieceHoverCard>();
            return hoverCard;
        }

        private Canvas ResolveCanvas()
        {
            if (targetCanvas != null)
                return targetCanvas;

            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = FindFirstObjectByType<Canvas>();
            return targetCanvas;
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
    }
}
