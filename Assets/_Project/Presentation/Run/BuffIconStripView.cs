using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.UI;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    public sealed class BuffIconStripView : MonoBehaviour
    {
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private Image iconTemplate;
        [SerializeField] private TMP_Text hoverDetailText;
        [SerializeField] private UiThemeSO theme;
        [SerializeField] private CriticalMassIconsSO criticalMassIcons;

        private readonly List<Image> _icons = new();
        private readonly List<BuffStripEntry> _entries = new();

        public void Refresh(BoardState board, BuildMessagesView messages = null)
        {
            _entries.Clear();
            _entries.AddRange(BuffStripEvaluator.Evaluate(board));
            _messagesView = messages;
            RebuildIcons();
            ClearDetail();
        }

        [SerializeField] private BuildMessagesView messagesView;
        private BuildMessagesView _messagesView;

        private void RebuildIcons()
        {
            if (iconContainer == null)
                return;

            EnsureIconCount(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var icon = _icons[i];
                icon.gameObject.SetActive(true);
                icon.color = entry.IsActive
                    ? theme != null ? theme.accentColor : Color.white
                    : new Color(0.65f, 0.65f, 0.65f, 0.85f);

                var sprite = criticalMassIcons != null ? criticalMassIcons.GetIconForEntry(entry) : null;
                icon.sprite = sprite;
                icon.preserveAspect = true;

                var label = icon.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = sprite != null ? string.Empty : Abbreviate(entry.DisplayName);

                EnsureHoverTarget(icon, i);
            }

            for (int i = _entries.Count; i < _icons.Count; i++)
                _icons[i].gameObject.SetActive(false);

            if (hoverDetailText != null && hoverDetailText.gameObject.activeSelf)
                hoverDetailText.text = string.Empty;
        }

        public void ShowDetail(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;

            var detail = _entries[index].DetailText;
            var messages = ResolveMessagesView();
            if (messages != null)
            {
                messages.SetBuffHoverMessage(detail);
                return;
            }

            if (hoverDetailText == null)
                return;

            hoverDetailText.text = detail;
            hoverDetailText.gameObject.SetActive(true);
        }

        public void ClearDetail()
        {
            var messages = ResolveMessagesView();
            if (messages != null)
            {
                messages.ClearBuffHover();
                return;
            }

            if (hoverDetailText != null)
            {
                hoverDetailText.text = string.Empty;
                hoverDetailText.gameObject.SetActive(false);
            }
        }

        private BuildMessagesView ResolveMessagesView()
        {
            if (_messagesView != null)
                return _messagesView;

            if (messagesView != null)
                return messagesView;

            _messagesView = FindFirstObjectByType<BuildMessagesView>();
            return _messagesView;
        }

        private void EnsureIconCount(int count)
        {
            while (_icons.Count < count)
            {
                Image icon;
                if (iconTemplate != null)
                {
                    icon = Instantiate(iconTemplate, iconContainer);
                    var label = icon.GetComponentInChildren<TMP_Text>(true);
                    if (label == null)
                    {
                        var textGo = new GameObject("Label", typeof(RectTransform));
                        textGo.transform.SetParent(icon.transform, false);
                        label = textGo.AddComponent<TextMeshProUGUI>();
                        label.alignment = TextAlignmentOptions.Center;
                        label.fontSize = 10f;
                    }
                }
                else
                {
                    var go = new GameObject("BuffIcon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(iconContainer, false);
                    icon = go.GetComponent<Image>();
                }

                icon.raycastTarget = true;
                _icons.Add(icon);
            }
        }

        private void EnsureHoverTarget(Image icon, int index)
        {
            var hover = icon.GetComponent<BuffIconHoverTarget>();
            if (hover == null)
                hover = icon.gameObject.AddComponent<BuffIconHoverTarget>();
            hover.Configure(this, index);
        }

        private static string Abbreviate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            if (name.Length <= 3)
                return name.ToUpperInvariant();

            return name.Substring(0, 3).ToUpperInvariant();
        }
    }
}
