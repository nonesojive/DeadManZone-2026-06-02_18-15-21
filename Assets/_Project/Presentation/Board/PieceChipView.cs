using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
  public sealed class PieceChipView : MonoBehaviour
  {
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    public void Bind(PieceDefinition definition, PieceDefinitionSO source = null)
    {
      if (definition == null)
      {
        Clear();
        return;
      }

      var theme = UiThemeProvider.Current;
      if (background != null)
        background.color = theme.GetCategoryTint(definition.Category);

      if (nameText != null)
        nameText.text = GetShortName(definition);

      if (iconImage != null)
      {
        var sprite = source?.icon;
        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
        if (source != null && source.categoryTint.a > 0.01f && sprite == null)
          background.color = source.categoryTint;
      }
    }

    public void Clear()
    {
      if (nameText != null)
        nameText.text = string.Empty;
      if (iconImage != null)
      {
        iconImage.sprite = null;
        iconImage.enabled = false;
      }
    }

    private static string GetShortName(PieceDefinition definition)
    {
      if (!string.IsNullOrEmpty(definition.DisplayName))
        return definition.DisplayName;

      var id = definition.Id ?? string.Empty;
      return id.Length <= 14 ? id : id.Substring(0, 12) + "…";
    }

    public static PieceChipView Create(Transform parent, PieceDefinition definition, PieceDefinitionSO source = null)
    {
      var root = new GameObject("PieceChip", typeof(RectTransform));
      root.transform.SetParent(parent, false);
      var rect = root.GetComponent<RectTransform>();
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = new Vector2(2f, 2f);
      rect.offsetMax = new Vector2(-2f, -2f);

      var bg = root.AddComponent<Image>();
      bg.raycastTarget = false;

      var iconGo = new GameObject("Icon", typeof(RectTransform));
      iconGo.transform.SetParent(root.transform, false);
      var iconRect = iconGo.GetComponent<RectTransform>();
      iconRect.anchorMin = new Vector2(0f, 0.5f);
      iconRect.anchorMax = new Vector2(0f, 0.5f);
      iconRect.pivot = new Vector2(0f, 0.5f);
      iconRect.sizeDelta = new Vector2(28f, 28f);
      iconRect.anchoredPosition = new Vector2(4f, 0f);
      var icon = iconGo.AddComponent<Image>();
      icon.raycastTarget = false;
      icon.enabled = false;

      var textGo = new GameObject("Name", typeof(RectTransform));
      textGo.transform.SetParent(root.transform, false);
      var textRect = textGo.GetComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = new Vector2(34f, 0f);
      textRect.offsetMax = new Vector2(-4f, 0f);
      var tmp = textGo.AddComponent<TextMeshProUGUI>();
      tmp.fontSize = 11;
      tmp.alignment = TextAlignmentOptions.MidlineLeft;
      tmp.color = UiThemeProvider.Current.textPrimary;
      tmp.raycastTarget = false;

      var chip = root.AddComponent<PieceChipView>();
      chip.background = bg;
      chip.iconImage = icon;
      chip.nameText = tmp;
      chip.Bind(definition, source);
      return chip;
    }
  }
}
