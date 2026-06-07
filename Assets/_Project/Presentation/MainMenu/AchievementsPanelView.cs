using DeadManZone.Core.Meta;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    public sealed class AchievementsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text listText;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Hide);
        }

        /// <summary>Replaces default back handler (e.g. to return to main menu).</summary>
        public void SetBackHandler(UnityEngine.Events.UnityAction handler)
        {
            if (backButton == null)
                return;

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(handler);
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Refresh()
        {
            if (listText == null)
                return;

            var data = MetaProgressionService.Load();
            var lines = new System.Text.StringBuilder();
            foreach (var def in AchievementCatalog.All)
            {
                bool unlocked = data.UnlockedAchievements.Contains(def.Id);
                lines.AppendLine(unlocked ? $"[X] {def.DisplayName}" : $"[ ] {def.DisplayName}");
                lines.AppendLine($"    {def.Description}");
            }

            listText.text = lines.ToString();
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            UiThemeApplicator.ApplyLabel(listText, secondary: true, theme);
            if (backButton != null)
                UiThemeApplicator.ApplyAccentButton(backButton, theme);
        }
    }
}
