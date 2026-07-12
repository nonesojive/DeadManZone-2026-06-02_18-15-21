using DeadManZone.Core.Combat;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    public sealed class BattleReportPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text dealtText;
        [SerializeField] private TMP_Text takenText;
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            CombatGrimdarkSkin.StyleCard(panelRoot);
            CombatGrimdarkSkin.StyleTitle(outcomeText, characterSpacing: 12f);
            CombatGrimdarkSkin.StyleBody(summaryText);
            CombatGrimdarkSkin.StyleBody(dealtText);
            CombatGrimdarkSkin.StyleBody(takenText);
            Hide();
        }

        private void OnContinueClicked()
        {
            Hide();
            RunManager.Instance?.DismissAftermath();
        }

        public void Show(BattleReport report)
        {
            if (report == null || panelRoot == null)
                return;

            panelRoot.SetActive(true);
            if (outcomeText != null)
            {
                if (report.IsDraw)
                {
                    outcomeText.text = "DRAW";
                    outcomeText.color = CombatGrimdarkSkin.Bone;
                }
                else if (report.PlayerWon)
                {
                    outcomeText.text = "VICTORY";
                    outcomeText.color = CombatGrimdarkSkin.VictoryGold;
                }
                else
                {
                    outcomeText.text = "DEFEAT";
                    outcomeText.color = CombatGrimdarkSkin.DefeatRed;
                }
            }

            if (summaryText != null)
                summaryText.text = FormatSummary(report);

            if (dealtText != null)
                dealtText.text = FormatTopList("Damage Dealt", report.TopDamageDealt);
            if (takenText != null)
                takenText.text = FormatTopList("Damage Taken", report.TopDamageTaken);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void ShowFromRunState()
        {
            var report = RunManager.Instance != null
                ? RunManager.Instance.Orchestrator.State?.LastBattleReport
                : null;
            Show(report);
        }

        /// <summary>Casualties/supplies plus the rout economy lines (ADR-0005): routed
        /// enemies granted no salvage roll, routed player units come back next round.</summary>
        internal static string FormatSummary(BattleReport report)
        {
            var summary = new System.Text.StringBuilder();
            summary.Append($"Casualties: −{report.ManpowerCasualties}\n");
            summary.Append($"Supplies: {report.SuppliesEarned}");

            if (report.EnemyRouted > 0)
                summary.Append($"\nEnemy broken: {report.EnemyRouted} routed / {report.EnemyKilled} killed");

            if (report.PlayerRouted > 0)
                summary.Append($"\nYour routed units return next round ({report.PlayerRouted})");

            return summary.ToString();
        }

        internal void InitializeForTests(
            GameObject testPanelRoot,
            TMP_Text outcome,
            TMP_Text summary,
            TMP_Text dealt,
            TMP_Text taken)
        {
            panelRoot = testPanelRoot;
            outcomeText = outcome;
            summaryText = summary;
            dealtText = dealt;
            takenText = taken;
        }

        private static string FormatTopList(string title, System.Collections.Generic.IReadOnlyList<BattleReportEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return $"{title}\n—";

            var lines = new System.Text.StringBuilder(title);
            lines.AppendLine();
            foreach (var entry in entries)
                lines.AppendLine($"{entry.DisplayName}: {entry.Damage}");
            return lines.ToString();
        }
    }
}
