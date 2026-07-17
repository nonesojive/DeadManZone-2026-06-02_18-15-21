using DeadManZone.Presentation.MainMenu;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Wave 4 faction-select overhaul: builds the 4x2 crest-grid + detail-pane + MARCH panel
    /// and wires it to a FactionSelectView. Invoked from CinematicMenuUiBuilder in place of the
    /// old three-hardcoded-buttons BuildFactionPanel. Layout only — all behavior/data lives in
    /// FactionSelectView (Presentation, not Editor).
    /// </summary>
    internal static class FactionSelectPanelBuilder
    {
        private const int CardCount = 8;
        private const int GridCols = 4;
        private const int GridRows = 2;
        private static readonly Vector2 CardSize = new(210, 270);

        internal static (GameObject panel, FactionSelectView view) Build(
            Transform parent,
            UiThemeSO theme,
            ScriptableObject menuTheme)
        {
            var panel = MenuSceneSetup.CreateStretchChild(parent, "FactionPanel");
            panel.SetActive(false);

            MenuSceneSetup.CreateLabelPublic(panel.transform, "Choose Your Faction", 40, FontStyles.Bold,
                new Vector2(0.5f, 0.93f), new Vector2(800, 60)).color = theme.textPrimary;

            var backButton = CreateMenuButton(panel.transform, "FactionBackButton", "Back",
                new Vector2(0.08f, 0.93f), menuTheme, theme, new Vector2(160, 50));

            var cardButtons = new Button[CardCount];
            var cardCrestImages = new Image[CardCount];
            var cardNameTexts = new TMP_Text[CardCount];
            var cardTaglineTexts = new TMP_Text[CardCount];
            var cardLockOverlays = new GameObject[CardCount];

            BuildGrid(panel.transform, theme, cardButtons, cardCrestImages, cardNameTexts, cardTaglineTexts, cardLockOverlays);

            var detailFrame = BuildDetailFrame(panel.transform, theme);
            var detailTexts = BuildDetailTexts(panel.transform, theme);
            var rosterStrip = BuildRosterStripContainer(panel.transform);

            var marchButton = CreateMenuButton(panel.transform, "MarchButton", "MARCH",
                new Vector2(0.765f, 0.10f), menuTheme, theme, new Vector2(320, 70), accent: true);

            var view = panel.AddComponent<FactionSelectView>();
            Wire(view, cardButtons, cardCrestImages, cardNameTexts, cardTaglineTexts, cardLockOverlays,
                detailFrame, detailTexts, rosterStrip, marchButton, backButton);

            return (panel, view);
        }

        private static void BuildGrid(
            Transform parent,
            UiThemeSO theme,
            Button[] cardButtons,
            Image[] cardCrestImages,
            TMP_Text[] cardNameTexts,
            TMP_Text[] cardTaglineTexts,
            GameObject[] cardLockOverlays)
        {
            var gridRoot = MenuSceneSetup.CreateStretchChild(parent, "FactionGrid");

            const float gridLeft = 0.05f, gridRight = 0.52f;
            const float gridTop = 0.86f, gridBottom = 0.20f;
            float colPitch = (gridRight - gridLeft) / GridCols;
            float rowPitch = (gridTop - gridBottom) / GridRows;

            for (int i = 0; i < CardCount; i++)
            {
                int col = i % GridCols;
                int row = i / GridCols;
                var anchor = new Vector2(
                    gridLeft + colPitch * (col + 0.5f),
                    gridTop - rowPitch * (row + 0.5f));

                BuildCard(gridRoot.transform, theme, i, anchor,
                    out cardButtons[i], out cardCrestImages[i], out cardNameTexts[i],
                    out cardTaglineTexts[i], out cardLockOverlays[i]);
            }
        }

        private static void BuildCard(
            Transform parent,
            UiThemeSO theme,
            int index,
            Vector2 anchor,
            out Button button,
            out Image crestImage,
            out TMP_Text nameText,
            out TMP_Text taglineText,
            out GameObject lockOverlay)
        {
            var cardGo = new GameObject($"FactionCard_{index}", typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);
            SetAnchoredPoint(cardGo.GetComponent<RectTransform>(), anchor, CardSize);

            var cardImage = cardGo.AddComponent<Image>();
            cardImage.color = theme.buttonNormal;
            button = cardGo.AddComponent<Button>();
            UiThemeApplicator.ApplyButton(button, theme);

            var crestGo = CreateStretchChild(cardGo.transform, "Crest",
                new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.94f));
            crestImage = crestGo.AddComponent<Image>();
            crestImage.preserveAspect = true;

            nameText = CreateCardLabel(cardGo.transform, "NameText", string.Empty, 20, FontStyles.Bold,
                new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.42f));
            taglineText = CreateCardLabel(cardGo.transform, "TaglineText", string.Empty, 14, FontStyles.Normal,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.24f));

            lockOverlay = CreateStretchChild(cardGo.transform, "LockOverlay", Vector2.zero, Vector2.one);
            var lockBg = lockOverlay.AddComponent<Image>();
            lockBg.color = new Color(0.02f, 0.02f, 0.02f, 0.72f);
            lockBg.raycastTarget = false;
            var lockLabel = CreateCardLabel(lockOverlay.transform, "LockLabel", "LOCKED", 18, FontStyles.Bold,
                Vector2.zero, Vector2.one);
            lockLabel.raycastTarget = false;
            lockOverlay.SetActive(false);
        }

        private static Image BuildDetailFrame(Transform parent, UiThemeSO theme)
        {
            var frameGo = CreateStretchChild(parent, "DetailFrame",
                new Vector2(0.55f, 0.06f), new Vector2(0.97f, 0.88f));
            var frameImage = frameGo.AddComponent<Image>();
            frameImage.raycastTarget = false;
            UiThemeApplicator.ApplySecurityTerminalFrame(frameImage, theme);
            frameGo.transform.SetAsFirstSibling();
            return frameImage;
        }

        private readonly struct DetailTexts
        {
            public readonly TMP_Text Name, Tagline, CmRule, Economy, Tentpole, Playstyle, Lock;

            public DetailTexts(TMP_Text name, TMP_Text tagline, TMP_Text cmRule, TMP_Text economy,
                TMP_Text tentpole, TMP_Text playstyle, TMP_Text @lock)
            {
                Name = name;
                Tagline = tagline;
                CmRule = cmRule;
                Economy = economy;
                Tentpole = tentpole;
                Playstyle = playstyle;
                Lock = @lock;
            }
        }

        private static DetailTexts BuildDetailTexts(Transform parent, UiThemeSO theme)
        {
            const float cx = 0.765f;
            var name = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 32, FontStyles.Bold,
                new Vector2(cx, 0.82f), new Vector2(760, 60));
            var tagline = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 20, FontStyles.Italic,
                new Vector2(cx, 0.755f), new Vector2(760, 50));
            var cmRule = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 19, FontStyles.Normal,
                new Vector2(cx, 0.68f), new Vector2(760, 45));
            var economy = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 19, FontStyles.Normal,
                new Vector2(cx, 0.615f), new Vector2(760, 45));
            var tentpole = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 19, FontStyles.Normal,
                new Vector2(cx, 0.55f), new Vector2(760, 45));
            var playstyle = MenuSceneSetup.CreateLabelPublic(parent, string.Empty, 19, FontStyles.Normal,
                new Vector2(cx, 0.485f), new Vector2(760, 45));
            var lockText = MenuSceneSetup.CreateLabelPublic(parent, "Locked", 20, FontStyles.Bold,
                new Vector2(cx, 0.42f), new Vector2(760, 40));

            foreach (var text in new[] { name, tagline, cmRule, economy, tentpole, playstyle, lockText })
                text.alignment = TextAlignmentOptions.TopLeft;

            lockText.gameObject.SetActive(false);
            return new DetailTexts(name, tagline, cmRule, economy, tentpole, playstyle, lockText);
        }

        private static Transform BuildRosterStripContainer(Transform parent)
        {
            var go = new GameObject("RosterStrip", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchoredPoint(go.GetComponent<RectTransform>(), new Vector2(0.765f, 0.28f), new Vector2(760, 56));

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return go.transform;
        }

        private static void Wire(
            FactionSelectView view,
            Button[] cardButtons,
            Image[] cardCrestImages,
            TMP_Text[] cardNameTexts,
            TMP_Text[] cardTaglineTexts,
            GameObject[] cardLockOverlays,
            Image detailFrame,
            DetailTexts detailTexts,
            Transform rosterStrip,
            Button marchButton,
            Button backButton)
        {
            var serialized = new SerializedObject(view);
            SetObjectArray(serialized, "cardButtons", cardButtons);
            SetObjectArray(serialized, "cardCrestImages", cardCrestImages);
            SetObjectArray(serialized, "cardNameTexts", cardNameTexts);
            SetObjectArray(serialized, "cardTaglineTexts", cardTaglineTexts);
            SetObjectArray(serialized, "cardLockOverlays", cardLockOverlays);

            serialized.FindProperty("detailFrameImage").objectReferenceValue = detailFrame;
            serialized.FindProperty("detailNameText").objectReferenceValue = detailTexts.Name;
            serialized.FindProperty("detailTaglineText").objectReferenceValue = detailTexts.Tagline;
            serialized.FindProperty("detailCmRuleText").objectReferenceValue = detailTexts.CmRule;
            serialized.FindProperty("detailEconomyText").objectReferenceValue = detailTexts.Economy;
            serialized.FindProperty("detailTentpoleText").objectReferenceValue = detailTexts.Tentpole;
            serialized.FindProperty("detailPlaystyleText").objectReferenceValue = detailTexts.Playstyle;
            serialized.FindProperty("detailLockText").objectReferenceValue = detailTexts.Lock;
            serialized.FindProperty("rosterStripContainer").objectReferenceValue = rosterStrip;
            serialized.FindProperty("marchButton").objectReferenceValue = marchButton;
            serialized.FindProperty("backButton").objectReferenceValue = backButton;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // ---------------------------------------------------------------- small local helpers

        private static Button CreateMenuButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchor,
            ScriptableObject menuTheme,
            UiThemeSO theme,
            Vector2 size,
            bool accent = false)
        {
            var button = MenuSceneSetup.CreateSmallButtonPublic(parent, label, anchor, size);
            button.gameObject.name = objectName;
            button.onClick.RemoveAllListeners();
            UiThemeSceneStyling.StyleButton(button, theme, accent);
            return button;
        }

        private static TMP_Text CreateCardLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = CreateStretchChild(parent, name, anchorMin, anchorMax);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            return label;
        }

        /// <summary>Generic anchor-stretch child: works both for screen-fraction panel regions
        /// (detail frame) and for card-relative sub-regions (crest/name/tagline/lock), since
        /// RectTransform anchors are always relative to the immediate parent's rect.</summary>
        private static GameObject CreateStretchChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
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

        private static void SetAnchoredPoint(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
