using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    public sealed class PhaseCommandPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text commandsText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private Image panelBackground;

        private readonly List<AvailableCommand> _available = new();
        private int _pauseIndex;
        private int _budget;

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponentInParent<CombatDirector>();

            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitDefaultCommands);
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipCommands);

            if (panelBackground != null)
                UiThemeApplicator.ApplyCard(panelBackground);
        }

        public void ShowCommands(
            IReadOnlyList<AvailableCommand> availableCommands,
            int pauseIndex,
            int budget,
            int totalSlots)
        {
            _available.Clear();
            if (availableCommands != null)
                _available.AddRange(availableCommands);

            _pauseIndex = pauseIndex;
            _budget = Mathf.Max(0, budget);
            gameObject.SetActive(true);

            if (commandsText == null)
                return;

            var theme = UiThemeProvider.Current;
            UiThemeApplicator.ApplyLabel(commandsText, false, theme);
            commandsText.text = BuildDisplayText(pauseIndex, budget, totalSlots).TrimEnd();
        }

        public void Hide() => gameObject.SetActive(false);

        public void InitializeForTests(TMP_Text testCommandsText) => commandsText = testCommandsText;

        private string BuildDisplayText(int pauseIndex, int budget, int totalSlots)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetPauseTitle(pauseIndex));
            sb.AppendLine(GetPauseFlavor(pauseIndex));
            sb.AppendLine(FormatBudget(budget, totalSlots));

            if (_available.Count == 0)
            {
                sb.AppendLine("No commands available.");
                sb.AppendLine("Skip to continue.");
                return sb.ToString();
            }

            sb.AppendLine("Submit applies (in order):");
            foreach (var cmd in _available)
                sb.AppendLine($"• {FormatCommand(cmd)}");

            return sb.ToString();
        }

        private static string GetPauseTitle(int pauseIndex) =>
            pauseIndex == 0 ? "First Pause" : "Second Pause";

        private static string GetPauseFlavor(int pauseIndex) =>
            pauseIndex == 0
                ? "The lines have buckled — issue orders."
                : "The fight nears its end — commit your reserves.";

        private static string FormatBudget(int budget, int totalSlots)
        {
            var chips = new StringBuilder("Actions ");
            for (int i = 0; i < totalSlots; i++)
                chips.Append(i < budget ? '●' : '○');
            chips.Append($"  ({budget}/{totalSlots})");
            return chips.ToString();
        }

        private static string FormatCommand(AvailableCommand cmd)
        {
            string pieceName = ResolvePieceName(cmd.SourcePieceId);
            return cmd.Type switch
            {
                CommandType.SetTactic => $"All-Out Assault — {pieceName}",
                CommandType.SpendRequisitionBuff => $"Spend Requisition (+buff) — {pieceName} ({cmd.RequisitionCost}R)",
                CommandType.CallStrike => $"Call Strike — {pieceName} ({cmd.RequisitionCost}R)",
                _ => $"{cmd.Type} — {pieceName}"
            };
        }

        private static string ResolvePieceName(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return instanceId;

            var manager = RunManager.Instance;
            if (manager == null || !manager.HasActiveRun)
                return instanceId;

            var piece = manager.Orchestrator.GetPlayerBoard().Pieces
                .FirstOrDefault(p => p.InstanceId == instanceId);
            if (piece?.Definition == null)
                return instanceId;

            return string.IsNullOrEmpty(piece.Definition.DisplayName)
                ? piece.Definition.Id
                : piece.Definition.DisplayName;
        }

        private void SubmitDefaultCommands()
        {
            if (RunManager.Instance == null)
                return;

            var toSubmit = new List<PhaseCommand>();
            int submitted = 0;
            foreach (var cmd in _available)
            {
                if (submitted >= _budget)
                    break;

                if (cmd.Type == CommandType.SpendRequisitionBuff &&
                    RunManager.Instance.State?.Combat?.Requisition < cmd.RequisitionCost)
                {
                    continue;
                }

                toSubmit.Add(new PhaseCommand
                {
                    AfterCheckpoint = _pauseIndex,
                    Type = cmd.Type,
                    SourcePieceId = cmd.SourcePieceId,
                    Cost = cmd.RequisitionCost,
                    Tactic = TacticType.Advance
                });
                submitted++;
            }

            if (toSubmit.Count > 0)
                RunManager.Instance.SubmitCombatCommands(toSubmit);

            Hide();
            combatDirector?.ContinueCombat();
        }

        private void SkipCommands()
        {
            Hide();
            combatDirector?.ContinueCombat();
        }
    }
}
