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
        [SerializeField] private TMP_Text suppliesIncomeText;
        [SerializeField] private TMP_Text manpowerIncomeText;
        [SerializeField] private TMP_Text authorityIncomeText;
        [SerializeField] private TMP_Text salvageNumberText;
        [SerializeField] private TMP_Text moraleValueText;
        [SerializeField] private TMP_Text strengthValueText;
        [SerializeField] private MatchupStrengthView matchupStrengthView;

        private ContentDatabase _database;
        private bool _hudTextsWired;

        // Runtime-built M1 meta strip (Dread clock + run seed) — its own small overlay
        // canvas so the hand-authored top bar's layout is never touched. Same pattern as
        // CombatArmyHealthHud. Restyled properly in the M6 UI sweep.
        private TMP_Text _dreadText;
        private TMP_Text _seedText;

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
            EnsureMetaStrip();

            bool bossPending = DreadRules.IsBossPending(state.Dread, state.BossesDefeated);

            if (fightTitleText != null)
            {
                fightTitleText.text = bossPending ? "BOSS" : "Fight";
                fightTitleText.color = bossPending ? new Color(1f, 0.42f, 0.38f) : Color.white;
                fightTitleText.textWrappingMode = TextWrappingModes.NoWrap; // narrow chip
            }

            if (fightIndexText != null)
                fightIndexText.text = state.FightIndex.ToString();

            if (_dreadText != null)
            {
                int threshold = DreadRules.NextThreshold(state.BossesDefeated);
                if (state.BossesDefeated >= 3)
                    _dreadText.text = "DREAD SATED";
                else if (bossPending)
                    _dreadText.text = $"DREAD {state.Dread}/{threshold} — THE ENEMY MASSES";
                else
                    _dreadText.text = $"DREAD {state.Dread}/{threshold}";
                _dreadText.color = bossPending
                    ? new Color(1f, 0.42f, 0.38f)
                    : new Color(0.78f, 0.74f, 0.66f);
            }

            if (_seedText != null)
                _seedText.text = $"SEED {state.RunSeed}";

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

        public void RefreshIncomePreview(RoundIncomePreview preview)
        {
            EnsureHudTextsWired();

            if (suppliesIncomeText != null)
                suppliesIncomeText.text = RoundIncomeCalculator.FormatIncomeLabel(preview.SuppliesIncome);

            if (manpowerIncomeText != null)
                manpowerIncomeText.text = RoundIncomeCalculator.FormatIncomeLabel(preview.ManpowerIncome);

            if (authorityIncomeText != null)
                authorityIncomeText.text = preview.AuthorityPool.ToString();

            if (salvageNumberText != null)
                salvageNumberText.text = FormatSalvageChance(preview.SalvageChancePercent);
        }

        public void ClearIncomePreview()
        {
            EnsureHudTextsWired();

            if (suppliesIncomeText != null)
                suppliesIncomeText.text = string.Empty;

            if (manpowerIncomeText != null)
                manpowerIncomeText.text = string.Empty;

            if (authorityIncomeText != null)
                authorityIncomeText.text = string.Empty;

            if (salvageNumberText != null)
                salvageNumberText.text = string.Empty;
        }

        private static string FormatSalvageChance(int percent) => $"{percent}%";

        /// <summary>Bottom-edge meta strip: Dread clock (left) + run seed (right), on a
        /// dedicated overlay canvas so the authored top bar never reflows. Sort 300 —
        /// under the combat HUD (400) and every pause/banner layer.</summary>
        private void EnsureMetaStrip()
        {
            if (_dreadText != null)
                return;

            // TOP-LEVEL on purpose: a canvas nested under the HUD inherits the top bar's
            // rect instead of the screen (the CombatArmyHealthHud lesson). Lands in the
            // active scene; if the arena scene unloads it, the null-check rebuilds it.
            var canvasGo = new GameObject("RunMetaStrip");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            _dreadText = CreateMetaLabel(canvasGo.transform, "DreadLabel",
                anchorMin: new Vector2(0.01f, 0f), anchorMax: new Vector2(0.45f, 0.045f),
                TextAlignmentOptions.BottomLeft);
            _seedText = CreateMetaLabel(canvasGo.transform, "SeedLabel",
                anchorMin: new Vector2(0.55f, 0f), anchorMax: new Vector2(0.99f, 0.045f),
                TextAlignmentOptions.BottomRight);
            _seedText.color = new Color(0.55f, 0.52f, 0.47f);
        }

        private static TMP_Text CreateMetaLabel(
            Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 17f;
            label.characterSpacing = 2f;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        public void RefreshSalvageIndicator(RunState state)
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
            salvageIndicatorText.text = $"Salvage: {displayName} — {FormatSalvageChance(state.SalvageChancePercent)}";
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
            ApplyLabel(suppliesIncomeText, true, theme);
            ApplyLabel(manpowerIncomeText, true, theme);
            ApplyLabel(authorityIncomeText, true, theme);
            ApplyLabel(salvageNumberText, false, theme);
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
            suppliesIncomeText ??= FindNamedText(searchRoot, "SuppliesIncome");
            manpowerIncomeText ??= FindNamedText(searchRoot, "ManpowerIncome");
            authorityIncomeText ??= FindNamedText(searchRoot, "AuthorityIncome");
            salvageNumberText ??= FindNamedText(searchRoot, "SalvageNumber");
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
