using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Shop;
using DeadManZone.Presentation.UI;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    public static class CardPrefabAuthoring
    {
        private const string PrefabsFolder = "Assets/_Project/Presentation/UI/Prefabs";
        private const float DefaultOfferSquare = 150f;

        [MenuItem("DeadManZone/UI/Bake Card Prefabs")]
        public static void BakeCardPrefabs()
        {
            EnsureFolderExists(PrefabsFolder);

            UiThemeSO theme = UiThemeSceneStyling.LoadTheme();
            BakeShopOfferCard(theme);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Baked ShopOfferCard prefab. UnitDetailCard is authored manually — use 'Bake Unit Detail Card Prefab' only if you intend to overwrite it.");
        }

        [MenuItem("DeadManZone/UI/Bake Unit Detail Card Prefab (Overwrites Manual Edits)")]
        public static void BakeUnitDetailCardMenu()
        {
            EnsureFolderExists(PrefabsFolder);
            BakeUnitDetailCard(UiThemeSceneStyling.LoadTheme());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BakeShopOfferCard(UiThemeSO theme)
        {
            GameObject card = BuildShopOfferCard(theme);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(card, CardPrefabPaths.ShopOfferCard, out bool success);
                if (!success)
                    throw new System.InvalidOperationException($"Failed to save prefab at {CardPrefabPaths.ShopOfferCard}.");
            }
            finally
            {
                Object.DestroyImmediate(card);
            }
        }

        private static void BakeUnitDetailCard(UiThemeSO theme)
        {
            if (!EditorUtility.DisplayDialog(
                    "Overwrite Unit Detail Card Prefab?",
                    "This replaces Assets/_Project/Presentation/UI/Prefabs/UnitDetailCard.prefab with a freshly generated layout. Manual prefab edits will be lost.",
                    "Overwrite",
                    "Cancel"))
                return;

            GameObject card = BuildUnitDetailCard(theme);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(card, CardPrefabPaths.UnitDetailCard, out bool success);
                if (!success)
                    throw new System.InvalidOperationException($"Failed to save prefab at {CardPrefabPaths.UnitDetailCard}.");
            }
            finally
            {
                Object.DestroyImmediate(card);
            }
        }

        private static GameObject BuildShopOfferCard(UiThemeSO theme)
        {
            var card = new GameObject("ShopOfferCard", typeof(RectTransform));
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                DefaultOfferSquare + ShopLayoutMetrics.CardPadding,
                DefaultOfferSquare + ShopLayoutMetrics.NameStripHeight + ShopLayoutMetrics.CardPadding);

            var image = card.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(image, theme);
            image.raycastTarget = false;

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 4, 4);

            var lockIndicatorGo = new GameObject("LockedOverlay", typeof(RectTransform));
            lockIndicatorGo.transform.SetParent(card.transform, false);
            var lockIndicatorRect = lockIndicatorGo.GetComponent<RectTransform>();
            Stretch(lockIndicatorRect);
            var lockedIndicator = lockIndicatorGo.AddComponent<Image>();
            lockedIndicator.color = new Color(theme.accentColor.r, theme.accentColor.g, theme.accentColor.b, 0.35f);
            lockedIndicator.enabled = false;
            lockedIndicator.raycastTarget = false;
            var lockOverlayLayout = lockIndicatorGo.AddComponent<LayoutElement>();
            lockOverlayLayout.ignoreLayout = true;
            lockIndicatorGo.transform.SetAsFirstSibling();

            var squareRootGo = new GameObject("SquareRoot", typeof(RectTransform));
            squareRootGo.transform.SetParent(card.transform, false);
            var squareRoot = squareRootGo.GetComponent<RectTransform>();
            squareRoot.sizeDelta = new Vector2(DefaultOfferSquare, DefaultOfferSquare);
            var squareLayout = squareRootGo.AddComponent<LayoutElement>();
            squareLayout.minWidth = DefaultOfferSquare;
            squareLayout.minHeight = DefaultOfferSquare;
            squareLayout.preferredWidth = DefaultOfferSquare;
            squareLayout.preferredHeight = DefaultOfferSquare;

            var squareBackground = squareRootGo.AddComponent<Image>();
            squareBackground.color = new Color(0f, 0f, 0f, 0.12f);
            squareBackground.raycastTarget = true;

            var previewRootGo = new GameObject("PreviewRoot", typeof(RectTransform));
            previewRootGo.transform.SetParent(squareRootGo.transform, false);
            var previewRoot = previewRootGo.GetComponent<RectTransform>();
            previewRoot.anchorMin = new Vector2(0.5f, 0.5f);
            previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            previewRoot.pivot = new Vector2(0.5f, 0.5f);
            previewRoot.anchoredPosition = Vector2.zero;
            previewRoot.sizeDelta = new Vector2(DefaultOfferSquare, DefaultOfferSquare);

            var blockRootGo = new GameObject("Blocks", typeof(RectTransform));
            blockRootGo.transform.SetParent(previewRootGo.transform, false);
            var blockRoot = blockRootGo.GetComponent<RectTransform>();
            blockRoot.anchorMin = new Vector2(0.5f, 0.5f);
            blockRoot.anchorMax = new Vector2(0.5f, 0.5f);
            blockRoot.pivot = new Vector2(0.5f, 0.5f);
            blockRoot.anchoredPosition = Vector2.zero;

            var offerView = card.AddComponent<ShopOfferView>();
            FixOfferPreviewComponent(previewRootGo, blockRoot, out ShopPiecePreview piecePreview);

            var priceBadgeGo = new GameObject("PriceBadge", typeof(RectTransform));
            priceBadgeGo.transform.SetParent(squareRootGo.transform, false);
            var priceBadgeRect = priceBadgeGo.GetComponent<RectTransform>();
            priceBadgeRect.anchorMin = new Vector2(0f, 1f);
            priceBadgeRect.anchorMax = new Vector2(0f, 1f);
            priceBadgeRect.pivot = new Vector2(0f, 1f);
            priceBadgeRect.anchoredPosition = new Vector2(6f, -6f);
            priceBadgeRect.sizeDelta = new Vector2(56f, 20f);
            var priceBadgeBackground = priceBadgeGo.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(priceBadgeBackground, theme);
            priceBadgeBackground.raycastTarget = false;

            var priceText = MenuSceneSetup.CreateLabelPublic(
                priceBadgeGo.transform, "0G", 11f, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(52f, 18f));
            priceText.name = "PriceBadgeText";
            priceText.alignment = TextAlignmentOptions.Center;
            UiThemeSceneStyling.StyleLabel(priceText, theme);

            var lockButton = MenuSceneSetup.CreateSmallButtonPublic(
                squareRootGo.transform, "O", new Vector2(1f, 1f), new Vector2(24f, 24f));
            var lockButtonRect = lockButton.GetComponent<RectTransform>();
            lockButtonRect.anchorMin = new Vector2(1f, 1f);
            lockButtonRect.anchorMax = new Vector2(1f, 1f);
            lockButtonRect.pivot = new Vector2(1f, 1f);
            lockButtonRect.anchoredPosition = new Vector2(-6f, -6f);
            UiThemeSceneStyling.StyleButton(lockButton, theme);
            var lockIconImage = lockButton.GetComponent<Image>();

            var dragSource = squareRootGo.AddComponent<ShopOfferDragSource>();

            var nameStripGo = new GameObject("NameStrip", typeof(RectTransform));
            nameStripGo.transform.SetParent(card.transform, false);
            var nameStrip = nameStripGo.GetComponent<RectTransform>();
            nameStrip.sizeDelta = new Vector2(DefaultOfferSquare, ShopLayoutMetrics.NameStripHeight);
            var nameStripLayout = nameStripGo.AddComponent<LayoutElement>();
            nameStripLayout.minHeight = ShopLayoutMetrics.NameStripHeight;
            nameStripLayout.preferredHeight = ShopLayoutMetrics.NameStripHeight;
            nameStripLayout.minWidth = DefaultOfferSquare;
            nameStripLayout.preferredWidth = DefaultOfferSquare;

            var nameStripBackground = nameStripGo.AddComponent<Image>();
            nameStripBackground.color = new Color(0f, 0f, 0f, 0.35f);
            nameStripBackground.raycastTarget = false;

            var pieceText = MenuSceneSetup.CreateLabelPublic(
                nameStripGo.transform, "piece", 12f, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(DefaultOfferSquare - 8f, ShopLayoutMetrics.NameStripHeight - 4f));
            pieceText.name = "PieceIdText";
            pieceText.alignment = TextAlignmentOptions.Center;
            pieceText.textWrappingMode = TextWrappingModes.NoWrap;
            pieceText.overflowMode = TextOverflowModes.Ellipsis;
            UiThemeSceneStyling.StyleLabel(pieceText, theme);

            var salvagedBadgeText = MenuSceneSetup.CreateLabelPublic(
                nameStripGo.transform, "Salvaged", 11f, FontStyles.Bold,
                new Vector2(1f, 1f), new Vector2(72f, 18f));
            salvagedBadgeText.name = "SalvagedBadgeText";
            salvagedBadgeText.rectTransform.pivot = new Vector2(1f, 1f);
            salvagedBadgeText.rectTransform.anchoredPosition = new Vector2(-4f, -4f);
            salvagedBadgeText.alignment = TextAlignmentOptions.TopRight;
            salvagedBadgeText.gameObject.SetActive(false);
            UiThemeSceneStyling.StyleLabel(salvagedBadgeText, theme);

            var serialized = new SerializedObject(offerView);
            SetObjectRef(serialized, "cardBackground", image);
            SetObjectRef(serialized, "squareRoot", squareRoot);
            SetObjectRef(serialized, "previewRoot", previewRoot);
            SetObjectRef(serialized, "piecePreview", piecePreview);
            SetObjectRef(serialized, "nameStripRoot", nameStrip);
            SetObjectRef(serialized, "pieceIdText", pieceText);
            SetObjectRef(serialized, "priceBadgeBackground", priceBadgeBackground);
            SetObjectRef(serialized, "priceBadgeText", priceText);
            SetObjectRef(serialized, "salvagedBadgeText", salvagedBadgeText);
            SetObjectRef(serialized, "lockIconButton", lockButton);
            SetObjectRef(serialized, "lockIconImage", lockIconImage);
            SetObjectRef(serialized, "lockedIndicator", lockedIndicator);
            SetObjectRef(serialized, "dragSource", dragSource);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return card;
        }

        private static GameObject BuildUnitDetailCard(UiThemeSO theme)
        {
            var card = new GameObject("UnitDetailCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(PieceCardView));
            var cardRoot = card.GetComponent<RectTransform>();
            cardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cardRoot.pivot = new Vector2(0.5f, 0.5f);
            cardRoot.sizeDelta = new Vector2(280f, 172f);

            var background = card.GetComponent<Image>();
            UiThemeApplicator.ApplyCard(background, theme);
            background.raycastTarget = false;

            var content = CreateContentRoot(cardRoot);
            var nameText = CreateLabel(content, "NameText", 15f, FontStyles.Bold, false);
            var hpText = CreateLabel(content, "HpText", 13f, FontStyles.Normal, true);
            var damageText = CreateLabel(content, "DamageText", 13f, FontStyles.Normal, true);
            var movementSpeedText = CreateLabel(content, "MovementSpeedText", 12f, FontStyles.Normal, true);
            var attackSpeedText = CreateLabel(content, "AttackSpeedText", 12f, FontStyles.Normal, true);
            var attackTypeText = CreateLabel(content, "AttackTypeText", 12f, FontStyles.Normal, true);
            var armorTypeText = CreateLabel(content, "ArmorTypeText", 12f, FontStyles.Normal, true);
            var synergyText = CreateLabel(content, "SynergyText", 12f, FontStyles.Bold, false);
            var synergyLinesText = CreateLabel(content, "SynergyLinesText", 11f, FontStyles.Normal, true);
            var criticalMassText = CreateLabel(content, "CriticalMassText", 11f, FontStyles.Normal, true);
            var salvageContextText = CreateLabel(content, "SalvageContextText", 11f, FontStyles.Italic, false);
            var abilityText = CreateLabel(content, "AbilityText", 11f, FontStyles.Normal, true);
            var tagChipContainer = CreateTagChipContainer(content);
            var tagChipTemplate = CreateTagChipTemplate(tagChipContainer, theme);
            var overflowTooltipText = CreateLabel(content, "OverflowTooltipText", 11f, FontStyles.Italic, true);

            var view = card.GetComponent<PieceCardView>();
            var serialized = new SerializedObject(view);
            SetObjectRef(serialized, "cardRoot", cardRoot);
            SetObjectRef(serialized, "canvasGroup", card.GetComponent<CanvasGroup>());
            SetObjectRef(serialized, "background", background);
            SetObjectRef(serialized, "theme", theme);
            SetObjectRef(serialized, "nameText", nameText);
            SetObjectRef(serialized, "hpText", hpText);
            SetObjectRef(serialized, "damageText", damageText);
            SetObjectRef(serialized, "movementSpeedText", movementSpeedText);
            SetObjectRef(serialized, "attackSpeedText", attackSpeedText);
            SetObjectRef(serialized, "attackTypeText", attackTypeText);
            SetObjectRef(serialized, "armorTypeText", armorTypeText);
            SetObjectRef(serialized, "synergyText", synergyText);
            SetObjectRef(serialized, "synergyLinesText", synergyLinesText);
            SetObjectRef(serialized, "criticalMassText", criticalMassText);
            SetObjectRef(serialized, "salvageContextText", salvageContextText);
            SetObjectRef(serialized, "abilityText", abilityText);
            SetObjectRef(serialized, "tagChipContainer", tagChipContainer);
            SetObjectRef(serialized, "tagChipTemplate", tagChipTemplate);
            SetObjectRef(serialized, "overflowTooltipText", overflowTooltipText);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return card;
        }

        private static RectTransform CreateContentRoot(RectTransform parent)
        {
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(parent, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(10f, 10f);
            content.offsetMax = new Vector2(-10f, -10f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 3f;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return content;
        }

        private static RectTransform CreateTagChipContainer(Transform parent)
        {
            var containerGo = new GameObject("TagChips", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            containerGo.transform.SetParent(parent, false);
            var container = containerGo.GetComponent<RectTransform>();

            var layout = containerGo.GetComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 5f;

            var fitter = containerGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return container;
        }

        private static TMP_Text CreateTagChipTemplate(Transform parent, UiThemeSO theme)
        {
            var chipGo = new GameObject("TagChipTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
            chipGo.transform.SetParent(parent, false);
            var chip = chipGo.GetComponent<TextMeshProUGUI>();
            chip.fontSize = 11f;
            chip.alignment = TextAlignmentOptions.Center;
            chip.raycastTarget = false;
            UiThemeSceneStyling.StyleLabel(chip, theme);
            chipGo.SetActive(false);
            return chip;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, float fontSize, FontStyles style, bool secondary)
        {
            var label = MenuSceneSetup.CreateLabelPublic(parent, name, fontSize, style, new Vector2(0.5f, 0.5f), new Vector2(260f, 20f));
            label.name = name;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            UiThemeSceneStyling.StyleLabel(label, UiThemeProvider.Current, secondary);
            if (secondary)
                label.enableAutoSizing = false;
            return label;
        }

        private static void FixOfferPreviewComponent(GameObject previewRootGo, RectTransform blockRoot, out ShopPiecePreview piecePreview)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(previewRootGo);
            piecePreview = previewRootGo.GetComponent<ShopPiecePreview>();
            if (piecePreview == null)
                piecePreview = previewRootGo.AddComponent<ShopPiecePreview>();

            var previewSerialized = new SerializedObject(piecePreview);
            SetObjectRef(previewSerialized, "blockRoot", blockRoot);
            previewSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetObjectRef(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new System.InvalidOperationException($"Missing serialized field '{propertyName}' on {serialized.targetObject.GetType().Name}.");

            property.objectReferenceValue = value;
        }
    }
}
