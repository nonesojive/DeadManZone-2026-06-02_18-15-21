using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunEndOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => GameScenes.LoadMainMenu());
        }

        public void Show(RunPhase phase, RunState state = null)
        {
            if (root != null)
                root.SetActive(true);

            bool victory = phase == RunPhase.Victory;
            if (titleText != null)
                titleText.text = victory ? "Victory" : "Defeat";

            if (bodyText != null)
            {
                string factionName = state?.FactionId ?? "your force";
                if (victory && state != null)
                {
                    bodyText.text =
                        $"The gauntlet is yours.\n" +
                        $"Faction: {factionName}\n" +
                        $"Fights cleared: {state.FightIndex}\n" +
                        $"Final supplies: {state.Supplies} · Manpower: {state.Manpower}";
                }
                else
                {
                    bodyText.text =
                        $"Your line broke.\n" +
                        $"Faction: {factionName}\n" +
                        (state != null ? $"Reached fight {state.FightIndex}" : "Regroup and try again.");
                }
            }
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null || root == null)
                return;

            var panel = root.transform.Find("Card")?.GetComponent<Image>();
            if (panel != null)
                UiThemeApplicator.ApplyModalFrame(panel, theme);

            UiThemeApplicator.ApplyLabel(titleText, false, theme);
            UiThemeApplicator.ApplyLabel(bodyText, true, theme);
            if (mainMenuButton != null)
                UiThemeApplicator.ApplyAccentButton(mainMenuButton, theme);
        }
    }
}
