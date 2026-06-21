using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.UI
{
    public sealed partial class PieceCardView
    {
        private void ApplyTheme()
        {
            var activeTheme = ResolveTheme();
            if (background != null)
            {
                // ponytail: keep authored frame sprites untinted; theme color only fills procedural cards.
                if (background.sprite == null)
                    background.color = activeTheme.cardColor;
                else
                    background.color = Color.white;
            }

            Color primary = activeTheme.textPrimary;
            Color secondary = activeTheme.textSecondary;

            SetColor(nameText, primary);
            SetColor(hpText, secondary);
            SetColor(damageText, secondary);
            SetColor(movementSpeedText, secondary);
            SetColor(attackSpeedText, secondary);
            SetColor(attackTypeText, secondary);
            SetColor(armorTypeText, secondary);
            SetColor(primaryTagText, secondary);
            SetColor(synergyLinesText, secondary);
            SetColor(criticalMassText, secondary);
            SetColor(salvageContextText, secondary);
            SetColor(abilityText, secondary);
            SetColor(overflowTooltipText, secondary);
        }

        private void BindMainStats(PieceCardViewModel model)
        {
            SetText(nameText, model.DisplayName);
            SetText(hpText, model.Hp.ToString());
            SetText(damageText, model.BaseDamage.ToString());
            SetText(movementSpeedText, model.MovementSpeedValue.ToString());
            SetText(attackSpeedText, model.AttackSpeedValue.ToString());
        }

        private void BindDedicatedSlots(PieceCardViewModel model)
        {
            BindPrimaryTag(model);
            BindArmorIcon(model);
            BindAttackTypeIcon(model);
            BindCombatRoleIcon(model);
            BindUnitImage();
        }

        private void BindPrimaryTag(PieceCardViewModel model)
        {
            if (primaryTagText == null)
                return;

            bool hasPrimary = model.PrimaryTag != null
                && !string.IsNullOrWhiteSpace(model.PrimaryTag.DisplayName);
            primaryTagText.gameObject.SetActive(hasPrimary);
            primaryTagText.text = hasPrimary ? model.PrimaryTag.DisplayName : string.Empty;
        }

        private void BindArmorIcon(PieceCardViewModel model)
        {
            if (armorIcon == null)
                return;

            bool hasArmor = model.ArmorType != ArmorType.None;
            armorIcon.gameObject.SetActive(hasArmor);
            if (!hasArmor)
                return;

            Sprite sprite = icons != null ? icons.GetArmorIcon(model.ArmorType) : null;
            if (sprite != null)
                armorIcon.sprite = sprite;
        }

        private void BindAttackTypeIcon(PieceCardViewModel model)
        {
            if (attackTypeIcon == null)
                return;

            bool hasAttackType = model.AttackType != AttackType.None;
            attackTypeIcon.gameObject.SetActive(hasAttackType);
            if (!hasAttackType)
                return;

            // ponytail: icon catalog fills in when attack-type sprites are imported.
            Sprite sprite = icons != null ? icons.GetAttackTypeIcon(model.AttackType) : null;
            if (sprite != null)
                attackTypeIcon.sprite = sprite;
        }

        private void BindCombatRoleIcon(PieceCardViewModel model)
        {
            if (combatRoleIcon == null)
                return;

            string roleId = model.CombatRoleTag?.Id;
            bool hasRole = !string.IsNullOrWhiteSpace(roleId);
            combatRoleIcon.gameObject.SetActive(hasRole);
            if (!hasRole)
                return;

            // ponytail: icon catalog fills in when combat-role sprites are imported.
            Sprite sprite = icons != null ? icons.GetCombatRoleIcon(roleId) : null;
            if (sprite != null)
                combatRoleIcon.sprite = sprite;
        }

        private void BindUnitImage()
        {
            if (unitImage == null)
                return;

            // ponytail: piece art hook lands when unit portrait pipeline exists.
            unitImage.gameObject.SetActive(unitImage.sprite != null);
        }

        private void BindOptionalSections(PieceCardViewModel model)
        {
            BindAttackType(model);
            BindArmorType(model);
            BindSynergySummary(model);
            BindSynergyLines(model);
            BindCriticalMass(model);
            BindSalvageContext(model);
            BindAbility(model);
        }

        private void BindTagChips(PieceCardViewModel model)
        {
            int visibleTagCount = model.ChipTags.Count + (model.OverflowCount > 0 ? 1 : 0);
            EnsureChipCount(visibleTagCount);

            int index = 0;
            for (int i = 0; i < model.ChipTags.Count; i++)
                SetChip(index++, model.ChipTags[i]?.DisplayName);

            if (model.OverflowCount > 0)
                SetChip(index++, $"+{model.OverflowCount}");

            for (int i = index; i < _chips.Count; i++)
            {
                if (_chips[i] != null)
                    _chips[i].gameObject.SetActive(false);
            }
        }

        private void BindOverflowTooltip(string overflowTooltip)
        {
            if (overflowTooltipText == null)
                return;

            bool hasOverflowTooltip = !string.IsNullOrWhiteSpace(overflowTooltip);
            overflowTooltipText.gameObject.SetActive(hasOverflowTooltip);
            overflowTooltipText.text = hasOverflowTooltip ? $"Hidden tags: {overflowTooltip}" : string.Empty;
        }

        private void BindAttackType(PieceCardViewModel model)
        {
            if (attackTypeText == null)
                return;

            // Authored cards use AttackTypeIcon_UnitCard; keep legacy text for procedural fallback only.
            if (!UsesProceduralFallback)
            {
                attackTypeText.gameObject.SetActive(false);
                return;
            }

            bool hasAttackType = model.AttackType != AttackType.None;
            attackTypeText.gameObject.SetActive(hasAttackType);
            if (!hasAttackType)
                return;

            string tooltip = string.IsNullOrWhiteSpace(model.AttackTypeTooltip)
                ? string.Empty
                : $"\n{model.AttackTypeTooltip}";
            attackTypeText.text = $"Attack Type: {model.AttackType}{tooltip}";
        }

        private void BindArmorType(PieceCardViewModel model)
        {
            if (armorTypeText == null)
                return;

            if (!UsesProceduralFallback)
            {
                armorTypeText.gameObject.SetActive(false);
                return;
            }

            bool hasArmorType = model.ArmorType != ArmorType.None;
            armorTypeText.gameObject.SetActive(hasArmorType);
            if (!hasArmorType)
                return;

            string tooltip = string.IsNullOrWhiteSpace(model.ArmorTypeTooltip)
                ? string.Empty
                : $"\n{model.ArmorTypeTooltip}";
            armorTypeText.text = $"Armor Type: {model.ArmorType}{tooltip}";
        }

        private void BindSynergySummary(PieceCardViewModel model)
        {
            if (synergyText == null)
                return;

            string bonus = string.Empty;
            if (model.SynergyDamageBonus > 0)
                bonus += $"+{model.SynergyDamageBonus} Damage ";
            if (model.SynergyArmorBuffSteps > 0)
                bonus += $"+{model.SynergyArmorBuffSteps} Armor ";
            if (model.SynergyMoveChargeBonus > 0)
                bonus += $"+{model.SynergyMoveChargeBonus}% Move charge ";

            synergyText.gameObject.SetActive(!string.IsNullOrEmpty(bonus));
            synergyText.text = "Synergy: " + bonus.Trim();
            synergyText.color = ResolveTheme().accentColor;
        }

        private void BindSynergyLines(PieceCardViewModel model)
        {
            if (synergyLinesText == null)
                return;

            bool hasLines = model.SynergyLines != null && model.SynergyLines.Count > 0;
            synergyLinesText.gameObject.SetActive(hasLines);
            synergyLinesText.text = hasLines ? string.Join("\n", model.SynergyLines) : string.Empty;
        }

        private void BindCriticalMass(PieceCardViewModel model)
        {
            if (criticalMassText == null)
                return;

            criticalMassText.gameObject.SetActive(false);
            criticalMassText.text = string.Empty;
        }

        private void BindSalvageContext(PieceCardViewModel model)
        {
            if (salvageContextText == null)
                return;

            bool hasContext = !string.IsNullOrWhiteSpace(model.SalvageContext);
            salvageContextText.gameObject.SetActive(hasContext);
            salvageContextText.text = hasContext ? model.SalvageContext : string.Empty;
            if (hasContext)
                salvageContextText.color = ResolveTheme().accentColor;
        }

        private void BindAbility(PieceCardViewModel model)
        {
            if (abilityText == null)
                return;

            bool hasAbility = !string.IsNullOrWhiteSpace(model.AbilityText);
            abilityText.gameObject.SetActive(hasAbility);
            abilityText.text = hasAbility ? model.AbilityText : string.Empty;
        }

        private void EnsureChipCount(int count)
        {
            while (_chips.Count < count)
                _chips.Add(CreateTagChipLabel());
        }

        private TMP_Text CreateTagChipLabel()
        {
            if (tagChipPrefab != null)
            {
                var chipGo = Instantiate(tagChipPrefab, tagChipContainer);
                chipGo.name = tagChipPrefab.name;
                chipGo.SetActive(true);
                NormalizeTagChipInstance(chipGo);
                var label = chipGo.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    return label;
            }

            if (tagChipTemplate != null)
            {
                var chip = Instantiate(tagChipTemplate, tagChipContainer);
                chip.gameObject.SetActive(true);
                return chip;
            }

            var fallbackGo = new GameObject("TagChip", typeof(RectTransform), typeof(TextMeshProUGUI));
            fallbackGo.transform.SetParent(tagChipContainer, false);
            var fallback = fallbackGo.GetComponent<TextMeshProUGUI>();
            fallback.fontSize = 11f;
            fallback.alignment = TextAlignmentOptions.Center;
            fallback.raycastTarget = false;
            return fallback;
        }

        private void SetChip(int index, string value)
        {
            if (index < 0 || index >= _chips.Count || _chips[index] == null)
                return;

            var chip = _chips[index];
            chip.gameObject.SetActive(true);
            chip.text = string.IsNullOrWhiteSpace(value) ? "?" : value.Trim();
            chip.color = ResolveTheme().textPrimary;
        }

        private static void NormalizeTagChipInstance(GameObject chipGo)
        {
            if (chipGo == null)
                return;

            const float chipHeight = 22f;
            const float maxWidth = 132f;

            var rect = chipGo.GetComponent<RectTransform>();
            if (rect != null && (rect.sizeDelta.y > chipHeight + 4f || rect.sizeDelta.x > maxWidth))
                rect.sizeDelta = new Vector2(Mathf.Min(rect.sizeDelta.x, maxWidth), chipHeight);

            var layout = chipGo.GetComponent<LayoutElement>() ?? chipGo.AddComponent<LayoutElement>();
            layout.minHeight = chipHeight;
            layout.preferredHeight = chipHeight;
            layout.flexibleHeight = 0f;
            layout.flexibleWidth = 0f;

            foreach (var image in chipGo.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
                var imageRect = image.rectTransform;
                if (imageRect == rect)
                    continue;

                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
                imageRect.sizeDelta = Vector2.zero;
            }

            foreach (var label in chipGo.GetComponentsInChildren<TMP_Text>(true))
            {
                label.enableAutoSizing = false;
                label.fontSize = 11f;
                label.margin = Vector4.zero;
            }
        }
    }
}
