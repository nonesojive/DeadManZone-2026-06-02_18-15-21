using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Draws a rectangular border around piece footprint bounds so multi-cell sizes read clearly.
    /// </summary>
    internal static class PieceFootprintOutline
    {
        public const float Thickness = 2f;
        private static readonly Color DefaultColor = new Color(0.08f, 0.10f, 0.12f, 0.9f);

        public static void Create(RectTransform parent, Color? color = null)
        {
            var container = CreateContainer(parent);
            StretchToParent(container);
            AddBars(container, color ?? DefaultColor);
        }

        public static void Create(
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot,
            Color? color = null)
        {
            var rect = CreateContainer(parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            AddBars(rect, color ?? DefaultColor);
        }

        private static RectTransform CreateContainer(RectTransform parent)
        {
            var container = new GameObject("FootprintOutline", typeof(RectTransform));
            var rect = container.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddBars(RectTransform container, Color borderColor)
        {
            float t = Thickness;
            CreateBar(container, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t), borderColor);
            CreateBar(container, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t), borderColor);
            CreateBar(container, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f), borderColor);
            CreateBar(container, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f), borderColor);
        }

        private static void CreateBar(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
