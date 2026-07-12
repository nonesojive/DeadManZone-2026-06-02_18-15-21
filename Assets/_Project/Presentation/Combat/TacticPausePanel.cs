using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Tactic radio + ability cards for combat pause windows.</summary>
    public sealed class TacticPausePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text authorityText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Transform tacticRow;
        [SerializeField] private Transform abilityRow;
        [SerializeField] private Button continueButton;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private Image panelBackground;

        private readonly TacticPauseValidator _validator = new();
        private readonly List<Toggle> _tacticToggles = new();
        private readonly List<AbilityToggleBinding> _abilityBindings = new();
        private CombatPauseContext _context;
        private TacticType _selectedTactic = TacticType.DisciplinedFire;

        private sealed class AbilityToggleBinding
        {
            public Toggle Toggle;
            public AvailableCommand Command;
        }

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponentInParent<CombatDirector>();

            if (continueButton != null)
                continueButton.onClick.AddListener(SubmitAndContinue);

            if (panelBackground != null)
                UiThemeApplicator.ApplySecurityTerminalFrame(panelBackground);

            EnsureRuntimeUi();
        }

        public void Hide() => gameObject.SetActive(false);

        public void ShowPause(CombatPauseContext context)
        {
            _context = context;
            if (_context == null)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            _selectedTactic = _context.PendingSelectedTactic ?? _context.ActiveTactic;

            if (titleText != null)
                titleText.text = GetPauseTitle(_context);

            BuildTacticToggles();
            BuildAbilityToggles();
            RestoreAbilitySelection(_context.PendingSelectedAbilities);
            RefreshValidation();
        }

        public void InitializeForTests(
            TMP_Text testAuthorityText,
            TMP_Text testReasonText,
            Button testContinueButton)
        {
            authorityText = testAuthorityText;
            reasonText = testReasonText;
            continueButton = testContinueButton;
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(SubmitAndContinue);
            }
        }

        private void EnsureRuntimeUi()
        {
            if (authorityText != null && continueButton != null)
                return;

            var theme = UiThemeProvider.Current;
            if (titleText == null)
            {
                titleText = CreateLabel(transform, "Pause Title", 22, FontStyles.Bold,
                    new Vector2(0.5f, 0.88f), new Vector2(700f, 36f));
                UiThemeApplicator.ApplyLabel(titleText, true, theme);
            }

            if (authorityText == null)
            {
                authorityText = CreateLabel(transform, "Authority", 18, FontStyles.Normal,
                    new Vector2(0.5f, 0.78f), new Vector2(700f, 28f));
                UiThemeApplicator.ApplyLabel(authorityText, false, theme);
            }

            if (reasonText == null)
            {
                reasonText = CreateLabel(transform, "", 16, FontStyles.Italic,
                    new Vector2(0.5f, 0.14f), new Vector2(700f, 24f));
                UiThemeApplicator.ApplyLabel(reasonText, false, theme);
            }

            if (tacticRow == null)
            {
                var row = new GameObject("TacticRow", typeof(RectTransform));
                row.transform.SetParent(transform, false);
                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.52f);
                rect.anchorMax = new Vector2(0.95f, 0.72f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                tacticRow = row.transform;
            }

            if (abilityRow == null)
            {
                var row = new GameObject("AbilityRow", typeof(RectTransform));
                row.transform.SetParent(transform, false);
                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.28f);
                rect.anchorMax = new Vector2(0.95f, 0.48f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                abilityRow = row.transform;
            }

            if (continueButton == null)
            {
                continueButton = CreateButton(transform, "Continue", new Vector2(0.5f, 0.05f));
            }

            ApplyGrimdarkSkin();
        }

        /// <summary>Match the FIGHT banner's kit: dark band behind the header, bone
        /// title, leather buttons — replaces the floating themed-text look.</summary>
        private void ApplyGrimdarkSkin()
        {
            CombatGrimdarkSkin.AddBand(transform, 0.755f, 0.945f, "HeaderBand");
            CombatGrimdarkSkin.AddBand(transform, 0.015f, 0.115f, "FooterBand");
            CombatGrimdarkSkin.StyleTitle(titleText);
            CombatGrimdarkSkin.StyleBody(authorityText);
            CombatGrimdarkSkin.StyleBody(reasonText);
            CombatGrimdarkSkin.StyleButton(continueButton);
        }

        private void BuildTacticToggles()
        {
            ClearChildren(tacticRow);
            _tacticToggles.Clear();

            var options = new[]
            {
                (TacticType.DisciplinedFire, "Disciplined Fire"),
                (TacticType.Advance, "Advance"),
                (TacticType.StandGround, "Hold the Line"),
                (TacticType.ProtectSupport, "Protect Support")
            };

            var visible = options
                .Where(o => TacticUnlockRules.IsUnlockedForList(_context?.StartingTactics, o.Item1))
                .ToList();

            if (visible.Count == 0)
                visible.Add(options[0]);

            if (!visible.Any(o => o.Item1 == _selectedTactic))
                _selectedTactic = visible[0].Item1;

            for (int i = 0; i < visible.Count; i++)
            {
                var (tactic, label) = visible[i];
                float x = 0.12f + i * (0.76f / Mathf.Max(1, visible.Count - 1));
                if (visible.Count == 1)
                    x = 0.5f;

                var toggle = CreateToggle(tacticRow, label, new Vector2(x, 0.5f));
                toggle.isOn = tactic == _selectedTactic;
                var captured = tactic;
                toggle.onValueChanged.AddListener(on =>
                {
                    if (!on)
                        return;

                    _selectedTactic = captured;
                    PersistDraft();
                    RefreshValidation();
                });
                _tacticToggles.Add(toggle);
            }
        }

        private void BuildAbilityToggles()
        {
            ClearChildren(abilityRow);
            _abilityBindings.Clear();

            if (_context.AvailableAbilities == null || _context.AvailableAbilities.Count == 0)
                return;

            int count = _context.AvailableAbilities.Count;
            for (int i = 0; i < count; i++)
            {
                var cmd = _context.AvailableAbilities[i];
                float x = 0.15f + i * (0.7f / Mathf.Max(1, count - 1));
                if (count == 1)
                    x = 0.5f;

                string label = $"{FormatAbility(cmd.Ability)} ({cmd.RequisitionCost}A)";
                var toggle = CreateToggle(abilityRow, label, new Vector2(x, 0.5f));
                var binding = new AbilityToggleBinding { Toggle = toggle, Command = cmd };
                toggle.onValueChanged.AddListener(_ =>
                {
                    PersistDraft();
                    RefreshValidation();
                });
                _abilityBindings.Add(binding);
            }
        }

        private void RestoreAbilitySelection(IReadOnlyList<GrantedAbility> pending)
        {
            if (pending == null || pending.Count == 0)
                return;

            foreach (var binding in _abilityBindings)
                binding.Toggle.isOn = pending.Contains(binding.Command.Ability);
        }

        private void RefreshValidation()
        {
            var selectedAbilities = GetSelectedAbilities();
            int totalCost = TacticPauseValidator.GetTotalPauseCost(
                _selectedTactic,
                _context.ActiveTactic,
                _context.CheckpointIndex,
                selectedAbilities);

            if (authorityText != null)
                authorityText.text = $"Authority cost: {totalCost} / {_context.Authority}";

            bool valid = _validator.ValidatePause(
                _selectedTactic,
                _context.ActiveTactic,
                _context.HasCommandPiece,
                _context.CheckpointIndex,
                _context.Authority,
                selectedAbilities,
                out var reason,
                _context.StartingTactics);

            if (reasonText != null)
                reasonText.text = valid ? string.Empty : reason;

            if (continueButton != null)
                continueButton.interactable = valid;
        }

        private void PersistDraft()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.Orchestrator.SavePauseDraft(_selectedTactic, GetSelectedAbilities());
        }

        private List<GrantedAbility> GetSelectedAbilities()
        {
            return _abilityBindings
                .Where(b => b.Toggle.isOn)
                .Select(b => b.Command.Ability)
                .ToList();
        }

        private void SubmitAndContinue()
        {
            if (RunManager.Instance == null || _context == null)
                return;

            var commands = new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterCheckpoint = _context.CheckpointIndex,
                    Type = CommandType.SetTactic,
                    Tactic = _selectedTactic,
                    SourcePieceId = "player_tactic"
                }
            };

            foreach (var binding in _abilityBindings.Where(b => b.Toggle.isOn))
            {
                commands.Add(new PhaseCommand
                {
                    AfterCheckpoint = _context.CheckpointIndex,
                    Type = CommandType.UseAbility,
                    Ability = binding.Command.Ability,
                    SourcePieceId = binding.Command.SourcePieceId,
                    Cost = binding.Command.RequisitionCost
                });
            }

            RunManager.Instance.SubmitCombatCommands(commands);
            Hide();
            combatDirector?.ContinueCombat();
        }

        private static string GetPauseTitle(CombatPauseContext context)
        {
            if (context?.Trigger == null)
                return "Combat Pause";

            string side = context.Trigger.TriggeredBy == CombatSide.Player ? "Your" : "Enemy";
            int percent = (int)(context.Trigger.Threshold * 100);
            return $"Pause — {side} forces at {percent}%";
        }

        private static string FormatAbility(GrantedAbility ability) =>
            ability switch
            {
                GrantedAbility.MortarShot => "Mortar Shot",
                GrantedAbility.ShieldAllies => "Shield Allies",
                GrantedAbility.CannonBlast => "Cannon Blast",
                _ => ability.ToString()
            };

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string text,
            int size,
            FontStyles style,
            Vector2 anchor,
            Vector2 sizeDelta)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchor)
        {
            var theme = UiThemeProvider.Current;
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 44f);

            var text = CreateLabel(go.transform, label, 18, FontStyles.Bold, new Vector2(0.5f, 0.5f), new Vector2(160f, 36f));
            text.raycastTarget = false;
            UiThemeApplicator.ApplyLabel(text, secondary: false, theme);

            var button = go.GetComponent<Button>();
            UiThemeApplicator.ApplyAccentButton(button, theme);
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string label, Vector2 anchor)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 36f);

            var image = go.GetComponent<Image>();
            image.sprite = null;
            image.color = CombatGrimdarkSkin.ButtonLeather;

            var text = CreateLabel(go.transform, label, 16, FontStyles.Normal, new Vector2(0.5f, 0.5f), new Vector2(170f, 32f));
            text.raycastTarget = false;
            text.color = CombatGrimdarkSkin.Bone;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = image;
            var colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.2f, 1.1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
            // Selected doctrine glows warm so the active choice reads at a glance.
            colors.selectedColor = new Color(1.45f, 1.32f, 1.0f, 1f);
            toggle.colors = colors;
            return toggle;
        }
    }
}
