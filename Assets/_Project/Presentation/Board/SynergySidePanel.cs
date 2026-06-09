using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    public sealed class SynergySidePanel : MonoBehaviour
    {
        [SerializeField] private VerticalLayoutGroup layoutGroup;
        private readonly List<SynergyTraitItem> _items = new();

        public void Refresh(BoardState board, UiThemeSO theme)
        {
            if (board == null) return;

            // Count unique pieces per synergy tag
            var counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            var seenDefinitions = new HashSet<string>();

            foreach (var piece in board.Pieces)
            {
                // Typically autobattlers count unique unit types, but some count every piece.
                // Assuming unique piece instance IDs for now, but usually it's unique definitions on board.
                // If the piece is on board, it counts.
                
                var definition = piece.Definition;
                if (definition.SynergyTags == null) continue;

                foreach (var tag in definition.SynergyTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    // We only count if it's a unique piece (instance)
                    // If we wanted unique definitions: if (!seenDefinitions.Add(definition.Id)) continue;
                    
                    counts[tag] = counts.GetValueOrDefault(tag) + 1;
                }
            }

            // Filter tags that have thresholds and are present
            var activeTraits = counts
                .Where(kvp => SynergyTraitRegistry.TryGet(kvp.Key, out _))
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            // Clear old items or pool them
            foreach (var item in _items) item.gameObject.SetActive(false);
            
            int i = 0;
            foreach (var trait in activeTraits)
            {
                if (i >= _items.Count)
                {
                    _items.Add(SynergyTraitItem.Create(layoutGroup.transform, theme));
                }

                if (TagRegistry.TryGet(trait.Key, out var tagDef) && SynergyTraitRegistry.TryGet(trait.Key, out var thresholds))
                {
                    var item = _items[i];
                    item.gameObject.SetActive(true);
                    item.Bind(tagDef, trait.Value, thresholds, theme);
                    i++;
                }
            }
        }

        public static SynergySidePanel Create(Transform parent, UiThemeSO theme)
        {
            var go = new GameObject("SynergySidePanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.1f);
            rect.anchorMax = new Vector2(0f, 0.9f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 0f);
            rect.anchoredPosition = new Vector2(10f, 0f);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);

            var layoutGo = new GameObject("Layout", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            layoutGo.transform.SetParent(go.transform, false);
            var layoutRect = layoutGo.GetComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.offsetMin = new Vector2(5f, 5f);
            layoutRect.offsetMax = new Vector2(-5f, -5f);

            var group = layoutGo.GetComponent<VerticalLayoutGroup>();
            group.spacing = 4f;
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlHeight = false;
            group.childForceExpandHeight = false;

            var fitter = layoutGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var panel = go.AddComponent<SynergySidePanel>();
            panel.layoutGroup = group;
            return panel;
        }
    }
}
