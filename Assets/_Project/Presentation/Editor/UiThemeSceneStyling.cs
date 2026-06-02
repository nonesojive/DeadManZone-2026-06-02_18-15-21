using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    internal static class UiThemeSceneStyling
    {
        public static UiThemeSO LoadTheme() => UiThemeEditor.EnsureThemeAsset();

        public static Image AddPanelBackground(Transform parent, UiThemeSO theme)
        {
            var bg = parent.GetComponent<Image>();
            if (bg == null)
                bg = parent.gameObject.AddComponent<Image>();

            bg.raycastTarget = false;
            UiThemeApplicator.ApplyPanel(bg, theme);
            return bg;
        }

        public static void StyleButton(Button button, UiThemeSO theme, bool accent = false)
        {
            if (button == null)
                return;

            if (accent)
                UiThemeApplicator.ApplyAccentButton(button, theme);
            else
                UiThemeApplicator.ApplyButton(button, theme);
        }

        public static void StyleLabel(TMP_Text label, UiThemeSO theme, bool secondary = false)
        {
            UiThemeApplicator.ApplyLabel(label, secondary, theme);
        }
    }
}
