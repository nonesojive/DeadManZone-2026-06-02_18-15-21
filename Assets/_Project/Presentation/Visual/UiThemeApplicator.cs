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
            ApplyThemedSurface(image, theme.panelSprite, theme.panelColor);
        }

        public static void ApplyCard(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.cardSprite, theme.cardColor);
        }

        public static void ApplyModalFrame(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.modalFrameSprite ?? theme.panelSprite, theme.panelColor);
        }

        public static void ApplySidebarPanel(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.sidebarPanelSprite ?? theme.panelSprite, theme.panelColor);
        }

        public static void ApplyInventoryPanel(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.inventoryPanelSprite ?? theme.panelSprite, theme.panelColor);
        }

        public static void ApplySecurityTerminalFrame(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(
                image,
                theme.securityTerminalFrameSprite ?? theme.modalFrameSprite ?? theme.panelSprite,
                theme.panelColor);
        }

        public static void ApplyBanner(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.bannerSprite ?? theme.cardSprite, theme.combatBannerColor);
        }

        public static void ApplySlotEmpty(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(image, theme.slotEmptySprite ?? theme.cardSprite, theme.cardColor);
        }

        public static void ApplyStorageSlotEmpty(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            ApplyThemedSurface(
                image,
                theme.storageSlotEmptySprite ?? theme.slotEmptySprite ?? theme.cardSprite,
                theme.GetReserveSlotColor());
        }

        public static void ApplySellZone(Image image, UiThemeSO theme = null)
        {
            if (image == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (theme.warningButtonSprite != null)
            {
                ApplyThemedSurface(image, theme.warningButtonSprite, theme.sellZoneColor);
                image.color = new Color(1f, 0.92f, 0.88f, 0.95f);
                return;
            }

            if (theme.dangerButtonSprite != null)
            {
                ApplyThemedSurface(image, theme.dangerButtonSprite, theme.sellZoneColor);
                return;
            }

            image.color = theme.sellZoneColor;
        }

        public static void ApplyBackgroundPlate(Image image, Sprite sprite, float alpha = 0.88f)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
        }

        public static void ApplyButton(Button button, UiThemeSO theme = null)
        {
            if (button == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (button.targetGraphic is Image image && TryApplyButtonSprites(button, image, theme, accent: false))
            {
                ApplyButtonLabel(button, theme.textPrimary);
                return;
            }

            if (button.targetGraphic is Image fallbackImage)
                fallbackImage.color = theme.buttonNormal;

            var colors = button.colors;
            colors.normalColor = theme.buttonNormal;
            colors.highlightedColor = theme.buttonHighlighted;
            colors.pressedColor = theme.buttonPressed;
            colors.selectedColor = theme.buttonHighlighted;
            button.colors = colors;

            ApplyButtonLabel(button, theme.textPrimary);
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
            if (button.targetGraphic is Image image && TryApplyButtonSprites(button, image, theme, accent: true))
            {
                ApplyButtonLabel(button, theme.textOnAccent);
                return;
            }

            if (button.targetGraphic is Image fallbackImage)
                fallbackImage.color = theme.accentColor;

            var colors = button.colors;
            colors.normalColor = theme.accentColor;
            colors.highlightedColor = Color.Lerp(theme.accentColor, Color.white, 0.15f);
            colors.pressedColor = theme.accentMutedColor;
            button.colors = colors;

            ApplyButtonLabel(button, theme.textOnAccent);
        }

        public static void ApplyDangerButton(Button button, UiThemeSO theme = null)
        {
            if (button == null)
                return;

            theme ??= UiThemeProvider.Current;
            if (button.targetGraphic is Image image && theme.dangerButtonSprite != null)
            {
                ApplyThemedSurface(image, theme.dangerButtonSprite, theme.dangerColor);
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.92f, 0.92f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                button.colors = colors;
                ApplyButtonLabel(button, theme.textPrimary);
                return;
            }

            ApplyAccentButton(button, theme);
        }

        private static bool TryApplyButtonSprites(Button button, Image image, UiThemeSO theme, bool accent)
        {
            var normal = accent
                ? theme.accentButtonSprite ?? theme.buttonNormalSprite
                : theme.secondaryButtonSprite ?? theme.buttonNormalSprite;
            if (normal == null)
                return false;

            ApplyThemedSurface(image, normal, accent ? theme.accentColor : theme.buttonNormal);

            var hasStates = theme.buttonHighlightedSprite != null
                || theme.buttonPressedSprite != null
                || theme.buttonDisabledSprite != null;

            if (hasStates)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = theme.buttonHighlightedSprite ?? normal,
                    pressedSprite = theme.buttonPressedSprite ?? normal,
                    disabledSprite = theme.buttonDisabledSprite ?? normal,
                    selectedSprite = theme.buttonHighlightedSprite ?? normal
                };
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
                colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }

            return true;
        }

        private static void ApplyThemedSurface(Image image, Sprite sprite, Color fallbackColor)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                return;
            }

            image.color = fallbackColor;
        }

        private static void ApplyButtonLabel(Button button, Color color)
        {
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = color;
        }
    }
}
