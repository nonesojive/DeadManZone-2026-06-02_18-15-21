using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat;
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

            ApplyGrimdarkSkin();
        }

        /// <summary>M6: runtime grimdark-kit pass over the scene-authored overlay
        /// (same pattern as BattleReportPresenter.Awake). Show() picks the outcome
        /// accent: victory bone/brass, defeat red/rust.</summary>
        private void ApplyGrimdarkSkin()
        {
            if (root != null)
                CombatGrimdarkSkin.StyleFrame(root.transform.Find("Card")?.GetComponent<Image>());

            CombatGrimdarkSkin.StyleTitle(titleText, characterSpacing: 12f);
            if (titleText != null)
                titleText.fontStyle |= FontStyles.UpperCase;
            CombatGrimdarkSkin.StyleBody(bodyText);
            CombatGrimdarkSkin.StyleButton(mainMenuButton);
        }

        public void Show(RunPhase phase, RunState state = null)
        {
            if (root != null)
                root.SetActive(true);

            bool victory = phase == RunPhase.Victory;
            if (titleText != null)
            {
                titleText.text = victory ? "Victory" : "Defeat";
                titleText.color = victory
                    ? CombatGrimdarkSkin.VictoryGold
                    : CombatGrimdarkSkin.DefeatRed;
            }

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

        /// <summary>Grimdark kit (M6); theme param kept so editor bake callers compile.
        /// Same visuals whether applied by editor setup or the Awake pass.</summary>
        public void ApplyTheme(UiThemeSO theme) => ApplyGrimdarkSkin();
    }
}
