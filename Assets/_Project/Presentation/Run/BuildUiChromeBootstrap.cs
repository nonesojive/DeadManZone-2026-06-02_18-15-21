using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Applies panel chrome to major build-phase UI regions and buttons.
    /// </summary>
    public static class BuildUiChromeBootstrap
    {
        public static void Apply(Transform buildPanel)
        {
            if (buildPanel == null)
                return;

            UiPanelChrome.Apply(buildPanel.Find("MainRow/ShopArea"));
            UiPanelChrome.Apply(buildPanel.Find("MainRow/BoardArea"));
            UiPanelChrome.Apply(buildPanel.Find("BottomBar/ReservesRegion"));
            UiPanelChrome.Apply(buildPanel.Find(RunHudPanelBuilder.PanelName));
            UiPanelChrome.Apply(buildPanel.Find("BottomBar/SellZone"));

            var topBar = buildPanel.Find("TopBar");
            var bottomBar = buildPanel.Find("BottomBar");
            UiPanelChrome.Apply(FindButtonByLabel(topBar, "MENU"));
            UiPanelChrome.Apply(FindButtonByLabel(topBar, "Last Log"));
            UiPanelChrome.Apply(FindButtonByLabel(bottomBar, "Begin Fight"));
        }

        private static Button FindButtonByLabel(Transform parent, string labelText)
        {
            if (parent == null)
                return null;

            foreach (var button in parent.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null && label.text.Contains(labelText))
                    return button;
            }

            return null;
        }
    }
}
