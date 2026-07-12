using DeadManZone.Core.Meta;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    public sealed class LeaderboardPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text listText;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Hide);

            // M6: grimdark kit (frame + buttons via StyleCard, kit typography).
            CombatGrimdarkSkin.StyleCard(root);
            CombatGrimdarkSkin.StylePanelText(root);
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

            var entries = MetaProgressionService.GetLeaderboard();
            if (entries.Count == 0)
            {
                listText.text = "No scores yet. Clear the gauntlet to rank.";
                return;
            }

            var lines = new System.Text.StringBuilder();
            int rank = 1;
            foreach (var entry in entries)
            {
                lines.AppendLine($"{rank}. {entry.FactionId} — {entry.Score} pts ({entry.FightsCleared} fights)");
                rank++;
            }

            listText.text = lines.ToString();
        }

        /// <summary>Grimdark kit (M6); theme param kept so editor bake callers compile.</summary>
        public void ApplyTheme(UiThemeSO theme)
        {
            CombatGrimdarkSkin.StyleCard(root);
            CombatGrimdarkSkin.StylePanelText(root);
        }
    }
}
