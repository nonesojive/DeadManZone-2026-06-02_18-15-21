using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Labels and checkpoint notch overlays for the army health bar band.</summary>
    internal static class CombatHudChromeBuilder
    {
        private static readonly Color LabelColor = new(0.68f, 0.64f, 0.58f, 0.88f);
        private static readonly Color NotchColor = new(0.52f, 0.48f, 0.42f, 0.7f);

        public static void AddSideLabel(Transform barRoot, string text, bool alignLeft)
        {
            if (barRoot == null)
                return;

            var labelGo = new GameObject("SideLabel", typeof(RectTransform));
            labelGo.transform.SetParent(barRoot, false);

            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(alignLeft ? 0f : 1f, 1f);
            rect.anchorMax = new Vector2(alignLeft ? 0f : 1f, 1f);
            rect.pivot = new Vector2(alignLeft ? 0f : 1f, 0f);
            rect.anchoredPosition = new Vector2(alignLeft ? 4f : -4f, 6f);
            rect.sizeDelta = new Vector2(160f, 22f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 2f;
            tmp.alignment = alignLeft ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomRight;
            tmp.color = LabelColor;
            tmp.raycastTarget = false;
        }

        public static void AddCheckpointNotches(Transform barRoot)
        {
            if (barRoot == null)
                return;

            var sliderBox = barRoot.Find("SliderBox");
            var notchParent = sliderBox != null ? sliderBox : barRoot;
            AddNotch(notchParent, 0.75f, "Notch75");
            AddNotch(notchParent, 0.30f, "Notch30");
        }

        public static void HideIconBox(Transform barRoot)
        {
            var iconBox = barRoot?.Find("IconBox");
            if (iconBox != null)
                iconBox.gameObject.SetActive(false);
        }

        private static void AddNotch(Transform parent, float normalizedX, string name)
        {
            const float notchWidth = 0.006f;
            var notchGo = new GameObject(name, typeof(RectTransform));
            notchGo.transform.SetParent(parent, false);
            var notch = notchGo.GetComponent<RectTransform>();
            notch.anchorMin = new Vector2(normalizedX - notchWidth * 0.5f, 0.05f);
            notch.anchorMax = new Vector2(normalizedX + notchWidth * 0.5f, 0.95f);
            notch.offsetMin = Vector2.zero;
            notch.offsetMax = Vector2.zero;

            var image = notchGo.AddComponent<Image>();
            image.color = NotchColor;
            image.raycastTarget = false;
        }
    }
}
