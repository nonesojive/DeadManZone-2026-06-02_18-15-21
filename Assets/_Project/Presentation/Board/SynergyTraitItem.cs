using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeadManZone.Presentation.Visual;
using DeadManZone.Core.Tags;

namespace DeadManZone.Presentation.Board
{
    public sealed class SynergyTraitItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;

        public void Bind(TagDefinition tag, int count, SynergyTraitThresholds thresholds, UiThemeSO theme)
        {
            nameText.text = tag.DisplayName;
            
            int activeLevel = 0;
            int nextThreshold = -1;
            
            for (int i = 0; i < thresholds.Thresholds.Length; i++)
            {
                if (count >= thresholds.Thresholds[i])
                {
                    activeLevel = i + 1;
                }
                else
                {
                    nextThreshold = thresholds.Thresholds[i];
                    break;
                }
            }

            countText.text = nextThreshold > 0 ? $"{count}/{nextThreshold}" : $"{count}";
            
            // Color based on active level
            Color bgColor = theme.cardColor;
            if (activeLevel > 0)
            {
                bgColor = activeLevel switch
                {
                    1 => new Color(0.8f, 0.5f, 0.2f, 0.8f), // Bronze
                    2 => new Color(0.75f, 0.75f, 0.75f, 0.8f), // Silver
                    3 => new Color(1f, 0.84f, 0f, 0.8f), // Gold
                    _ => theme.accentColor
                };
            }
            else
            {
                bgColor.a = 0.5f; // Muted if not active
            }

            backgroundImage.color = bgColor;
            
            if (iconImage != null)
            {
                // In a real implementation, we'd resolve an icon for the tag.
                // For now, we'll just use a placeholder or hide it.
                iconImage.gameObject.SetActive(false);
            }
        }

        public static SynergyTraitItem Create(Transform parent, UiThemeSO theme)
        {
            var go = new GameObject("SynergyTraitItem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 32f);

            var bg = go.GetComponent<Image>();
            bg.color = theme.cardColor;

            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(go.transform, false);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(0.7f, 1f);
            nameRect.offsetMin = new Vector2(8f, 0f);
            nameRect.offsetMax = Vector2.zero;
            var nameT = nameGo.AddComponent<TextMeshProUGUI>();
            nameT.fontSize = 12;
            nameT.alignment = TextAlignmentOptions.MidlineLeft;
            nameT.color = theme.textPrimary;

            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(go.transform, false);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.7f, 0f);
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = new Vector2(-8f, 0f);
            var countT = countGo.AddComponent<TextMeshProUGUI>();
            countT.fontSize = 12;
            countT.alignment = TextAlignmentOptions.MidlineRight;
            countT.color = theme.textSecondary;

            var item = go.AddComponent<SynergyTraitItem>();
            item.nameText = nameT;
            item.countText = countT;
            item.backgroundImage = bg;

            return item;
        }
    }
}
