using DeadManZone.Presentation.UI;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Creates/finds the scene-authored critical mass drawer under ShopScene.</summary>
    public static class CriticalMassDrawerBootstrap
    {
        public const string DrawerName = "CriticalMassDrawer";

        public static CriticalMassDrawerView Ensure(Transform buildPanel)
        {
            if (buildPanel == null)
                return null;

            var existing = buildPanel.Find(DrawerName)?.GetComponent<CriticalMassDrawerView>();
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                return existing;
            }

            return CreateDrawer(buildPanel);
        }

        public static CriticalMassDrawerView CreateDrawer(Transform buildPanel)
        {
            var theme = UiThemeProvider.Current;
            var icons = Resources.Load<CriticalMassIconsSO>("DeadManZone/CriticalMassIcons");

            var drawerGo = new GameObject(DrawerName, typeof(RectTransform));
            drawerGo.transform.SetParent(buildPanel, false);
            drawerGo.transform.SetAsLastSibling();
            var drawerRect = drawerGo.GetComponent<RectTransform>();
            drawerRect.anchorMin = Vector2.zero;
            drawerRect.anchorMax = Vector2.one;
            drawerRect.offsetMin = Vector2.zero;
            drawerRect.offsetMax = Vector2.zero;

            var backdropGo = CreateStretchChild(drawerGo.transform, "Backdrop");
            var backdropImage = backdropGo.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.45f);
            backdropImage.raycastTarget = true;
            var backdropButton = backdropGo.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropGo.SetActive(false);

            var panelGo = CreateStretchChild(drawerGo.transform, "Panel");
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = theme != null ? theme.panelColor : new Color(0.1f, 0.1f, 0.12f, 0.98f);
            panelImage.raycastTarget = true;
            var panelCanvasGroup = panelGo.AddComponent<CanvasGroup>();
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;

            var header = CreateStretchChild(panelGo.transform, "Header");
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 48f);
            headerRect.anchoredPosition = Vector2.zero;
            var headerLabel = header.AddComponent<TextMeshProUGUI>();
            headerLabel.text = "Critical Mass";
            headerLabel.fontSize = 20f;
            headerLabel.fontStyle = FontStyles.Bold;
            headerLabel.alignment = TextAlignmentOptions.MidlineLeft;
            headerLabel.margin = new Vector4(16f, 0f, 16f, 0f);
            headerLabel.raycastTarget = false;
            if (theme != null)
                headerLabel.color = theme.textPrimary;

            var scrollGo = CreateStretchChild(panelGo.transform, "Scroll");
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(12f, 12f);
            scrollRect.offsetMax = new Vector2(-12f, -56f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.content = content;
            scroll.viewport = viewport;

            var rowTemplate = CreateRowTemplate(content);

            var tabGo = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            tabGo.transform.SetParent(drawerGo.transform, false);
            var tabRect = tabGo.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(1f, 0.45f);
            tabRect.anchorMax = new Vector2(1f, 0.55f);
            tabRect.pivot = new Vector2(1f, 0.5f);
            tabRect.sizeDelta = new Vector2(108f, 120f);
            tabRect.anchoredPosition = Vector2.zero;
            var tabImage = tabGo.GetComponent<Image>();
            tabImage.color = theme != null ? theme.cardColor : new Color(0.18f, 0.18f, 0.22f, 0.98f);
            tabImage.raycastTarget = true;
            var tabButton = tabGo.GetComponent<Button>();
            if (theme != null)
            {
                var colors = tabButton.colors;
                colors.normalColor = theme.accentColor;
                colors.highlightedColor = theme.accentColor * 1.1f;
                colors.pressedColor = theme.accentColor * 0.85f;
                tabButton.colors = colors;
            }

            var tabLabelGo = new GameObject("Label", typeof(RectTransform));
            tabLabelGo.transform.SetParent(tabGo.transform, false);
            var tabLabelRect = tabLabelGo.GetComponent<RectTransform>();
            tabLabelRect.anchorMin = Vector2.zero;
            tabLabelRect.anchorMax = Vector2.one;
            tabLabelRect.offsetMin = new Vector2(8f, 8f);
            tabLabelRect.offsetMax = new Vector2(-8f, -8f);
            var tabLabel = tabLabelGo.AddComponent<TextMeshProUGUI>();
            tabLabel.alignment = TextAlignmentOptions.Center;
            tabLabel.fontSize = 13f;
            tabLabel.text = "0 active buffs";
            tabLabel.enableWordWrapping = true;
            tabLabel.raycastTarget = false;
            if (theme != null)
                tabLabel.color = theme.textPrimary;

            var drawer = drawerGo.AddComponent<CriticalMassDrawerView>();
            drawer.Configure(
                tabRect,
                tabButton,
                tabLabel,
                panelRect,
                panelCanvasGroup,
                backdropGo.GetComponent<RectTransform>(),
                content,
                rowTemplate,
                theme,
                icons);
            return drawer;
        }

        private static GameObject CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static RectTransform CreateRowTemplate(RectTransform parent)
        {
            var rowGo = new GameObject("RowTemplate", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);
            rowGo.SetActive(false);
            var row = rowGo.GetComponent<RectTransform>();
            row.sizeDelta = new Vector2(0f, 72f);
            var layoutElement = rowGo.GetComponent<LayoutElement>();
            layoutElement.minHeight = 72f;

            var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.padding = new RectOffset(4, 4, 4, 4);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGo.transform.SetParent(row, false);
            iconGo.GetComponent<LayoutElement>().preferredWidth = 40f;
            iconGo.GetComponent<LayoutElement>().preferredHeight = 40f;
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);

            var textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(row, false);
            textColumn.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;

            var textColumnRect = textColumn.GetComponent<RectTransform>();
            AddLabel(textColumnRect, "Title", 16f, FontStyles.Bold);
            AddLabel(textColumnRect, "Progress", 14f, FontStyles.Normal);
            AddLabel(textColumnRect, "Detail", 11f, FontStyles.Italic);
            return row;
        }

        private static void AddLabel(RectTransform parent, string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
        }
    }
}
