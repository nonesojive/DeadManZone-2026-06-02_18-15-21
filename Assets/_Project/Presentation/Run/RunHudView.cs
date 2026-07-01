using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text fightTitleText;
        [SerializeField] private TMP_Text fightIndexText;
        [SerializeField] private TMP_Text gateMessageText;
        [SerializeField] private TMP_Text salvageIndicatorText;
        [SerializeField] private TMP_Text suppliesValueText;
        [SerializeField] private TMP_Text manpowerValueText;
        [SerializeField] private TMP_Text authorityValueText;
        [SerializeField] private TMP_Text moraleValueText;
        [SerializeField] private TMP_Text strengthValueText;
        [SerializeField] private MatchupStrengthView matchupStrengthView;

        private ContentDatabase _database;
        private bool _hudTextsWired;

        private void Awake() => EnsureHudTextsWired();

        public void Configure(
            TMP_Text fightTitle,
            TMP_Text fightIndex,
            TMP_Text gateMessage,
            TMP_Text suppliesValue,
            TMP_Text manpowerValue,
            TMP_Text authorityValue,
            TMP_Text moraleValue,
            TMP_Text salvageIndicator = null,
            MatchupStrengthView matchupStrength = null,
            TMP_Text strengthValue = null)
        {
            fightTitleText = fightTitle;
            fightIndexText = fightIndex;
            gateMessageText = gateMessage;
            suppliesValueText = suppliesValue;
            manpowerValueText = manpowerValue;
            authorityValueText = authorityValue;
            moraleValueText = moraleValue;
            salvageIndicatorText = salvageIndicator;
            matchupStrengthView = matchupStrength;
            strengthValueText = strengthValue;
        }

        public void RefreshMatchup(MatchupAssessment? assessment)
        {
            matchupStrengthView?.Refresh(assessment);
            if (assessment.HasValue)
                RefreshPlayerStrength(assessment.Value.Player);
        }

        public void RefreshMatchupFromBoards(BoardState playerBoard, BoardState enemyBoard)
        {
            var playerStrength = playerBoard != null
                ? ArmyStrengthCalculator.Evaluate(playerBoard)
                : default;
            RefreshPlayerStrength(playerStrength);

            if (playerBoard == null || enemyBoard == null)
            {
                RefreshMatchup(null);
                return;
            }

            var assessment = MatchupAssessment.Compare(
                playerStrength,
                ArmyStrengthCalculator.Evaluate(enemyBoard));
            RefreshMatchup(assessment);
        }

        public void RefreshPlayerStrength(ArmyStrengthSnapshot snapshot)
        {
            EnsureHudTextsWired();
            if (strengthValueText == null)
                return;

            strengthValueText.text = FormatStrength(snapshot.EffectiveTotal);
        }

        private static string FormatStrength(int effectiveTotal) =>
            effectiveTotal.ToString("N0");

        public void Refresh(RunState state, string battleGateMessage = null)
        {
            if (state == null)
                return;

            EnsureHudTextsWired();

            if (fightTitleText != null)
                fightTitleText.text = "Fight";

            if (fightIndexText != null)
                fightIndexText.text = state.FightIndex.ToString();

            if (suppliesValueText != null)
                suppliesValueText.text = state.Supplies.ToString();

            if (manpowerValueText != null)
                manpowerValueText.text = state.Manpower.ToString();

            if (authorityValueText != null)
                authorityValueText.text = state.Authority.ToString();

            if (moraleValueText != null)
                moraleValueText.text = state.Morale.ToString();

            if (gateMessageText != null)
            {
                bool hasGate = !string.IsNullOrEmpty(battleGateMessage);
                gateMessageText.gameObject.SetActive(hasGate);
                if (hasGate)
                    gateMessageText.text = battleGateMessage;
            }

            RefreshSalvageIndicator(state);
        }

        private void RefreshSalvageIndicator(RunState state)
        {
            if (salvageIndicatorText == null)
                return;

            if (state == null || string.IsNullOrEmpty(state.LastEnemyFactionId))
            {
                salvageIndicatorText.gameObject.SetActive(false);
                return;
            }

            _database ??= ContentDatabase.Load();
            var faction = _database?.GetFaction(state.LastEnemyFactionId);
            string displayName = faction != null && !string.IsNullOrEmpty(faction.displayName)
                ? faction.displayName
                : state.LastEnemyFactionId;

            salvageIndicatorText.gameObject.SetActive(true);
            salvageIndicatorText.text = $"Salvage: {displayName} — {state.SalvageChancePercent}%";
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null)
                return;

            ApplyLabel(fightTitleText, false, theme);
            ApplyLabel(fightIndexText, false, theme);
            ApplyLabel(gateMessageText, true, theme);
            ApplyLabel(salvageIndicatorText, true, theme);
            ApplyLabel(suppliesValueText, false, theme);
            ApplyLabel(manpowerValueText, false, theme);
            ApplyLabel(authorityValueText, false, theme);
            ApplyLabel(moraleValueText, false, theme);
            ApplyLabel(strengthValueText, false, theme);
            matchupStrengthView?.ApplyTheme(theme);
        }

        private static void ApplyLabel(TMP_Text label, bool secondary, UiThemeSO theme)
        {
            if (label != null)
                UiThemeApplicator.ApplyLabel(label, secondary, theme);
        }

        private void EnsureHudTextsWired()
        {
            if (_hudTextsWired)
                return;

            strengthValueText ??= FindNamedText(transform, "StrengthNumber");

            var searchRoot = ResolveHudSearchRoot();
            fightIndexText ??= FindNamedText(searchRoot, "FightNumber", "FightIndex");
            fightTitleText ??= FindNamedText(searchRoot, "FightTitle", "FightLabel");
            suppliesValueText ??= FindNamedText(searchRoot, "SuppliesNumber");
            manpowerValueText ??= FindNamedText(searchRoot, "ManpowerNumber");
            authorityValueText ??= FindNamedText(searchRoot, "AuthorityNumber");
            moraleValueText ??= FindNamedText(searchRoot, "MoraleNumber", "MoralNumber");
            strengthValueText ??= FindNamedText(searchRoot, "StrengthNumber");

            _hudTextsWired = true;
        }

        private Transform ResolveHudSearchRoot()
        {
            var topResourcePanel = transform.Find("TopResourcePanel");
            if (topResourcePanel != null)
                return topResourcePanel;

            var topBar = transform.parent;
            if (topBar != null)
            {
                var nestedPanel = topBar.Find("TopResourcePanel");
                if (nestedPanel != null)
                    return nestedPanel;
            }

            return transform;
        }

        private static TMP_Text FindNamedText(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0)
                return null;

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (text.name == names[i])
                        return text;
                }
            }

            return null;
        }
    }
}
