using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    internal static class UiThemeSceneStyling
    {
        public static UiThemeSO LoadTheme()
        {
            if (AssetDatabase.IsValidFolder(BunkerSurvivalUiKitSetup.KitRoot))
                return BunkerSurvivalUiKitSetup.EnsureBunkerSurvivalTheme();

            return UiThemeEditor.EnsureThemeAsset();
        }

        public static Image AddPanelBackground(Transform parent, UiThemeSO theme)
        {
            var bg = parent.GetComponent<Image>();
            if (bg == null)
                bg = parent.gameObject.AddComponent<Image>();

            bg.raycastTarget = false;
            UiThemeApplicator.ApplyPanel(bg, theme);
            return bg;
        }

        public static Image AddSidebarBackground(Transform parent, UiThemeSO theme)
        {
            var bg = parent.GetComponent<Image>();
            if (bg == null)
                bg = parent.gameObject.AddComponent<Image>();

            bg.raycastTarget = false;
            UiThemeApplicator.ApplySidebarPanel(bg, theme);
            return bg;
        }

        public static Image AddModalFrame(Transform parent, UiThemeSO theme, string name = "PanelFrame")
        {
            var frame = MenuSceneSetup.CreateStretchChild(parent, name);
            var image = frame.AddComponent<Image>();
            image.raycastTarget = false;
            UiThemeApplicator.ApplyModalFrame(image, theme);
            return image;
        }

        public static Image AddInventoryPanel(Transform parent, UiThemeSO theme)
        {
            var bg = parent.GetComponent<Image>();
            if (bg == null)
                bg = parent.gameObject.AddComponent<Image>();

            bg.raycastTarget = false;
            UiThemeApplicator.ApplyInventoryPanel(bg, theme);
            return bg;
        }

        public static void AddDecorBackground(Transform canvasParent, UiThemeSO theme, bool menuScene)
        {
            var sprite = menuScene ? theme.menuBackgroundSprite : theme.runBackgroundSprite;
            if (sprite == null)
                return;

            var bg = MenuSceneSetup.CreateStretchChild(canvasParent, "DecorBackground");
            bg.transform.SetAsFirstSibling();
            var image = bg.AddComponent<Image>();
            UiThemeApplicator.ApplyBackgroundPlate(image, sprite, menuScene ? 0.72f : 0.55f);
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

        public static void StyleSlider(Slider slider, UiThemeSO theme)
        {
            if (slider == null)
                return;

            var background = slider.transform.Find("Background")?.GetComponent<Image>();
            if (background != null)
                UiThemeApplicator.ApplyCard(background, theme);

            var fill = slider.fillRect?.GetComponent<Image>();
            if (fill != null)
            {
                if (theme.accentButtonSprite != null)
                {
                    fill.sprite = theme.accentButtonSprite;
                    fill.type = Image.Type.Sliced;
                    fill.color = Color.white;
                }
                else
                {
                    fill.color = theme.accentColor;
                }
            }
        }
    }
}
