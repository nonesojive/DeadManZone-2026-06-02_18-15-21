using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.UI;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Migrates legacy UnitCardPanel scenes that still reference PieceHoverCard.</summary>
    public static class UnitCardPanelBootstrap
    {
        public static void EnsureOnBuildPanel(Transform buildPanel)
        {
            if (buildPanel == null)
                return;

            foreach (var panel in buildPanel.GetComponentsInChildren<UnitCardPanelView>(true))
                panel.EnsureCardView();
        }
    }
}
