using DeadManZone.Presentation.Visual;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Removes legacy glowing panel chrome from build-phase UI regions.
    /// </summary>
    public static class BuildUiChromeBootstrap
    {
        public static void RemoveFromBuildPanel(Transform buildPanel)
        {
            if (buildPanel == null)
                return;

            UiPanelChrome.RemoveFromSubtree(buildPanel);
        }
    }
}
