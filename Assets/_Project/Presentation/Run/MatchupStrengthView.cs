using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    public sealed class MatchupStrengthView : MonoBehaviour
    {
        [SerializeField] private TMP_Text matchupText;

        public void Configure(TMP_Text text) => matchupText = text;

        public void Refresh(MatchupAssessment? assessment)
        {
            if (matchupText == null)
                return;

            if (!assessment.HasValue)
            {
                matchupText.gameObject.SetActive(false);
                return;
            }

            matchupText.gameObject.SetActive(true);
            matchupText.text = Format(assessment.Value);
        }

        public static string Format(MatchupAssessment assessment)
        {
            string player = FormatSide(assessment.Player.EffectiveTotal, assessment.Player.SynergyBonus);
            string enemy = FormatSide(assessment.Enemy.EffectiveTotal, assessment.Enemy.SynergyBonus);
            string label = MatchupAssessment.FormatLabel(assessment.Label);
            return $"{player}  vs  {enemy}  ·  {label}";
        }

        private static string FormatSide(int total, int synergyBonus) =>
            synergyBonus > 0 ? $"{total:N0} (+{synergyBonus:N0})" : $"{total:N0}";

        /// <summary>Grimdark kit body text (M6); theme param kept for caller compatibility.</summary>
        public void ApplyTheme(UiThemeSO theme)
        {
            if (matchupText != null)
                matchupText.color = CombatGrimdarkSkin.BodyText;
        }
    }
}
