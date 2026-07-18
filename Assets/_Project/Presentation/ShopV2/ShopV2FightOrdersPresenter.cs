using System;
using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds FightOrders, BeginCombatPanel and the FrontReportModal to RunState fight options. Attach to `ShopV2Canvas` (finds all three as children).</summary>
    public sealed class ShopV2FightOrdersPresenter : ShopV2PresenterBase
    {
        private const int CardCount = 3;

        // ---------------------------------------------------------------------------------
        // Card visual vocabulary. Like the shop slots, the FrontReportModal was authored as a
        // MOCK: card 1's border was hand-painted bronze to showcase the selected state, so it
        // read as permanently highlighted. The presenter owns every stateful colour and writes
        // all of them on every bind — nothing is inherited from the art.
        //
        // Two independent axes, deliberately kept apart:
        //   TIER ACCENT  — what KIND of fight this is (easy/normal/hard). Derived from the
        //                  tier, tints the Tier label and the March button.
        //   BORDER STATE — whether this card is idle / hovered / CHOSEN. Uses the tier accent
        //                  as its lit colour so a chosen hard front still reads as hard.
        // ---------------------------------------------------------------------------------
        private static readonly Color BorderIdle = new(0.17f, 0.14f, 0.10f, 1f);

        private static readonly Color AccentEasy = new(0.92f, 0.87f, 0.74f, 1f);
        private static readonly Color AccentNormal = new(0.73f, 0.57f, 0.31f, 1f);
        private static readonly Color AccentHard = new(0.90f, 0.78f, 0.50f, 1f);

        /// <summary>2026-07-17 boss-pending soft-lock fix: card 0 repurposed as the boss
        /// report reads with this warning-red accent so it never reads as just another
        /// (easy/normal/hard) front — it IS the fight, no choice involved.</summary>
        private static readonly Color AccentBoss = new(0.86f, 0.32f, 0.26f, 1f);

        /// <summary>A front you cannot currently take (e.g. not enough authority) reads dead.</summary>
        private static readonly Color Unchoosable = new(0.45f, 0.42f, 0.38f, 1f);

        private static Color AccentFor(FightOptionTier tier) => tier switch
        {
            FightOptionTier.Easy => AccentEasy,
            FightOptionTier.Hard => AccentHard,
            _ => AccentNormal
        };

        private sealed class Card
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Border;
            public TMP_Text Tier;
            public TMP_Text Faction;
            public TMP_Text Strength;
            public TMP_Text Arena;
            public TMP_Text Stakes;
            public GameObject Condition;
            public TMP_Text ConditionLabel;
            public ShopV2Tooltip ConditionTooltip;

            public Button March;
            public Image MarchBorder;
            public TMP_Text MarchLabel;
            public ShopV2ButtonAffordance MarchAffordance;

            /// <summary>Live state the visuals derive from — never read back off the graphics.</summary>
            public Color Accent = AccentNormal;
            public bool Chosen;
            public bool Hovered;
            public bool Choosable = true;
        }

        private TMP_Text _chosen;
        private Button _openButton;
        private Button _beginCombatButton;
        private TMP_Text _hint;
        private GameObject _modal;
        private readonly List<Card> _cards = new();

        private bool _autoOpenedThisRound;
        private int _lastFightIndex = int.MinValue;

        /// <summary>2026-07-17 boss-pending soft-lock fix: true while card 0 is repurposed as
        /// the full-width boss report (no Fight Options this round — DreadRules crossed a
        /// boss threshold). Read by OnMarchClicked so its card-0 click doesn't try to
        /// ChooseFightOption(0) against an empty options list.</summary>
        private bool _bossMode;

        /// <summary>Card 0's authored width/position (three-card layout) and the span computed
        /// from cards 0..N-1's own authored rects (never hardcoded pixels) — so a boss round
        /// can stretch card 0 across the whole row and a normal round can restore it, without
        /// this presenter assuming any particular authored layout.</summary>
        private bool _cardSpanCached;
        private Vector2 _normalCard0Size;
        private Vector2 _bossCardSize;

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
                    CacheCardSpan();
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
            var border = root.Find("Border");
            var marchTransform = root.Find("MarchButton");

            var card = new Card
            {
                Root = root.gameObject,
                Rect = root.GetComponent<RectTransform>(),
                Border = border != null ? border.GetComponent<Image>() : null,
                Tier = FindText(root, "Tier"),
                Faction = FindText(root, "Faction"),
                Strength = FindText(root, "Strength"),
                Arena = FindText(root, "Arena"),
                Stakes = FindText(root, "Stakes"),
                Condition = condition != null ? condition.gameObject : null,
                ConditionLabel = conditionLabel != null ? conditionLabel.GetComponent<TMP_Text>() : null,
                ConditionTooltip = condition != null ? condition.GetComponent<ShopV2Tooltip>() : null,
                March = marchTransform != null ? marchTransform.GetComponent<Button>() : null,
                MarchBorder = marchTransform != null ? marchTransform.Find("Border")?.GetComponent<Image>() : null,
                MarchLabel = marchTransform != null ? marchTransform.Find("Label")?.GetComponent<TMP_Text>() : null,
                MarchAffordance = marchTransform != null
                    ? marchTransform.GetComponent<ShopV2ButtonAffordance>()
                    : null
            };
            _cards.Add(card);

            if (card.March != null)
            {
                int captured = index;
                card.March.onClick.AddListener(() => OnMarchClicked(captured));
            }

            // Hovering the card lights its border. NOTE: the card body's raycastTarget must be
            // true for this to fire — that flag is AUTHORED in the scene.
            var hover = root.GetComponent<ShopV2FightCardHover>();
            if (hover == null)
                hover = root.gameObject.AddComponent<ShopV2FightCardHover>();

            hover.HoverChanged += hovered =>
            {
                card.Hovered = hovered;
                ApplyCardVisual(card);
            };
        }

        /// <summary>Reads card 0's authored width and the row's total span (card 0's left edge
        /// to card N-1's right edge) straight off their RectTransforms — never hardcoded — so
        /// boss mode can stretch card 0 across the row and restore it exactly, on whatever
        /// layout the report was hand-authored with.</summary>
        private void CacheCardSpan()
        {
            if (_cards.Count == 0 || _cards[0].Rect == null || _cards[^1].Rect == null)
                return;

            var first = _cards[0].Rect;
            var last = _cards[^1].Rect;
            _normalCard0Size = first.sizeDelta;
            float span = (last.anchoredPosition.x + last.sizeDelta.x) - first.anchoredPosition.x;
            _bossCardSize = new Vector2(span, first.sizeDelta.y);
            _cardSpanCached = true;
        }

        /// <summary>The boss the next combat will be, or null on a normal round. Single source
        /// of truth for every boss-branch check in this presenter.</summary>
        private static BossDefinition PendingBoss()
        {
            var orchestrator = RunManager.Instance?.Orchestrator;
            return orchestrator != null && orchestrator.IsBossFightPending ? orchestrator.GetPendingBoss() : null;
        }

        /// <summary>
        /// The single place a card's stateful colours are written. Chosen outranks hover, so
        /// the front you have committed to always reads as chosen.
        /// </summary>
        private static void ApplyCardVisual(Card card)
        {
            var accent = card.Choosable ? card.Accent : Unchoosable;

            if (card.Border != null)
            {
                card.Border.color = card.Chosen
                    ? accent
                    : (card.Hovered && card.Choosable
                        ? new Color(accent.r, accent.g, accent.b, 0.80f)
                        : BorderIdle);
            }

            if (card.Tier != null)
                card.Tier.color = accent;

            // The march button's border carries the TIER accent, so the hover/press states have to
            // be derived from it — a fixed gold highlight would erase the tier read on hover.
            // Hand the affordance the base and let it own the interaction layer on top.
            if (card.MarchAffordance != null)
                card.MarchAffordance.SetPalette(accent);
            else if (card.MarchBorder != null)
                card.MarchBorder.color = accent;

            if (card.MarchLabel != null)
            {
                card.MarchLabel.color = accent;
                card.MarchLabel.text = card.Chosen ? "MARCHING HERE" : "MARCH HERE";
            }

            if (card.March != null)
                card.March.interactable = card.Choosable || card.Chosen;
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

            var boss = PendingBoss();
            if (boss != null)
            {
                _chosen.text = $"BOSS FIGHT — {boss.DisplayName.ToUpperInvariant()}";
                return;
            }

            var option = ChosenOption(state);
            _chosen.text = option == null
                ? "NO FRONT CHOSEN"
                : $"{option.Tier.ToString().ToUpperInvariant()} — {Display(option.EnemyFactionId)}";
        }

        /// <summary>2026-07-17 boss-pending soft-lock fix: a boss round crosses a Dread
        /// threshold — GenerateFightOptions deliberately empties FightOptions for it (see
        /// RunOrchestrator), which used to leave every card SetActive(false) and the whole
        /// report reading empty. Card 0 becomes the boss report instead; cards 1/2 stay hidden.</summary>
        private void RefreshCards(RunState state)
        {
            var boss = PendingBoss();
            _bossMode = boss != null;

            if (_bossMode)
            {
                ShowBossCard(boss);
                return;
            }

            if (_cardSpanCached && _cards.Count > 0 && _cards[0].Rect != null)
                _cards[0].Rect.sizeDelta = _normalCard0Size;

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
                if (card.Stakes != null)
                    card.Stakes.text = BuildStakes(option.Tier);

                bool hasCondition = !string.IsNullOrEmpty(option.ConditionId);
                if (card.Condition != null)
                    card.Condition.SetActive(hasCondition);

                if (hasCondition)
                {
                    // Name + effect both come from ConditionCatalog, which is also where the rule
                    // itself lives — so the chip can't claim something the fight won't do.
                    if (card.ConditionLabel != null)
                        card.ConditionLabel.text = ConditionCatalog.DisplayName(option.ConditionId);

                    card.ConditionTooltip?.SetContent(
                        ConditionCatalog.DisplayName(option.ConditionId),
                        ConditionCatalog.Describe(option.ConditionId));
                }

                // Derive the card's whole look from the option + run state.
                var manager = RunManager.Instance;
                card.Accent = AccentFor(option.Tier);
                card.Chosen = state.ChosenFightOption == i;
                card.Choosable = manager == null || manager.CanChooseFightOption(i, out _);
                ApplyCardVisual(card);
            }
        }

        /// <summary>Card 0, stretched across the whole row — "a single dramatic full-width
        /// boss card" in place of the three normal fronts. No march choice to make (the boss
        /// IS the fight), so it reads Chosen/lit and its button just closes the modal
        /// (OnMarchClicked short-circuits on _bossMode).</summary>
        private void ShowBossCard(BossDefinition boss)
        {
            var card = _cards[0];
            card.Root.SetActive(true);
            for (int i = 1; i < _cards.Count; i++)
                _cards[i].Root.SetActive(false);

            if (_cardSpanCached && card.Rect != null)
                card.Rect.sizeDelta = _bossCardSize;

            if (card.Tier != null)
                card.Tier.text = "BOSS FIGHT";
            if (card.Faction != null)
                card.Faction.text = boss.DisplayName.ToUpperInvariant();
            if (card.Strength != null)
            {
                var enemyBoard = RunManager.Instance?.Orchestrator?.GetUpcomingEnemyBoard();
                int bossStrength = enemyBoard != null
                    ? ArmyStrengthCalculator.Evaluate(enemyBoard).EffectiveTotal
                    : 0;
                card.Strength.text = $"~{bossStrength}";
            }
            if (card.Arena != null)
                card.Arena.text = $"ARENA: {Display(ArenaThemes.SignatureTheme(boss.EnemyFactionId))}";
            if (card.Stakes != null)
                card.Stakes.text =
                    $"{Display(boss.EnemyFactionId)} · THE ENEMY COMES TO YOU\nNO FRONT TO CHOOSE — PREPARE YOUR LINES";

            card.Condition?.SetActive(false);

            card.Accent = AccentBoss;
            card.Chosen = true;
            card.Choosable = true;
            ApplyCardVisual(card);
            if (card.MarchLabel != null)
                card.MarchLabel.text = "PREPARE FOR BATTLE";
        }

        /// <summary>2026-07-17 boss-pending soft-lock fix: BEGIN COMBAT used to stay disabled
        /// forever on a boss round because ChosenFightOption never leaves -1 (nothing to
        /// choose — GenerateFightOptions leaves FightOptions empty for boss rounds). A pending
        /// boss now arms the button on its own, same as picking a front does.</summary>
        private void RefreshBeginCombat(RunState state)
        {
            var manager = RunManager.Instance;
            string failureReason = null;
            bool canStart = manager != null && manager.CanStartBattle(out failureReason);
            var boss = PendingBoss();
            bool hasChosen = state.ChosenFightOption >= 0 || boss != null;

            if (_beginCombatButton != null)
                _beginCombatButton.interactable = hasChosen && canStart;

            if (_hint == null)
                return;

            if (boss != null)
                _hint.text = canStart
                    ? $"THE BOSS AWAITS — {boss.DisplayName.ToUpperInvariant()}"
                    : failureReason ?? string.Empty;
            else if (!hasChosen)
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

            bool hasUnchosenOptions = state.FightOptions != null && state.FightOptions.Count > 0 && state.ChosenFightOption < 0;
            bool bossRound = PendingBoss() != null;
            if (hasUnchosenOptions || bossRound)
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
            // Boss round: card 0 is the boss report, not a choosable front — there's nothing
            // to ChooseFightOption against an empty options list. Just close the modal; BEGIN
            // COMBAT is already armed (RefreshBeginCombat).
            if (_bossMode)
            {
                if (_modal != null)
                    _modal.SetActive(false);
                return;
            }

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
            if (manager == null || manager.State == null)
                return;

            bool bossPending = manager.Orchestrator?.IsBossFightPending ?? false;
            bool hasFront = bossPending || manager.State.ChosenFightOption >= 0;
            if (hasFront && manager.CanStartBattle(out _))
                manager.BeginCombat();
        }

        /// <summary>
        /// The tier's stakes — what the front COSTS and what it PAYS. Ported from the
        /// legacy FrontReportPanel so hiding that panel loses no intel. Numbers come from
        /// DreadRules (ADR-0004), never hardcoded, so tuning the rules retunes the card.
        /// </summary>
        private static string BuildStakes(FightOptionTier tier)
        {
            int dread = DreadRules.DreadFor(tier);
            string victory = $"+{dread} DREAD ON VICTORY";

            return tier switch
            {
                FightOptionTier.Easy =>
                    $"COSTS {DreadRules.EasyAuthorityCost} AUTHORITY\n{victory}",
                FightOptionTier.Hard =>
                    $"{victory}\nSPOILS +{DreadRules.HardVictorySupplies}S · +{DreadRules.HardVictoryManpower}M",
                _ => victory
            };
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

    /// <summary>Surfaces pointer enter/exit on a fight card so the presenter can light its
    /// border. The highlight used to be baked into the mock art; it is state now.</summary>
    public sealed class ShopV2FightCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<bool> HoverChanged;

        public void OnPointerEnter(PointerEventData eventData) => HoverChanged?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData) => HoverChanged?.Invoke(false);

        /// <summary>The modal closing mid-hover never fires an exit; drop the highlight so the
        /// card does not come back lit next time the report opens.</summary>
        private void OnDisable() => HoverChanged?.Invoke(false);
    }
}
