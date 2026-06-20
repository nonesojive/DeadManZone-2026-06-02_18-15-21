using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using TMPro;
using UnityEngine;

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
            SetColor(synergyLinesText, secondary);
            SetColor(criticalMassText, secondary);
            SetColor(salvageContextText, secondary);
            SetColor(abilityText, secondary);
            SetColor(overflowTooltipText, secondary);
        }

        private void BindMainStats(PieceCardViewModel model)
        {
            SetText(nameText, model.DisplayName);
            SetText(hpText, $"HP: {model.Hp}");
            SetText(damageText, $"DMG: {model.BaseDamage}");
            SetText(movementSpeedText, $"Move: {model.MovementSpeed}");
            SetText(attackSpeedText, $"Atk Speed: {model.AttackSpeed}");
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
            int visibleTagCount = model.IdentityTags.Count + model.OptionalTags.Count;
            int totalChipCount = visibleTagCount + (model.OverflowCount > 0 ? 1 : 0);
            EnsureChipCount(totalChipCount);

            int index = 0;
            for (int i = 0; i < model.IdentityTags.Count; i++)
                SetChip(index++, model.IdentityTags[i]?.DisplayName);

            for (int i = 0; i < model.OptionalTags.Count; i++)
                SetChip(index++, model.OptionalTags[i]?.DisplayName);

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

            bool hasHint = !string.IsNullOrWhiteSpace(model.CriticalMassHint);
            criticalMassText.gameObject.SetActive(hasHint);
            criticalMassText.text = hasHint ? model.CriticalMassHint : string.Empty;
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
            chip.color = ResolveTheme().textPrimary;
        }
    }
}
