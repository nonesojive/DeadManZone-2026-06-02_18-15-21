using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    public sealed class PieceHoverCard : MonoBehaviour
    {
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image background;
        [SerializeField] private UiThemeSO theme;

        [Header("Stats")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text movementSpeedText;
        [SerializeField] private TMP_Text attackSpeedText;
        [SerializeField] private TMP_Text attackTypeText;
        [SerializeField] private TMP_Text armorTypeText;

        [Header("Tags")]
        [SerializeField] private RectTransform tagChipContainer;
        [SerializeField] private TMP_Text tagChipTemplate;
        [SerializeField] private TMP_Text overflowTooltipText;

        private readonly List<TMP_Text> _chips = new();
        private RectTransform _contentRoot;

        private void Awake()
        {
            EnsureRuntimeUi();
            Hide();
        }

        public void Bind(PieceCardViewModel model, string overflowTooltip)
        {
            if (model == null)
                return;

            EnsureRuntimeUi();
            ApplyTheme();

            SetText(nameText, model.DisplayName);
            SetText(hpText, $"HP: {model.Hp}");
            SetText(damageText, $"DMG: {model.BaseDamage}");
            SetText(movementSpeedText, $"Move: {FormatMovement(model.MovementSpeed)}");
            SetText(attackSpeedText, $"Atk Speed: {FormatAttackSpeed(model.AttackSpeed)}");
            SetText(attackTypeText, $"Attack Type: {FormatAttackType(model.AttackType)}");
            SetText(armorTypeText, $"Armor Type: {FormatArmor(model.ArmorType)}");

            int visibleTagCount = model.IdentityTags.Count + model.OptionalTags.Count;
            int totalChipCount = visibleTagCount + (model.OverflowCount > 0 ? 1 : 0);
            EnsureChipCount(totalChipCount);

            int index = 0;
            for (int i = 0; i < model.IdentityTags.Count; i++)
            {
                SetChip(index++, model.IdentityTags[i]?.DisplayName);
            }

            for (int i = 0; i < model.OptionalTags.Count; i++)
            {
                SetChip(index++, model.OptionalTags[i]?.DisplayName);
            }

            if (model.OverflowCount > 0)
            {
                SetChip(index++, $"+{model.OverflowCount}");
            }

            for (int i = index; i < _chips.Count; i++)
            {
                if (_chips[i] != null)
                    _chips[i].gameObject.SetActive(false);
            }

            if (overflowTooltipText != null)
            {
                bool hasOverflowTooltip = !string.IsNullOrWhiteSpace(overflowTooltip);
                overflowTooltipText.gameObject.SetActive(hasOverflowTooltip);
                overflowTooltipText.text = hasOverflowTooltip ? $"Hidden tags: {overflowTooltip}" : string.Empty;
            }
        }

        public void SetScreenPosition(Canvas canvas, Vector2 screenPosition, Vector2 offset)
        {
            if (cardRoot == null)
                return;

            if (canvas == null || canvas.transform is not RectTransform canvasRect)
            {
                cardRoot.position = screenPosition + offset;
                return;
            }

            Vector2 target = screenPosition + offset;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, target, camera, out var localPoint))
                return;

            Vector2 halfSize = cardRoot.rect.size * 0.5f;
            const float padding = 12f;
            localPoint.x = Mathf.Clamp(localPoint.x, canvasRect.rect.xMin + halfSize.x + padding, canvasRect.rect.xMax - halfSize.x - padding);
            localPoint.y = Mathf.Clamp(localPoint.y, canvasRect.rect.yMin + halfSize.y + padding, canvasRect.rect.yMax - halfSize.y - padding);
            cardRoot.anchoredPosition = localPoint;
        }

        public void Show()
        {
            if (cardRoot == null || canvasGroup == null)
                return;

            cardRoot.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Hide()
        {
            if (cardRoot == null || canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            cardRoot.gameObject.SetActive(false);
        }

        private void EnsureRuntimeUi()
        {
            if (cardRoot == null)
                cardRoot = transform as RectTransform;
            if (cardRoot == null)
                cardRoot = gameObject.AddComponent<RectTransform>();

            cardRoot.anchorMin = cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cardRoot.pivot = new Vector2(0.5f, 0.5f);
            if (cardRoot.sizeDelta.x < 1f || cardRoot.sizeDelta.y < 1f)
                cardRoot.sizeDelta = new Vector2(280f, 172f);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (background == null)
                background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();

            if (_contentRoot == null)
            {
                var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentGo.transform.SetParent(cardRoot, false);
                _contentRoot = contentGo.GetComponent<RectTransform>();
                _contentRoot.anchorMin = Vector2.zero;
                _contentRoot.anchorMax = Vector2.one;
                _contentRoot.offsetMin = new Vector2(10f, 10f);
                _contentRoot.offsetMax = new Vector2(-10f, -10f);

                var layout = contentGo.GetComponent<VerticalLayoutGroup>();
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.spacing = 3f;

                var fitter = contentGo.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            nameText ??= CreateLabel("Name", 15f, FontStyles.Bold, false);
            hpText ??= CreateLabel("Hp", 13f, FontStyles.Normal, true);
            damageText ??= CreateLabel("Damage", 13f, FontStyles.Normal, true);
            movementSpeedText ??= CreateLabel("MovementSpeed", 12f, FontStyles.Normal, true);
            attackSpeedText ??= CreateLabel("AttackSpeed", 12f, FontStyles.Normal, true);
            attackTypeText ??= CreateLabel("AttackType", 12f, FontStyles.Normal, true);
            armorTypeText ??= CreateLabel("ArmorType", 12f, FontStyles.Normal, true);

            if (tagChipContainer == null)
            {
                var tagsGo = new GameObject("TagChips", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                tagsGo.transform.SetParent(_contentRoot, false);
                tagChipContainer = tagsGo.GetComponent<RectTransform>();
                var tagsLayout = tagsGo.GetComponent<HorizontalLayoutGroup>();
                tagsLayout.childControlHeight = false;
                tagsLayout.childControlWidth = false;
                tagsLayout.childForceExpandHeight = false;
                tagsLayout.childForceExpandWidth = false;
                tagsLayout.spacing = 5f;
                var tagsFitter = tagsGo.GetComponent<ContentSizeFitter>();
                tagsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                tagsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            overflowTooltipText ??= CreateLabel("OverflowTooltip", 11f, FontStyles.Italic, true);
        }

        private TMP_Text CreateLabel(string name, float fontSize, FontStyles style, bool secondary)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_contentRoot, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            if (secondary)
                text.enableAutoSizing = false;
            return text;
        }

        private void ApplyTheme()
        {
            var activeTheme = theme != null ? theme : UiThemeProvider.Current;
            if (background != null)
                background.color = activeTheme.cardColor;

            Color primary = activeTheme.textPrimary;
            Color secondary = activeTheme.textSecondary;

            SetColor(nameText, primary);
            SetColor(hpText, secondary);
            SetColor(damageText, secondary);
            SetColor(movementSpeedText, secondary);
            SetColor(attackSpeedText, secondary);
            SetColor(attackTypeText, secondary);
            SetColor(armorTypeText, secondary);
            SetColor(overflowTooltipText, secondary);
        }

        private void EnsureChipCount(int count)
        {
            while (_chips.Count < count)
            {
                TMP_Text chip;
                if (tagChipTemplate != null)
                {
                    chip = Instantiate(tagChipTemplate, tagChipContainer);
                    chip.gameObject.SetActive(true);
                }
                else
                {
                    var chipGo = new GameObject("TagChip", typeof(RectTransform), typeof(TextMeshProUGUI));
                    chipGo.transform.SetParent(tagChipContainer, false);
                    chip = chipGo.GetComponent<TextMeshProUGUI>();
                    chip.fontSize = 11f;
                    chip.alignment = TextAlignmentOptions.Center;
                    chip.raycastTarget = false;
                }

                _chips.Add(chip);
            }
        }

        private void SetChip(int index, string value)
        {
            if (index < 0 || index >= _chips.Count || _chips[index] == null)
                return;

            var chip = _chips[index];
            chip.gameObject.SetActive(true);
            chip.text = string.IsNullOrWhiteSpace(value) ? "?" : value.Trim();
            chip.color = (theme != null ? theme : UiThemeProvider.Current).textPrimary;
        }

        private static void SetColor(TMP_Text text, Color color)
        {
            if (text != null)
                text.color = color;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static string FormatMovement(MovementSpeedTier tier) => tier.ToString();
        private static string FormatAttackSpeed(AttackSpeedTier tier) => tier.ToString();
        private static string FormatAttackType(AttackType type) => type.ToString();
        private static string FormatArmor(ArmorType type) => type.ToString();
    }
}
