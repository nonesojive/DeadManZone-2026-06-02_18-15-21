using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text currenciesText;

        public void Refresh(RunState state, string battleGateMessage = null)
        {
            if (state == null)
                return;

            if (statusText != null)
            {
                string gateLine = string.IsNullOrEmpty(battleGateMessage) ? "" : $"\n{battleGateMessage}";
                statusText.text =
                    $"Fight {state.FightIndex} / {RunOrchestrator.MaxFights}\n" +
                    $"Phase: {state.Phase}{gateLine}";
            }

            if (currenciesText != null)
            {
                int rerollCost = RunOrchestrator.BaseRerollCost + state.RerollCountThisRound;
                currenciesText.text =
                    $"Supplies: {state.Supplies}   Manpower: {state.Manpower}   " +
                    $"Authority: {state.Authority}   Morale: {state.Morale}   Reroll: {rerollCost}S";
            }
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            UiThemeApplicator.ApplyLabel(statusText, secondary: false, theme);
            UiThemeApplicator.ApplyLabel(currenciesText, secondary: true, theme);
        }
    }
}
