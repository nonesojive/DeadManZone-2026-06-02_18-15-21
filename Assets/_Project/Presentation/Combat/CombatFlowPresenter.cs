using System.Collections;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Wires RunManager combat state to CombatDirector and pause UI.</summary>
    public sealed class CombatFlowPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private TacticPausePanel tacticPausePanel;
        [SerializeField] private PhaseCommandPanel phaseCommandPanel;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private float loadingDurationSeconds = 1f;

        private Coroutine _loadingRoutine;

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands += OnPausedForCommands;

            if (RunManager.Instance?.State?.Phase == RunPhase.Combat)
                BeginCombatPresentation();
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands -= OnPausedForCommands;

            if (_loadingRoutine != null)
            {
                StopCoroutine(_loadingRoutine);
                _loadingRoutine = null;
            }
        }

        public void BeginCombatPresentation()
        {
            ShowLoadingOverlay();
            _loadingRoutine = StartCoroutine(LoadingThenReplay());
        }

        private IEnumerator LoadingThenReplay()
        {
            if (loadingDurationSeconds > 0f)
                yield return new WaitForSeconds(loadingDurationSeconds);
            else
                yield return null;

            HideLoadingOverlay();
            combatDirector?.ReplayFromSaveAndPauseAtCheckpoint();
            _loadingRoutine = null;
        }

        private void OnPausedForCommands(DeadManZone.Core.Combat.CombatPhase completedPhase)
        {
            if (RunManager.Instance == null)
                return;

            var context = RunManager.Instance.Orchestrator.GetCombatPauseContext();
            if (tacticPausePanel != null)
            {
                tacticPausePanel.ShowPause(context);
                return;
            }

            if (phaseCommandPanel == null)
                return;

            var available = RunManager.Instance.Orchestrator.GetAvailableCommands();
            int budget = RunManager.Instance.Orchestrator.GetPrimaryActionBudget();
            phaseCommandPanel.ShowCommands(available, completedPhase, budget, budget);
        }

        private void ShowLoadingOverlay()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(true);

            if (loadingText != null)
                loadingText.text = "Entering combat…";
        }

        private void HideLoadingOverlay()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        }
    }
}
