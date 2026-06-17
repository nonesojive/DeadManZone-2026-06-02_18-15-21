using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Visual
{
    /// <summary>
    /// Draws a black border with a soft white outer glow around panels and buttons.
    /// </summary>
    public static class UiPanelChrome
    {
        public const string ChromeRootName = "UiPanelChrome";

        private const float GlowThickness = 3f;
        private const float BorderThickness = 2f;
        private static readonly Color GlowColor = new(1f, 1f, 1f, 0.24f);
        private static readonly Color BorderColor = new(0.02f, 0.02f, 0.03f, 0.96f);

        public static void Apply(Transform target)
        {
            if (target == null)
                return;

            var root = EnsureChromeRoot(target);
            RebuildChrome(root);
        }

        public static void Apply(Button button) => Apply(button != null ? button.transform : null);

        public static void Remove(Transform target)
        {
            if (target == null)
                return;

            var chrome = target.Find(ChromeRootName);
            if (chrome == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(chrome.gameObject);
            else
#endif
                Object.Destroy(chrome.gameObject);
        }

        public static void RemoveFromSubtree(Transform root)
        {
            if (root == null)
                return;

            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                var chrome = all[i];
                if (chrome.name != ChromeRootName)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(chrome.gameObject);
                else
#endif
                    Object.Destroy(chrome.gameObject);
            }
        }

        private static RectTransform EnsureChromeRoot(Transform target)
        {
            var existing = target.Find(ChromeRootName);
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            var go = new GameObject(ChromeRootName, typeof(RectTransform));
            go.transform.SetParent(target, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-GlowThickness, -GlowThickness);
            rect.offsetMax = new Vector2(GlowThickness, GlowThickness);
            return rect;
        }

        private static void RebuildChrome(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(child);
                else
#endif
                    Object.Destroy(child);
            }

            var glow = CreateLayer(root, "Glow");
            AddEdgeBars(glow, GlowThickness, GlowColor, inset: 0f);

            var border = CreateLayer(root, "Border");
            AddEdgeBars(border, BorderThickness, BorderColor, inset: GlowThickness);
        }

        private static RectTransform CreateLayer(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void AddEdgeBars(RectTransform parent, float thickness, Color color, float inset)
        {
            CreateBar(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -inset), new Vector2(0f, thickness), color);
            CreateBar(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, inset), new Vector2(0f, thickness), color);
            CreateBar(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(inset, 0f), new Vector2(thickness, 0f), color);
            CreateBar(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-inset, 0f), new Vector2(thickness, 0f), color);
        }

        private static void CreateBar(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
