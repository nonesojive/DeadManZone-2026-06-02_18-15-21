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

        public void Refresh(RunState state, string battleGateMessage = null)
        {
            if (state == null || statusText == null)
                return;

            int rerollCost = RunOrchestrator.BaseRerollCost + state.RerollCountThisRound;
            string gateLine = string.IsNullOrEmpty(battleGateMessage) ? "" : $"\n{battleGateMessage}";
            statusText.text =
                $"Fight {state.FightIndex}/{RunOrchestrator.MaxFights} · {state.Phase}{gateLine}\n" +
                $"Supplies {state.Supplies} · Manpower {state.Manpower} · " +
                $"Authority {state.Authority} · Morale {state.Morale} · Reroll {rerollCost}g";
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            UiThemeApplicator.ApplyLabel(statusText, secondary: false, theme);
        }
    }
}
