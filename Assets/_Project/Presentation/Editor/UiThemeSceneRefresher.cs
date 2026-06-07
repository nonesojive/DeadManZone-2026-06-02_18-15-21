using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.MainMenu;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    public static class UiThemeSceneRefresher
    {
        public static void RefreshOpenScene(VisualProfileSO profile)
        {
            UiThemeProvider.InvalidateCache();
            VisualProfileProvider.InvalidateCache();

            var theme = profile?.uiTheme ?? UiThemeSceneStyling.LoadTheme();

            foreach (var view in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                switch (view)
                {
                    case RunHudView hud:
                        hud.ApplyTheme(theme);
                        break;
                    case PauseMenuView pause:
                        pause.ApplyTheme(theme);
                        break;
                    case RunEndOverlayView end:
                        end.ApplyTheme(theme);
                        break;
                    case AchievementsPanelView achievements:
                        achievements.ApplyTheme(theme);
                        break;
                    case LeaderboardPanelView leaderboard:
                        leaderboard.ApplyTheme(theme);
                        break;
                    case BoardView board:
                        board.RefreshZoneColors();
                        break;
                }
            }

            RefreshCanvasBackgrounds(theme);
            RefreshThemedUiElements(theme);
            foreach (var applier in Object.FindObjectsByType<VisualProfileApplier>(FindObjectsSortMode.None))
                applier.ApplyNow(profile);

            MenuThemeEditor.EnsureMenuTheme(theme);
        }

        private static void RefreshCanvasBackgrounds(UiThemeSO theme)
        {
            if (theme == null)
                return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.name == "MenuCanvas" || HasDirectChildNamed(canvas.transform, "RunScene"))
                    ApplyCanvasBackground(canvas.gameObject, theme);
            }
        }

        private static void RefreshThemedUiElements(UiThemeSO theme)
        {
            if (theme == null)
                return;

            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!IsUnderThemedRoot(button.transform))
                    continue;

                var isAccent = IsAccentButtonName(button.name);
                if (isAccent)
                    UiThemeApplicator.ApplyAccentButton(button, theme);
                else
                    UiThemeApplicator.ApplyButton(button, theme);
            }

            foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!IsUnderThemedRoot(label.transform))
                    continue;

                var secondary = label.fontStyle.HasFlag(FontStyles.Italic);
                UiThemeApplicator.ApplyLabel(label, secondary, theme);
            }

            foreach (var image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!IsUnderThemedRoot(image.transform) || image.GetComponent<Button>() != null)
                    continue;

                RefreshThemedImage(image, theme);
            }
        }

        private static void RefreshThemedImage(Image image, UiThemeSO theme)
        {
            var name = image.gameObject.name;
            var parentName = image.transform.parent != null ? image.transform.parent.name : string.Empty;

            if (name is "DecorBackground" or "LoadingDecor" or "CombatDecor" or "LaneTint")
                return;

            if (name == "SellZone" || parentName == "SellZone")
            {
                UiThemeApplicator.ApplySellZone(image, theme);
                return;
            }

            if (name == RunHudPanelBuilder.PanelName)
            {
                UiThemeApplicator.ApplyStorageSlotEmpty(image, theme);
                image.color = theme.GetReserveSlotColor();
                return;
            }

            if (name == ReservesLabelStripFactory.StripName)
            {
                UiThemeApplicator.ApplyStorageSlotEmpty(image, theme);
                image.color = theme.GetReserveSlotColor();
                return;
            }

            if (name == "PhaseBanner" || parentName == "PhaseBanner")
            {
                UiThemeApplicator.ApplyBanner(image, theme);
                return;
            }

            if (name.Contains("TacticPause") || parentName == "TacticPauseSheet")
            {
                UiThemeApplicator.ApplySecurityTerminalFrame(image, theme);
                return;
            }

            if (name is "LastBattleLogSheet" or "BattleReportSheet" or "MainCard" or "OptionsCard"
                or "Card" or "PanelFrame")
            {
                UiThemeApplicator.ApplyModalFrame(image, theme);
                return;
            }

            if (name is "LogScroll")
            {
                UiThemeApplicator.ApplyInventoryPanel(image, theme);
                return;
            }

            if (name is "TopBar" or "BottomBar")
            {
                UiThemeApplicator.ApplySidebarPanel(image, theme);
                return;
            }

            if (name.Contains("Column") && name.EndsWith("Column"))
            {
                UiThemeApplicator.ApplyInventoryPanel(image, theme);
                return;
            }

            if (name.Contains("Tile") || name.Contains("ReservesTile"))
            {
                UiThemeApplicator.ApplySlotEmpty(image, theme);
                return;
            }

            if (name.Contains("Card") || name.Contains("Offer") || name.Contains("Toggle"))
                UiThemeApplicator.ApplyCard(image, theme);
            else if (name.Contains("Panel") || name.Contains("Bar") || name.Contains("Sheet"))
                UiThemeApplicator.ApplyPanel(image, theme);
        }

        private static bool IsAccentButtonName(string buttonName) =>
            buttonName.Contains("Continue") || buttonName.Contains("NewRun")
            || buttonName.Contains("Begin Fight") || buttonName.Contains("IronVanguard")
            || buttonName.Contains("DustScourge") || buttonName.Contains("Cartel")
            || buttonName.Contains("Close") || buttonName.Contains("Submit");

        private static bool IsUnderThemedRoot(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name is "MenuCanvas" or "RunScene" or "RunCanvas")
                    return true;
                transform = transform.parent;
            }

            return false;
        }

        private static bool HasDirectChildNamed(Transform parent, string childName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName)
                    return true;
            }

            return false;
        }

        private static void ApplyCanvasBackground(GameObject canvasGo, UiThemeSO theme)
        {
            var bg = canvasGo.GetComponent<Image>();
            if (bg == null)
                bg = canvasGo.AddComponent<Image>();

            bg.color = theme.backgroundColor;
            bg.raycastTarget = false;
            EditorUtility.SetDirty(canvasGo);
        }
    }
}
