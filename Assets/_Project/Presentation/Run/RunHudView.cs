using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text fightTitleText;
        [SerializeField] private TMP_Text fightIndexText;
        [SerializeField] private TMP_Text gateMessageText;
        [SerializeField] private TMP_Text salvageIndicatorText;
        [SerializeField] private TMP_Text suppliesValueText;
        [SerializeField] private TMP_Text manpowerValueText;
        [SerializeField] private TMP_Text authorityValueText;
        [SerializeField] private TMP_Text moraleValueText;

        private ContentDatabase _database;

        public void Configure(
            TMP_Text fightTitle,
            TMP_Text fightIndex,
            TMP_Text gateMessage,
            TMP_Text suppliesValue,
            TMP_Text manpowerValue,
            TMP_Text authorityValue,
            TMP_Text moraleValue,
            TMP_Text salvageIndicator = null)
        {
            fightTitleText = fightTitle;
            fightIndexText = fightIndex;
            gateMessageText = gateMessage;
            suppliesValueText = suppliesValue;
            manpowerValueText = manpowerValue;
            authorityValueText = authorityValue;
            moraleValueText = moraleValue;
            salvageIndicatorText = salvageIndicator;
        }

        public void Refresh(RunState state, string battleGateMessage = null)
        {
            if (state == null)
                return;

            if (fightTitleText != null)
                fightTitleText.text = "Fight";

            if (fightIndexText != null)
                fightIndexText.text = $"{state.FightIndex}/{RunOrchestrator.MaxFights}";

            if (suppliesValueText != null)
                suppliesValueText.text = state.Supplies.ToString();

            if (manpowerValueText != null)
                manpowerValueText.text = state.Manpower.ToString();

            if (authorityValueText != null)
                authorityValueText.text = state.Authority.ToString();

            if (moraleValueText != null)
                moraleValueText.text = state.Morale.ToString();

            if (gateMessageText != null)
            {
                bool hasGate = !string.IsNullOrEmpty(battleGateMessage);
                gateMessageText.gameObject.SetActive(hasGate);
                if (hasGate)
                    gateMessageText.text = battleGateMessage;
            }

            RefreshSalvageIndicator(state);
        }

        private void RefreshSalvageIndicator(RunState state)
        {
            if (salvageIndicatorText == null)
                return;

            if (state == null || string.IsNullOrEmpty(state.LastEnemyFactionId))
            {
                salvageIndicatorText.gameObject.SetActive(false);
                return;
            }

            _database ??= ContentDatabase.Load();
            var faction = _database?.GetFaction(state.LastEnemyFactionId);
            string displayName = faction != null && !string.IsNullOrEmpty(faction.displayName)
                ? faction.displayName
                : state.LastEnemyFactionId;

            salvageIndicatorText.gameObject.SetActive(true);
            salvageIndicatorText.text = $"Salvage: {displayName} — {state.SalvageChancePercent}%";
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            ApplyLabel(fightTitleText, false, theme);
            ApplyLabel(fightIndexText, false, theme);
            ApplyLabel(gateMessageText, true, theme);
            ApplyLabel(salvageIndicatorText, true, theme);
            ApplyLabel(suppliesValueText, false, theme);
            ApplyLabel(manpowerValueText, false, theme);
            ApplyLabel(authorityValueText, false, theme);
            ApplyLabel(moraleValueText, false, theme);
        }

        private static void ApplyLabel(TMP_Text label, bool secondary, UiThemeSO theme)
        {
            if (label != null)
                UiThemeApplicator.ApplyLabel(label, secondary, theme);
        }
    }
}
