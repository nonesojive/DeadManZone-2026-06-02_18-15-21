using DeadManZone.Presentation.UI;
using DeadManZone.Presentation.Visual;
using DeadManZone.Data.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    public static class TagChipPrefabAuthoring
    {
        private const string PrefabsFolder = "Assets/_Project/Presentation/UI/Prefabs";

        [MenuItem(DeadManZoneEditorMenus.Ui + "Create Tag Chip Prefab")]
        public static void CreateTagChipPrefab() => SaveTagChipPrefab(confirmOverwrite: false);

        [MenuItem(DeadManZoneEditorMenus.Ui + "Reset Tag Chip Prefab To Default")]
        public static void ResetTagChipPrefabToDefault()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.TagChip) != null
                && !EditorUtility.DisplayDialog(
                    "Reset Tag Chip Prefab?",
                    "This replaces TagChip.prefab with the compact default (Image + Label, ~22px tall). Custom edits will be lost.",
                    "Reset",
                    "Cancel"))
                return;

            SaveTagChipPrefab(confirmOverwrite: true);
        }

        private static void SaveTagChipPrefab(bool confirmOverwrite)
        {
            EnsureFolderExists(PrefabsFolder);
            var theme = UiThemeProvider.Current;
            var chip = BuildTagChip(theme);

            try
            {
                if (!AuthoredCardPrefabGuard.TrySavePrefab(chip, CardPrefabPaths.TagChip, out bool success) || !success)
                    throw new System.InvalidOperationException($"Failed to save prefab at {CardPrefabPaths.TagChip}.");

                Debug.Log(confirmOverwrite
                    ? $"Reset {CardPrefabPaths.TagChip} to compact default."
                    : $"Created {CardPrefabPaths.TagChip}. Assign it on PieceCardView.tagChipPrefab.");
            }
            finally
            {
                Object.DestroyImmediate(chip);
            }
        }

        [MenuItem(DeadManZoneEditorMenus.Ui + "Fix Tag Chip Layout On Unit Detail Card")]
        public static void FixTagChipLayoutOnUnitDetailCard()
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.UnitDetailCard);
            if (cardPrefab == null)
            {
                Debug.LogError($"Missing {CardPrefabPaths.UnitDetailCard}.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(cardPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                FixTagChipContainerLayout(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Updated TagChips layout on UnitDetailCard.prefab (chips no longer forced to stretch).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem(DeadManZoneEditorMenus.Ui + "Wire Tag Chip Prefab On Unit Detail Card")]
        public static void WireTagChipOnUnitDetailCard()
        {
            var chipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.TagChip);
            if (chipPrefab == null)
            {
                Debug.LogError($"Missing {CardPrefabPaths.TagChip}. Run DeadManZone/UI/Create Tag Chip Prefab first.");
                return;
            }

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.UnitDetailCard);
            if (cardPrefab == null)
            {
                Debug.LogError($"Missing {CardPrefabPaths.UnitDetailCard}.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(cardPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cardView = root.GetComponent<PieceCardView>();
                if (cardView == null)
                {
                    Debug.LogError("UnitDetailCard.prefab has no PieceCardView.");
                    return;
                }

                var serialized = new SerializedObject(cardView);
                serialized.FindProperty("tagChipPrefab").objectReferenceValue = chipPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                HideLegacyInlineTemplate(root.transform);
                FixTagChipContainerLayout(root.transform);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Wired {CardPrefabPaths.TagChip} on UnitDetailCard.prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject BuildTagChip(UiThemeSO theme)
        {
            var chip = new GameObject("TagChip", typeof(RectTransform));
            var rect = chip.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72f, 24f);

            var background = chip.AddComponent<Image>();
            UiThemeApplicator.ApplyCard(background, theme);
            background.raycastTarget = false;

            var layout = chip.AddComponent<LayoutElement>();
            layout.minHeight = 24f;
            layout.preferredHeight = 24f;
            layout.minWidth = 40f;

            var fitter = chip.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(chip.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "Tag";
            label.fontSize = 11f;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 11f;
            label.enableAutoSizing = false;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.margin = Vector4.zero;
            UiThemeApplicator.ApplyLabel(label, secondary: false, theme);
            label.enableAutoSizing = false;
            label.fontSize = 11f;

            return chip;
        }

        private static void FixTagChipContainerLayout(Transform cardRoot)
        {
            var container = cardRoot.Find("TagChips");
            if (container == null)
                return;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                return;

            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
        }

        private static void HideLegacyInlineTemplate(Transform cardRoot)
        {
            var container = cardRoot.Find("TagChips");
            if (container == null)
                return;

            var template = container.Find("TagChipTemplate");
            if (template != null)
                template.gameObject.SetActive(false);
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
    }
}
