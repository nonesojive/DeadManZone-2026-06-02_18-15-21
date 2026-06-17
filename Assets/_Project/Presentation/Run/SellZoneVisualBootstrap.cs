using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Applies themed sell-zone frame + centered icon overlay from <see cref="UiThemeSO"/>.
    /// </summary>
    public static class SellZoneVisualBootstrap
    {
        public const string IconChildName = "SellIcon";
        private const float IconFill = 0.52f;

        public static void Apply(Transform sellZone, UiThemeSO theme = null)
        {
            if (sellZone == null)
                return;

            theme ??= UiThemeProvider.Current;

            var background = sellZone.GetComponent<Image>();
            if (background != null)
                UiThemeApplicator.ApplySellZone(background, theme);

            if (theme?.sellZoneIconSprite == null)
                return;

            var icon = EnsureIconImage(sellZone);
            icon.sprite = theme.sellZoneIconSprite;
            icon.color = Color.white;
            icon.enabled = true;
            LayoutIcon(icon.rectTransform);

            foreach (var label in sellZone.GetComponentsInChildren<TMP_Text>(true))
                label.gameObject.SetActive(false);
        }

        private static Image EnsureIconImage(Transform parent)
        {
            var existing = parent.Find(IconChildName);
            if (existing != null && existing.TryGetComponent(out Image image))
                return image;

            var iconGo = new GameObject(IconChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(parent, false);

            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.type = Image.Type.Simple;
            return icon;
        }

        private static void LayoutIcon(RectTransform iconRect)
        {
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;

            var parent = iconRect.parent as RectTransform;
            if (parent == null)
                return;

            var parentSize = parent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
                parentSize = parent.sizeDelta;

            var iconSize = Mathf.Min(parentSize.x, parentSize.y) * IconFill;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }
    }
}
