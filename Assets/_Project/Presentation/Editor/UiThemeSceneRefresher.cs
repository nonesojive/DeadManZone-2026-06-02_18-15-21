using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.MainMenu;
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

            var theme = profile?.uiTheme ?? UiThemeEditor.EnsureThemeAsset();

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

                var isAccent = button.name.Contains("Continue") || button.name.Contains("Start")
                    || button.name.Contains("Begin Fight");
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

            foreach (var panel in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!IsUnderThemedRoot(panel.transform) || panel.GetComponent<Button>() != null)
                    continue;

                if (panel.name.Contains("Card") || panel.name.Contains("Offer"))
                    UiThemeApplicator.ApplyCard(panel, theme);
                else if (panel.name.Contains("Panel") || panel.name.Contains("Bar") || panel.name.Contains("Overlay"))
                    UiThemeApplicator.ApplyPanel(panel, theme);
            }
        }

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
