using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Visual
{
    public static class UiThemeApplicator
    {
        public static void ApplyPanel(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            image.color = theme.panelColor;
        }

        public static void ApplyCard(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            image.color = theme.cardColor;
        }

        public static void ApplyButton(Button button, UiThemeSO theme = null)
        {
            if (button == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (button.targetGraphic is Image image)
                image.color = theme.buttonNormal;

            var colors = button.colors;
            colors.normalColor = theme.buttonNormal;
            colors.highlightedColor = theme.buttonHighlighted;
            colors.pressedColor = theme.buttonPressed;
            colors.selectedColor = theme.buttonHighlighted;
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = theme.textPrimary;
        }

        public static void ApplyLabel(TMP_Text label, bool secondary = false, UiThemeSO theme = null)
        {
            if (label == null)
                return;

            theme ??= UiThemeProvider.Current;
            label.color = secondary ? theme.textSecondary : theme.textPrimary;
        }

        public static void ApplyAccentButton(Button button, UiThemeSO theme = null)
        {
            if (button == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (button.targetGraphic is Image image)
                image.color = theme.accentColor;

            var colors = button.colors;
            colors.normalColor = theme.accentColor;
            colors.highlightedColor = Color.Lerp(theme.accentColor, Color.white, 0.15f);
            colors.pressedColor = theme.accentMutedColor;
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = theme.textOnAccent;
        }
    }
}
