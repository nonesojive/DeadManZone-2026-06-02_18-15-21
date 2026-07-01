using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.UI
{
    public sealed partial class PieceCardView : MonoBehaviour
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
        [SerializeField] private TMP_Text attackRangeText;
        [SerializeField] private Image armorIcon;
        [SerializeField] private Image attackTypeIcon;
        [SerializeField] private Image combatRoleIcon;
        [SerializeField] private Image unitImage;
        [SerializeField] private TMP_Text primaryTagText;
        [SerializeField] private UnitCardIconsSO icons;
        [SerializeField] private TMP_Text attackTypeText;
        [SerializeField] private TMP_Text armorTypeText;
        [SerializeField] private TMP_Text synergyText;
        [SerializeField] private TMP_Text synergyLinesText;
        [SerializeField] private TMP_Text criticalMassText;
        [SerializeField] private TMP_Text salvageContextText;
        [SerializeField] private TMP_Text abilityText;

        [Header("Tags")]
        [SerializeField] private RectTransform tagChipContainer;
        [SerializeField] private GameObject tagChipPrefab;
        [SerializeField] private TMP_Text tagChipTemplate;
        [SerializeField] private TMP_Text overflowTooltipText;

        private readonly List<TMP_Text> _chips = new();
        private RectTransform _contentRoot;

        // ponytail: authored prefabs skip procedural layout; upgrade path is wire nameText in the inspector.
        private bool UsesProceduralFallback => nameText == null;

        public void Bind(PieceCardViewModel model, string overflowTooltip)
        {
            if (model == null)
                return;

            if (UsesProceduralFallback)
                EnsureRuntimeUi();
            else
                ResolveAuthoredRefsIfNeeded();

            ApplyTheme();

            BindMainStats(model);
            BindDedicatedSlots(model);
            BindOptionalSections(model);
            BindTagChips(model);
            BindOverflowTooltip(overflowTooltip);
        }

        public void Show()
        {
            if (UsesProceduralFallback)
                EnsureRuntimeUi();

            cardRoot ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (cardRoot == null || canvasGroup == null)
                return;

            gameObject.SetActive(true);
            cardRoot.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Hide()
        {
            if (cardRoot != null && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                cardRoot.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

#if UNITY_INCLUDE_TESTS
        public void InitializeForTests(
            TMP_Text name,
            TMP_Text hp,
            Image authoredBackground = null,
            TMP_Text damage = null,
            TMP_Text movementSpeed = null,
            TMP_Text attackSpeed = null,
            TMP_Text attackRange = null,
            Image armor = null,
            Image attackType = null,
            Image combatRole = null,
            Image unitArt = null,
            TMP_Text primaryTag = null,
            UnitCardIconsSO cardIcons = null,
            TMP_Text attackTypeLabel = null,
            TMP_Text armorType = null,
            TMP_Text synergy = null,
            TMP_Text synergyLines = null,
            TMP_Text criticalMass = null,
            TMP_Text salvageContext = null,
            TMP_Text ability = null,
            RectTransform chipContainer = null,
            GameObject chipPrefab = null,
            TMP_Text chipTemplate = null,
            TMP_Text overflowTooltip = null)
        {
            nameText = name;
            hpText = hp;
            background = authoredBackground;
            damageText = damage;
            movementSpeedText = movementSpeed;
            attackSpeedText = attackSpeed;
            attackRangeText = attackRange;
            armorIcon = armor;
            attackTypeIcon = attackType;
            combatRoleIcon = combatRole;
            unitImage = unitArt;
            primaryTagText = primaryTag;
            icons = cardIcons;
            attackTypeText = attackTypeLabel;
            armorTypeText = armorType;
            synergyText = synergy;
            synergyLinesText = synergyLines;
            criticalMassText = criticalMass;
            salvageContextText = salvageContext;
            abilityText = ability;
            tagChipContainer = chipContainer;
            tagChipPrefab = chipPrefab;
            tagChipTemplate = chipTemplate;
            overflowTooltipText = overflowTooltip;
        }

        public string NameTextForTests => nameText?.text;
        public string HpTextForTests => hpText?.text;
        public string DamageTextForTests => damageText?.text;
        public string MovementSpeedTextForTests => movementSpeedText?.text;
        public string AttackSpeedTextForTests => attackSpeedText?.text;
        public string AttackRangeTextForTests => attackRangeText?.text;
        public string PrimaryTagTextForTests => primaryTagText?.text;
        public Sprite ArmorIconSpriteForTests => armorIcon?.sprite;
        public Sprite AttackTypeIconSpriteForTests => attackTypeIcon?.sprite;
        public Sprite CombatRoleIconSpriteForTests => combatRoleIcon?.sprite;
        public int TagChipCountForTests => _chips.Count(c => c != null && c.gameObject.activeSelf);
        public string AbilityTextForTests => abilityText?.text;
#endif

        private void ResolveAuthoredRefsIfNeeded()
        {
            abilityText ??= FindAuthoredText("AbilityText_UnitCard");
            attackRangeText ??= FindAuthoredText("AttackRange_UnitCard");
        }

        private TMP_Text FindAuthoredText(string objectName)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == objectName)
                    return texts[i];
            }

            return null;
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

            if (background != null)
                background.color = ResolveTheme().cardColor;

            if (_contentRoot == null)
                _contentRoot = CreateContentRoot();

            nameText ??= CreateLabel("Name", 15f, FontStyles.Bold, false);
            hpText ??= CreateLabel("Hp", 13f, FontStyles.Normal, true);
            damageText ??= CreateLabel("Damage", 13f, FontStyles.Normal, true);
            movementSpeedText ??= CreateLabel("MovementSpeed", 12f, FontStyles.Normal, true);
            attackSpeedText ??= CreateLabel("AttackSpeed", 12f, FontStyles.Normal, true);
            attackRangeText ??= CreateLabel("AttackRange", 12f, FontStyles.Normal, true);
            attackTypeText ??= CreateLabel("AttackType", 12f, FontStyles.Normal, true);
            armorTypeText ??= CreateLabel("ArmorType", 12f, FontStyles.Normal, true);
            synergyText ??= CreateLabel("SynergyBonus", 12f, FontStyles.Bold, false);
            synergyLinesText ??= CreateLabel("SynergyLines", 11f, FontStyles.Normal, true);
            criticalMassText ??= CreateLabel("CriticalMass", 11f, FontStyles.Normal, true);
            salvageContextText ??= CreateLabel("SalvageContext", 11f, FontStyles.Italic, false);
            abilityText ??= CreateLabel("Ability", 11f, FontStyles.Normal, true);

            if (tagChipContainer == null)
                tagChipContainer = CreateChipContainer();

            overflowTooltipText ??= CreateLabel("OverflowTooltip", 11f, FontStyles.Italic, true);
        }

        private RectTransform CreateContentRoot()
        {
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(cardRoot, false);
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

        private RectTransform CreateChipContainer()
        {
            var tagsGo = new GameObject("TagChips", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            tagsGo.transform.SetParent(_contentRoot, false);
            var container = tagsGo.GetComponent<RectTransform>();
            var tagsLayout = tagsGo.GetComponent<HorizontalLayoutGroup>();
            tagsLayout.childControlHeight = false;
            tagsLayout.childControlWidth = false;
            tagsLayout.childForceExpandHeight = false;
            tagsLayout.childForceExpandWidth = false;
            tagsLayout.spacing = 5f;
            var tagsFitter = tagsGo.GetComponent<ContentSizeFitter>();
            tagsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tagsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return container;
        }

        private TMP_Text CreateLabel(string labelName, float fontSize, FontStyles style, bool secondary)
        {
            var go = new GameObject(labelName, typeof(RectTransform), typeof(TextMeshProUGUI));
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

        private UiThemeSO ResolveTheme() => theme != null ? theme : UiThemeProvider.Current;

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
    }
}
