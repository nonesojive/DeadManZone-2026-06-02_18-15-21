using DeadManZone.Core.Board;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class DragGhost : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        public static DragGhost Create(Transform parent, string pieceId, PieceDefinition definition = null)
        {
            var theme = UiThemeProvider.Current;
            var root = new GameObject("DragGhost", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 52f);

            var image = root.AddComponent<Image>();
            image.color = theme.cardColor;
            image.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            var source = PieceVisualLookup.GetSource(pieceId);
            if (definition != null && !string.IsNullOrEmpty(definition.DisplayName))
                tmp.text = definition.DisplayName;
            else if (source != null && !string.IsNullOrEmpty(source.displayName))
                tmp.text = source.displayName;
            else
                tmp.text = pieceId ?? "piece";

            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = theme.textPrimary;
            tmp.raycastTarget = false;

            if (definition != null)
                image.color = Color.Lerp(theme.cardColor, theme.GetCategoryTint(definition.Category), 0.4f);

            var ghost = root.AddComponent<DragGhost>();
            ghost.background = image;
            ghost.label = tmp;
            return ghost;
        }
    }
}
