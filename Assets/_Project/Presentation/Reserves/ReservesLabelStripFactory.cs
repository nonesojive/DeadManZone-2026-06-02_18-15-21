using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Reserves
{
    /// <summary>
    /// Vertical reserves label strip matching board zone strip styling.
    /// </summary>
    public static class ReservesLabelStripFactory
    {
        public const string StripName = "ReservesLabelStrip";

        public static RectTransform Ensure(Transform reservesRegion, UiThemeSO theme)
        {
            var strip = reservesRegion.Find(StripName) as RectTransform;
            if (strip == null)
                strip = CreateStrip(reservesRegion, theme);

            HideLegacyTitle(reservesRegion);
            return strip;
        }

        private static RectTransform CreateStrip(Transform reservesRegion, UiThemeSO theme)
        {
            var stripGo = new GameObject(StripName, typeof(RectTransform));
            stripGo.transform.SetParent(reservesRegion, false);
            stripGo.transform.SetAsFirstSibling();

            var stripRect = stripGo.GetComponent<RectTransform>();
            stripRect.anchorMin = Vector2.zero;
            stripRect.anchorMax = new Vector2(0.1f, 1f);
            stripRect.offsetMin = Vector2.zero;
            stripRect.offsetMax = Vector2.zero;

            var image = stripGo.AddComponent<Image>();
            UiThemeApplicator.ApplyStorageSlotEmpty(image, theme);
            image.color = theme.GetReserveSlotColor();
            image.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(stripGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(120f, 18f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "RESERVES";
            label.fontSize = 11;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            labelRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
            UiThemeApplicator.ApplyLabel(label, secondary: false, theme);

            return stripRect;
        }

        private static void HideLegacyTitle(Transform reservesRegion)
        {
            foreach (var text in reservesRegion.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name == "Label" && text.transform.parent == reservesRegion)
                {
                    text.gameObject.SetActive(false);
                    return;
                }

                if (text.text == "Reserves" && text.transform.parent == reservesRegion)
                    text.gameObject.SetActive(false);
            }
        }
    }
}
