using DeadManZone.Core.Run;
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

        public void Show(RunPhase phase)
        {
            if (root != null)
                root.SetActive(true);

            bool victory = phase == RunPhase.Victory;
            if (titleText != null)
                titleText.text = victory ? "Victory" : "Defeat";

            if (bodyText != null)
            {
                bodyText.text = victory
                    ? "The gauntlet is yours. Iron Vanguard holds the line."
                    : "Your line broke. Regroup and try a new campaign.";
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
                UiThemeApplicator.ApplyCard(panel, theme);

            UiThemeApplicator.ApplyLabel(titleText, false, theme);
            UiThemeApplicator.ApplyLabel(bodyText, true, theme);
            if (mainMenuButton != null)
                UiThemeApplicator.ApplyAccentButton(mainMenuButton, theme);
        }
    }
}
