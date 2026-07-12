using System.Collections.Generic;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds FightOrders, BeginCombatPanel and the FrontReportModal to RunState fight options. Attach to `ShopV2Canvas` (finds all three as children).</summary>
    public sealed class ShopV2FightOrdersPresenter : ShopV2PresenterBase
    {
        private const int CardCount = 3;

        private sealed class Card
        {
            public GameObject Root;
            public TMP_Text Tier;
            public TMP_Text Faction;
            public TMP_Text Strength;
            public TMP_Text Arena;
            public GameObject Condition;
            public TMP_Text ConditionLabel;
        }

        private TMP_Text _chosen;
        private Button _openButton;
        private Button _beginCombatButton;
        private TMP_Text _hint;
        private GameObject _modal;
        private readonly List<Card> _cards = new();

        private bool _autoOpenedThisRound;
        private int _lastFightIndex = int.MinValue;

        private void Awake()
        {
            var missing = new List<string>();

            var fightOrders = transform.Find("FightOrders");
            if (fightOrders != null)
            {
                var chosen = fightOrders.Find("Chosen");
                _chosen = chosen != null ? chosen.GetComponent<TMP_Text>() : null;
                var open = fightOrders.Find("OpenButton");
                _openButton = open != null ? open.GetComponent<Button>() : null;
            }
            else
            {
                missing.Add("FightOrders");
            }

            var beginPanel = transform.Find("BeginCombatPanel");
            if (beginPanel != null)
            {
                var button = beginPanel.Find("BeginCombatButton");
                _beginCombatButton = button != null ? button.GetComponent<Button>() : null;
                var hint = beginPanel.Find("Hint");
                _hint = hint != null ? hint.GetComponent<TMP_Text>() : null;
            }
            else
            {
                missing.Add("BeginCombatPanel");
            }

            var modal = transform.Find("FrontReportModal");
            if (modal != null)
            {
                _modal = modal.gameObject;
                var panel = modal.Find("ReportPanel");
                if (panel != null)
                {
                    for (int i = 0; i < CardCount; i++)
                        BindCard(panel, i, missing);
                }
                else
                {
                    missing.Add("FrontReportModal/ReportPanel");
                }
            }
            else
            {
                missing.Add("FrontReportModal");
            }

            if (_openButton != null)
                _openButton.onClick.AddListener(OpenModal);
            if (_beginCombatButton != null)
                _beginCombatButton.onClick.AddListener(OnBeginCombatClicked);

            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2FightOrdersPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        private void BindCard(Transform panel, int index, List<string> missing)
        {
            var root = panel.Find($"FightCard_{index}");
            if (root == null)
            {
                missing.Add($"FightCard_{index}");
                return;
            }

            var condition = root.Find("Condition");
            var conditionLabel = condition != null ? condition.Find("Label") : null;
            var card = new Card
            {
                Root = root.gameObject,
                Tier = FindText(root, "Tier"),
                Faction = FindText(root, "Faction"),
                Strength = FindText(root, "Strength"),
                Arena = FindText(root, "Arena"),
                Condition = condition != null ? condition.gameObject : null,
                ConditionLabel = conditionLabel != null ? conditionLabel.GetComponent<TMP_Text>() : null
            };
            _cards.Add(card);

            var marchTransform = root.Find("MarchButton");
            var marchButton = marchTransform != null ? marchTransform.GetComponent<Button>() : null;
            if (marchButton != null)
            {
                int captured = index;
                marchButton.onClick.AddListener(() => OnMarchClicked(captured));
            }
        }

        protected override void Refresh(RunState state)
        {
            if (state == null)
                return;

            RefreshChosenLabel(state);
            RefreshCards(state);
            RefreshBeginCombat(state);
            MaybeAutoOpenModal(state);
        }

        private void RefreshChosenLabel(RunState state)
        {
            if (_chosen == null)
                return;

            var option = ChosenOption(state);
            _chosen.text = option == null
                ? "NO FRONT CHOSEN"
                : $"{option.Tier.ToString().ToUpperInvariant()} — {Display(option.EnemyFactionId)}";
        }

        private void RefreshCards(RunState state)
        {
            var options = state.FightOptions;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (options == null || i >= options.Count || options[i] == null)
                {
                    card.Root.SetActive(false);
                    continue;
                }

                var option = options[i];
                card.Root.SetActive(true);

                if (card.Tier != null)
                    card.Tier.text = option.Tier.ToString().ToUpperInvariant();
                if (card.Faction != null)
                    card.Faction.text = Display(option.EnemyFactionId);
                if (card.Strength != null)
                    card.Strength.text = $"~{option.StrengthPreview}";
                if (card.Arena != null)
                    card.Arena.text = $"ARENA: {Display(option.ThemeId)}";

                bool hasCondition = !string.IsNullOrEmpty(option.ConditionId);
                if (card.Condition != null)
                    card.Condition.SetActive(hasCondition);
                if (card.ConditionLabel != null && hasCondition)
                    card.ConditionLabel.text = Display(option.ConditionId);
            }
        }

        private void RefreshBeginCombat(RunState state)
        {
            var manager = RunManager.Instance;
            string failureReason = null;
            bool canStart = manager != null && manager.CanStartBattle(out failureReason);
            bool hasChosen = state.ChosenFightOption >= 0;

            if (_beginCombatButton != null)
                _beginCombatButton.interactable = hasChosen && canStart;

            if (_hint == null)
                return;

            if (!hasChosen)
                _hint.text = "CHOOSE A FRONT FROM THE REPORT";
            else if (!canStart)
                _hint.text = failureReason ?? string.Empty;
            else
            {
                var option = ChosenOption(state);
                _hint.text = option == null
                    ? string.Empty
                    : $"MARCHING: {option.Tier.ToString().ToUpperInvariant()} — {Display(option.EnemyFactionId)}";
            }
        }

        private void MaybeAutoOpenModal(RunState state)
        {
            if (state.FightIndex != _lastFightIndex)
            {
                _lastFightIndex = state.FightIndex;
                _autoOpenedThisRound = false;
            }

            if (_modal == null || _autoOpenedThisRound || _modal.activeSelf)
                return;

            if (state.FightOptions != null && state.FightOptions.Count > 0 && state.ChosenFightOption < 0)
            {
                _modal.SetActive(true);
                _autoOpenedThisRound = true;
            }
        }

        private void OpenModal()
        {
            if (_modal != null)
                _modal.SetActive(true);
        }

        private void OnMarchClicked(int index)
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.CanChooseFightOption(index, out _))
                return;

            manager.ChooseFightOption(index);
            if (_modal != null)
                _modal.SetActive(false);
        }

        private static void OnBeginCombatClicked()
        {
            var manager = RunManager.Instance;
            if (manager == null)
                return;

            if (manager.State != null && manager.State.ChosenFightOption >= 0 && manager.CanStartBattle(out _))
                manager.BeginCombat();
        }

        private static FightOptionRecord ChosenOption(RunState state)
        {
            int chosen = state.ChosenFightOption;
            if (state.FightOptions == null || chosen < 0 || chosen >= state.FightOptions.Count)
                return null;
            return state.FightOptions[chosen];
        }

        private static string Display(string id) =>
            string.IsNullOrEmpty(id) ? string.Empty : id.Replace('_', ' ').ToUpperInvariant();

        private static TMP_Text FindText(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
