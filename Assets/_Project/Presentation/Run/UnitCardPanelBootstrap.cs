using DeadManZone.Presentation.UI;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Bootstraps unit card panels and clears obsolete hover-card leftovers.</summary>
    public static class UnitCardPanelBootstrap
    {
        public static void EnsureOnBuildPanel(Transform buildPanel)
        {
            if (buildPanel == null)
                return;

            LegacyUnitCardCleanup.RemoveFloatingHoverLayers();

            foreach (var panel in buildPanel.GetComponentsInChildren<UnitCardPanelView>(true))
                panel.EnsureCardView();
        }
    }
}
