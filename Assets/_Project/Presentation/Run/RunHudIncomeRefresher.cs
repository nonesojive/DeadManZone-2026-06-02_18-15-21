using DeadManZone.Core.Run;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    public static class RunHudIncomeRefresher
    {
        public static void Refresh()
        {
            var orchestrator = RunManager.Instance?.Orchestrator;
            var state = RunManager.Instance?.State;
            orchestrator?.SyncSalvageChancePercent();
            RoundIncomePreview? preview = orchestrator != null && state?.Phase == RunPhase.Build
                ? orchestrator.GetNextCombatIncomePreview()
                : null;

            foreach (var hud in Object.FindObjectsByType<RunHudView>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (preview.HasValue)
                    hud.RefreshIncomePreview(preview.Value);
                else
                    hud.ClearIncomePreview();

                if (state != null)
                    hud.RefreshSalvageIndicator(state);
            }
        }
    }
}
