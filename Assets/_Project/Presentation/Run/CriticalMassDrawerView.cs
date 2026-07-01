using System.Collections;
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
    /// <summary>Right-edge tab + slide-over panel for critical mass progress.</summary>
    public sealed class CriticalMassDrawerView : MonoBehaviour
    {
        private const float PanelOpenMinX = 0.5f;
        private const float PanelClosedMinX = 1f;
        private const float SlideSeconds = 0.2f;

        [SerializeField] private RectTransform tabRoot;
        [SerializeField] private Button tabButton;
        [SerializeField] private TMP_Text tabLabel;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform backdrop;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private RectTransform rowTemplate;
        [SerializeField] private UiThemeSO theme;
        [SerializeField] private CriticalMassIconsSO criticalMassIcons;

        private readonly List<RectTransform> _rows = new();
        private readonly List<BuffStripEntry> _entries = new();
        private bool _isOpen;
        private bool _wired;
        private Coroutine _slideRoutine;

        private void Awake() => SetPanelImmediate(false);

        private void OnEnable() => WireInteractions();

        public void Configure(
            RectTransform tab,
            Button button,
            TMP_Text label,
            RectTransform panel,
            CanvasGroup panelGroup,
            RectTransform backdropRect,
            RectTransform rows,
            RectTransform template,
            UiThemeSO uiTheme,
            CriticalMassIconsSO icons)
        {
            tabRoot = tab;
            tabButton = button;
            tabLabel = label;
            panelRoot = panel;
            panelCanvasGroup = panelGroup;
            backdrop = backdropRect;
            rowContainer = rows;
            rowTemplate = template;
            theme = uiTheme;
            criticalMassIcons = icons;
            _wired = false;
            WireInteractions();
            SetPanelImmediate(false);
        }

        public void Refresh(BuildBoardSet boards)
        {
            _entries.Clear();
            _entries.AddRange(BuffStripEvaluator.Evaluate(boards));
            RebuildRows();

            int activeCount = BuffStripEvaluator.CountActive(boards);
            if (tabLabel != null)
                tabLabel.text = activeCount == 1 ? "1 active buff" : $"{activeCount} active buffs";
        }

        public void Toggle()
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        public void Open() => AnimatePanel(true);

        public void Close() => AnimatePanel(false);

        private void WireInteractions()
        {
            if (_wired || tabButton == null)
                return;

            tabButton.onClick.AddListener(Toggle);
            if (tabLabel != null)
                tabLabel.raycastTarget = false;

            if (backdrop != null)
            {
                var backdropButton = backdrop.GetComponent<Button>();
                if (backdropButton != null)
                    backdropButton.onClick.AddListener(Close);
            }

            _wired = true;
        }

        private void AnimatePanel(bool open)
        {
            _isOpen = open;
            if (backdrop != null)
                backdrop.gameObject.SetActive(open);
            SetPanelRaycasts(open);
            if (_slideRoutine != null)
                StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(SlidePanel(open));
        }

        private void SetPanelImmediate(bool open)
        {
            _isOpen = open;
            if (panelRoot != null)
                panelRoot.anchorMin = new Vector2(open ? PanelOpenMinX : PanelClosedMinX, 0f);
            if (backdrop != null)
                backdrop.gameObject.SetActive(open);
            SetPanelRaycasts(open);
        }

        private void SetPanelRaycasts(bool open)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.blocksRaycasts = open;
                panelCanvasGroup.interactable = open;
            }
        }

        private IEnumerator SlidePanel(bool open)
        {
            if (panelRoot == null)
                yield break;

            float start = panelRoot.anchorMin.x;
            float target = open ? PanelOpenMinX : PanelClosedMinX;
            float elapsed = 0f;
            while (elapsed < SlideSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SlideSeconds);
                float x = Mathf.Lerp(start, target, t);
                panelRoot.anchorMin = new Vector2(x, 0f);
                yield return null;
            }

            panelRoot.anchorMin = new Vector2(target, 0f);
            _slideRoutine = null;
        }

        private void RebuildRows()
        {
            if (rowContainer == null)
                return;

            EnsureRowCount(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
                BindRow(_rows[i], _entries[i]);

            for (int i = _entries.Count; i < _rows.Count; i++)
                _rows[i].gameObject.SetActive(false);
        }

        private void BindRow(RectTransform row, BuffStripEntry entry)
        {
            row.gameObject.SetActive(true);

            var icon = row.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                var sprite = criticalMassIcons != null ? criticalMassIcons.GetIconForEntry(entry) : null;
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.color = entry.IsActive
                    ? theme != null ? theme.accentColor : Color.white
                    : new Color(0.65f, 0.65f, 0.65f, 0.85f);
            }

            SetText(row, "Title", entry.DisplayName);
            SetText(row, "Progress", BuffStripEvaluator.FormatProgressLabel(entry));
            SetText(row, "Detail", entry.DetailText);
        }

        private void EnsureRowCount(int count)
        {
            while (_rows.Count < count)
            {
                RectTransform row;
                if (rowTemplate != null)
                {
                    row = Instantiate(rowTemplate, rowContainer);
                    row.gameObject.SetActive(true);
                }
                else
                {
                    row = CreateRuntimeRow(rowContainer);
                }

                _rows.Add(row);
            }
        }

        private RectTransform CreateRuntimeRow(RectTransform parent)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            var row = rowGo.GetComponent<RectTransform>();
            row.sizeDelta = new Vector2(0f, 72f);

            CreateRowChild(row, "Icon", typeof(Image), 36f);
            CreateRowChild(row, "Title", typeof(TextMeshProUGUI), 18f);
            CreateRowChild(row, "Progress", typeof(TextMeshProUGUI), 14f);
            CreateRowChild(row, "Detail", typeof(TextMeshProUGUI), 12f);
            return row;
        }

        private static void CreateRowChild(RectTransform parent, string name, System.Type component, float fontOrSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (component == typeof(Image))
            {
                var image = go.AddComponent<Image>();
                image.rectTransform.sizeDelta = new Vector2(fontOrSize, fontOrSize);
                return;
            }

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontOrSize;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
        }

        private static void SetText(RectTransform row, string childName, string value)
        {
            var label = FindText(row, childName);
            if (label != null)
                label.text = value ?? string.Empty;
        }

        private static TMP_Text FindText(RectTransform row, string childName) =>
            row.Find(childName)?.GetComponent<TMP_Text>()
            ?? row.Find($"TextColumn/{childName}")?.GetComponent<TMP_Text>();
    }
}
