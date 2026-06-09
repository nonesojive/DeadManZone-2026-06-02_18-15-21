using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Fills the piece footprint with a semi-transparent faction/neutral token background.
    /// </summary>
    internal static class PieceFootprintBackground
    {
        private const float Inset = 1f;

        public static void Create(RectTransform parent, Color color)
        {
            var fill = CreateFill(parent);
            StretchToParent(fill, Inset);
            ApplyColor(fill, color);
        }

        public static void Create(
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot,
            Color color)
        {
            var rect = CreateFill(parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(
                Mathf.Max(size.x - Inset * 2f, 4f),
                Mathf.Max(size.y - Inset * 2f, 4f));
            ApplyColor(rect, color);
        }

        private static RectTransform CreateFill(RectTransform parent)
        {
            var fill = new GameObject("FootprintBackground", typeof(RectTransform));
            var rect = fill.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.SetAsFirstSibling();
            return rect;
        }

        private static void StretchToParent(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void ApplyColor(RectTransform fill, Color color)
        {
            var image = fill.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
