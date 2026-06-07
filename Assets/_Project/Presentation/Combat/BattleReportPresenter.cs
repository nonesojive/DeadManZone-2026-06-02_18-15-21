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
                    outcomeText.text = "Draw";
                else
                    outcomeText.text = report.PlayerWon ? "Victory" : "Defeat";
            }

            if (summaryText != null)
            {
                summaryText.text =
                    $"Casualties: −{report.ManpowerCasualties}\n" +
                    $"Supplies: {report.SuppliesEarned}\n" +
                    $"Morale: {report.MoraleDelta:+#;-#;0}";
            }

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
