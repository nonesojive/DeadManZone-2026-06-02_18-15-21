using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Wires RunManager combat state to CombatDirector and PhaseCommandPanel.</summary>
    public sealed class CombatFlowPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private PhaseCommandPanel phaseCommandPanel;

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands += OnPausedForCommands;

            if (RunManager.Instance?.State?.Phase == Core.Run.RunPhase.Combat)
                combatDirector?.ReplayFromSaveAndPauseAtCheckpoint();
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands -= OnPausedForCommands;
        }

        private void OnPausedForCommands(Core.Combat.CombatPhase completedPhase)
        {
            if (RunManager.Instance == null || phaseCommandPanel == null)
                return;

            var available = RunManager.Instance.Orchestrator.GetAvailableCommands();
            int budget = RunManager.Instance.Orchestrator.GetPrimaryActionBudget();
            phaseCommandPanel.ShowCommands(available, completedPhase, budget, budget);
        }
    }
}
