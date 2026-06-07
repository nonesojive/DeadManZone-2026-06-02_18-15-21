using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Builds the structured run HUD (fight header + four resource columns).
    /// </summary>
    public static class RunHudPanelBuilder
    {
        public const string PanelName = "RunHudPanel";
        public const int PanelVersion = 2;

        public sealed class BuiltPanel
        {
            public RectTransform Root;
            public TMP_Text FightTitle;
            public TMP_Text FightIndex;
            public TMP_Text GateMessage;
            public TMP_Text SuppliesValue;
            public TMP_Text ManpowerValue;
            public TMP_Text AuthorityValue;
            public TMP_Text MoraleValue;
        }

        public static BuiltPanel Create(Transform buildPanelRoot, UiThemeSO theme)
        {
            var existing = buildPanelRoot.Find(PanelName);
            if (existing == null)
                existing = buildPanelRoot.Find("TopBar/" + PanelName);
            if (existing != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(existing.gameObject);
                else
#endif
                    Object.Destroy(existing.gameObject);
            }

            var panelGo = new GameObject(PanelName, typeof(RectTransform));
            panelGo.transform.SetParent(buildPanelRoot, false);
            panelGo.transform.SetAsLastSibling();
            var panelRect = panelGo.GetComponent<RectTransform>();
            Stretch(panelRect);

            var version = panelGo.AddComponent<RunHudPanelVersion>();
            version.SetVersion(PanelVersion);

            ApplyFrameStyle(panelGo.AddComponent<Image>(), theme);

            var header = CreateRegion(panelGo.transform, "HeaderRow", new Vector2(0f, 0.50f), Vector2.one);
            var grid = CreateRegion(panelGo.transform, "ResourceGrid", Vector2.zero, new Vector2(1f, 0.50f));

            AddHorizontalRule(header.transform, 0f, theme);

            var fightTitle = CreateHudLabel(header.transform, "Fight", 20, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(10f, 0f), new Vector2(120f, 32f), TextAlignmentOptions.MidlineLeft, theme);

            var fightIndex = CreateHudLabel(header.transform, "1/10", 20, FontStyles.Bold,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-10f, 0f), new Vector2(72f, 32f), TextAlignmentOptions.MidlineRight, theme);

            var gateMessage = CreateHudLabel(header.transform, "", 13, FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 2f), new Vector2(320f, 18f), TextAlignmentOptions.Center, theme, secondary: true);
            gateMessage.gameObject.SetActive(false);

            AddHorizontalRule(grid.transform, 1f, theme);

            var supplies = CreateResourceColumn(grid.transform, "Supplies", theme, 0);
            var manpower = CreateResourceColumn(grid.transform, "Manpower", theme, 1);
            var authority = CreateResourceColumn(grid.transform, "Authority", theme, 2);
            var morale = CreateResourceColumn(grid.transform, "Morale", theme, 3);

            return new BuiltPanel
            {
                Root = panelRect,
                FightTitle = fightTitle,
                FightIndex = fightIndex,
                GateMessage = gateMessage,
                SuppliesValue = supplies,
                ManpowerValue = manpower,
                AuthorityValue = authority,
                MoraleValue = morale
            };
        }

        public static bool NeedsRebuild(Transform buildPanelRoot)
        {
            if (buildPanelRoot == null)
                return true;

            var panel = buildPanelRoot.Find(PanelName);
            if (panel == null)
                panel = buildPanelRoot.Find("TopBar/" + PanelName);

            if (panel == null)
                return true;

            var version = panel.GetComponent<RunHudPanelVersion>();
            return version == null || version.Version < PanelVersion;
        }

        public static void WireRunHudView(RunHudView hud, BuiltPanel panel)
        {
            if (hud == null || panel == null)
                return;

            hud.Configure(
                panel.FightTitle,
                panel.FightIndex,
                panel.GateMessage,
                panel.SuppliesValue,
                panel.ManpowerValue,
                panel.AuthorityValue,
                panel.MoraleValue);
        }

        public static void ApplyFrameStyle(Image frame, UiThemeSO theme)
        {
            if (frame == null || theme == null)
                return;

            UiThemeApplicator.ApplyStorageSlotEmpty(frame, theme);
            frame.color = theme.GetReserveSlotColor();
            frame.raycastTarget = false;
        }

        private static TMP_Text CreateResourceColumn(
            Transform grid,
            string labelText,
            UiThemeSO theme,
            int columnIndex)
        {
            float width = 1f / 4f;
            float minX = columnIndex * width;
            float maxX = minX + width;

            var column = CreateRegion(grid, labelText + "Column", new Vector2(minX, 0f), new Vector2(maxX, 1f));

            if (columnIndex > 0)
                AddVerticalRule(column.transform, theme);

            CreateHudLabel(column.transform, labelText, 14, FontStyles.Bold,
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(96f, 24f), TextAlignmentOptions.Center, theme, secondary: true);

            return CreateHudLabel(column.transform, "0", 18, FontStyles.Bold,
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(96f, 30f), TextAlignmentOptions.Center, theme);
        }

        private static void AddHorizontalRule(Transform parent, float anchorY, UiThemeSO theme)
        {
            var rule = CreateRegion(parent, "Rule", new Vector2(0.02f, anchorY), new Vector2(0.98f, anchorY));
            var rect = rule.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 1f);
            var image = rule.AddComponent<Image>();
            image.color = new Color(theme.textPrimary.r, theme.textPrimary.g, theme.textPrimary.b, 0.22f);
            image.raycastTarget = false;
        }

        private static void AddVerticalRule(Transform column, UiThemeSO theme)
        {
            var rule = CreateRegion(column, "Divider", new Vector2(0f, 0.08f), new Vector2(0f, 0.92f));
            var rect = rule.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1f, 0f);
            var image = rule.AddComponent<Image>();
            image.color = new Color(theme.textPrimary.r, theme.textPrimary.g, theme.textPrimary.b, 0.22f);
            image.raycastTarget = false;
        }

        private static TMP_Text CreateHudLabel(
            Transform parent,
            string text,
            float fontSize,
            FontStyles style,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAlignmentOptions alignment,
            UiThemeSO theme,
            bool secondary = false)
        {
            var go = new GameObject(text + "Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.raycastTarget = false;
            UiThemeApplicator.ApplyLabel(label, secondary, theme);
            return label;
        }

        private static GameObject CreateRegion(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class RunHudPanelVersion : MonoBehaviour
    {
        [SerializeField] private int version = RunHudPanelBuilder.PanelVersion;

        public int Version => version;

        public void SetVersion(int value) => version = value;
    }
}
